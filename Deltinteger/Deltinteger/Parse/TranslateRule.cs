using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class TranslateRule
    {
        public static bool AllowRecursion = false;

        public static Rule GetRule(RuleNode ruleNode, ScopeGroup root, ParsingData parserData)
        {
            var result = new TranslateRule(ruleNode, root, parserData);
            return result.Rule;
        }

        public readonly VarCollection VarCollection;
        private readonly Rule Rule;
        private readonly List<Element> Actions = new List<Element>();
        private readonly List<Condition> Conditions = new List<Condition>();
        public readonly bool IsGlobal;
        private readonly List<A_Skip> ReturnSkips = new List<A_Skip>(); // Return statements whos skip count needs to be filled out.
        private readonly ContinueSkip ContinueSkip; // Contains data about the wait/skip for continuing loops.
        private readonly List<MethodStack> MethodStack = new List<MethodStack>(); // The user method stack
        private readonly List<UserMethod> MethodStackNoRecursive = new List<UserMethod>();
        public readonly ParsingData ParserData;

        private TranslateRule(RuleNode ruleNode, ScopeGroup root, ParsingData parserData)
        {
            VarCollection = parserData.VarCollection;
            ParserData = parserData;

            Rule = new Rule(ruleNode.Name, ruleNode.Event, ruleNode.Team, ruleNode.Player);
            IsGlobal = Rule.IsGlobal;

            ContinueSkip = new ContinueSkip(IsGlobal, Actions, VarCollection);

            ParseConditions(root, ruleNode.Conditions);

            // Parse the block of the rule.
            ScopeGroup ruleScope = root.Child();
            ParseBlock(ruleScope, ruleNode.Block, false, null);
            
            if (ruleScope.Out().Length != 0) throw new Exception();

            Rule.Actions = Actions.ToArray();
            Rule.Conditions = Conditions.ToArray();

            // Fufill remaining skips
            foreach (var skip in ReturnSkips)
                if (Actions.Last() != skip)
                    skip.ParameterValues = new IWorkshopTree[] { new V_Number(Actions.Count - ReturnSkips.IndexOf(skip)) };
                else
                    Actions.Remove(skip);
            ReturnSkips.Clear();
        }

        void ParseConditions(ScopeGroup scope, Node[] expressions)
        {
            foreach(var expr in expressions)
            {
                Element parsedIf = ParseExpression(scope, expr);
                // If the parsed if is a V_Compare, translate it to a condition.
                // Makes "(value1 == value2) == true" to just "value1 == value2"
                if (parsedIf is V_Compare)
                {
                    Element left = (Element)parsedIf.ParameterValues[0];
                    if (!left.ElementData.IsValue)
                        throw SyntaxErrorException.InvalidMethodType(true, left.Name, ((Node)expr).Range);

                    Element right = (Element)parsedIf.ParameterValues[2];
                    if (!right.ElementData.IsValue)
                        throw SyntaxErrorException.InvalidMethodType(true, right.Name, ((Node)expr).Range);

                    Conditions.Add(
                        new Condition(
                            left,
                            (EnumMember)parsedIf.ParameterValues[1],
                            right
                        )
                    );
                }
                // If not, just do "parsedIf == true"
                else
                {
                    if (!parsedIf.ElementData.IsValue)
                        throw SyntaxErrorException.InvalidMethodType(true, parsedIf.Name, ((Node)expr).Range);

                    Conditions.Add(new Condition(
                        parsedIf, EnumData.GetEnumValue(Operators.Equal), new V_True()
                    ));
                }
            }
        }

        Element ParseExpression(ScopeGroup scope, Node expression)
        {
            switch (expression)
            {
                // Math and boolean operations.
                case OperationNode operationNode:
                {
                    Element left = ParseExpression(scope, operationNode.Left);
                    Element right = ParseExpression(scope, operationNode.Right);

                    /*
                    if (Constants.BoolOperations.Contains(operationNode.Operation))
                    {
                        if (left.ElementData.ValueType != Elements.ValueType.Any && left.ElementData.ValueType != Elements.ValueType.Boolean)
                            throw new SyntaxErrorException($"Expected boolean, got {left .ElementData.ValueType.ToString()} instead.", ((Node)operationNode.Left).Range);
                        
                        if (right.ElementData.ValueType != Elements.ValueType.Any && right.ElementData.ValueType != Elements.ValueType.Boolean)
                            throw new SyntaxErrorException($"Expected boolean, got {right.ElementData.ValueType.ToString()} instead.", ((Node)operationNode.Right).Range);
                    }
                    */

                    switch (operationNode.Operation)
                    {
                        // Math: ^, *, %, /, +, -
                        case "^":
                            return Element.Part<V_RaiseToPower>(left, right);

                        case "*":
                            return Element.Part<V_Multiply>(left, right);

                        case "%":
                            return Element.Part<V_Modulo>(left, right);

                        case "/":
                            return Element.Part<V_Divide>(left, right);

                        case "+":
                            return Element.Part<V_Add>(left, right);

                        case "-":
                            return Element.Part<V_Subtract>(left, right);


                        // BoolCompare: &, |
                        case "&":
                            return Element.Part<V_And>(left, right);

                        case "|":
                            return Element.Part<V_Or>(left, right);

                        // Compare: <, <=, ==, >=, >, !=
                        case "<":
                            return Element.Part<V_Compare>(left, EnumData.GetEnumValue(Operators.LessThan), right);

                        case "<=":
                            return Element.Part<V_Compare>(left, EnumData.GetEnumValue(Operators.LessThanOrEqual), right);

                        case "==":
                            return Element.Part<V_Compare>(left, EnumData.GetEnumValue(Operators.Equal), right);

                        case ">=":
                            return Element.Part<V_Compare>(left, EnumData.GetEnumValue(Operators.GreaterThanOrEqual), right);

                        case ">":
                            return Element.Part<V_Compare>(left, EnumData.GetEnumValue(Operators.GreaterThan), right);

                        case "!=":
                            return Element.Part<V_Compare>(left, EnumData.GetEnumValue(Operators.NotEqual), right);
                    }
                    
                    throw new Exception($"Operation {operationNode.Operation} not implemented.");
                }

                // Number
                case NumberNode numberNode:
                    return new V_Number(numberNode.Value);
                
                // Bool
                case BooleanNode boolNode:
                    if (boolNode.Value)
                        return new V_True();
                    else
                        return new V_False();
                
                // Not operation
                case NotNode notNode:
                    return Element.Part<V_Not>(ParseExpression(scope, notNode.Value));

                // Strings
                case StringNode stringNode:
                    Element[] stringFormat = new Element[stringNode.Format?.Length ?? 0];
                    for (int i = 0; i < stringFormat.Length; i++)
                        stringFormat[i] = ParseExpression(scope, stringNode.Format[i]);
                    return V_String.ParseString(stringNode.Range, stringNode.Value, stringFormat);

                // Null
                case NullNode nullNode:
                    return new V_Null();

                // TODO check if groups need to be implemented here

                // Methods
                case MethodNode methodNode:
                    return ParseMethod(scope, methodNode, true);

                // Variable
                case VariableNode variableNode:
                    return scope.GetVar(variableNode.Name, variableNode.Range)
                        .GetVariable(variableNode.Target != null ? ParseExpression(scope, variableNode.Target) : null);

                // Get value in array
                case ValueInArrayNode viaNode:
                    return Element.Part<V_ValueInArray>(ParseExpression(scope, viaNode.Value), ParseExpression(scope, viaNode.Index));

                // Create array
                case CreateArrayNode createArrayNode:
                {
                    Element prev = null;
                    Element current = null;

                    for (int i = 0; i < createArrayNode.Values.Length; i++)
                    {
                        current = new V_Append()
                        {
                            ParameterValues = new IWorkshopTree[2]
                        };

                        if (prev != null)
                            current.ParameterValues[0] = prev;
                        else
                            current.ParameterValues[0] = new V_EmptyArray();

                        current.ParameterValues[1] = ParseExpression(scope, createArrayNode.Values[i]);
                        prev = current;
                    }

                    return current ?? new V_EmptyArray();
                }

                // Ternary Conditional (a ? b : c)
                case TernaryConditionalNode ternaryNode:
                    return Element.TernaryConditional
                    (
                        ParseExpression(scope, ternaryNode.Condition),
                        ParseExpression(scope, ternaryNode.Consequent),
                        ParseExpression(scope, ternaryNode.Alternative)
                    );

                // Enums
                case EnumNode enumNode:
                    return EnumData.ToElement(enumNode.EnumMember) 
                    ?? throw SyntaxErrorException.EnumCantBeValue(enumNode.Type, enumNode.Range);

                // New object
                case CreateObjectNode createObjectNode:

                    DefinedType typeData = ParserData.GetDefinedType(createObjectNode.TypeName, createObjectNode.Range);

                    IndexedVar store = VarCollection.AssignVar(scope, typeData.Name + " store", IsGlobal, null);
                    store.Type = typeData;

                    ScopeGroup typeScope = typeData.GetRootScope(store, ParserData);

                    // Set the default variables in the struct
                    for (int i = 0; i < typeData.DefinedVars.Length; i++)
                    {
                        if (typeData.DefinedVars[i].Value != null)
                            Actions.AddRange(
                                store.SetVariable(ParseExpression(typeScope, typeData.DefinedVars[i].Value), null, new V_Number(i))
                            );
                    }

                    Constructor constructor = typeData.Constructors.FirstOrDefault(c => c.Parameters.Length == createObjectNode.Parameters.Length);
                    if (constructor == null && !(createObjectNode.Parameters.Length == 0 && typeData.Constructors.Length == 0))
                        throw new SyntaxErrorException(
                            $"No constructors in the {typeData.TypeKind} {typeData.Name} have {createObjectNode.Parameters.Length} parameters.",
                            createObjectNode.Range);
                    if (constructor != null)
                    {
                        ScopeGroup constructorScope = typeScope.Child();
                        for (int i = 0; i < constructor.Parameters.Length; i++)
                        {
                            IndexedVar var = VarCollection.AssignVar(constructorScope, constructor.Parameters[i].Name, IsGlobal, createObjectNode);
                            Actions.AddRange
                            (
                                var.InScope(ParseExpression(scope, createObjectNode.Parameters[i]))
                            );
                        }

                        ParseBlock(constructorScope, constructor.BlockNode, true, null);
                    }

                    return store.GetVariable();

                // Expression tree
                case ExpressionTreeNode expressionTree:
                    return new ParseExpressionTree(this, scope, expressionTree).ResultingElement;
                
                // This
                case ThisNode thisNode:
                    return scope.GetThis(thisNode.Range).GetVariable();
            }

            throw new Exception();
        }

        IWorkshopTree[] ParseParameters(ScopeGroup scope, ParameterBase[] parameters, Node[] values, string methodName, Range methodRange)
        {
            // Syntax error if there are too many parameters.
            if (values.Length > parameters.Length)
                throw SyntaxErrorException.TooManyParameters(methodName, parameters.Length, values.Length, values[parameters.Length].Range);

            // Parse the parameters
            List<IWorkshopTree> parsedParameters = new List<IWorkshopTree>();
            for(int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] is Parameter || parameters[i] is EnumParameter)
                {
                    // Get the default parameter value if there are not enough parameters.
                    if (values.Length <= i)
                    {
                        IWorkshopTree defaultValue = parameters[i].GetDefault();

                        // If there is no default value, throw a syntax error.
                        if (defaultValue == null)
                            throw SyntaxErrorException.MissingParameter(parameters[i].Name, methodName, methodRange);
                        
                        parsedParameters.Add(defaultValue);
                    }
                    else
                    {
                        if (parameters[i] is Parameter)
                            // Parse the parameter
                            parsedParameters.Add(ParseExpression(scope, values[i]));
                        else if (parameters[i] is EnumParameter)
                        {
                            // Parse the enum
                            if (values[i] is EnumNode)
                            {
                                EnumNode enumNode = (EnumNode)values[i];
                                parsedParameters.Add(
                                    (IWorkshopTree)EnumData.ToElement(enumNode.EnumMember)
                                    ?? (IWorkshopTree)enumNode.EnumMember
                                );
                            }
                            else
                                throw new SyntaxErrorException("Expected the enum " + ((EnumParameter)parameters[i]).EnumData.CodeName + ", got a value instead.", ((Node)values[i]).Range);
                        }
                    }
                }
                else if (parameters[i] is VarRefParameter)
                {
                    // A VarRef parameter is always required, there will never be a default to fallback on.
                    if (values.Length <= i)
                        throw SyntaxErrorException.MissingParameter(parameters[i].Name, methodName, methodRange);
                    
                    // A VarRef parameter must be a variable
                    if (!(values[i] is VariableNode))
                        throw new SyntaxErrorException("Expected variable", ((Node)values[i]).Range);
                    
                    VariableNode variableNode = (VariableNode)values[i];

                    Element target = null;
                    if (variableNode.Target != null)
                        target = ParseExpression(scope, variableNode.Target);

                    parsedParameters.Add(new VarRef((IndexedVar)scope.GetVar(variableNode.Name, variableNode.Range), target));
                        
                }
                else throw new NotImplementedException();
            }
            return parsedParameters.ToArray();
        }

        Element ParseMethod(ScopeGroup scope, MethodNode methodNode, bool needsToBeValue)
        {
            methodNode.RelatedScopeGroup = scope;

            IMethod method = scope.GetMethod(methodNode.Name, methodNode.Range);
            
            // Parse the parameters
            IWorkshopTree[] parsedParameters = ParseParameters(scope, method.Parameters, methodNode.Parameters, methodNode.Name, methodNode.Range);

            Element result;
            if (method is ElementList)
            {
                ElementList elementData = (ElementList)method;
                result = elementData.GetObject();
                result.ParameterValues = parsedParameters.ToArray();

                foreach (var usageDiagnostic in elementData.UsageDiagnostics)
                    ParserData.Diagnostics.AddDiagnostic(usageDiagnostic.GetDiagnostic(methodNode.Range));
            }
            else if (method is CustomMethodData)
            {
                switch (((CustomMethodData)method).CustomMethodType)
                {
                    case CustomMethodType.Action:
                        if (needsToBeValue)
                            throw SyntaxErrorException.InvalidMethodType(true, methodNode.Name, methodNode.Range);
                        break;

                    case CustomMethodType.MultiAction_Value:
                    case CustomMethodType.Value:
                        if (!needsToBeValue)
                            throw SyntaxErrorException.InvalidMethodType(false, methodNode.Name, methodNode.Range);
                        break;
                }

                var customMethodResult = ((CustomMethodData)method)
                    .GetObject(this, scope, parsedParameters.ToArray())
                    .Result();

                // Some custom methods have extra actions.
                if (customMethodResult.Elements != null)
                    Actions.AddRange(customMethodResult.Elements);

                result = customMethodResult.Result;
            }
            else if (method is UserMethod)
            {
                result = ParseUserMethod(scope, methodNode, (UserMethod)method, parsedParameters.ToArray());
            }
            else throw new NotImplementedException();

            methodNode.RelatedElement = result;
            return result;
        }

        Element ParseUserMethod(ScopeGroup scope, MethodNode methodNode, UserMethod method, IWorkshopTree[] parameters)
        {
            UserMethod userMethod = (UserMethod)method;
            Element result;
            if (!userMethod.IsRecursive)
            {
                // Check the method stack if this method was already called.
                // Throw a syntax error if it was.
                if (MethodStackNoRecursive.Contains(userMethod))
                    throw SyntaxErrorException.RecursionNotAllowed(methodNode.Range);

                var methodScope = scope.Root().Child();

                // Add the parameter variables to the scope.
                IndexedVar[] parameterVars = new IndexedVar[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    // Create a new variable using the parameter.
                    parameterVars[i] = VarCollection.AssignVar(methodScope, userMethod.Parameters[i].Name, IsGlobal, methodNode);
                    Actions.AddRange(parameterVars[i].SetVariable((Element)parameters[i]));
                }

                // The variable that stores the return value.
                IndexedVar returns = VarCollection.AssignVar(scope, $"{methodNode.Name}: return temp value", IsGlobal, null);

                // Add the method to the method stack
                MethodStackNoRecursive.Add(userMethod);

                userMethod.Block.RelatedScopeGroup = methodScope;

                // Parse the block of the method
                ParseBlock(methodScope, userMethod.Block, true, returns);

                // Take the method scope out of scope.
                Actions.AddRange(methodScope.Out());

                // Remove the method from the stack.
                MethodStackNoRecursive.Remove(userMethod);

                result = returns.GetVariable();
            }
            else
            {
                // Check the method stack if this method was already called. It will be null if it wasn't called.
                MethodStack lastMethod = MethodStack.FirstOrDefault(ms => ms.UserMethod == userMethod);
                if (lastMethod != null)
                {
                    ContinueSkip.Setup();

                    // Re-push the paramaters.
                    for (int i = 0; i < lastMethod.ParameterVars.Length; i++)
                    {
                        Actions.AddRange
                        (
                            lastMethod.ParameterVars[i].InScope(ParseExpression(scope, methodNode.Parameters[i]))
                        );
                    }

                    // Add to the continue skip array.
                    Actions.AddRange(
                        lastMethod.ContinueSkipArray.SetVariable(
                            Element.Part<V_Append>(lastMethod.ContinueSkipArray.GetVariable(), new V_Number(ContinueSkip.GetSkipCount() + 3))
                        )
                    );

                    // Loop back to the start of the method.
                    ContinueSkip.SetSkipCount(lastMethod.ActionIndex);
                    Actions.Add(Element.Part<A_Loop>());

                    result = lastMethod.Return.GetVariable();
                }
                else
                {
                    var methodScope = scope.Root().Child(true);

                    // Add the parameter variables to the scope.
                    RecursiveVar[] parameterVars = new RecursiveVar[userMethod.Parameters.Length];
                    for (int i = 0; i < parameterVars.Length; i++)
                    {
                        // Create a new variable using the parameter input.
                        parameterVars[i] = (RecursiveVar)VarCollection.AssignVar(methodScope, userMethod.Parameters[i].Name, IsGlobal, methodNode);
                        Actions.AddRange
                        (
                            parameterVars[i].InScope(ParseExpression(scope, methodNode.Parameters[i]))
                        );
                    }

                    var returns = VarCollection.AssignVar(null, $"{methodNode.Name}: return temp value", IsGlobal, null);

                    // Setup the continue skip array.
                    IndexedVar continueSkipArray = VarCollection.AssignVar(null, $"{methodNode.Name}: continue skip array", IsGlobal, null);
                    var stack = new MethodStack(userMethod, parameterVars, ContinueSkip.GetSkipCount(), returns, continueSkipArray);

                    // Add the method to the stack.
                    MethodStack.Add(stack);

                    userMethod.Block.RelatedScopeGroup = methodScope;
                    
                    // Parse the method block
                    ParseBlock(methodScope, userMethod.Block, true, returns);

                    // No return value if the method is being used as an action.
                    result = returns.GetVariable();

                    // Take the method out of scope.
                    Actions.AddRange(methodScope.Out());

                    // Setup the next continue skip.
                    ContinueSkip.Setup();
                    ContinueSkip.SetSkipCount(Element.Part<V_LastOf>(continueSkipArray.GetVariable()));

                    // Remove the last continue skip.
                    Actions.AddRange(
                        continueSkipArray.SetVariable(
                            Element.Part<V_ArraySlice>(
                                continueSkipArray.GetVariable(), 
                                new V_Number(0),
                                Element.Part<V_Subtract>(
                                    Element.Part<V_CountOf>(continueSkipArray.GetVariable()), new V_Number(1)
                                )
                            )
                        )
                    );

                    // Loop if the method goes any deeper by checking the length of the continue skip array.
                    Actions.Add(
                        Element.Part<A_LoopIf>(
                            Element.Part<V_Compare>(
                                Element.Part<V_CountOf>(continueSkipArray.GetVariable()),
                                EnumData.GetEnumValue(Operators.NotEqual),
                                new V_Number(0)
                            )
                        )
                    );

                    // Reset the continue skip.
                    ContinueSkip.ResetSkip();
                    Actions.AddRange(continueSkipArray.SetVariable(new V_Number()));
                    
                    // Remove the method from the stack.
                    MethodStack.Remove(stack);
                }
            }
            return result;
        }

        void ParseBlock(ScopeGroup scopeGroup, BlockNode blockNode, bool fulfillReturns, IndexedVar returnVar)
        {
            if (scopeGroup == null)
                throw new ArgumentNullException(nameof(scopeGroup));

            blockNode.RelatedScopeGroup = scopeGroup;

            int returnSkipStart = ReturnSkips.Count;
            
            for (int i = 0; i < blockNode.Statements.Length; i++)
                ParseStatement(scopeGroup, blockNode.Statements[i], returnVar);

            if (fulfillReturns)
                for (int i = ReturnSkips.Count - 1; i >= returnSkipStart; i--)
                {
                    ReturnSkips[i].ParameterValues = new IWorkshopTree[]
                    {
                        new V_Number(Actions.Count - 1 - Actions.IndexOf(ReturnSkips[i]))
                    };
                    ReturnSkips.RemoveAt(i);
                }
        }

        void ParseStatement(ScopeGroup scope, Node statement, IndexedVar returnVar)
        {
            switch (statement)
            {
                // Method
                case MethodNode methodNode:
                    Element method = ParseMethod(scope, methodNode, false);
                    if (method != null)
                        Actions.Add(method);
                    return;
                
                // Variable set
                case VarSetNode varSetNode:
                    ParseVarset(scope, varSetNode);
                    return;

                // Foreach
                case ForEachNode forEachNode:
                {
                    ContinueSkip.Setup();

                    ScopeGroup forGroup = scope.Child();

                    Element array = ParseExpression(scope, forEachNode.Array);

                    IndexedVar index = VarCollection.AssignVar(scope, $"'{forEachNode.VariableName}' for index", IsGlobal, null);

                    int offset = 0;

                    Element getVariableReference()
                    {
                        if (offset == 0)
                            return Element.Part<V_ValueInArray>(array, index.GetVariable());
                        else
                            return Element.Part<V_ValueInArray>(array, Element.Part<V_Add>(index.GetVariable(), getOffset()));
                    }
                    V_Number getOffset()
                    {
                        return new V_Number(offset);
                    }

                    ElementReferenceVar variable = new ElementReferenceVar(forEachNode.VariableName, forGroup, forEachNode, getVariableReference());
                    // VarCollection.AssignElementReferenceVar(
                    //     forGroup, 
                    //     forEachNode.VariableName, 
                    //     forEachNode, 
                    //     getVariableReference()
                    // );

                    // Reset the counter.
                    Actions.AddRange(index.SetVariable(new V_Number(0)));

                    // The action the for loop starts on.
                    int forStartIndex = ContinueSkip.GetSkipCount();

                    A_SkipIf skipCondition = new A_SkipIf() { ParameterValues = new IWorkshopTree[2] };
                    skipCondition.ParameterValues[0] = 
                        Element.Part<V_Not>(Element.Part<V_Compare>(
                            index.GetVariable(),
                            EnumData.GetEnumValue(Operators.LessThan),
                            Element.Part<V_CountOf>(array)
                        ));
                    Actions.Add(skipCondition);

                    List<A_SkipIf> rangeSkips = new List<A_SkipIf>();
                    for (; offset < forEachNode.Repeaters; offset++)
                    {
                        if (offset > 0)
                        {
                            variable.Reference = getVariableReference();

                            A_SkipIf skipper = new A_SkipIf() { ParameterValues = new Element[2] };
                            skipper.ParameterValues[0] = Element.Part<V_Not>(
                                Element.Part<V_Compare>(
                                    Element.Part<V_Add>(index.GetVariable(), getOffset()),
                                    EnumData.GetEnumValue(Operators.LessThan),
                                    Element.Part<V_CountOf>(array)
                                )
                            );
                            rangeSkips.Add(skipper);
                            Actions.Add(skipper);
                        }

                        // Parse the for's block. Use a child to prevent conflicts with repeaters.
                        ScopeGroup tempChild = forGroup.Child();
                        ParseBlock(tempChild, forEachNode.Block, false, returnVar);
                        Actions.AddRange(tempChild.Out());
                    }

                    // Increment the index
                    Actions.AddRange(index.SetVariable(Element.Part<V_Add>(index.GetVariable(), new V_Number(forEachNode.Repeaters))));

                    // Add the for's finishing elements
                    ContinueSkip.SetSkipCount(forStartIndex);
                    Actions.Add(Element.Part<A_Loop>());

                    rangeSkips.ForEach(var => var.ParameterValues[1] = new V_Number(GetSkipCount(var)));
                    
                    // Set the skip
                    if (skipCondition != null)
                        skipCondition.ParameterValues[1] = new V_Number(GetSkipCount(skipCondition));

                    ContinueSkip.ResetSkip();
                    return;
                }

                // For
                case ForNode forNode:
                {
                    ContinueSkip.Setup();

                    ScopeGroup forContainer = scope.Child();

                    // Set the variable
                    if (forNode.VarSetNode != null)
                        ParseVarset(scope, forNode.VarSetNode);
                    if (forNode.DefineNode != null)
                        ParseDefine(forContainer, forNode.DefineNode);
                    
                    ScopeGroup forGroup = forContainer.Child();

                    // The action the for loop starts on.
                    int forStartIndex = ContinueSkip.GetSkipCount();

                    A_SkipIf skipCondition = null;
                    // Skip if the condition is false.
                    if (forNode.Expression != null) // If it has an expression
                    {
                        skipCondition = new A_SkipIf() { ParameterValues = new IWorkshopTree[2] };
                        skipCondition.ParameterValues[0] = Element.Part<V_Not>(ParseExpression(forGroup, forNode.Expression));
                        Actions.Add(skipCondition);
                    }

                    // Parse the for's block.
                    ParseBlock(forGroup, forNode.Block, false, returnVar);

                    // Parse the statement
                    if (forNode.Statement != null)
                        ParseVarset(forGroup, forNode.Statement);
                    
                    // Take the for out of scope.
                    Actions.AddRange(forGroup.Out());

                    ContinueSkip.SetSkipCount(forStartIndex);
                    Actions.Add(Element.Part<A_Loop>());
                    
                    // Set the skip
                    if (skipCondition != null)
                        skipCondition.ParameterValues[1] = new V_Number(GetSkipCount(skipCondition));
                    
                    // Take the defined variable in the for out of scope
                    Actions.AddRange(forContainer.Out());

                    ContinueSkip.ResetSkip();
                    return;
                }

                // While
                case WhileNode whileNode:
                {
                    ContinueSkip.Setup();

                    // The action the while loop starts on.
                    int whileStartIndex = ContinueSkip.GetSkipCount();

                    A_SkipIf skipCondition = new A_SkipIf() { ParameterValues = new IWorkshopTree[2] };
                    skipCondition.ParameterValues[0] = Element.Part<V_Not>(ParseExpression(scope, whileNode.Expression));
                    Actions.Add(skipCondition);

                    ScopeGroup whileGroup = scope.Child();

                    ParseBlock(whileGroup, whileNode.Block, false, returnVar);

                    // Take the while out of scope.
                    Actions.AddRange(whileGroup.Out());

                    ContinueSkip.SetSkipCount(whileStartIndex);
                    Actions.Add(Element.Part<A_Loop>());

                    skipCondition.ParameterValues[1] = new V_Number(GetSkipCount(skipCondition));

                    ContinueSkip.ResetSkip();
                    return;
                }

                // If
                case IfNode ifNode:
                {
                    A_SkipIf if_SkipIf = new A_SkipIf();
                    Actions.Add(if_SkipIf);

                    var ifScope = scope.Child();

                    // Parse the if body.
                    ParseBlock(ifScope, ifNode.IfData.Block, false, returnVar);

                    // Take the if out of scope.
                    Actions.AddRange(ifScope.Out());

                    // Determines if the "Skip" action after the if block will be created.
                    // Only if there is if-else or else statements.
                    bool addIfSkip = ifNode.ElseIfData.Length > 0 || ifNode.ElseBlock != null;

                    // Update the initial SkipIf's skip count now that we know the number of actions the if block has.
                    // Add one to the body length if a Skip action is going to be added.
                    if_SkipIf.ParameterValues = new IWorkshopTree[]
                    {
                        Element.Part<V_Not>(ParseExpression(scope, ifNode.IfData.Expression)),
                        new V_Number(Actions.Count - 1 - Actions.IndexOf(if_SkipIf) + (addIfSkip ? 1 : 0))
                    };

                    // Create the "Skip" action.
                    A_Skip if_Skip = new A_Skip();
                    if (addIfSkip)
                    {
                        Actions.Add(if_Skip);
                    }

                    // Parse else-ifs
                    A_Skip[] elseif_Skips = new A_Skip[ifNode.ElseIfData.Length]; // The ElseIf's skips
                    for (int i = 0; i < ifNode.ElseIfData.Length; i++)
                    {
                        // Create the SkipIf action for the else if.
                        A_SkipIf elseif_SkipIf = new A_SkipIf();
                        Actions.Add(elseif_SkipIf);

                        // Parse the else-if body.
                        var elseifScope = scope.Child();
                        ParseBlock(elseifScope, ifNode.ElseIfData[i].Block, false, returnVar);

                        // Take the else-if out of scope.
                        Actions.AddRange(elseifScope.Out());

                        // Determines if the "Skip" action after the else-if block will be created.
                        // Only if there is additional if-else or else statements.
                        bool addIfElseSkip = i < ifNode.ElseIfData.Length - 1 || ifNode.ElseBlock != null;

                        // Set the SkipIf's parameters.
                        elseif_SkipIf.ParameterValues = new IWorkshopTree[]
                        {
                            Element.Part<V_Not>(ParseExpression(scope, ifNode.ElseIfData[i].Expression)),
                            new V_Number(Actions.Count - 1 - Actions.IndexOf(elseif_SkipIf) + (addIfElseSkip ? 1 : 0))
                        };

                        // Create the "Skip" action for the else-if.
                        if (addIfElseSkip)
                        {
                            elseif_Skips[i] = new A_Skip();
                            Actions.Add(elseif_Skips[i]);
                        }
                    }

                    // Parse else body.
                    if (ifNode.ElseBlock != null)
                    {
                        var elseScope = scope.Child();
                        ParseBlock(elseScope, ifNode.ElseBlock, false, returnVar);

                        // Take the else out of scope.
                        Actions.AddRange(elseScope.Out());
                    }

                    // Replace dummy skip with real skip now that we know the length of the if, if-else, and else's bodies.
                    // Replace if's dummy.
                    if_Skip.ParameterValues = new IWorkshopTree[]
                    {
                        new V_Number(Actions.Count - 1 - Actions.IndexOf(if_Skip))
                    };

                    // Replace else-if's dummy.
                    for (int i = 0; i < elseif_Skips.Length; i++)
                        if (elseif_Skips[i] != null)
                        {
                            elseif_Skips[i].ParameterValues = new IWorkshopTree[]
                            {
                                new V_Number(Actions.Count - 1 - Actions.IndexOf(elseif_Skips[i]))
                            };
                        }

                    return;
                }
                
                // Return
                case ReturnNode returnNode:

                    if (returnNode.Value != null)
                    {
                        Element result = ParseExpression(scope, returnNode.Value);
                        if (returnVar != null)
                            Actions.AddRange(returnVar.SetVariable(result));
                    }

                    A_Skip returnSkip = new A_Skip();
                    Actions.Add(returnSkip);
                    ReturnSkips.Add(returnSkip);

                    return;
                
                // Define
                case DefineNode defineNode:
                    ParseDefine(scope, defineNode);
                    return;

                case ExpressionTreeNode expressionTree:
                    new ParseExpressionTree(this, scope, expressionTree);
                    return;
            }
        }

        void ParseVarset(ScopeGroup scope, VarSetNode varSetNode)
        {
            var varSetData = new ParseExpressionTree(this, scope, varSetNode.Variable);

            if (!(varSetData.ResultingVariable is IndexedVar))
                throw new SyntaxErrorException($"Variable '{varSetData.ResultingVariable.Name}' is readonly.", varSetNode.Range);

            IndexedVar variable = (IndexedVar)varSetData.ResultingVariable;
            
            Element value = null;
            if (varSetNode.Value != null)
                value = ParseExpression(scope, varSetNode.Value);

            Element initialVar = variable.GetVariable(varSetData.Target);

            Element[] index = new Element[varSetNode.Index?.Length ?? 0];
            for (int i = 0; i < index.Length; i++)
            {
                index[i] = ParseExpression(scope, varSetNode.Index[i]);
                initialVar = Element.Part<V_ValueInArray>(initialVar, index[index.Length - i - 1]);
            }

            switch (varSetNode.Operation)
            {
                case "+=":
                    value = Element.Part<V_Add>(initialVar, value);
                    break;

                case "-=":
                    value = Element.Part<V_Subtract>(initialVar, value);
                    break;

                case "*=":
                    value = Element.Part<V_Multiply>(initialVar, value);
                    break;

                case "/=":
                    value = Element.Part<V_Divide>(initialVar, value);
                    break;

                case "^=":
                    value = Element.Part<V_RaiseToPower>(initialVar, value);
                    break;

                case "%=":
                    value = Element.Part<V_Modulo>(initialVar, value);
                    break;
                
                case "++":
                    value = Element.Part<V_Add>(initialVar, new V_Number(1));
                    break;
                
                case "--":
                    value = Element.Part<V_Subtract>(initialVar, new V_Number(1));
                    break;
            }

            Actions.AddRange(variable.SetVariable(value, varSetData.Target, index));
        }

        void ParseDefine(ScopeGroup scope, DefineNode defineNode)
        {
            IndexedVar var;
            if (defineNode.UseVar == null)
                var = VarCollection.AssignVar(scope, defineNode.VariableName, IsGlobal, defineNode);
            else
                var = VarCollection.AssignVar(scope, defineNode.VariableName, IsGlobal, defineNode.UseVar.Variable, defineNode.UseVar.Index, defineNode);

            // Set the defined variable if the variable is defined like "define var = 1"
            Element[] inScopeActions = var.InScope(defineNode.Value != null ? ParseExpression(scope, defineNode.Value) : null);
            if (inScopeActions != null)
                Actions.AddRange(inScopeActions);
            
            if (defineNode.Type != null)
                var.Type = ParserData.GetDefinedType(defineNode.Type, defineNode.Range);
        }

        int GetSkipCount(Element skipElement)
        {
            return Actions.Count - Actions.IndexOf(skipElement) - 1;
        }
    
        class ParseExpressionTree
        {
            public Var ResultingVariable { get; private set; }
            public Element ResultingElement { get; private set; }
            public Element Target { get; private set; }
            
            public ParseExpressionTree(TranslateRule translator, ScopeGroup scope, Node root)
            {
                if (root is VariableNode)
                {
                    Var var = scope.GetVar(((VariableNode)root).Name, root.Range);
                    ResultingVariable = var;
                    ResultingElement = var.GetVariable();
                    return;
                }

                List<Node> nodes = flatten((ExpressionTreeNode)root);
                ScopeGroup currentScope = scope;

                Element nodeResult = null;
                for (int index = 0; index < nodes.Count; index++)
                {
                    // If the node is a variable node, get the value.
                    if (nodes[index] is VariableNode)
                    {
                        VariableNode variableNode = (VariableNode)nodes[index];
                        Var var = currentScope.GetVar(variableNode.Name, variableNode.Range);

                        // If this is the last node, parse it as an expression.
                        if (index == nodes.Count - 1)
                            ResultingVariable = var;

                        // Set the nodeResult.
                        nodeResult = var.GetVariable(Target);
                    }
                    // If not, parse the node as an expression.
                    else
                        nodeResult = translator.ParseExpression(currentScope, nodes[index]);

                    // SupportedType will equal null if the element is not a defined type.
                    if (nodeResult.SupportedType == null)
                    {
                        // If there is no supported type, assume the element or variable is containing a player.
                        // Reset the scope.
                        currentScope = scope;

                        // If this isn't the last node, set the target and reset the nodeResult.
                        if (index < nodes.Count - 1)
                        {
                            Target = nodeResult;
                            nodeResult = null;
                        }
                    }
                    else
                        // Set the target scope to the type.
                        currentScope = nodeResult.SupportedType.Type.GetRootScope(nodeResult.SupportedType, translator.ParserData);
                }
                ResultingElement = nodeResult;
            }

            static List<Node> flatten(ExpressionTreeNode root)
            {
                List<Node> nodes = new List<Node>();
                ExpressionTreeNode flatten = root;
                while (true)
                {
                    nodes.Add(flatten.Tree[0]);
                    if (flatten.Tree.Length == 2)
                    {
                        if (flatten.Tree[1] is ExpressionTreeNode)
                            flatten = (ExpressionTreeNode)flatten.Tree[1];
                        else
                        {
                            nodes.Add(flatten.Tree[1]);
                            break;
                        }
                    }
                }
                return nodes;
            }
        }
    }
}