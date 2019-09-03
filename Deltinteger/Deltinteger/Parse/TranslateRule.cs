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
        public readonly List<Element> Actions = new List<Element>();
        private readonly List<Condition> Conditions = new List<Condition>();
        public readonly bool IsGlobal;
        private readonly List<A_Skip> ReturnSkips = new List<A_Skip>(); // Return statements whos skip count needs to be filled out.
        public readonly ContinueSkip ContinueSkip; // Contains data about the wait/skip for continuing loops.
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
            
            if (ruleScope.RecursiveMethodStackPop().Length != 0) throw new Exception();

            // Fulfill remaining returns.
            FulfillReturns(0);

            Finish();
        }

        public TranslateRule(Rule rule, ScopeGroup root, ParsingData parserData)
        {
            VarCollection = parserData.VarCollection;
            ParserData = parserData;

            Rule = rule;
            IsGlobal = Rule.IsGlobal;

            ContinueSkip = new ContinueSkip(IsGlobal, Actions, VarCollection);
        }

        public void Finish()
        {
            Rule.Actions = Actions.ToArray();
            Rule.Conditions = Conditions.ToArray();
        }

        void FulfillReturns(int startAt)
        {
            for (int i = ReturnSkips.Count - 1; i >= startAt; i--)
            {
                int skipCount = Actions.Count - 1 - Actions.IndexOf(ReturnSkips[i]);
                
                if (skipCount == 0)
                {
                    throw new Exception("Zero length return skip");
                    //Actions.Remove(ReturnSkips[i]);
                }
                else
                {
                    ReturnSkips[i].ParameterValues = new IWorkshopTree[]
                    {
                        new V_Number(skipCount)
                    };
                }
                
                ReturnSkips.RemoveAt(i);
            }
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
                        throw SyntaxErrorException.InvalidMethodType(true, left.Name, expr.Location);

                    Element right = (Element)parsedIf.ParameterValues[2];
                    if (!right.ElementData.IsValue)
                        throw SyntaxErrorException.InvalidMethodType(true, right.Name, expr.Location);

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
                        throw SyntaxErrorException.InvalidMethodType(true, parsedIf.Name, expr.Location);

                    Conditions.Add(new Condition(
                        parsedIf, EnumData.GetEnumValue(Operators.Equal), new V_True()
                    ));
                }
            }
        }

        public Element ParseExpression(ScopeGroup scope, Node expression)
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


                        // BoolCompare: &&, ||
                        case "&&":
                            return Element.Part<V_And>(left, right);

                        case "||":
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
                    return V_String.ParseString(stringNode.Location, stringNode.Value, stringFormat);

                // Null
                case NullNode nullNode:
                    return new V_Null();

                // TODO check if groups need to be implemented here

                // Methods
                case MethodNode methodNode:
                    return ParseMethod(scope, methodNode, true);

                // Variable
                case VariableNode variableNode:

                    Element[] index = new Element[variableNode.Index.Length];
                    for (int i = 0; i < index.Length; i++)
                        index[i] = ParseExpression(scope, variableNode.Index[i]);

                    Var var = scope.GetVar(variableNode.Name, variableNode.Location);
                    if (!var.Gettable())
                        throw SyntaxErrorException.VariableIsReadonly(var.Name, variableNode.Location);

                    Element result = var.GetVariable();
                    for (int i = 0; i < index.Length; i++)
                        result = Element.Part<V_ValueInArray>(result, index[i]);

                    return result;

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
                    ?? throw SyntaxErrorException.EnumCantBeValue(enumNode.Type, enumNode.Location);

                // New object
                case CreateObjectNode createObjectNode:

                    DefinedType typeData = ParserData.GetDefinedType(createObjectNode.TypeName, createObjectNode.Location);

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
                        throw SyntaxErrorException.NotAConstructor(typeData.TypeKind, typeData.Name, createObjectNode.Parameters.Length,createObjectNode.Location);
                    
                    if (constructor != null)
                    {
                        ScopeGroup constructorScope = typeScope.Child();

                        IWorkshopTree[] parameters = ParseParameters(
                            constructorScope, 
                            constructor.Parameters, 
                            createObjectNode.Parameters, 
                            createObjectNode.TypeName, 
                            createObjectNode.Location
                        );

                        AssignParameterVariables(constructorScope, constructor.Parameters, parameters, createObjectNode);

                        ParseBlock(constructorScope, constructor.BlockNode, true, null);
                        constructorScope.Out();
                    }

                    return store.GetVariable();

                // Expression tree
                case ExpressionTreeNode expressionTree:
                    return new ParseExpressionTree(this, scope, expressionTree).ResultingElement;
                
                // This
                case ThisNode thisNode:
                    return scope.GetThis(thisNode.Location).GetVariable();
            }

            throw new Exception();
        }

        IWorkshopTree[] ParseParameters(ScopeGroup scope, ParameterBase[] parameters, Node[] values, string methodName, LanguageServer.Location methodRange)
        {
            // Syntax error if there are too many parameters.
            if (values.Length > parameters.Length)
                throw SyntaxErrorException.TooManyParameters(methodName, parameters.Length, values.Length, values[parameters.Length].Location);

            // Parse the parameters
            List<IWorkshopTree> parsedParameters = new List<IWorkshopTree>();
            for(int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] is Parameter || parameters[i] is TypeParameter || parameters[i] is EnumParameter || parameters[i] is ConstantParameter)
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
                        if (parameters[i] is Parameter || parameters[i] is TypeParameter)
                        {
                            // Parse the parameter
                            Element result = ParseExpression(scope, values[i]);
                            parsedParameters.Add(result);

                            if (parameters[i] is TypeParameter && result.SupportedType?.Type != ((TypeParameter)parameters[i]).Type)
                                throw SyntaxErrorException.InvalidValueType(((TypeParameter)parameters[i]).Type.Name, result.SupportedType?.Type.Name ?? "any", values[i].Location);
                        }
                        else if (parameters[i] is EnumParameter)
                        {
                            EnumData expectedType = ((EnumParameter)parameters[i]).EnumData;
                            // Parse the enum
                            if (values[i] is EnumNode)
                            {
                                EnumNode enumNode = (EnumNode)values[i];

                                if (enumNode.EnumMember.Enum != expectedType)
                                    throw SyntaxErrorException.IncorrectEnumType(expectedType.CodeName, enumNode.EnumMember.Enum.CodeName, values[i].Location);

                                parsedParameters.Add(
                                    (IWorkshopTree)EnumData.ToElement(enumNode.EnumMember)
                                    ?? (IWorkshopTree)enumNode.EnumMember
                                );
                            }
                            else if (values[i] is VariableNode)
                            {
                                Var var = scope.GetVar(((VariableNode)values[i]).Name, null);
                                
                                if (var is ElementReferenceVar && ((ElementReferenceVar)var).Reference is EnumMember)
                                {
                                    EnumMember member = (EnumMember)((ElementReferenceVar)var).Reference;
                                    if (member.Enum != expectedType)
                                        throw SyntaxErrorException.IncorrectEnumType(expectedType.CodeName, member.Enum.CodeName, values[i].Location);

                                    parsedParameters.Add(
                                        (IWorkshopTree)EnumData.ToElement(member)
                                        ?? (IWorkshopTree)member
                                    );
                                }
                                else
                                    throw SyntaxErrorException.ExpectedEnumGotValue(((EnumParameter)parameters[i]).EnumData.CodeName, values[i].Location);
                            }
                            else
                                throw SyntaxErrorException.ExpectedEnumGotValue(((EnumParameter)parameters[i]).EnumData.CodeName, values[i].Location);
                        }
                        else if (parameters[i] is ConstantParameter)
                        {
                            if (values[i] is IConstantSupport == false)
                                throw new SyntaxErrorException("Parameter must be a " + ((ConstantParameter)parameters[i]).Type.Name + " constant.", values[i].Location);
                            object value = ((IConstantSupport)values[i]).GetValue();

                            if (!((ConstantParameter)parameters[i]).IsValid(value))
                                throw new SyntaxErrorException("Parameter must be a " + ((ConstantParameter)parameters[i]).Type.Name + ".", values[i].Location);

                            parsedParameters.Add(new ConstantObject(value));
                        }
                    }
                }
                else if (parameters[i] is VarRefParameter)
                {
                    // A VarRef parameter is always required, there will never be a default to fallback on.
                    if (values.Length <= i)
                        throw SyntaxErrorException.MissingParameter(parameters[i].Name, methodName, methodRange);

                    var varData = new ParseExpressionTree(this, scope, values[i]);
                    
                    // A VarRef parameter must be a variable
                    if (varData.ResultingVariable == null)
                        throw SyntaxErrorException.ExpectedVariable(values[i].Location);
                    
                    parsedParameters.Add(new VarRef(varData.ResultingVariable, varData.VariableIndex, varData.Target));
                        
                }
                else throw new NotImplementedException();
            }
            return parsedParameters.ToArray();
        }

        Var[] AssignParameterVariables(ScopeGroup methodScope, ParameterBase[] parameters, IWorkshopTree[] values, Node methodNode)
        {
            Var[] parameterVars = new Var[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is Element)
                {
                    // Create a new variable using the parameter.
                    parameterVars[i] = VarCollection.AssignVar(methodScope, parameters[i].Name, IsGlobal, methodNode);
                    ((IndexedVar)parameterVars[i]).Type = ((Element)values[i]).SupportedType?.Type;
                    Actions.AddRange(((IndexedVar)parameterVars[i]).SetVariable((Element)values[i]));
                }
                else if (values[i] is EnumMember)
                {
                    parameterVars[i] = new ElementReferenceVar(parameters[i].Name, methodScope, methodNode, values[i]);
                }
                else throw new NotImplementedException();
            }
            return parameterVars;
        }

        Element ParseMethod(ScopeGroup scope, MethodNode methodNode, bool needsToBeValue)
        {
            methodNode.RelatedScopeGroup = scope;

            IMethod method = scope.GetMethod(methodNode.Name, methodNode.Location);
            
            // Parse the parameters
            IWorkshopTree[] parsedParameters = ParseParameters(scope, method.Parameters, methodNode.Parameters, methodNode.Name, methodNode.Location);

            Element result;
            if (method is ElementList)
            {
                ElementList elementData = (ElementList)method;
                Element element = elementData.GetObject();
                element.ParameterValues = parsedParameters.ToArray();

                if (element.ElementData.IsValue)
                    result = element;
                else
                {
                    Actions.Add(element);
                    result = null;
                }

                foreach (var usageDiagnostic in elementData.UsageDiagnostics)
                    ParserData.Diagnostics.AddDiagnostic(methodNode.Location.uri, usageDiagnostic.GetDiagnostic(methodNode.Location.range));
            }
            else if (method is CustomMethodData)
            {
                switch (((CustomMethodData)method).CustomMethodType)
                {
                    case CustomMethodType.Action:
                        if (needsToBeValue)
                            throw SyntaxErrorException.InvalidMethodType(true, methodNode.Name, methodNode.Location);
                        break;

                    case CustomMethodType.Value:
                        if (!needsToBeValue)
                            throw SyntaxErrorException.InvalidMethodType(false, methodNode.Name, methodNode.Location);
                        break;

                    //case CustomMethodType.MultiAction_Value:
                }

                var customMethodResult = ((CustomMethodData)method)
                    .GetObject(this, scope, parsedParameters.ToArray(), methodNode.Location, methodNode.Parameters.Select(p => p.Location).ToArray())
                    .Result();

                // Some custom methods have extra actions.
                if (customMethodResult.Elements != null)
                    Actions.AddRange(customMethodResult.Elements);

                result = customMethodResult.Result;
            }
            else if (method is UserMethod)
            {
                result = ParseUserMethod(scope, methodNode, (UserMethod)method, parsedParameters.ToArray());
                if (!needsToBeValue)
                    result = null;
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
                    throw SyntaxErrorException.RecursionNotAllowed(methodNode.Location);

                var methodScope = scope.Root().Child();

                // Add the parameter variables to the scope.
                AssignParameterVariables(methodScope, userMethod.Parameters, parameters, methodNode);

                // The variable that stores the return value.
                IndexedVar returns = VarCollection.AssignVar(scope, $"{methodNode.Name}: return temp value", IsGlobal, null);
                returns.Type = method.Type;

                // Add the method to the method stack
                MethodStackNoRecursive.Add(userMethod);

                userMethod.Block.RelatedScopeGroup = methodScope;

                // Parse the block of the method
                ParseBlock(methodScope, userMethod.Block, true, returns);

                // Take the method scope out of scope.
                methodScope.Out();

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
                        if (lastMethod.ParameterVars[i] is RecursiveVar)
                            Actions.AddRange
                            (
                                ((RecursiveVar)lastMethod.ParameterVars[i]).InScope((Element)parameters[i])
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
                    Var[] parameterVars = new Var[userMethod.Parameters.Length];
                    for (int i = 0; i < parameterVars.Length; i++)
                    {
                        if (parameters[i] is Element)
                        {
                            // Create a new variable using the parameter input.
                            parameterVars[i] = (RecursiveVar)VarCollection.AssignVar(methodScope, userMethod.Parameters[i].Name, IsGlobal, methodNode);
                            ((RecursiveVar)parameterVars[i]).Type = ((Element)parameters[i]).SupportedType?.Type;
                            Actions.AddRange
                            (
                                ((RecursiveVar)parameterVars[i]).InScope((Element)parameters[i])
                            );
                        }
                        else if (parameters[i] is EnumMember)
                        {
                            parameterVars[i] = new ElementReferenceVar(userMethod.Parameters[i].Name, methodScope, methodNode, parameters[i]);
                        }
                        else throw new NotImplementedException();
                    }

                    var returns = VarCollection.AssignVar(null, $"{methodNode.Name}: return temp value", IsGlobal, null);
                    returns.Type = method.Type;

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
                    Actions.AddRange(methodScope.RecursiveMethodStackPop());
                    methodScope.Out();

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
                FulfillReturns(returnSkipStart);
        }

        void ParseStatement(ScopeGroup scope, Node statement, IndexedVar returnVar)
        {
            switch (statement)
            {
                // Method
                case MethodNode methodNode:
                    Element method = ParseMethod(scope, methodNode, false);
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

                    IndexedVar index = VarCollection.AssignVar(scope, $"'{forEachNode.Variable.VariableName}' for index", IsGlobal, null);

                    int offset = 0;

                    Element getVariableReference()
                    {
                        return Element.Part<V_ValueInArray>(array, indexer());
                    }
                    Element indexer()
                    {
                        if (offset == 0)
                            return index.GetVariable();
                        else
                            return Element.Part<V_Add>(index.GetVariable(), getOffset());
                    }
                    V_Number getOffset()
                    {
                        return new V_Number(offset);
                    }

                    IndexedVar arrayVar = null;
                    ElementOrigin origin = ElementOrigin.GetElementOrigin(array);
                    if (origin == null && forEachNode.Variable.Type != null)
                        throw new SyntaxErrorException("Could not get the struct source.", forEachNode.Variable.Location);
                    if (origin != null)
                    {
                        arrayVar = origin.OriginVar(VarCollection, null, null);
                    }

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
                            //variable.Reference = getVariableReference();

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

                        Var variable;
                        if (arrayVar != null)
                        {
                            variable = arrayVar.CreateChild(tempChild, forEachNode.Variable.VariableName, new Element[]{indexer()}, forEachNode.Variable);
                            variable.Type = ParserData.GetDefinedType(forEachNode.Variable.Type, forEachNode.Variable.Location);
                        }
                        else
                            variable = new ElementReferenceVar(forEachNode.Variable.VariableName, tempChild, forEachNode, getVariableReference());

                        ParseBlock(tempChild, forEachNode.Block, false, returnVar);
                        tempChild.Out();
                    }
                    // Take the foreach out of scope.
                    forGroup.Out();

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
                    forGroup.Out();

                    ContinueSkip.SetSkipCount(forStartIndex);
                    Actions.Add(Element.Part<A_Loop>());
                    
                    // Set the skip
                    if (skipCondition != null)
                        skipCondition.ParameterValues[1] = new V_Number(GetSkipCount(skipCondition));
                    
                    // Take the defined variable in the for out of scope
                    forContainer.Out();

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
                    whileGroup.Out();

                    ContinueSkip.SetSkipCount(whileStartIndex);
                    Actions.Add(Element.Part<A_Loop>());

                    skipCondition.ParameterValues[1] = new V_Number(GetSkipCount(skipCondition));

                    ContinueSkip.ResetSkip();
                    return;
                }

                // If
                case IfNode ifNode:
                {
                    A_SkipIf if_SkipIf = new A_SkipIf() { ParameterValues = new IWorkshopTree[2] };
                    if_SkipIf.ParameterValues[0] = Element.Part<V_Not>(ParseExpression(scope, ifNode.IfData.Expression));

                    Actions.Add(if_SkipIf);

                    var ifScope = scope.Child();

                    // Parse the if body.
                    ParseBlock(ifScope, ifNode.IfData.Block, false, returnVar);

                    // Take the if out of scope.
                    ifScope.Out();

                    // Determines if the "Skip" action after the if block will be created.
                    // Only if there is if-else or else statements.
                    bool addIfSkip = ifNode.ElseIfData.Length > 0 || ifNode.ElseBlock != null;

                    // Create the "Skip" action.
                    A_Skip if_Skip = new A_Skip();
                    if (addIfSkip)
                    {
                        Actions.Add(if_Skip);
                    }

                    // Update the initial SkipIf's skip count now that we know the number of actions the if block has.
                    if_SkipIf.ParameterValues[1] = new V_Number(GetSkipCount(if_SkipIf));

                    // Parse else-ifs
                    A_Skip[] elseif_Skips = new A_Skip[ifNode.ElseIfData.Length]; // The ElseIf's skips
                    for (int i = 0; i < ifNode.ElseIfData.Length; i++)
                    {
                        // Create the SkipIf action for the else if.
                        A_SkipIf elseif_SkipIf = new A_SkipIf() { ParameterValues = new IWorkshopTree[2] };
                        elseif_SkipIf.ParameterValues[0] = Element.Part<V_Not>(ParseExpression(scope, ifNode.ElseIfData[i].Expression));

                        Actions.Add(elseif_SkipIf);

                        // Parse the else-if body.
                        var elseifScope = scope.Child();
                        ParseBlock(elseifScope, ifNode.ElseIfData[i].Block, false, returnVar);

                        // Take the else-if out of scope.
                        elseifScope.Out();

                        // Determines if the "Skip" action after the else-if block will be created.
                        // Only if there is additional if-else or else statements.
                        bool addIfElseSkip = i < ifNode.ElseIfData.Length - 1 || ifNode.ElseBlock != null;

                        // Create the "Skip" action for the else-if.
                        if (addIfElseSkip)
                        {
                            elseif_Skips[i] = new A_Skip();
                            Actions.Add(elseif_Skips[i]);
                        }

                        // Set the SkipIf's parameters.
                        elseif_SkipIf.ParameterValues[1] = new V_Number(GetSkipCount(elseif_SkipIf));
                    }

                    // Parse else body.
                    if (ifNode.ElseBlock != null)
                    {
                        var elseScope = scope.Child();
                        ParseBlock(elseScope, ifNode.ElseBlock, false, returnVar);

                        // Take the else out of scope.
                        elseScope.Out();
                    }

                    // Replace dummy skip with real skip now that we know the length of the if, if-else, and else's bodies.
                    // Replace if's dummy.
                    if (addIfSkip)
                        if_Skip.ParameterValues = new IWorkshopTree[]
                        {
                            new V_Number(GetSkipCount(if_Skip))
                        };

                    // Replace else-if's dummy.
                    for (int i = 0; i < elseif_Skips.Length; i++)
                        if (elseif_Skips[i] != null)
                        {
                            elseif_Skips[i].ParameterValues = new IWorkshopTree[]
                            {
                                new V_Number(GetSkipCount(elseif_Skips[i]))
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
                    Actions.Add(Element.Part<A_Skip>(new V_Number(-1)));
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
                throw SyntaxErrorException.VariableIsReadonly(varSetData.ResultingVariable.Name, varSetNode.Location);

            IndexedVar variable = (IndexedVar)varSetData.ResultingVariable;
            Element[] index = varSetData.VariableIndex;
            
            Element value = null;
            if (varSetNode.Value != null)
                value = ParseExpression(scope, varSetNode.Value);

            Element initialVar = variable.GetVariable(varSetData.Target);

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
                var.Type = ParserData.GetDefinedType(defineNode.Type, defineNode.Location);
        }

        int GetSkipCount(Element skipElement)
        {
            int index = Actions.IndexOf(skipElement);
            if (index == -1)
                throw new Exception("skipElement not found.");

            return Actions.Count - index - 1;
        }
    
        class ParseExpressionTree
        {
            public Var ResultingVariable { get; }
            public Element[] VariableIndex { get; }
            public Element ResultingElement { get; }
            public Element Target { get; }
            
            public ParseExpressionTree(TranslateRule translator, ScopeGroup scope, Node root)
            {
                if (root is VariableNode)
                {
                    VariableNode variableNode = (VariableNode)root;

                    Var var = scope.GetVar(((VariableNode)root).Name, root.Location);
                    ResultingVariable = var;

                    //if (!ResultingVariable.Gettable()) throw SyntaxErrorException.CantReadVariable(ResultingVariable.Name, root.Location);

                    if (ResultingVariable.Gettable())
                        ResultingElement = var.GetVariable();

                    VariableIndex = new Element[variableNode.Index.Length];
                    for (int i = 0; i < VariableIndex.Length; i++)
                        VariableIndex[i] = translator.ParseExpression(scope, variableNode.Index[i]);
                    
                    for (int i = 0; i < VariableIndex.Length; i++)
                    {
                        if (!ResultingVariable.Gettable()) throw SyntaxErrorException.CantReadVariable(ResultingVariable.Name, root.Location);
                        ResultingElement = Element.Part<V_ValueInArray>(ResultingElement, VariableIndex[i]);
                    }

                    return;
                }
                
                if (root is ExpressionTreeNode == false) throw new SyntaxErrorException("Error", root.Location);

                List<Node> nodes = flatten((ExpressionTreeNode)root);
                ScopeGroup currentScope = scope;

                Element nodeResult = null;
                for (int index = 0; index < nodes.Count; index++)
                {
                    // If the node is a variable node, get the value.
                    if (nodes[index] is VariableNode)
                    {
                        VariableNode variableNode = (VariableNode)nodes[index];
                        Var var = currentScope.GetVar(variableNode.Name, variableNode.Location);

                        // If this is the last node, set the resulting var.
                        if (index == nodes.Count - 1)
                            ResultingVariable = var;

                        // Get the variable index
                        Element[] varIndex = new Element[variableNode.Index.Length];
                        for (int i = 0; i < varIndex.Length; i++)
                            varIndex[i] = translator.ParseExpression(scope, variableNode.Index[i]);

                        // Set the nodeResult.
                        nodeResult = var.GetVariable(Target);

                        // Apply the index
                        for (int i = 0; i < varIndex.Length; i++)
                            nodeResult = Element.Part<V_ValueInArray>(nodeResult, varIndex[i]);
                    }
                    // If not, parse the node as an expression.
                    else
                        nodeResult = translator.ParseExpression(currentScope, nodes[index]);

                    // SupportedType will equal null if the element is not a defined type.
                    if (nodeResult.SupportedType == null)
                    {
                        // If there is no supported type, assume the element or variable is containing a player.
                        // Reset the scope.
                        //currentScope = scope;
                        currentScope = translator.ParserData.Root;

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