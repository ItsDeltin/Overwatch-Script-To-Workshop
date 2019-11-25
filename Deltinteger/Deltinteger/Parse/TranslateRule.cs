using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

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
        public readonly List<MethodStack> MethodStackRecursive = new List<MethodStack>(); // The user method stack
        public readonly List<ICallable> MethodStackNotRecursive = new List<ICallable>();
        public readonly ParsingData ParserData;

        private TranslateRule(RuleNode ruleNode, ScopeGroup root, ParsingData parserData)
        {
            VarCollection = parserData.VarCollection;
            ParserData = parserData;

            Rule = new Rule(ruleNode.Name, ruleNode.Event, ruleNode.Team, ruleNode.Player);
            Rule.Disabled = ruleNode.Disabled;
            IsGlobal = Rule.IsGlobal;

            ContinueSkip = new ContinueSkip(IsGlobal, Actions, VarCollection);

            ParseConditions(root, ruleNode.Conditions);

            // Parse the block of the rule.
            ScopeGroup ruleScope = root.Child();
            ParseBlock(ruleScope, ruleScope, ruleNode.Block, false, null);
            
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
                Element parsedIf = ParseExpression(scope, scope, expr);
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

        public Element ParseExpression(ScopeGroup getter, ScopeGroup scope, Node expression)
        {
            switch (expression)
            {
                // Math and boolean operations.
                case OperationNode operationNode:
                {
                    Element left = ParseExpression(getter, scope, operationNode.Left);
                    Element right = ParseExpression(getter, scope, operationNode.Right);

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
                            return left * right;

                        case "%":
                            return left % right;

                        case "/":
                            return left / right;

                        case "+":
                            return left + right;

                        case "-":
                            return left - right;


                        // BoolCompare: &&, ||
                        case "&&":
                            return Element.Part<V_And>(left, right);

                        case "||":
                            return Element.Part<V_Or>(left, right);

                        // Compare: <, <=, ==, >=, >, !=
                        case "<":
                            return left < right;

                        case "<=":
                            return left <= right;

                        case "==":
                            return Element.Part<V_Compare>(left, EnumData.GetEnumValue(Operators.Equal), right);

                        case ">=":
                            return left >= right;

                        case ">":
                            return left > right;

                        case "!=":
                            return Element.Part<V_Compare>(left, EnumData.GetEnumValue(Operators.NotEqual), right);
                    }
                    
                    throw new Exception($"Operation {operationNode.Operation} not implemented.");
                }

                // Number
                case NumberNode numberNode:
                    return numberNode.Value;
                
                // Bool
                case BooleanNode boolNode:
                    if (boolNode.Value)
                        return new V_True();
                    else
                        return new V_False();
                
                // Not operation
                case NotNode notNode:
                    return !(ParseExpression(getter, scope, notNode.Value));
                
                case InvertNode invertNode:
                    return -ParseExpression(getter, scope, invertNode.Value);

                // Strings
                case StringNode stringNode:

                    Element[] stringFormat = new Element[stringNode.Format?.Length ?? 0];
                    for (int i = 0; i < stringFormat.Length; i++)
                        stringFormat[i] = ParseExpression(getter, scope, stringNode.Format[i]);
                    if (stringNode.Localized)
                        return V_String.ParseString(stringNode.Location, stringNode.Value, stringFormat);
                    else
                        return V_CustomString.ParseString(stringNode.Location, stringNode.Value, stringFormat);

                // Null
                case NullNode nullNode:
                    return new V_Null();

                // TODO check if groups need to be implemented here

                // Methods
                case MethodNode methodNode:
                    return ParseMethod(getter, scope, methodNode, true);

                // Variable
                case VariableNode variableNode:

                    Element[] index = new Element[variableNode.Index.Length];
                    for (int i = 0; i < index.Length; i++)
                        index[i] = ParseExpression(getter, scope, variableNode.Index[i]);

                    Var var = scope.GetVar(getter, variableNode.Name, variableNode.Location);
                    if (!var.Gettable())
                        throw SyntaxErrorException.VariableIsReadonly(var.Name, variableNode.Location);

                    Element result = var.GetVariable();
                    for (int i = 0; i < index.Length; i++)
                        result = Element.Part<V_ValueInArray>(result, index[i]);

                    return result;

                // Get value in array
                case ValueInArrayNode viaNode:
                    return Element.Part<V_ValueInArray>(ParseExpression(getter, scope, viaNode.Value), ParseExpression(getter, scope, viaNode.Index));

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

                        current.ParameterValues[1] = ParseExpression(getter, scope, createArrayNode.Values[i]);
                        prev = current;
                    }

                    return current ?? new V_EmptyArray();
                }

                // Ternary Conditional (a ? b : c)
                case TernaryConditionalNode ternaryNode:
                    return Element.TernaryConditional
                    (
                        ParseExpression(getter, scope, ternaryNode.Condition),
                        ParseExpression(getter, scope, ternaryNode.Consequent),
                        ParseExpression(getter, scope, ternaryNode.Alternative),
                        true
                    );

                // Enums
                case EnumNode enumNode:
                    return EnumData.ToElement(enumNode.EnumMember) 
                    ?? throw SyntaxErrorException.EnumCantBeValue(enumNode.Type, enumNode.Location);

                // New object
                case CreateObjectNode createObjectNode:
                    DefinedType typeData = ParserData.GetDefinedType(createObjectNode.TypeName, createObjectNode.Location);
                    return typeData.New(createObjectNode, getter, scope, this);

                // Expression tree
                case ExpressionTreeNode expressionTree:
                    return new ParseExpressionTree(this, getter, scope, expressionTree).ResultingElement;
                
                // This
                case ThisNode thisNode:
                    return getter.GetThis(thisNode.Location);
                
                // Type convert
                case TypeConvertNode typeConvertNode:
                    DefinedType type = ParserData.GetDefinedType(typeConvertNode.Type, typeConvertNode.Location);
                    Element element = ParseExpression(getter, scope, typeConvertNode.Expression);
                    type.GetSource(this, element, typeConvertNode.Location);
                    return element;
                
                case RootNode rootNode: throw new SyntaxErrorException("'root' cannot be used like an expression.", rootNode.Location);
            }

            throw new Exception();
        }

        public IWorkshopTree[] ParseParameters(ScopeGroup getter, ScopeGroup scope, ParameterBase[] parameters, Node[] values, string methodName, LanguageServer.Location methodRange)
        {
            // Syntax error if there are too many parameters.
            if (values.Length > parameters.Length)
                throw SyntaxErrorException.TooManyParameters(methodName, parameters.Length, values.Length, values[parameters.Length].Location);

            // Parse the parameters
            List<IWorkshopTree> parsedParameters = new List<IWorkshopTree>();
            for(int i = 0; i < parameters.Length; i++)
            {
                // Get the default parameter value if there are not enough parameters.
                if (values.Length <= i)
                {                    
                    parsedParameters.Add(GetDefaultValue(parameters[i], methodName, methodRange));
                }
                else
                {
                    parsedParameters.Add(parameters[i].Parse(this, getter, scope, values[i]));
                }
            }
            return parsedParameters.ToArray();
        }

        public IWorkshopTree[] ParsePickyParameters(ScopeGroup getter, ScopeGroup scope, ParameterBase[] parameters, PickyParameter[] values, string methodName, LanguageServer.Location methodRange)
        {
            for (int f = 0; f < values.Length; f++)
            {
                // Syntax error if the parameter does not exist.
                if (!parameters.Any(param => param.Name.Replace(" ", "") == values[f].Name))
                    throw new SyntaxErrorException(values[f].Name + " is not a parameter in the method " + methodName + ".", values[f].Location);

                // Check if there are any duplicates.
                for (int n = 0; n < values.Length; n++)
                if (f != n && values[f].Name == values[n].Name)
                {
                    int use = Math.Max(f, n);
                    throw new SyntaxErrorException(values[use].Name + " was already set.", values[use].Location);
                }
            }

            // Parse the parameters
            List<IWorkshopTree> parsedParameters = new List<IWorkshopTree>();
            for(int i = 0; i < parameters.Length; i++)
            {
                PickyParameter setter = values.FirstOrDefault(value => value.Name == parameters[i].Name.Replace(" ", ""));

                IWorkshopTree result;

                if (setter == null)
                    result = GetDefaultValue(parameters[i], methodName, methodRange);
                else
                    result = parameters[i].Parse(this, getter, scope, setter.Expression);

                parsedParameters.Add(result);
            }

            return parsedParameters.ToArray();
        }

        private IWorkshopTree GetDefaultValue(ParameterBase parameter, string methodName, Location methodRange)
        {
            IWorkshopTree defaultValue = parameter.GetDefault();

            // If there is no default value, throw a syntax error.
            if (defaultValue == null)
                throw SyntaxErrorException.MissingParameter(parameter.Name, methodName, methodRange);
            
            return defaultValue;
        }

        public Var[] AssignParameterVariables(ScopeGroup methodScope, ParameterBase[] parameters, IWorkshopTree[] values, Node methodNode)
        {
            Var[] parameterVars = new Var[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is Element)
                {
                    // Create a new variable using the parameter.
                    if (!parameters[i].Extended)
                        parameterVars[i] = IndexedVar.AssignVar   (VarCollection, methodScope, parameters[i].Name, IsGlobal, methodNode);
                    else
                        parameterVars[i] = IndexedVar.AssignVarExt(VarCollection, methodScope, parameters[i].Name, IsGlobal, methodNode);

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

        Element ParseMethod(ScopeGroup getter, ScopeGroup scope, MethodNode methodNode, bool needsToBeValue)
        {
            methodNode.RelatedScopeGroup = scope;

            IMethod method = scope.GetMethod(getter, methodNode.Name, methodNode.Location);
            methodNode.Method = method;
            
            // Parse the parameters
            IWorkshopTree[] parsedParameters;
            // Normal
            if (methodNode.Parameters != null)
                parsedParameters = ParseParameters(getter, getter, method.Parameters, methodNode.Parameters, methodNode.Name, methodNode.Location);
            // Picky parameters
            else if (methodNode.PickyParameters != null)
                parsedParameters = ParsePickyParameters(getter, getter, method.Parameters, methodNode.PickyParameters, methodNode.Name, methodNode.Location);
            else throw new Exception();

            return method.Parse(this, needsToBeValue, scope, methodNode, parsedParameters);
        }

        public void ParseBlock(ScopeGroup getter, ScopeGroup scopeGroup, BlockNode blockNode, bool fulfillReturns, IndexedVar returnVar)
        {
            if (scopeGroup == null)
                throw new ArgumentNullException(nameof(scopeGroup));

            blockNode.RelatedScopeGroup = scopeGroup;

            int returnSkipStart = ReturnSkips.Count;
            
            for (int i = 0; i < blockNode.Statements.Length; i++)
                ParseStatement(getter, scopeGroup, blockNode.Statements[i], returnVar);

            if (fulfillReturns)
                FulfillReturns(returnSkipStart);
        }

        void ParseStatement(ScopeGroup getter, ScopeGroup scope, Node statement, IndexedVar returnVar)
        {
            switch (statement)
            {
                // Method
                case MethodNode methodNode:
                    Element method = ParseMethod(getter, scope, methodNode, false);
                    return;
                
                // Variable set
                case VarSetNode varSetNode:
                    ParseVarset(getter, scope, varSetNode);
                    return;

                // Foreach
                case ForEachNode forEachNode:
                {
                    ContinueSkip.Setup();

                    ScopeGroup forGroup = scope.Child();

                    Element array = ParseExpression(getter, scope, forEachNode.Array);

                    IndexedVar index = IndexedVar.AssignInternalVarExt(VarCollection, scope, $"'{forEachNode.Variable.VariableName}' for index", IsGlobal);

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
                            return index.GetVariable() + getOffset();
                    }
                    V_Number getOffset()
                    {
                        return new V_Number(offset);
                    }

                    IndexedVar arrayVar = null;
                    ElementOrigin origin = ElementOrigin.GetElementOrigin(array);
                    if (origin == null && forEachNode.Variable.Type != null)
                        throw new SyntaxErrorException("Could not get the type source.", forEachNode.Variable.Location);
                    if (origin != null)
                    {
                        arrayVar = origin.OriginVar(VarCollection, null, null);
                    }

                    // Reset the counter.
                    Actions.AddRange(index.SetVariable(0));

                    // The action the for loop starts on.
                    int forStartIndex = ContinueSkip.GetSkipCount();

                    A_SkipIf skipCondition = new A_SkipIf() { ParameterValues = new IWorkshopTree[2] };
                    skipCondition.ParameterValues[0] = 
                        !(index.GetVariable() < Element.Part<V_CountOf>(array)
                    );
                    Actions.Add(skipCondition);

                    List<A_SkipIf> rangeSkips = new List<A_SkipIf>();
                    for (; offset < forEachNode.Repeaters; offset++)
                    {
                        if (offset > 0)
                        {
                            //variable.Reference = getVariableReference();

                            A_SkipIf skipper = new A_SkipIf() { ParameterValues = new Element[2] };
                            skipper.ParameterValues[0] = !(
                                    index.GetVariable() + getOffset() < Element.Part<V_CountOf>(array)
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
                            if (forEachNode.Variable.Type != null)
                                variable.Type = ParserData.GetDefinedType(forEachNode.Variable.Type, forEachNode.Variable.Location);
                        }
                        else
                            variable = new ElementReferenceVar(forEachNode.Variable.VariableName, tempChild, forEachNode, getVariableReference());

                        ParseBlock(tempChild, tempChild, forEachNode.Block, false, returnVar);
                        tempChild.Out(this);
                    }
                    // Take the foreach out of scope.
                    forGroup.Out(this);

                    // Increment the index
                    Actions.AddRange(index.SetVariable(index.GetVariable() + forEachNode.Repeaters));

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
                        ParseVarset(getter, scope, forNode.VarSetNode);
                    if (forNode.DefineNode != null)
                        ParseDefine(getter, forContainer, forNode.DefineNode);
                    
                    ScopeGroup forGroup = forContainer.Child();

                    // The action the for loop starts on.
                    int forStartIndex = ContinueSkip.GetSkipCount();

                    A_SkipIf skipCondition = null;
                    // Skip if the condition is false.
                    if (forNode.Expression != null) // If it has an expression
                    {
                        skipCondition = new A_SkipIf() { ParameterValues = new IWorkshopTree[2] };
                        skipCondition.ParameterValues[0] = !(ParseExpression(forGroup, forGroup, forNode.Expression));
                        Actions.Add(skipCondition);
                    }

                    // Parse the for's block.
                    ParseBlock(forGroup, forGroup, forNode.Block, false, returnVar);

                    // Parse the statement
                    if (forNode.Statement != null)
                        ParseVarset(forGroup, forGroup, forNode.Statement);
                    
                    // Take the for out of scope.
                    forGroup.Out(this);

                    ContinueSkip.SetSkipCount(forStartIndex);
                    Actions.Add(Element.Part<A_Loop>());
                    
                    // Set the skip
                    if (skipCondition != null)
                        skipCondition.ParameterValues[1] = new V_Number(GetSkipCount(skipCondition));
                    
                    // Take the defined variable in the for out of scope
                    forContainer.Out(this);

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
                    skipCondition.ParameterValues[0] = !(ParseExpression(getter, scope, whileNode.Expression));
                    Actions.Add(skipCondition);

                    ScopeGroup whileGroup = scope.Child();

                    ParseBlock(whileGroup, whileGroup, whileNode.Block, false, returnVar);

                    // Take the while out of scope.
                    whileGroup.Out(this);

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
                    if_SkipIf.ParameterValues[0] = !(ParseExpression(getter, scope, ifNode.IfData.Expression));

                    Actions.Add(if_SkipIf);

                    var ifScope = scope.Child();

                    // Parse the if body.
                    ParseBlock(ifScope, ifScope, ifNode.IfData.Block, false, returnVar);

                    // Take the if out of scope.
                    ifScope.Out(this);

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
                        elseif_SkipIf.ParameterValues[0] = !(ParseExpression(getter, scope, ifNode.ElseIfData[i].Expression));

                        Actions.Add(elseif_SkipIf);

                        // Parse the else-if body.
                        var elseifScope = scope.Child();
                        ParseBlock(elseifScope, elseifScope, ifNode.ElseIfData[i].Block, false, returnVar);

                        // Take the else-if out of scope.
                        elseifScope.Out(this);

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
                        ParseBlock(elseScope, elseScope, ifNode.ElseBlock, false, returnVar);

                        // Take the else out of scope.
                        elseScope.Out(this);
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
                        Element result = ParseExpression(getter, scope, returnNode.Value);
                        if (returnVar != null)
                            Actions.AddRange(returnVar.SetVariable(result));
                    }

                    A_Skip returnSkip = new A_Skip();
                    Actions.Add(returnSkip);
                    Actions.Add(Element.Part<A_Skip>(new V_Number(-1)));
                    ReturnSkips.Add(returnSkip);
                    return;

                case DeleteNode deleteNode:
                    DefinedClass.Delete(ParseExpression(getter, scope, deleteNode.Delete), this);
                    return;
                
                // Define
                case DefineNode defineNode:
                    ParseDefine(getter, scope, defineNode);
                    return;

                case ExpressionTreeNode expressionTree:
                    new ParseExpressionTree(this, getter, scope, expressionTree);
                    return;
                
                default:
                    throw new SyntaxErrorException("Expected statement.", statement.Location);
            }
        }

        public static void CheckMethodType(bool needsToBeValue, CustomMethodType type, string methodName, Location location)
        {
            if (type == CustomMethodType.Action)
            {
                if (needsToBeValue)
                    throw SyntaxErrorException.InvalidMethodType(true, methodName, location);
            }
            else if (type == CustomMethodType.Value)
            {
                if (!needsToBeValue)
                        throw SyntaxErrorException.InvalidMethodType(false, methodName, location);
            }
        }

        void ParseVarset(ScopeGroup getter, ScopeGroup scope, VarSetNode varSetNode)
        {
            var varSetData = new ParseExpressionTree(this, getter, scope, varSetNode.Variable);

            if (!(varSetData.ResultingVariable is IndexedVar))
                throw SyntaxErrorException.VariableIsReadonly(varSetData.ResultingVariable.Name, varSetNode.Location);

            IndexedVar variable = (IndexedVar)varSetData.ResultingVariable;
            Element[] index = varSetData.VariableIndex;
            
            Element value = null;
            if (varSetNode.Value != null)
                value = ParseExpression(getter, scope, varSetNode.Value);

            Element initialVar = variable.GetVariable(varSetData.Target);

            Operation? operation = null;

            switch (varSetNode.Operation)
            {
                case "+=":
                    operation = Operation.Add;
                    break;

                case "-=":
                    operation = Operation.Subtract;
                    break;

                case "*=":
                    operation = Operation.Multiply;
                    break;

                case "/=":
                    operation = Operation.Divide;
                    break;

                case "^=":
                    operation = Operation.RaiseToPower;
                    break;

                case "%=":
                    operation = Operation.Modulo;
                    break;
                
                case "++":
                    operation = Operation.Add;
                    value = 1;
                    break;
                
                case "--":
                    operation = Operation.Subtract;
                    value = 1;
                    break;
            }

            if (operation == null)
                Actions.AddRange(variable.SetVariable(value, varSetData.Target, index));
            else
                Actions.AddRange(variable.ModifyVariable((Operation)operation, value, varSetData.Target, index));
        }

        void ParseDefine(ScopeGroup getter, ScopeGroup scope, DefineNode defineNode)
        {
            IndexedVar var;
            if (!defineNode.Extended)
                var = IndexedVar.AssignVar   (VarCollection, scope, defineNode.VariableName, IsGlobal, defineNode);
            else
                var = IndexedVar.AssignVarExt(VarCollection, scope, defineNode.VariableName, IsGlobal, defineNode);

            // Set the defined variable if the variable is defined like "define var = 1"
            Element[] inScopeActions = var.InScope(defineNode.Value != null ? ParseExpression(getter, scope, defineNode.Value) : null);
            if (inScopeActions != null)
                Actions.AddRange(inScopeActions);
            
            if (defineNode.Type != null)
                var.Type = ParserData.GetDefinedType(defineNode.Type, defineNode.Location);
        }

        public int GetSkipCount(Element skipElement)
        {
            int index = Actions.IndexOf(skipElement);
            if (index == -1)
                throw new Exception("skipElement not found.");

            return Actions.Count - index - 1;
        }
        public static int GetSkipCount(List<Element> actions, Element skipElement)
        {
            int index = actions.IndexOf(skipElement);
            if (index == -1)
                throw new Exception("skipElement not found.");

            return actions.Count - index - 1;
        }
    
        public class ParseExpressionTree
        {
            public Var ResultingVariable { get; }
            public Element[] VariableIndex { get; }
            public Element ResultingElement { get; }
            public Element Target { get; }
            
            public ParseExpressionTree(TranslateRule translator, ScopeGroup getter, ScopeGroup scope, Node root)
            {
                if (root is VariableNode)
                {
                    VariableNode variableNode = (VariableNode)root;

                    Var var = scope.GetVar(getter, ((VariableNode)root).Name, root.Location);
                    ResultingVariable = var;

                    //if (!ResultingVariable.Gettable()) throw SyntaxErrorException.CantReadVariable(ResultingVariable.Name, root.Location);

                    if (ResultingVariable.Gettable())
                        ResultingElement = var.GetVariable();

                    VariableIndex = new Element[variableNode.Index.Length];
                    for (int i = 0; i < VariableIndex.Length; i++)
                        VariableIndex[i] = translator.ParseExpression(getter, scope, variableNode.Index[i]);
                    
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
                    if (nodes[index] is RootNode)
                    {
                        currentScope = translator.ParserData.Root;
                        nodeResult = new V_Null();
                    }
                    // If the node is a variable node, get the value.
                    else if (nodes[index] is VariableNode)
                    {
                        VariableNode variableNode = (VariableNode)nodes[index];
                        Var var = currentScope.GetVar(getter, variableNode.Name, variableNode.Location);

                        // If this is the last node, set the resulting var.
                        if (index == nodes.Count - 1)
                            ResultingVariable = var;

                        // Get the variable index
                        VariableIndex = new Element[variableNode.Index.Length];
                        for (int i = 0; i < VariableIndex.Length; i++)
                            VariableIndex[i] = translator.ParseExpression(getter, scope, variableNode.Index[i]);

                        // Set the nodeResult.
                        nodeResult = var.GetVariable(Target);

                        // Apply the index
                        for (int i = 0; i < VariableIndex.Length; i++)
                            nodeResult = Element.Part<V_ValueInArray>(nodeResult, VariableIndex[i]);
                    }
                    // If not, parse the node as an expression.
                    else
                        nodeResult = translator.ParseExpression(getter, currentScope, nodes[index]);

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
                        currentScope = nodeResult.SupportedType.Type.GetRootScope(nodeResult, nodeResult.SupportedType, translator.ParserData, Target);
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