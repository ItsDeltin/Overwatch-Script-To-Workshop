#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Model;
using Deltin.Deltinteger.Parse.Types;
using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse;

sealed class IsPatternMatchingExpression : IExpression
{
    readonly IExpression lhs;
    readonly CodeType returnType;
    EnumType? targetEnumType;
    EnumMember? targetEnumMember;
    Var[] bindingVariables;

    public IsPatternMatchingExpression(ParseInfo parseInfo, Scope scope, BinaryOperatorExpression op)
    {
        lhs = parseInfo.GetExpression(scope, op.Left);
        returnType = parseInfo.Types.Boolean();
        GetPattern(parseInfo, scope, op.Right);
    }

    void GetPattern(ParseInfo parseInfo, Scope scope, IParseExpression patternExpression)
    {
        List<IParseExpression> GetRhsPath()
        {
            var path = new List<IParseExpression>();
            void Travel(IParseExpression value)
            {
                if (value is BinaryOperatorExpression boe && boe.IsDotExpression())
                {
                    Travel(boe.Left);
                    Travel(boe.Right);
                }
                else if (value is Identifier or FunctionExpression)
                    path.Add(value);
            }
            Travel(patternExpression);
            return path;
        }
        var path = GetRhsPath();

        CodeType? activeType = null;

        Identifier[]? bindingVariableDefinitions = null;

        for (int i = 0; i < path.Count; i++)
        {
            var currentPathItem = path[i];
            Identifier identifier;

            // ...
            if (currentPathItem is Identifier rhsAsIdentifier)
                identifier = rhsAsIdentifier;
            // Will need to check for extraneous identifier elements.
            else if (currentPathItem is FunctionExpression rhsAsFunction)
            {
                if (rhsAsFunction.Target is Identifier functionTarget)
                    identifier = functionTarget;
                // Invalid target, error here.
                else break;

                // Will need to check for extraneous function elements.
                bindingVariableDefinitions = [.. rhsAsFunction.Parameters.Select(p => (Identifier)p.Expression)];
            }
            else
            {
                // Error here
                break;
            }

            string partName = identifier.Token.Text;

            if (i == 0)
            {
                activeType = TypeFromContext.GetCodeTypeFromContext(parseInfo, scope, identifier);
            }
            else if (activeType is EnumType enumType)
            {
                targetEnumType = enumType;

                // Find item with name.
                var maybeMember = enumType.EnumMembers.FirstOrNull(member => member.Name == partName);

                // Member not found.
                if (maybeMember is null)
                    break;

                targetEnumMember = maybeMember.Value;
            }
            else
            {
                // Error here
                break;
            }
        }

        void GetBindingVariables()
        {
            bindingVariables = [..
                bindingVariableDefinitions.Select((bindingVariableDefinition, i) =>
                    GetBindingVariable(bindingVariableDefinition, targetEnumMember.Value.Items[i]))];
        }

        Var GetBindingVariable(Identifier identifier, CodeType bindToType)
        {
            var bindingVariable = new PatternBindingVariable(bindToType, scope, new PatternBindingVariableContextHandler(parseInfo, identifier)).GetVar();
            return bindingVariable;
        }

        GetBindingVariables();
    }

    public IWorkshopTree Parse(ActionSet actionSet)
    {
        var operand = lhs.Parse(actionSet);
        var unfolder = new ParallelEnumUnfolder(operand as IStructValue);

        // Bind pattern variables.
        for (int i = 0; i < bindingVariables.Length; i++)
        {
            var itemType = targetEnumMember.Value.Items[i];
            var assigner = itemType.GetGettableAssigner(AssigningAttributes.Empty);
            var gettable = assigner.Unfold(unfolder);

            actionSet.IndexAssigner.Add(bindingVariables[i], gettable);
        }


        return Element.Compare(targetEnumType.KeyOf(operand), Operator.Equal, targetEnumMember.Value.GetKey(actionSet));
    }

    public CodeType Type() => returnType;

    class PatternBindingVariable(CodeType bindingVariableType, IScopeHandler scopeHandler, IVarContextHandler contextHandler) : VarBuilder(scopeHandler, contextHandler)
    {
        protected override void Apply() { }
        protected override void CheckComponents() { }
        protected override void GetCodeType() => ApplyCodeType(bindingVariableType);
    }

    class PatternBindingVariableContextHandler(ParseInfo parseInfo, Identifier identifier) : IVarContextHandler
    {
        public ParseInfo ParseInfo => parseInfo;
        public IParseType GetCodeType() => null;
        public void GetComponents(VariableComponentCollection componentCollection, VariableSetKind variableSetKind) { }
        public Location GetDefineLocation() => new Location(parseInfo.Script.Uri, identifier.Range);
        public string GetName() => identifier.Token.Text;
        public DocRange GetNameRange() => identifier.Range;
        public DocRange GetTypeRange() => null;
    }

    class ParallelEnumUnfolder(IStructValue SourceEnum) : IUnfoldGettable
    {
        int currentSlot = 0;
        public IWorkshopTree NextValue() => SourceEnum.GetValue($"slot{currentSlot++}");
    }
}