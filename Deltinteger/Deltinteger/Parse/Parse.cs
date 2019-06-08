using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Antlr4;
using Antlr4.Runtime;
using Deltin.Deltinteger;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class Parser
    {
        static Log Log = new Log("Parse");

        public static Rule[] ParseText(string document, out ParserElements parserData)
        {
            parserData = ParserElements.GetParser(document, null);

            // TODO fix usevar
            Var.Setup(parserData.RuleSetNode.UseGlobalVar, parserData.RuleSetNode.UsePlayerVar);

            // Get the defined variables.
            foreach (var definedVar in parserData.RuleSetNode.DefinedVars)
                // The new var is stored in Var.VarCollection
                new DefinedVar(ScopeGroup.Root, definedVar);

            // Get the user methods.
            for (int i = 0; i < parserData.RuleSetNode.UserMethods.Length; i++)
                new UserMethod(parserData.RuleSetNode.UserMethods[i]); 

            // Parse the rules.
            Rule[] rules = new Rule[parserData.RuleSetNode.Rules.Length];

            for (int i = 0; i < rules.Length; i++)
            {
                try
                {
                    Rule newRule = Translate.GetRule(parserData.RuleSetNode.Rules[i]);
                    Log.Write(LogLevel.Normal, $"Built rule: {newRule.Name}");
                    rules[i] = newRule;
                }
                catch (SyntaxErrorException ex)
                {
                    parserData.ErrorListener.Error(ex.Message, ex.Range);
                }
            }

            Log.Write(LogLevel.Normal, new ColorMod("Build succeeded.", ConsoleColor.Green));

            // List all variables
            Log.Write(LogLevel.Normal, new ColorMod("Variable Guide:", ConsoleColor.Blue));

            if (ScopeGroup.Root.VarCollection().Count > 0)
            {
                int nameLength = ScopeGroup.Root.VarCollection().Max(v => v.Name.Length);

                bool other = false;
                foreach (DefinedVar var in ScopeGroup.Root.VarCollection())
                {
                    ConsoleColor textcolor = other ? ConsoleColor.White : ConsoleColor.DarkGray;
                    other = !other;

                    Log.Write(LogLevel.Normal,
                        // Names
                        new ColorMod(var.Name + new string(' ', nameLength - var.Name.Length) + "  ", textcolor),
                        // Variable
                        new ColorMod(
                            (var.IsGlobal ? "global" : "player") 
                            + " " + 
                            var.Variable.ToString() +
                            (var.IsInArray ? $"[{var.Index}]" : "")
                            , textcolor)
                    );
                }
            }

            return rules;
        }
    }

    public class ParserElements
    {
        public static ParserElements GetParser(string document, Pos documentPos)
        {
            AntlrInputStream inputStream = new AntlrInputStream(document);

            // Lexer
            DeltinScriptLexer lexer = new DeltinScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);

            // Parse
            DeltinScriptParser parser = new DeltinScriptParser(commonTokenStream);
            var errorListener = new ErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            DeltinScriptParser.RulesetContext context = parser.ruleset();

            AdditionalErrorChecking aec = new AdditionalErrorChecking(parser, errorListener);
            aec.Visit(context);

            BuildAstVisitor bav = new BuildAstVisitor(documentPos);
            RulesetNode ruleSet = (RulesetNode)bav.Visit(context);
            
            return new ParserElements()
            {
                Parser = parser,
                RulesetContext = context,
                RuleSetNode = ruleSet,
                Bav = bav,
                ErrorListener = errorListener
            };
        }

        public DeltinScriptParser Parser { get; set; }
        public DeltinScriptParser.RulesetContext RulesetContext { get; set; }
        public RulesetNode RuleSetNode { get; set; }
        public ErrorListener ErrorListener { get; set; } 
        public BuildAstVisitor Bav { get; set; }
    }

    class Translate
    {
        public static Rule GetRule(RuleNode ruleNode)
        {
            return new Translate(ruleNode).Rule;
        }

        private readonly Rule Rule;
        private readonly List<Element> Actions = new List<Element>();
        private readonly List<Condition> Conditions = new List<Condition>();
        private readonly bool IsGlobal;
        private readonly List<A_Skip> ReturnSkips = new List<A_Skip>(); // Return statements whos skip count needs to be filled out.
        private ContinueSkip ContinueSkip; // Contains data about the wait/skip for continuing loops.

        private Translate(RuleNode ruleNode)
        {
            Rule = new Rule(ruleNode.Name);

            ParseConditions(ruleNode.Conditions);
            ParseBlock(ScopeGroup.Root.Child(), ruleNode.Block, false);

            Rule.Conditions = Conditions.ToArray();
            Rule.Actions = Actions.ToArray();

            // Fufill remaining skips
            foreach (var skip in ReturnSkips)
                skip.ParameterValues = new object[] { new V_Number(Actions.Count - ReturnSkips.IndexOf(skip)) };
            ReturnSkips.Clear();
        }

        void ParseConditions(IExpressionNode[] expressions)
        {
            foreach(var expr in expressions)
            {
                Element parsedIf = ParseExpression(ScopeGroup.Root, expr);
                // If the parsed if is a V_Compare, translate it to a condition.
                // Makes "(value1 == value2) == true" to just "value1 == value2"
                if (parsedIf is V_Compare)
                    Conditions.Add(
                        new Condition(
                            (Element)parsedIf.ParameterValues[0],
                            (Operators)parsedIf.ParameterValues[1],
                            (Element)parsedIf.ParameterValues[2]
                        )
                    );
                // If not, just do "parsedIf == true"
                else
                    Conditions.Add(new Condition(
                        parsedIf, Operators.Equal, new V_True()
                    ));
            }
        }

        Element ParseExpression(ScopeGroup scope, IExpressionNode expression)
        {
            switch (expression)
            {
                // Math and boolean operations.
                case OperationNode operationNode:
                {
                    Element left = ParseExpression(scope, operationNode.Left);
                    Element right = ParseExpression(scope, operationNode.Right);

                    if (Constants.BoolOperations.Contains(operationNode.Operation))
                    {
                        if (left.ElementData.ValueType != Elements.ValueType.Any && left.ElementData.ValueType != Elements.ValueType.Boolean)
                            throw new SyntaxErrorException($"Expected boolean datatype, got {left .ElementData.ValueType.ToString()} instead.", ((Node)operationNode.Left).Range);
                        if (right.ElementData.ValueType != Elements.ValueType.Any && right.ElementData.ValueType != Elements.ValueType.Boolean)
                            throw new SyntaxErrorException($"Expected boolean datatype, got {right.ElementData.ValueType.ToString()} instead.", ((Node)operationNode.Right).Range);
                    }

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
                            return Element.Part<V_Compare>(left, Operators.LessThan, right);

                        case "<=":
                            return Element.Part<V_Compare>(left, Operators.LessThanOrEqual, right);

                        case "==":
                            return Element.Part<V_Compare>(left, Operators.Equal, right);

                        case ">=":
                            return Element.Part<V_Compare>(left, Operators.GreaterThanOrEqual, right);

                        case ">":
                            return Element.Part<V_Compare>(left, Operators.GreaterThan, right);

                        case "!=":
                            return Element.Part<V_Compare>(left, Operators.NotEqual, right);
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
                #warning todo
                // TODO replace token with range
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
                        .GetVariable(ParseExpression(scope, variableNode.Target));

                // Get value in array
                case ValueInArrayNode viaNode:
                    return Element.Part<V_ValueInArray>(viaNode.Value, viaNode.Index);

                // Create array
                case CreateArrayNode createArrayNode:
                {
                    Element prev = null;
                    Element current = null;

                    for (int i = 0; i < createArrayNode.Values.Length; i++)
                    {
                        current = new V_Append()
                        {
                            ParameterValues = new object[2]
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

                // Seperator

            }

            throw new Exception();
        }

        Element ParseMethod(ScopeGroup scope, MethodNode methodNode, bool needsToBeValue)
        {
            // Get the kind of method the method is (Method (Overwatch), Custom Method, or User Method.)
            var methodType = GetMethodType(methodNode.Name);
            if (methodType == null)
                throw new SyntaxErrorException($"The method {methodNode.Name} does not exist.", methodNode.Range);

            Element method;
            switch (methodType)
            {
                case MethodType.Method:
                {
                    Type owMethod = Element.GetMethod(methodNode.Name);

                    method = (Element)Activator.CreateInstance(owMethod);
                    Parameter[] parameterData = owMethod.GetCustomAttributes<Parameter>().ToArray();
                    
                    List<object> parsedParameters = new List<object>();
                    for (int i = 0; i < parameterData.Length; i++)
                    {
                        if (methodNode.Parameters.Length > i)
                        {
                            // Parse the parameter.
                            parsedParameters.Add(ParseParameter(scope, methodNode.Parameters[i], methodNode.Name, parameterData[i]));
                        }
                        else 
                        {
                            if (parameterData[i].ParameterType == ParameterType.Value && parameterData[i].DefaultType == null)
                                throw new SyntaxErrorException($"Missing parameter \"{parameterData[i].Name}\" in the method \"{methodNode.Name}\" and no default type to fallback on.", 
                                    methodNode.Range);
                            else
                                parsedParameters.Add(parameterData[i].GetDefault());
                        }
                    }

                    method.ParameterValues = parsedParameters.ToArray();
                    break;
                }

                case MethodType.CustomMethod:
                {
                    MethodInfo customMethod = CustomMethods.GetCustomMethod(methodNode.Name);
                    Parameter[] parameterData = customMethod.GetCustomAttributes<Parameter>().ToArray();
                    object[] parsedParameters = new Element[parameterData.Length];

                    for (int i = 0; i < parameterData.Length; i++)
                        if (methodNode.Parameters.Length > i)
                            parsedParameters[i] = ParseParameter(scope, methodNode.Parameters[i], methodNode.Name, parameterData[i]);
                        else
                            throw new SyntaxErrorException($"Missing parameter \"{parameterData[i].Name}\" in the method \"{methodNode.Name}\" and no default type to fallback on.", 
                                methodNode.Range);

                    MethodResult result = (MethodResult)customMethod.Invoke(null, new object[] { IsGlobal, parsedParameters });
                    switch (result.MethodType)
                    {
                        case CustomMethodType.Action:
                            if (needsToBeValue)
                                throw new IncorrectElementTypeException(methodNode.Name, true);
                            break;

                        case CustomMethodType.MultiAction_Value:
                        case CustomMethodType.Value:
                            if (!needsToBeValue)
                                throw new IncorrectElementTypeException(methodNode.Name, false);
                            break;
                    }

                    // Some custom methods have extra actions.
                    if (result.Elements != null)
                        Actions.AddRange(result.Elements);
                    method = result.Result;

                    break;
                }

                case MethodType.UserMethod:
                {
                    using (var methodScope = ScopeGroup.Root.Child())
                    {
                        UserMethod userMethod = UserMethod.GetUserMethod(methodNode.Name);

                        // Add the parameter variables to the scope.
                        DefinedVar[] parameterVars = new DefinedVar[userMethod.Parameters.Length];
                        for (int i = 0; i < parameterVars.Length; i++)
                        {
                            if (methodNode.Parameters.Length > i)
                            {
                                // Create a new variable using the parameter input.
                                parameterVars[i] = DefinedVar.AssignDefinedVar(methodScope, IsGlobal, userMethod.Parameters[i].Name, methodNode.Range);
                                Actions.Add(parameterVars[i].SetVariable(ParseExpression(scope, methodNode.Parameters[i])));
                            }
                            else throw new SyntaxErrorException($"Missing parameter \"{userMethod.Parameters[i].Name}\" in the method \"{methodNode.Name}\".",
                                methodNode.Range);
                        }

                        method = ParseBlock(methodScope.Child(), userMethod.Block, true);
                        // No return value if the method is being used as an action.
                        if (!needsToBeValue)
                            method = null;
                        break;
                    }
                }

                default: throw new NotImplementedException(); // Keep the compiler from complaining about method not being set.
            }

            return method;
        }

        object ParseParameter(ScopeGroup scope, IExpressionNode node, string methodName, Parameter parameterData)
        {
            object value = null;

            switch (node)
            {
                case EnumNode enumNode:

                    if (parameterData.ParameterType != ParameterType.Enum)
                        throw new SyntaxErrorException($"Expected value type \"{parameterData.ValueType.ToString()}\" on {methodName}'s parameter \"{parameterData.Name}\"."
                            , enumNode.Range);

                    if (enumNode.Type != parameterData.EnumType.Name)
                        throw new SyntaxErrorException($"Expected enum type \"{parameterData.EnumType.ToString()}\" on {methodName}'s parameter \"{parameterData.Name}\"."
                            , enumNode.Range);

                    try
                    {
                        value = Enum.Parse(parameterData.EnumType, enumNode.Value);
                    }
                    catch (Exception ex) when (ex is ArgumentNullException || ex is ArgumentException || ex is OverflowException)
                    {
                        throw new SyntaxErrorException($"The value {enumNode.Value} does not exist in the enum {enumNode.Type}."
                            , enumNode.Range);
                    }
                    
                    break;

                default:

                    if (parameterData.ParameterType != ParameterType.Value)
                    throw new SyntaxErrorException($"Expected enum type \"{parameterData.EnumType.Name}\" on {methodName}'s parameter \"{parameterData.Name}\"."
                        , ((Node)node).Range);

                    value = ParseExpression(scope, node);

                    Element element = value as Element;
                    ElementData elementData = element.GetType().GetCustomAttribute<ElementData>();

                    if (elementData.ValueType != Elements.ValueType.Any &&
                        !parameterData.ValueType.HasFlag(elementData.ValueType))
                        throw new SyntaxErrorException($"Expected value type \"{parameterData.ValueType.ToString()}\" on {methodName}'s parameter \"{parameterData.Name}\", got \"{elementData.ValueType.ToString()}\" instead."
                            , ((Node)node).Range);

                    break;
            }

            if (value == null)
                throw new SyntaxErrorException("Could not parse parameter.", ((Node)node).Range);

            return value;
        }
    
        private static MethodType? GetMethodType(string name)
        {
            if (Element.GetMethod(name) != null)
                return MethodType.Method;
            if (CustomMethods.GetCustomMethod(name) != null)
                return MethodType.CustomMethod;
            if (UserMethod.GetUserMethod(name) != null)
                return MethodType.UserMethod;
            return null;
        }

        private enum MethodType
        {
            Method,
            CustomMethod,
            UserMethod
        }

        Element ParseBlock(ScopeGroup scopeGroup, BlockNode blockNode, bool fulfillReturns)
        {
            int returnSkipStart = ReturnSkips.Count;

            Var returned = null;
            if (fulfillReturns)
                returned = Var.AssignVar(IsGlobal);
            
            for (int i = 0; i < blockNode.Statements.Length; i++)
                ParseStatement(scopeGroup, blockNode.Statements[i], returned, i == blockNode.Statements.Length - 1);

            if (fulfillReturns)
            {
                for (int i = ReturnSkips.Count - 1; i >= returnSkipStart; i--)
                {
                    ReturnSkips[i].ParameterValues = new object[]
                    {
                        new V_Number(Actions.Count - 1 - Actions.IndexOf(ReturnSkips[i]))
                    };
                    ReturnSkips.RemoveAt(i);
                }
                return returned.GetVariable();
            }

            return null;
        }

        void ParseStatement(ScopeGroup scope, IStatementNode statement, Var returned, bool isLast)
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

                    DefinedVar variable = scope.GetVar(varSetNode.Variable, varSetNode.Range);
                    Element target = ParseExpression(scope, varSetNode.Target);
                    Element value = ParseExpression(scope, varSetNode.Value);
                    Element index = ParseExpression(scope, varSetNode.Index);

                    switch (varSetNode.Operation)
                    {
                        case "+=":
                            value = Element.Part<V_Add>(variable.GetVariable(target, index), value);
                            break;

                        case "-=":
                            value = Element.Part<V_Subtract>(variable.GetVariable(target, index), value);
                            break;

                        case "*=":
                            value = Element.Part<V_Multiply>(variable.GetVariable(target, index), value);
                            break;

                        case "/=":
                            value = Element.Part<V_Divide>(variable.GetVariable(target, index), value);
                            break;

                        case "^=":
                            value = Element.Part<V_RaiseToPower>(variable.GetVariable(target, index), value);
                            break;

                        case "%=":
                            value = Element.Part<V_Modulo>(variable.GetVariable(target, index), value);
                            break;
                    }

                    Actions.Add(variable.SetVariable(value, target, index));
                    return;

                // For
                case ForEachNode forEachNode:
                {
                    ContinueSkip.Setup();

                    // The action the for loop starts on.
                    int forActionStartIndex = Actions.Count() - 1;

                    ScopeGroup forGroup = scope.Child();

                    // Create the for's temporary variable.
                    DefinedVar forTempVar = Var.AssignDefinedVar(
                        scopeGroup: forGroup,
                        name      : forEachNode.Variable,
                        isGlobal  : IsGlobal,
                        range     : forEachNode.Range
                        );

                    // Reset the counter.
                    Actions.Add(forTempVar.SetVariable(new V_Number(0)));

                    // Parse the for's block.
                    ParseBlock(forGroup, forEachNode.Block, false);

                    // Take the variable out of scope.
                    forGroup.Out();

                    // Add the for's finishing elements
                    Actions.Add(forTempVar.SetVariable( // Indent the index by 1.
                        Element.Part<V_Add>
                        (
                            forTempVar.GetVariable(),
                            new V_Number(1)
                        )
                    ));

                    ContinueSkip.SetSkipCount(forActionStartIndex);

                    // The target array in the for statement.
                    Element forArrayElement = ParseExpression(scope, forEachNode.Array);

                    Actions.Add(Element.Part<A_LoopIf>( // Loop if the for condition is still true.
                        Element.Part<V_Compare>
                        (
                            forTempVar.GetVariable(),
                            Operators.LessThan,
                            Element.Part<V_CountOf>(forArrayElement)
                        )
                    ));

                    ContinueSkip.ResetSkip();
                    return;
                }

                // If
                case IfNode ifNode:
                {
                    A_SkipIf if_SkipIf = new A_SkipIf();
                    Actions.Add(if_SkipIf);

                    using (var ifScope = scope.Child())
                    {
                        // Parse the if body.
                        ParseBlock(ifScope, ifNode.IfData.Block, false);
                    }

                    // Determines if the "Skip" action after the if block will be created.
                    // Only if there is if-else or else statements.
                    bool addIfSkip = ifNode.ElseIfData.Length > 0 || ifNode.ElseBlock != null;

                    // Update the initial SkipIf's skip count now that we know the number of actions the if block has.
                    // Add one to the body length if a Skip action is going to be added.
                    if_SkipIf.ParameterValues = new object[]
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
                        using (var elseifScope = scope.Child())
                        {
                            ParseBlock(elseifScope, ifNode.ElseIfData[i].Block, false);
                        }

                        // Determines if the "Skip" action after the else-if block will be created.
                        // Only if there is additional if-else or else statements.
                        bool addIfElseSkip = i < ifNode.ElseIfData.Length - 1 || ifNode.ElseBlock != null;

                        // Set the SkipIf's parameters.
                        elseif_SkipIf.ParameterValues = new object[]
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
                        using (var elseScope = scope.Child())
                            ParseBlock(elseScope, ifNode.ElseBlock, false);

                    // Replace dummy skip with real skip now that we know the length of the if, if-else, and else's bodies.
                    // Replace if's dummy.
                    if_Skip.ParameterValues = new object[]
                    {
                        new V_Number(Actions.Count - 1 - Actions.IndexOf(if_Skip))
                    };

                    // Replace else-if's dummy.
                    for (int i = 0; i < elseif_Skips.Length; i++)
                    {
                        elseif_Skips[i].ParameterValues = new object[]
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
                        Actions.Add(returned.SetVariable(result));
                    }

                    if (!isLast)
                    {
                        A_Skip returnSkip = new A_Skip();
                        Actions.Add(returnSkip);
                        ReturnSkips.Add(returnSkip);
                    }

                    return;
                
                // Define
                case ScopedDefineNode defineNode:

                    var var = Var.AssignDefinedVar(scope, IsGlobal, defineNode.VariableName, defineNode.Range);

                    // Set the defined variable if the variable is defined like "define var = 1"
                    if (defineNode.Value != null)
                        Actions.Add(var.SetVariable(ParseExpression(scope, defineNode.Value)));

                    return;
            }
        }
    }
}
