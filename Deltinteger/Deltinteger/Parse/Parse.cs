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
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class ParserData
    {
        public static ParserData GetParser(string document, Pos documentPos)
        {
            ParserData parserData = new ParserData();

            AntlrInputStream inputStream = new AntlrInputStream(document);

            // Lexer
            DeltinScriptLexer lexer = new DeltinScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);

            // Parse
            DeltinScriptParser parser = new DeltinScriptParser(commonTokenStream);
            var errorListener = new ErrorListener();
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);

            DeltinScriptParser.RulesetContext ruleSetContext = parser.ruleset();

            Log log = new Log("Parse");
            log.Write(LogLevel.Verbose, ruleSetContext.ToStringTree(parser));

            parserData.Diagnostics = new List<Diagnostic>();
            parserData.Diagnostics.AddRange(errorListener.Errors);

            // Get the ruleset node.
            RulesetNode ruleSetNode = null;
            if (parserData.Diagnostics.Count == 0)
            {
                parserData.Bav = new BuildAstVisitor(documentPos, parserData.Diagnostics);
                ruleSetNode = (RulesetNode)parserData.Bav.Visit(ruleSetContext);
            }

            AdditionalErrorChecking aec = new AdditionalErrorChecking(parser, parserData.Diagnostics);
            aec.Visit(ruleSetContext);

            if (parserData.Diagnostics.Count == 0)
            {
                parserData.VarCollection = new VarCollection();
                ScopeGroup root = new ScopeGroup();
                parserData.UserMethods = new List<UserMethod>();

                // Get the variables
                foreach (var definedVar in ruleSetNode.DefinedVars)
                    if (definedVar.UseVar == null)
                        parserData.VarCollection.AssignDefinedVar(root, definedVar.IsGlobal, definedVar.VariableName, definedVar.Range);
                    else
                        //new Var(root, definedVar.VariableName, definedVar.IsGlobal, definedVar.UseVar.Variable, definedVar.UseVar.Index, definedVar.Range, parserData.VarCollection);
                        parserData.VarCollection.AssignDefinedVar(root, definedVar.IsGlobal, definedVar.VariableName, definedVar.UseVar.Variable, definedVar.UseVar.Index, definedVar.Range);

                // Get the user methods.
                for (int i = 0; i < ruleSetNode.UserMethods.Length; i++)
                    parserData.UserMethods.Add(new UserMethod(ruleSetNode.UserMethods[i]));

                // Parse the rules.
                parserData.Rules = new List<Rule>();

                // The looper rule
                parserData.GlobalLoop = new Looper(true);
                parserData.PlayerLoop = new Looper(false);

                for (int i = 0; i < ruleSetNode.Rules.Length; i++)
                {
                    try
                    {
                        var result = Translate.GetRule(ruleSetNode.Rules[i], root, parserData);
                        parserData.Rules.Add(result.Rule);
                        parserData.Diagnostics.AddRange(result.Diagnostics);
                    }
                    catch (SyntaxErrorException ex)
                    {
                        parserData.Diagnostics.Add(new Diagnostic(ex.Message, ex.Range) { severity = Diagnostic.Error });
                    }
                }

                if (parserData.GlobalLoop.Used)
                    parserData.Rules.Add(parserData.GlobalLoop.Finalize());
                if (parserData.PlayerLoop.Used)
                    parserData.Rules.Add(parserData.PlayerLoop.Finalize());

                parserData.Success = true;
            }
            
            return parserData;
        }

        public List<Diagnostic> Diagnostics;
        public BuildAstVisitor Bav { get; private set; }
        public List<Rule> Rules { get; private set; }
        public List<UserMethod> UserMethods { get; private set; }
        public bool Success { get; private set; }
        public VarCollection VarCollection { get; private set; }
        private Looper GlobalLoop { get; set; }
        private Looper PlayerLoop { get; set; }

        public IMethod GetMethod(string name)
        {
            return (IMethod)UserMethods?.FirstOrDefault(um => um.Name == name) 
            ?? (IMethod)CustomMethodData.GetCustomMethod(name) 
            ?? (IMethod)Element.GetElement(name);
        }

        public Looper GetLooper(bool isGlobal)
        {
            return isGlobal? GlobalLoop : PlayerLoop;
        }
    }

    public class Translate
    {
        public static bool AllowRecursion = false;

        public static TranslateResult GetRule(RuleNode ruleNode, ScopeGroup root, ParserData parserData)
        {
            var result = new Translate(ruleNode, root, parserData);
            return new TranslateResult(result.Rule, result.Diagnostics.ToArray());
        }

        private readonly ScopeGroup Root;
        public readonly VarCollection VarCollection;
        private readonly UserMethod[] UserMethods;
        private readonly Rule Rule;
        private readonly List<Element> Actions = new List<Element>();
        private readonly List<Condition> Conditions = new List<Condition>();
        public readonly bool IsGlobal;
        private readonly List<A_Skip> ReturnSkips = new List<A_Skip>(); // Return statements whos skip count needs to be filled out.
        private readonly ContinueSkip ContinueSkip; // Contains data about the wait/skip for continuing loops.
        private readonly List<Diagnostic> Diagnostics = new List<Diagnostic>();
        private readonly List<MethodStack> MethodStack = new List<MethodStack>(); // The user method stack
        private readonly List<UserMethod> MethodStackNoRecursive = new List<UserMethod>();
        public readonly ParserData ParserData;

        private Translate(RuleNode ruleNode, ScopeGroup root, ParserData parserData)
        {
            Root = root;
            VarCollection = parserData.VarCollection;
            UserMethods = parserData.UserMethods.ToArray();
            ParserData = parserData;

            Rule = new Rule(ruleNode.Name, ruleNode.Event, ruleNode.Team, ruleNode.Player);
            IsGlobal = Rule.IsGlobal;

            ContinueSkip = new ContinueSkip(IsGlobal, Actions, VarCollection);

            ParseConditions(ruleNode.Conditions);
            ParseBlock(root.Child(), ruleNode.Block, false, null);

            Rule.Actions = Actions.ToArray();
            Rule.Conditions = Conditions.ToArray();

            // Fufill remaining skips
            foreach (var skip in ReturnSkips)
                skip.ParameterValues = new IWorkshopTree[] { new V_Number(Actions.Count - ReturnSkips.IndexOf(skip)) };
            ReturnSkips.Clear();
        }

        void ParseConditions(IExpressionNode[] expressions)
        {
            foreach(var expr in expressions)
            {
                Element parsedIf = ParseExpression(Root, expr);
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

        Element ParseExpression(ScopeGroup scope, IExpressionNode expression)
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

                case EnumNode enumNode:
                    return EnumData.ToElement(enumNode.EnumMember) 
                    ?? throw SyntaxErrorException.EnumCantBeValue(enumNode.Type, enumNode.Range);

                // Seperator

            }

            throw new Exception();
        }

        Element ParseMethod(ScopeGroup scope, MethodNode methodNode, bool needsToBeValue)
        {
            methodNode.RelatedScopeGroup = scope;

            IMethod method = ParserData.GetMethod(methodNode.Name);

            // Syntax error if the method does not exist.
            if (method == null)
                throw SyntaxErrorException.NonexistentMethod(methodNode.Name, methodNode.Range);

            // Syntax error if there are too many parameters.
            if (methodNode.Parameters.Length > method.Parameters.Length)
                throw SyntaxErrorException.TooManyParameters(method.Name, method.Parameters.Length, methodNode.Parameters.Length, ((Node)methodNode.Parameters[method.Parameters.Length]).Range);
            
            // Parse the parameters
            List<IWorkshopTree> parsedParameters = new List<IWorkshopTree>();
            for(int i = 0; i < method.Parameters.Length; i++)
            {
                if (method.Parameters[i] is Parameter || method.Parameters[i] is EnumParameter)
                {
                    // Get the default parameter value if there are not enough parameters.
                    if (methodNode.Parameters.Length <= i)
                    {
                        IWorkshopTree defaultValue = method.Parameters[i].GetDefault();

                        // If there is no default value, throw a syntax error.
                        if (defaultValue == null)
                            throw SyntaxErrorException.MissingParameter(method.Parameters[i].Name, method.Name, methodNode.Range);
                        
                        parsedParameters.Add(defaultValue);
                    }
                    else
                    {
                        if (method.Parameters[i] is Parameter)
                            // Parse the parameter
                            parsedParameters.Add(ParseExpression(scope, methodNode.Parameters[i]));
                        else if (method.Parameters[i] is EnumParameter)
                        {
                            // Parse the enum
                            if (methodNode.Parameters[i] is EnumNode)
                            {
                                EnumNode enumNode = (EnumNode)methodNode.Parameters[i];
                                parsedParameters.Add(
                                    (IWorkshopTree)EnumData.ToElement(enumNode.EnumMember)
                                    ?? (IWorkshopTree)enumNode.EnumMember
                                );
                            }
                            else
                                throw new SyntaxErrorException("Expected the enum " + ((EnumParameter)method.Parameters[i]).EnumData.CodeName + ", got a value instead.", ((Node)methodNode.Parameters[i]).Range);
                        }
                    }
                }
                else if (method.Parameters[i] is VarRefParameter)
                {
                    // A VarRef parameter is always required, there will never be a default to fallback on.
                    if (methodNode.Parameters.Length <= i)
                        throw SyntaxErrorException.MissingParameter(method.Parameters[i].Name, method.Name, methodNode.Range);
                    
                    // A VarRef parameter must be a variable
                    if (!(methodNode.Parameters[i] is VariableNode))
                        throw new SyntaxErrorException("Expected variable", ((Node)methodNode.Parameters[i]).Range);
                    
                    VariableNode variableNode = (VariableNode)methodNode.Parameters[i];

                    Element target = null;
                    if (variableNode.Target != null)
                        target = ParseExpression(scope, variableNode.Target);

                    parsedParameters.Add(new VarRef((IndexedVar)scope.GetVar(variableNode), target));
                        
                }
                else throw new NotImplementedException();
            }

            Element result;

            if (method is ElementList)
            {
                result = ((ElementList)method).GetObject();
                result.ParameterValues = parsedParameters.ToArray();
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
                UserMethod userMethod = (UserMethod)method;

                if (!userMethod.IsRecursive)
                {
                    if (MethodStackNoRecursive.Contains(userMethod))
                        throw SyntaxErrorException.RecursionNotAllowed(methodNode.Range);

                    var methodScope = Root.Child();

                    // Add the parameter variables to the scope.
                    IndexedVar[] parameterVars = new IndexedVar[parsedParameters.Count];
                    for (int i = 0; i < parsedParameters.Count; i++)
                    {
                        // Create a new variable using the parameter.
                        parameterVars[i] = VarCollection.AssignDefinedVar(methodScope, IsGlobal, userMethod.Parameters[i].Name, methodNode.Range);
                        Actions.AddRange(parameterVars[i].SetVariable((Element)parsedParameters[i]));
                    }

                    var returns = VarCollection.AssignVar(scope, $"{methodNode.Name}: return temp value", IsGlobal);

                    MethodStackNoRecursive.Add(userMethod);

                    var userMethodScope = methodScope.Child();
                    userMethod.Block.RelatedScopeGroup = userMethodScope;
                    
                    ParseBlock(userMethodScope, userMethod.Block, true, returns);

                    MethodStackNoRecursive.Remove(userMethod);

                    // No return value if the method is being used as an action.
                    if (needsToBeValue)
                        result = returns.GetVariable();
                    else
                        result = null;
                }
                else
                {                        
                    MethodStack lastMethod = MethodStack.FirstOrDefault(ms => ms.UserMethod == userMethod);
                    if (lastMethod != null)
                    {
                        ContinueSkip.Setup();

                        // ! Workaround for SmallMessage string re-evaluation workshop bug. Remove if blizzard fixes it
                        Actions.Add(A_Wait.MinimumWait);
                        // !

                        for (int i = 0; i < lastMethod.ParameterVars.Length; i++)
                        {
                            Actions.AddRange
                            (
                                lastMethod.ParameterVars[i].InScope(ParseExpression(scope, methodNode.Parameters[i]))
                            );
                        }

                        // ?--- Multidimensional Array 
                        Actions.Add(
                            Element.Part<A_SetGlobalVariable>(EnumData.GetEnumValue(Variable.B), lastMethod.ContinueSkipArray.GetVariable())
                        );
                        Actions.Add(
                            Element.Part<A_ModifyGlobalVariable>(EnumData.GetEnumValue(Variable.B), EnumData.GetEnumValue(Operation.AppendToArray), new V_Number(ContinueSkip.GetSkipCount() + 4))
                        );
                        Actions.AddRange(
                            lastMethod.ContinueSkipArray.SetVariable(Element.Part<V_GlobalVariable>(EnumData.GetEnumValue(Variable.B)))
                        );
                        // ?---

                        ContinueSkip.SetSkipCount(lastMethod.ActionIndex);
                        Actions.Add(Element.Part<A_Loop>());

                        if (needsToBeValue)
                            result = lastMethod.Return.GetVariable();
                        else
                            result = null;
                    }
                    else
                    {
                        var methodScope = Root.Child(true);

                        // Add the parameter variables to the scope.
                        RecursiveVar[] parameterVars = new RecursiveVar[userMethod.Parameters.Length];
                        for (int i = 0; i < parameterVars.Length; i++)
                        {
                            // Create a new variable using the parameter input.
                            //parameterVars[i] = VarCollection.AssignRecursiveVar(methodScope, IsGlobal, userMethod.Parameters[i].Name, methodNode.Range);
                            parameterVars[i] = (RecursiveVar)VarCollection.AssignDefinedVar(methodScope, IsGlobal, userMethod.Parameters[i].Name, methodNode.Range);
                            Actions.AddRange
                            (
                                parameterVars[i].InScope(ParseExpression(scope, methodNode.Parameters[i]))
                            );
                        }

                        var returns = VarCollection.AssignVar(null, $"{methodNode.Name}: return temp value", IsGlobal);

                        IndexedVar continueSkipArray = VarCollection.AssignVar(null, $"{methodNode.Name}: continue skip temp value", IsGlobal);
                        var stack = new MethodStack(userMethod, parameterVars, ContinueSkip.GetSkipCount(), returns, continueSkipArray);
                        MethodStack.Add(stack);

                        var userMethodScope = methodScope.Child();
                        userMethod.Block.RelatedScopeGroup = userMethodScope;
                        
                        ParseBlock(userMethodScope, userMethod.Block, true, returns);

                        // No return value if the method is being used as an action.
                        if (needsToBeValue)
                            result = returns.GetVariable();
                        else
                            result = null;

                        // ! Workaround for SmallMessage string re-evaluation workshop bug. Remove if blizzard fixes it
                        Actions.Add(A_Wait.MinimumWait);
                        // !
                        
                        foreach (IndexedVar var in methodScope.AllChildVariables())
                        {
                            Element[] outOfScopeActions = var.OutOfScope();
                            if (outOfScopeActions != null)
                                Actions.AddRange(outOfScopeActions);
                        }

                        ContinueSkip.Setup();
                        ContinueSkip.SetSkipCount(Element.Part<V_LastOf>(continueSkipArray.GetVariable()));

                        // ?--- Multidimensional Array 
                        Actions.Add(
                            Element.Part<A_SetGlobalVariable>(EnumData.GetEnumValue(Variable.B), continueSkipArray.GetVariable())
                        );
                        Actions.AddRange(
                            continueSkipArray.SetVariable(
                                Element.Part<V_ArraySlice>(
                                    Element.Part<V_GlobalVariable>(EnumData.GetEnumValue(Variable.B)), 
                                    new V_Number(0),
                                    Element.Part<V_Subtract>(
                                        Element.Part<V_CountOf>(Element.Part<V_GlobalVariable>(EnumData.GetEnumValue(Variable.B))),
                                        new V_Number(1)
                                    )
                                )
                            )
                        );
                        // ?---

                        Actions.Add(
                            Element.Part<A_LoopIf>(
                                Element.Part<V_Compare>(
                                    Element.Part<V_CountOf>(continueSkipArray.GetVariable()),
                                    EnumData.GetEnumValue(Operators.NotEqual),
                                    new V_Number(0)
                                )
                            )
                        );
                        ContinueSkip.ResetSkip();
                        Actions.AddRange(continueSkipArray.SetVariable(new V_Number()));
                        
                        MethodStack.Remove(stack);
                    }
                }
            }
            else throw new NotImplementedException();

            methodNode.RelatedElement = result;
            return result;
        }

        void ParseBlock(ScopeGroup scopeGroup, BlockNode blockNode, bool fulfillReturns, IndexedVar returnVar)
        {
            if (scopeGroup == null)
                throw new ArgumentNullException(nameof(scopeGroup));

            blockNode.RelatedScopeGroup = scopeGroup;

            int returnSkipStart = ReturnSkips.Count;

            //returned = Var.AssignVar(IsGlobal);
            
            for (int i = 0; i < blockNode.Statements.Length; i++)
                ParseStatement(scopeGroup, blockNode.Statements[i], returnVar, i == blockNode.Statements.Length - 1);

            if (fulfillReturns)
            {
                for (int i = ReturnSkips.Count - 1; i >= returnSkipStart; i--)
                {
                    ReturnSkips[i].ParameterValues = new IWorkshopTree[]
                    {
                        new V_Number(Actions.Count - 1 - Actions.IndexOf(ReturnSkips[i]))
                    };
                    ReturnSkips.RemoveAt(i);
                }
                //return returnVar.GetVariable();
            }

            //return null;
        }

        void ParseStatement(ScopeGroup scope, IStatementNode statement, IndexedVar returnVar, bool isLast)
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

                // For
                case ForEachNode forEachNode:
                {
                    ContinueSkip.Setup();

                    ScopeGroup forGroup = scope.Child();

                    Element array = ParseExpression(scope, forEachNode.Array);

                    IndexedVar index = VarCollection.AssignVar(scope, $"'{forEachNode.VariableName}' for index", IsGlobal);

                    Var variable;
                    bool isDefined = forEachNode.Define;

                    // Set/get the variable
                    if (isDefined)
                        //variable = VarCollection.AssignDefinedVar(forGroup, IsGlobal, forEachNode.VariableName, forEachNode.Range);
                        variable = VarCollection.AssignElementReferenceVar(forGroup, forEachNode.VariableName, forEachNode.Range, Element.Part<V_ValueInArray>(array, index.GetVariable()));
                    else
                        variable = scope.GetVar(forEachNode.VariableName, forEachNode.Range);

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
                    
                    // Update the array variable
                    if (!isDefined)
                        Actions.AddRange(((IndexedVar)variable).SetVariable(Element.Part<V_ValueInArray>(array, index.GetVariable())));

                    // Parse the for's block.
                    ParseBlock(forGroup, forEachNode.Block, false, returnVar);

                    // Increment the index
                    Actions.AddRange(index.SetVariable(Element.Part<V_Add>(index.GetVariable(), new V_Number(1))));

                    // Add the for's finishing elements
                    ContinueSkip.SetSkipCount(forStartIndex);
                    Actions.Add(Element.Part<A_Loop>());
                    
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

                    ScopeGroup forGroup = scope.Child();

                    // Set the variable
                    if (forNode.VarSetNode != null)
                        ParseVarset(scope, forNode.VarSetNode);
                    if (forNode.DefineNode != null)
                        ParseDefine(forGroup, forNode.DefineNode);

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
                        //ParseStatement(forGroup, forNode.Statement, returnVar, false);

                    ContinueSkip.SetSkipCount(forStartIndex);
                    Actions.Add(Element.Part<A_Loop>());
                    
                    // Set the skip
                    if (skipCondition != null)
                        skipCondition.ParameterValues[1] = new V_Number(GetSkipCount(skipCondition));

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

                    if (!isLast)
                    {
                        A_Skip returnSkip = new A_Skip();
                        Actions.Add(returnSkip);
                        ReturnSkips.Add(returnSkip);
                    }

                    return;
                
                // Define
                case ScopedDefineNode defineNode:
                    ParseDefine(scope, defineNode);
                    return;
            }
        }

        void ParseVarset(ScopeGroup scope, VarSetNode varSetNode)
        {
            Var gotVar = scope.GetVar(varSetNode.Variable, varSetNode.Range);
            if (!(gotVar is IndexedVar))
                throw new SyntaxErrorException($"Variable '{gotVar.Name}' is readonly.", varSetNode.Range);

            IndexedVar variable = (IndexedVar)gotVar;

            Element target = null;
            if (varSetNode.Target != null) 
                target = ParseExpression(scope, varSetNode.Target);
            
            Element value = null;
            if (varSetNode.Value != null)
                value = ParseExpression(scope, varSetNode.Value);

            Element initialVar = variable.GetVariable(target);

            Element index = null;
            if (varSetNode.Index != null)
            {
                index = ParseExpression(scope, varSetNode.Index);
                initialVar = Element.Part<V_ValueInArray>(initialVar, index);
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

            Actions.AddRange(variable.SetVariable(value, target, index));
        }

        void ParseDefine(ScopeGroup scope, ScopedDefineNode defineNode)
        {
            IndexedVar var;
            if (defineNode.UseVar == null)
                var = VarCollection.AssignDefinedVar(scope, IsGlobal, defineNode.VariableName, defineNode.Range);
            else
                var = VarCollection.AssignDefinedVar(scope, IsGlobal, defineNode.VariableName, defineNode.UseVar.Variable, defineNode.UseVar.Index, defineNode.Range);
                //var = new Var(scope, defineNode.VariableName, IsGlobal, defineNode.UseVar.Variable, defineNode.UseVar.Index, defineNode.Range, VarCollection);

            // Set the defined variable if the variable is defined like "define var = 1"
            if (defineNode.Value != null)
                Actions.AddRange(var.InScope(ParseExpression(scope, defineNode.Value)));
        }

        int GetSkipCount(Element skipElement)
        {
            return Actions.Count - Actions.IndexOf(skipElement) - 1;
        }
    }

    public class TranslateResult
    {
        public readonly Rule Rule;
        public readonly Diagnostic[] Diagnostics;

        public TranslateResult(Rule rule, Diagnostic[] diagnostics)
        {
            Rule = rule;
            Diagnostics = diagnostics;
        }
    }
}
