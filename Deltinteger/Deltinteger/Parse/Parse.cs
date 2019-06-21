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

            List<Diagnostic> diagnostics = new List<Diagnostic>();
            diagnostics.AddRange(errorListener.Errors);

            // Get the ruleset node.
            BuildAstVisitor bav = null;
            RulesetNode ruleSetNode = null;
            if (diagnostics.Count == 0)
            {
                bav = new BuildAstVisitor(documentPos, diagnostics);
                ruleSetNode = (RulesetNode)bav.Visit(ruleSetContext);
            }

            VarCollection varCollection = null;
            ScopeGroup root = null;
            List<UserMethod> userMethods = null;
            Rule[] rules = null;
            bool success = false;

            AdditionalErrorChecking aec = new AdditionalErrorChecking(parser, diagnostics);
            aec.Visit(ruleSetContext);

            bool parse = diagnostics.Count == 0;

            if (parse)
            {
                varCollection = new VarCollection();
                root = new ScopeGroup();
                userMethods = new List<UserMethod>();

                foreach (var definedVar in ruleSetNode.DefinedVars)
                    varCollection.AssignDefinedVar(root, definedVar.IsGlobal, definedVar.VariableName, definedVar.Range);

                // Get the user methods.
                for (int i = 0; i < ruleSetNode.UserMethods.Length; i++)
                    userMethods.Add(new UserMethod(ruleSetNode.UserMethods[i]));

                // Parse the rules.
                rules = new Rule[ruleSetNode.Rules.Length];

                for (int i = 0; i < rules.Length; i++)
                {
                    try
                    {
                        var result = Translate.GetRule(ruleSetNode.Rules[i], root, varCollection, userMethods.ToArray());
                        rules[i] = result.Rule;
                        diagnostics.AddRange(result.Diagnostics);
                    }
                    catch (SyntaxErrorException ex)
                    {
                        diagnostics.Add(new Diagnostic(ex.Message, ex.Range) { severity = Diagnostic.Error });
                    }
                }

                success = true;
            }
            
            return new ParserData()
            {
                Parser = parser,
                RulesetContext = ruleSetContext,
                RuleSetNode = ruleSetNode,
                Bav = bav,
                Diagnostics = diagnostics,
                Rules = rules,
                UserMethods = userMethods?.ToArray(),
                Root = root,
                Success = success,
                VarCollection = varCollection
            };
        }

        public DeltinScriptParser Parser { get; private set; }
        public DeltinScriptParser.RulesetContext RulesetContext { get; private set; }
        public RulesetNode RuleSetNode { get; private set; }
        public List<Diagnostic> Diagnostics;
        public BuildAstVisitor Bav { get; private set; }
        public Rule[] Rules { get; private set; }
        public UserMethod[] UserMethods { get; private set; }
        public ScopeGroup Root { get; private set; }
        public bool Success { get; private set; }
        public VarCollection VarCollection { get; private set; }
    }

    class Translate
    {
        public static bool AllowRecursion = false;

        public static TranslateResult GetRule(RuleNode ruleNode, ScopeGroup root, VarCollection varCollection, UserMethod[] userMethods)
        {
            var result = new Translate(ruleNode, root, varCollection, userMethods);
            return new TranslateResult(result.Rule, result.Diagnostics.ToArray());
        }

        private readonly ScopeGroup Root;
        private readonly VarCollection VarCollection;
        private readonly UserMethod[] UserMethods;
        private readonly Rule Rule;
        private readonly List<Element> Actions = new List<Element>();
        private readonly List<Condition> Conditions = new List<Condition>();
        private readonly bool IsGlobal;
        private readonly List<A_Skip> ReturnSkips = new List<A_Skip>(); // Return statements whos skip count needs to be filled out.
        private readonly ContinueSkip ContinueSkip; // Contains data about the wait/skip for continuing loops.
        private readonly List<Diagnostic> Diagnostics = new List<Diagnostic>();
        private readonly List<MethodStack> MethodStack = new List<MethodStack>(); // The user method stack
        private readonly List<UserMethod> MethodStackNoRecursive = new List<UserMethod>();

        private Translate(RuleNode ruleNode, ScopeGroup root, VarCollection varCollection, UserMethod[] userMethods)
        {
            Root = root;
            VarCollection = varCollection;
            UserMethods = userMethods;

            Rule = new Rule(ruleNode.Name, ruleNode.Event, ruleNode.Team, ruleNode.Player);
            IsGlobal = Rule.IsGlobal;

            ContinueSkip = new ContinueSkip(IsGlobal, Actions, varCollection);

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
                    return scope.GetVar(variableNode.Name, variableNode.Range, Diagnostics)
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

                case EnumNode enumNode:
                    return EnumData.Special(enumNode.EnumMember) 
                    ?? throw SyntaxErrorException.EnumCantBeValue(enumNode.Type, enumNode.Range);

                // Seperator

            }

            throw new Exception();
        }

        Element ParseMethod(ScopeGroup scope, MethodNode methodNode, bool needsToBeValue)
        {
            methodNode.RelatedScopeGroup = scope;

            // Get the kind of method the method is (Method (Overwatch), Custom Method, or User Method.)
            var methodType = GetMethodType(UserMethods, methodNode.Name);

            // Throw exception if the method does not exist.
            if (methodType == null)
                throw SyntaxErrorException.NonexistentMethod(methodNode.Name, methodNode.Range);

            Element method;
            switch (methodType)
            {
                case MethodType.Method:
                {
                    Type owMethod = Element.GetMethod(methodNode.Name);

                    method = (Element)Activator.CreateInstance(owMethod);
                    Parameter[] parameterData = owMethod.GetCustomAttributes<Parameter>().ToArray();
                    
                    List<IWorkshopTree> parsedParameters = new List<IWorkshopTree>();
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
                                // Throw exception if there is no default method to fallback on.
                                throw SyntaxErrorException.MissingParameter(parameterData[i].Name, methodNode.Name, methodNode.Range);
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
                            // Throw exception if there is no default method to fallback on.
                            throw SyntaxErrorException.MissingParameter(parameterData[i].Name, methodNode.Name, methodNode.Range);

                    MethodResult result = (MethodResult)customMethod.Invoke(null, new object[] { IsGlobal, VarCollection, parsedParameters });
                    switch (result.MethodType)
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

                    // Some custom methods have extra actions.
                    if (result.Elements != null)
                        Actions.AddRange(result.Elements);
                    method = result.Result;

                    break;
                }

                case MethodType.UserMethod:
                {
                    if (!AllowRecursion)
                    {
                        UserMethod userMethod = UserMethod.GetUserMethod(UserMethods, methodNode.Name);

                        if (MethodStackNoRecursive.Contains(userMethod))
                            throw SyntaxErrorException.RecursionNotAllowed(methodNode.Range);

                        var methodScope = Root.Child();

                        // Add the parameter variables to the scope.
                        DefinedVar[] parameterVars = new DefinedVar[userMethod.Parameters.Length];
                        for (int i = 0; i < parameterVars.Length; i++)
                        {
                            if (methodNode.Parameters.Length > i)
                            {
                                // Create a new variable using the parameter input.
                                parameterVars[i] = VarCollection.AssignDefinedVar(methodScope, IsGlobal, userMethod.Parameters[i].Name, methodNode.Range);
                                Actions.Add(parameterVars[i].SetVariable(ParseExpression(scope, methodNode.Parameters[i])));
                            }
                            else
                                throw SyntaxErrorException.MissingParameter(userMethod.Parameters[i].Name, methodNode.Name, methodNode.Range);
                        }

                        var returns = VarCollection.AssignVar($"{methodNode.Name}: return temp value", IsGlobal);

                        MethodStackNoRecursive.Add(userMethod);

                        var userMethodScope = methodScope.Child();
                        userMethod.Block.RelatedScopeGroup = userMethodScope;
                        
                        ParseBlock(userMethodScope, userMethod.Block, true, returns);

                        MethodStackNoRecursive.Remove(userMethod);

                        // No return value if the method is being used as an action.
                        if (needsToBeValue)
                            method = returns.GetVariable();
                        else
                            method = null;

                        break;
                    }
                    else
                    {
                        UserMethod userMethod = UserMethod.GetUserMethod(UserMethods, methodNode.Name);
                        
                        MethodStack lastMethod = MethodStack.FirstOrDefault(ms => ms.UserMethod == userMethod);
                        if (lastMethod != null)
                        {
                            ContinueSkip.Setup();

                            for (int i = 0; i < lastMethod.ParameterVars.Length; i++)
                                if (methodNode.Parameters.Length > i)
                                    Actions.Add(lastMethod.ParameterVars[i].Push(ParseExpression(scope, methodNode.Parameters[i])));

                            // ?--- Multidimensional Array 
                            Actions.Add(
                                Element.Part<A_SetGlobalVariable>(EnumData.GetEnumValue(Variable.B), lastMethod.ContinueSkipArray.GetVariable())
                            );
                            Actions.Add(
                                Element.Part<A_ModifyGlobalVariable>(EnumData.GetEnumValue(Variable.B), EnumData.GetEnumValue(Operation.AppendToArray), new V_Number(ContinueSkip.GetSkipCount() + 4))
                            );
                            Actions.Add(
                                lastMethod.ContinueSkipArray.SetVariable(Element.Part<V_GlobalVariable>(EnumData.GetEnumValue(Variable.B)))
                            );
                            // ?---

                            ContinueSkip.SetSkipCount(lastMethod.ActionIndex);
                            Actions.Add(Element.Part<A_Loop>());

                            if (needsToBeValue)
                                method = lastMethod.Return.GetVariable();
                            else
                                method = null;
                        }
                        else
                        {
                            var methodScope = Root.Child();

                            // Add the parameter variables to the scope.
                            ParameterVar[] parameterVars = new ParameterVar[userMethod.Parameters.Length];
                            for (int i = 0; i < parameterVars.Length; i++)
                            {
                                if (methodNode.Parameters.Length > i)
                                {
                                    // Create a new variable using the parameter input.
                                    parameterVars[i] = VarCollection.AssignParameterVar(Actions, methodScope, IsGlobal, userMethod.Parameters[i].Name, methodNode.Range);
                                    Actions.Add(parameterVars[i].Push(ParseExpression(scope, methodNode.Parameters[i])));
                                }
                                else
                                    throw SyntaxErrorException.MissingParameter(userMethod.Parameters[i].Name, methodNode.Name, methodNode.Range);
                            }

                            var returns = VarCollection.AssignVar($"{methodNode.Name}: return temp value", IsGlobal);

                            Var continueSkipArray = VarCollection.AssignVar($"{methodNode.Name}: continue skip temp value", IsGlobal);
                            var stack = new MethodStack(userMethod, parameterVars, ContinueSkip.GetSkipCount(), returns, continueSkipArray);
                            MethodStack.Add(stack);

                            var userMethodScope = methodScope.Child();
                            userMethod.Block.RelatedScopeGroup = userMethodScope;
                            
                            ParseBlock(userMethodScope, userMethod.Block, true, returns);

                            // No return value if the method is being used as an action.
                            if (needsToBeValue)
                                method = returns.GetVariable();
                            else
                                method = null;

                            Actions.Add(Element.Part<A_Wait>(new V_Number(Constants.MINIMUM_WAIT)));
                            for (int i = 0; i < parameterVars.Length; i++)
                            {
                                parameterVars[i].Pop();
                            }

                            ContinueSkip.Setup();
                            ContinueSkip.SetSkipCount(Element.Part<V_LastOf>(continueSkipArray.GetVariable()));

                            // ?--- Multidimensional Array 
                            Actions.Add(
                                Element.Part<A_SetGlobalVariable>(EnumData.GetEnumValue(Variable.B), continueSkipArray.GetVariable())
                            );
                            Actions.Add(
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

                            MethodStack.Remove(stack);
                        }
                        break;
                    }
                }

                default: throw new NotImplementedException();
            }

            methodNode.RelatedElement = method;
            return method;
        }

        IWorkshopTree ParseParameter(ScopeGroup scope, IExpressionNode node, string methodName, Parameter parameterData)
        {
            IWorkshopTree value = null;

            switch (node)
            {
                case EnumNode enumNode:

                    /*
                    if (parameterData.ParameterType != ParameterType.Enum)
                        throw SyntaxErrorException.ExpectedType(true, parameterData.ValueType.ToString(), methodName, parameterData.Name, enumNode.Range);

                    if (enumNode.Type != parameterData.EnumType.Name)
                        throw SyntaxErrorException.ExpectedType(false, parameterData.EnumType.ToString(), methodName, parameterData.Name, enumNode.Range);
                    */

                    value = (IWorkshopTree)EnumData.Special(enumNode.EnumMember) ?? (IWorkshopTree)enumNode.EnumMember;

                    //if (value == null)
                      //  throw SyntaxErrorException.InvalidEnumValue(enumNode.Type, enumNode.Value, enumNode.Range);
                    
                    break;

                default:

                    if (parameterData.ParameterType != ParameterType.Value)
                        throw SyntaxErrorException.ExpectedType(false, parameterData.EnumType.Name, methodName, parameterData.Name, ((Node)node).Range);

                    value = ParseExpression(scope, node);

                    Element element = value as Element;
                    ElementData elementData = element.GetType().GetCustomAttribute<ElementData>();

                    if (elementData.ValueType != Elements.ValueType.Any &&
                    !parameterData.ValueType.HasFlag(elementData.ValueType))
                        throw SyntaxErrorException.InvalidType(parameterData.ValueType, elementData.ValueType, ((Node)node).Range);

                    break;
            }

            if (value == null)
                throw new Exception("Failed to parse parameter.");

            return value;
        }
    
        public static MethodType? GetMethodType(UserMethod[] userMethods, string name)
        {
            if (Element.GetMethod(name) != null)
                return MethodType.Method;
            if (CustomMethods.GetCustomMethod(name) != null)
                return MethodType.CustomMethod;
            if (UserMethod.GetUserMethod(userMethods, name) != null)
                return MethodType.UserMethod;
            return null;
        }

        public enum MethodType
        {
            Method,
            CustomMethod,
            UserMethod
        }

        void ParseBlock(ScopeGroup scopeGroup, BlockNode blockNode, bool fulfillReturns, Var returnVar)
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

        void ParseStatement(ScopeGroup scope, IStatementNode statement, Var returnVar, bool isLast)
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

                    // The action the for loop starts on.
                    int forActionStartIndex = Actions.Count() - 1;

                    ScopeGroup forGroup = scope.Child();

                    // Create the for's temporary variable.
                    DefinedVar forTempVar = VarCollection.AssignDefinedVar(
                        scopeGroup: forGroup,
                        name      : forEachNode.Variable,
                        isGlobal  : IsGlobal,
                        range     : forEachNode.Range
                        );

                    // Reset the counter.
                    Actions.Add(forTempVar.SetVariable(new V_Number(0)));

                    // Parse the for's block.
                    ParseBlock(forGroup, forEachNode.Block, false, returnVar);

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
                            EnumData.GetEnumValue(Operators.LessThan),
                            Element.Part<V_CountOf>(forArrayElement)
                        )
                    ));

                    ContinueSkip.ResetSkip();
                    return;
                }

                // For
                case ForNode forNode:
                {
                    ContinueSkip.Setup();

                    // The action the for loop starts on.
                    int forActionStartIndex = Actions.Count() - 1;

                    ScopeGroup forGroup = scope.Child();

                    // Set the variable
                    if (forNode.VarSetNode != null)
                        ParseVarset(scope, forNode.VarSetNode);
                    if (forNode.DefineNode != null)
                        ParseDefine(scope, forNode.DefineNode);

                    // Parse the for's block.
                    ParseBlock(forGroup, forNode.Block, false, returnVar);

                    Element expression = null;
                    if (forNode.Expression != null)
                        expression = ParseExpression(forGroup, forNode.Expression);

                    // Check the expression
                    if (forNode.Expression != null) // If it has an expression
                    {                        
                        // Parse the statement
                        if (forNode.Statement != null)
                            ParseStatement(forGroup, forNode.Statement, returnVar, false);

                        ContinueSkip.SetSkipCount(forActionStartIndex);
                        Actions.Add(Element.Part<A_LoopIf>(expression));
                    }
                    // If there is no expression but there is a statement, parse the statement.
                    else if (forNode.Statement != null)
                    {
                        ParseStatement(forGroup, forNode.Statement, returnVar, false);
                        ContinueSkip.SetSkipCount(forActionStartIndex);
                        // Add the loop
                        Actions.Add(Element.Part<A_Loop>());
                    }

                    ContinueSkip.ResetSkip();
                    return;
                }

                // While
                case WhileNode whileNode:
                {
                    ContinueSkip.Setup();

                    // The action the while loop starts on.
                    int whileStartIndex = Actions.Count() - 2;

                    ScopeGroup whileGroup = scope.Child();

                    ParseBlock(whileGroup, whileNode.Block, false, returnVar);

                    ContinueSkip.SetSkipCount(whileStartIndex);

                    // Add the loop-if
                    Element expression = ParseExpression(scope, whileNode.Expression);
                    Actions.Add(Element.Part<A_LoopIf>(expression));

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
                            Actions.Add(returnVar.SetVariable(result));
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
            DefinedVar variable = scope.GetVar(varSetNode.Variable, varSetNode.Range, Diagnostics);

            Element target = null;
            if (varSetNode.Target != null) 
                target = ParseExpression(scope, varSetNode.Target);
            
            Element value = ParseExpression(scope, varSetNode.Value);

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
            }

            Actions.Add(variable.SetVariable(value, target, index));
        }

        void ParseDefine(ScopeGroup scope, ScopedDefineNode defineNode)
        {
            var var = VarCollection.AssignDefinedVar(scope, IsGlobal, defineNode.VariableName, defineNode.Range);

            // Set the defined variable if the variable is defined like "define var = 1"
            if (defineNode.Value != null)
                Actions.Add(var.SetVariable(ParseExpression(scope, defineNode.Value)));
        }
    }

    class TranslateResult
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
