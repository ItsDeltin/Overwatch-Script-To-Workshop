#nullable enable

using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Model;
using Deltin.Deltinteger.Parse.Types;
using Deltin.Deltinteger.Parse.Variables.Build;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler.Parse;

namespace Deltin.Deltinteger.Parse;

class PatternMatching
{
    public static MatchedEnumPattern? GetPattern(ParseInfo parseInfo, Scope scope, IParseExpression patternExpression)
    {
        // Expands the expression from a dot chain into a list of expressions.
        List<IParseExpression>? GetRhsPath()
        {
            var path = new List<IParseExpression>();

            // Ensures that only one error is added if something is wrong with the input pattern.
            bool didError = false;

            void Travel(IParseExpression value)
            {
                if (didError) return;

                if (value is BinaryOperatorExpression boe && boe.IsDotExpression())
                {
                    Travel(boe.Left);
                    Travel(boe.Right);
                }
                else if (value is Identifier or FunctionExpression)
                    path.Add(value);
                else
                {
                    parseInfo.Error("This expression cannot be used for pattern matching", value.Range);
                    didError = true;
                }
            }
            Travel(patternExpression);
            return didError ? null : path;
        }
        var path = GetRhsPath();

        // If path is null, an invalid expression is being used for pattern matching.
        if (path is null)
            return null;

        // Current type in the path. Will need to be changed for any future pattern parts
        // that contain non-types in the path (e.g. modules)
        CodeType? nextActiveType = null;

        // Binding variable definitions found within the path.
        Identifier[]? bindingVariableDefinitions = null;

        // The discovered enum target.
        (EnumType, EnumMember)? targetEnumMember = null;

        for (int i = 0; i < path.Count; i++)
        {
            bool isLast = i == path.Count - 1;

            // Ensure current path state doesn't carry over to the next loop.
            var activeType = nextActiveType;
            nextActiveType = null;

            var currentPathItem = path[i];
            Identifier identifier;

            // Extract the identifier from the current expression in the path.
            if (currentPathItem is Identifier rhsAsIdentifier)
                identifier = rhsAsIdentifier;
            // Will need to check for extraneous identifier elements.
            else if (currentPathItem is FunctionExpression rhsAsFunction)
            {
                if (rhsAsFunction.Target is Identifier functionTarget)
                    identifier = functionTarget;
                // Invalid target, error here.
                else
                {
                    parseInfo.Error("Invalid function target in pattern expression", rhsAsFunction.Target.Range);
                    return null;
                }

                // Ensure binding variables are formatted correctly.
                foreach (var bindingExpression in rhsAsFunction.Parameters)
                {
                    if (bindingExpression.Expression is not Identifier)
                    {
                        parseInfo.Error("Binding expression must be a variable name", bindingExpression.Expression.Range);
                        return null;
                    }
                }

                // Will need to check for extraneous function elements.
                bindingVariableDefinitions = [.. rhsAsFunction.Parameters.Select(p => (Identifier)p.Expression)];
            }
            else
            {
                // GetRhsPath shouldn't provide an expression that isn't a Function or Identifier
                throw new System.Exception("Invalid expression type while parsing pattern expression");
            }

            // No token; error will be added by parser.
            if (identifier.Token is null)
                return null;

            var partRange = identifier.Range;
            string partName = identifier.Token.Text;

            if (i == 0)
            {
                nextActiveType = TypeFromContext.GetCodeTypeFromContext(parseInfo, scope, identifier);

                if (isLast)
                {
                    parseInfo.Error($"The type '{nextActiveType.GetName()}' cannot be used as a pattern", partRange);
                    return null;
                }
            }
            else if (activeType is EnumType enumType)
            {
                // Find item with name.
                var maybeMember = enumType.EnumMembers.FirstOrNull(member => member.Name == partName);

                // Ensure that the member exists in the enumerator.
                if (maybeMember is null)
                {
                    parseInfo.Error($"Enum member '{partName}' does not exist in the enum ${enumType.GetName()}", partRange);
                    return null;
                }

                targetEnumMember = (enumType, maybeMember.Value);
            }
            else
            {
                parseInfo.Error($"The item '{partName}' cannot be used for pattern matching", partRange);
                return null;
            }
        }

        if (targetEnumMember is not null)
        {
            Var[] GetBindingVariables()
            {
                // No binding definitions provided.
                if (bindingVariableDefinitions is null)
                    return [];

                var memberItems = targetEnumMember.Value.Item2.Items;

                // Add error if there are too many variable bindings.
                if (bindingVariableDefinitions.Length > memberItems.Length)
                {
                    parseInfo.Error(
                        $"Extraneous variable binding for enum member '{targetEnumMember.Value.Item2.Name}'",
                        bindingVariableDefinitions[memberItems.Length].Range);
                    // No need to return null here.
                }

                // 'Take' will prevent taking too many items if the user adds to many binding variables.
                return [..
                    bindingVariableDefinitions.Take(memberItems.Length).Select((bindingVariableDefinition, i) =>
                        GetBindingVariable(bindingVariableDefinition, memberItems[i])
                    )];
            }

            Var GetBindingVariable(Identifier identifier, CodeType bindToType)
            {
                var bindingVariable = new PatternBindingVariable(bindToType, scope, new PatternBindingVariableContextHandler(parseInfo, identifier)).GetVar();
                return bindingVariable;
            }

            return new MatchedEnumPattern(GetBindingVariables(), targetEnumMember.Value.Item1, targetEnumMember.Value.Item2);
        }
        return null;
    }

    class PatternBindingVariable(CodeType bindingVariableType, IScopeHandler scopeHandler, IVarContextHandler contextHandler) : VarBuilder(scopeHandler, contextHandler)
    {
        protected override void Apply()
        {
            _varInfo.CodeLensType = CodeLensSourceType.ScopedVariable;
            _varInfo.TokenType = SemanticTokenType.Parameter;
        }
        protected override void CheckComponents() { }
        protected override void GetCodeType() => ApplyCodeType(bindingVariableType);
    }

    class PatternBindingVariableContextHandler(ParseInfo parseInfo, Identifier identifier) : IVarContextHandler
    {
        public ParseInfo ParseInfo => parseInfo;
        public IParseType? GetCodeType() => null;
        public void GetComponents(VariableComponentCollection componentCollection, VariableSetKind variableSetKind) { }
        public Location GetDefineLocation() => new Location(parseInfo.Script.Uri, identifier.Range);
        public string GetName() => identifier.Token.Text;
        public DocRange GetNameRange() => identifier.Range;
        public DocRange? GetTypeRange() => null;
    }
}

record MatchedEnumPattern(Var[] BindingVariables, EnumType TargetEnumType, EnumMember TargetEnumMember)
{
    public IWorkshopTree ToWorkshop(ActionSet actionSet, IWorkshopTree operand)
    {
        var unfolder = new ParallelEnumUnfolder(operand as IStructValue);

        // Bind pattern variables.
        for (int i = 0; i < BindingVariables.Length; i++)
        {
            var itemType = TargetEnumMember.Items[i];
            var assigner = itemType.GetGettableAssigner(AssigningAttributes.Empty);
            var gettable = assigner.Unfold(unfolder);

            actionSet.IndexAssigner.Add(BindingVariables[i], gettable);
        }


        return Element.Compare(TargetEnumType.KeyOf(operand), Operator.Equal, TargetEnumMember.GetKey(actionSet));
    }
}

class ParallelEnumUnfolder(IStructValue SourceEnum) : IUnfoldGettable
{
    int currentSlot = 0;
    public IWorkshopTree NextValue() => SourceEnum.GetValue($"slot{currentSlot++}");
}