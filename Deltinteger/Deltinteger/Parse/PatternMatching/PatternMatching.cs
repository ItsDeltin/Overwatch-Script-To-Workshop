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

namespace Deltin.Deltinteger.Parse;

class PatternMatching
{
    public static MatchedEnumPattern? GetPattern(
        ParseInfo parseInfo,
        Scope scope,
        Token patternStartToken,
        IParseExpression patternExpression,
        PatternOperand operand)
    {
        // Expands the expression from a dot chain into a list of expressions.
        List<(Token LeftHandToken, IParseExpression Expression, Token? RightHandToken)>? GetRhsPath()
        {
            var path = new List<(Token, IParseExpression, Token?)>();

            // Ensures that only one error is added if something is wrong with the input pattern.
            bool didError = false;

            void Travel(IParseExpression value, Token leftHandToken)
            {
                if (didError) return;

                if (value is BinaryOperatorExpression boe && boe.IsDotExpression())
                {
                    Travel(boe.Left, leftHandToken);
                    Travel(boe.Right, boe.Operator.Token);
                }
                else if (value is Identifier identifier)
                    path.Add((leftHandToken, value, identifier.NextToken));
                else if (value is FunctionExpression function)
                    path.Add((leftHandToken, value, function.LeftParentheses));
                else
                {
                    parseInfo.Error("This expression cannot be used for pattern matching", value.Range);
                    didError = true;
                }
            }
            Travel(patternExpression, patternStartToken);
            return didError ? null : path;
        }
        var path = GetRhsPath();

        void AddCompletionOfPathItem(
            DocRange? range,
            CodeType? activeType,
            bool addTypeCompletion)
        {
            if (range is null) return;

            parseInfo.Script.AddCompletionRange(ICompletionRange.New(range, p => [
                // Add enum members to completion if applicable.
                ..activeType is EnumType enumType
                    ? enumType.ReturningScope().GetCompletion(parseInfo.TranslateInfo, p.Pos, p.Immediate)
                    : [],

                // Add types to completion if applicable.
                .. addTypeCompletion
                    ? CodeTypeHelpers.GetAllTypesInScope(scope)
                        .Where(typeProvider => TypeProvider.IsTypeProviderOfKind(typeProvider, TypeKind.Enum))
                        .Select(typeProvider => typeProvider.GetInstance(new()).GetCompletion())
                    : []
            ]));
        }

        var operandType = operand.Expression.Type();

        // Current type in the path. Will need to be changed for any future pattern parts
        // that contain non-types in the path (e.g. modules)
        // This currently defaults to the type of the operand expression
        // for the matching shorthand feature.
        CodeType? nextActiveType = operandType is EnumType ? operandType : null;
        bool nextIsShorthand = true;

        // Ensure that completion is created if no items were added to the path.
        if (path is null || path.Count == 0)
        {
            var nextToken = parseInfo.Script.NextToken(patternStartToken);
            if (nextToken is not null)
            {
                var completionRange = patternStartToken.Range.End + nextToken.Range.Start;
                AddCompletionOfPathItem(completionRange, nextActiveType, true);
            }
        }

        // If path is null, an invalid expression is being used for pattern matching.
        if (path is null)
            return null;

        DocRange? CompletionRangeOfPathItem(int item)
        {
            var lhsToken = path[item].LeftHandToken;
            var rhsToken = path[item].RightHandToken ?? parseInfo.Script.NextToken(lhsToken);
            return rhsToken is null ? null : lhsToken.Range.End + rhsToken.Range.Start;
        }

        // Binding variable definitions found within the path.
        Identifier[]? bindingVariableDefinitions = null;

        // The discovered enum target.
        (EnumType EnumType, EnumMember EnumMember)? targetEnumMember = null;

        for (int i = 0; i < path.Count; i++)
        {
            bool isLast = i == path.Count - 1;
            bool doMatchTypes = i == 0;

            // Ensure current path state doesn't carry over to the next loop.
            var activeType = nextActiveType;
            nextActiveType = null;

            var isShorthand = nextIsShorthand;
            nextIsShorthand = false;

            var currentPathItem = path[i].Expression;

            // Add completion data for the current item in the path.
            AddCompletionOfPathItem(CompletionRangeOfPathItem(i), activeType, doMatchTypes);

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

            if (activeType is EnumType enumType)
            {
                // Find item with name.
                var maybeMember = enumType.EnumMembers.FirstOrNull(member => member.Name == partName);

                // Ensure that the member exists in the enumerator.
                if (maybeMember is null)
                {
                    // Error if soft match is not enabled.
                    if (!isShorthand)
                    {
                        parseInfo.Error($"Enum member '{partName}' does not exist in the enum ${enumType.GetName()}", partRange);
                        return null;
                    }
                }
                else
                {
                    targetEnumMember = (enumType, maybeMember.Value);
                    continue;
                }
            }

            if (doMatchTypes)
            {
                nextActiveType = TypeFromContext.GetCodeTypeFromContext(parseInfo, scope, identifier);

                if (isLast)
                {
                    parseInfo.Error($"The type '{nextActiveType.GetName()}' cannot be used as a pattern", partRange);
                    return null;
                }
            }
            else
            {
                parseInfo.Error($"The item '{partName}' cannot be used for pattern matching", partRange);
                return null;
            }
        }

        // If the operand can be modified, then the binded variables can as well.
        bool operandIsMutable = operand.LinkedVariable is not null &&
            operand.LinkedVariable.SetVariable.Calling.Attributes.CanBeSet;

        if (targetEnumMember is not null)
        {
            var targetEnumKind = targetEnumMember.Value.EnumType.EnumKind;
            var isOperandAndTargetEqual = CodeTypeHelpers.DoesTypeImplement(targetEnumMember.Value.EnumType, operandType);

            // Ensure that the target is compatible with the operand.
            // Parallel enums can't be pattern matched with incompatible data type.
            bool invalidDueToParallelEnum = targetEnumKind is EnumKind.Parallel && !isOperandAndTargetEqual;
            if (invalidDueToParallelEnum)
            {
                parseInfo.Error(
                    $"Operand type '{operandType.GetName()}' cannot be used to pattern match with parallel enum type '{targetEnumMember.Value.EnumType.GetName()}'",
                    patternStartToken.Range
                );
                return null;
            }

            // Check the inverse, make sure a struct is not being pattern matched with
            // an incompatible target.
            bool invalidDueToBadOperand = !CodeTypeHelpers.IsCompatibleWithAny(operandType) && !isOperandAndTargetEqual;
            if (invalidDueToBadOperand)
            {
                parseInfo.Error(
                    $"Constant or parallel operand type '{operandType.GetName()}' cannot be used to pattern match with enum type '{targetEnumMember.Value.EnumType.GetName()}'",
                    patternStartToken.Range
                );
                return null;
            }

            Var[] GetBindingVariables()
            {
                // No binding definitions provided.
                if (bindingVariableDefinitions is null)
                    return [];

                var memberItems = targetEnumMember.Value.EnumMember.Items;

                // Add error if there are too many variable bindings.
                if (bindingVariableDefinitions.Length > memberItems.Length)
                {
                    parseInfo.Error(
                        $"Extraneous variable binding for enum member '{targetEnumMember.Value.EnumMember.Name}'",
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
                var bindingVariable = new PatternBindingVariable(
                    bindToType,
                    scope,
                    new PatternBindingVariableContextHandler(parseInfo, identifier),
                    operandIsMutable).GetVar();
                return bindingVariable;
            }

            return new MatchedEnumPattern(
                GetBindingVariables(),
                targetEnumMember.Value.EnumType,
                targetEnumMember.Value.EnumMember,
                operand);
        }
        return null;
    }

    public static PatternOperand GetPatternOperand(ParseInfo parseInfo, IExpression expression)
    {
        var variable = new VariableResolve(parseInfo, new(), expression, null, MutedVariableResolveErrorHandler.Instance);
        return new PatternOperand(expression, variable.DoesResolveToVariable ? variable : null);
    }

    sealed class PatternBindingVariable(
        CodeType bindingVariableType,
        IScopeHandler scopeHandler,
        IVarContextHandler contextHandler,
        bool operandIsMutable) : VarBuilder(scopeHandler, contextHandler)
    {
        protected override void Apply()
        {
            _varInfo.CodeLensType = CodeLensSourceType.ScopedVariable;
            _varInfo.TokenType = SemanticTokenType.Parameter;
            _varInfo.TokenModifiers.Add(TokenModifier.Declaration);

            if (!operandIsMutable)
            {
                _varInfo.VariableTypeHandler.SetWorkshopReference();
                _varInfo.TokenModifiers.Add(TokenModifier.Readonly);
            }
        }
        protected override void CheckComponents() { }
        protected override void GetCodeType() => ApplyCodeType(bindingVariableType);
    }

    sealed class PatternBindingVariableContextHandler(ParseInfo parseInfo, Identifier identifier) : IVarContextHandler
    {
        public ParseInfo ParseInfo => parseInfo;
        public IParseType? GetCodeType() => null;
        public void GetComponents(VariableComponentCollection componentCollection, VariableSetKind variableSetKind) { }
        public Location GetDefineLocation() => new Location(parseInfo.Script.Uri, identifier.Range);
        public string GetName() => identifier.Token.Text;
        public DocRange GetNameRange() => identifier.Range;
        public DocRange? GetTypeRange() => null;
    }

    sealed class MutedVariableResolveErrorHandler : IVariableResolveErrorHandler
    {
        public static MutedVariableResolveErrorHandler Instance = new();

        private MutedVariableResolveErrorHandler() { }

        public void Error(string message, DocRange errorRange) { }
    }
}

sealed record MatchedEnumPattern(
    Var[] BindingVariables,
    EnumType TargetEnumType,
    EnumMember TargetEnumMember,
    PatternOperand Operand)
{
    public IWorkshopTree ToWorkshop(ActionSet actionSet)
    {
        IWorkshopTree sourceValue;
        IUnfoldGettable unfolder;
        bool isParallel = TargetEnumType.EnumKind is EnumKind.Parallel;

        // Create the unfolder that will be used for this.
        if (Operand.LinkedVariable is not null)
        {
            // Potentially mutable variable was provided.
            var parsedElements = Operand.LinkedVariable.ParseElements(actionSet);
            unfolder = isParallel
                ? new ParallelEnumUnfolder((IStructValue)parsedElements.IndexReference, parsedElements.Target)
                : new SingleEnumUnfolder(parsedElements.AsGettable());

            sourceValue = parsedElements.AsGettable().GetVariable();
        }
        else
        {
            sourceValue = Operand.Expression.Parse(actionSet);
            unfolder = isParallel
                ? new ParallelEnumUnfolder((IStructValue)sourceValue, null)
                : new SingleEnumUnfolder(new WorkshopElementReference(sourceValue));
        }

        // Bind pattern variables.
        for (int i = 0; i < BindingVariables.Length; i++)
        {
            var itemType = TargetEnumMember.Items[i];
            var assigner = itemType.GetGettableAssigner(AssigningAttributes.Empty);
            var gettable = assigner.Unfold(unfolder);

            actionSet.IndexAssigner.Add(BindingVariables[i], gettable);
        }


        return Element.Compare(
            TargetEnumType.KeyOf(sourceValue),
            Operator.Equal,
            TargetEnumMember.GetKey(actionSet));
    }
}

sealed class ParallelEnumUnfolder(IStructValue SourceEnum, IWorkshopTree? TargetPlayer) : IUnfoldGettable
{
    int currentSlot = 0;
    public IGettable NextValue()
    {
        var slot = SourceEnum.GetGettable($"slot{currentSlot++}");
        if (TargetPlayer is not null)
            return new TargetGettable(slot, (Element)TargetPlayer);
        return slot;
    }
}

sealed class SingleEnumUnfolder(IGettable SourceValue) : IUnfoldGettable
{
    int currentIndex = 1;
    public IGettable NextValue() => SourceValue.ChildFromClassReference(Element.Num(currentIndex++));
}

readonly record struct PatternOperand(IExpression Expression, VariableResolve? LinkedVariable);