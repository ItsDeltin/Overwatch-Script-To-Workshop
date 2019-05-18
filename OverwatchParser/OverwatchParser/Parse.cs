using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4;
using Antlr4.Runtime;
using OverwatchParser.Elements;
using System.Reflection;

namespace OverwatchParser.Parse
{
    public class Parser
    {
        static Log Log = new Log("Parse");

        public static Rule[] ParseText(string text)
        {
            AntlrInputStream inputStream = new AntlrInputStream(text);

            // Lexer
            DeltinScriptLexer speakLexer = new DeltinScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(speakLexer);

            // Parse
            DeltinScriptParser speakParser = new DeltinScriptParser(commonTokenStream);

            // Get context
            DeltinScriptParser.RulesetContext context = speakParser.ruleset();

            Log.Write(context.ToStringTree());

            Visitor visitor = new Visitor();
            visitor.Visit(context);

            {
                // Get the internal global variable to use.
                if (!Enum.TryParse(context.useGlobalVar().PART().ToString(), out Variable useGlobalVar))
                    throw new SyntaxErrorException("useGlobalVar must be a character.", 0, 0);

                // Get the internal player variable to use.
                if (!Enum.TryParse(context.usePlayerVar().PART().ToString(), out Variable usePlayerVar))
                    throw new SyntaxErrorException("usePlayerVar must be a character.", 0, 0);

                Var.Setup(useGlobalVar, usePlayerVar);
            }

            // Get the defined variables.
            var vardefine = context.vardefine();

            for (int i = 0; i < vardefine.Length; i++)
                // The new var is stored in Var.VarCollection
                new DefinedVar(vardefine[i]);

            // Parse the rules.
            var rules = context.ow_rule();
            var compiledRules = new List<Rule>();

            for (int i = 0; i < rules.Length; i++)
            {
                ParseRule parsing = new ParseRule(rules[i]);

                Log.Write($"Building rule: {parsing.Rule.Name}");
                parsing.Parse();
                Rule rule = parsing.Rule;
                Log.Write($"Finished rule: {parsing.Rule.Name}");

                compiledRules.Add(rule);
            }

            Log.Write("Build succeeded.");

            // List all variables
            Log.Write("Variable Guide:");
            foreach (DefinedVar var in DefinedVar.VarCollection)
                Console.WriteLine($"{var.Name}: {(var.IsGlobal ? "global" : "player")} {var.Variable}{(var.IsInArray ? $"[{var.Index}]" : "")}");

            return compiledRules.ToArray();
        }
    }

    class ParseRule
    {
        public Rule Rule { get; private set; }

        private readonly List<Element> Actions = new List<Element>();
        private readonly List<Condition> Conditions = new List<Condition>();

        private DeltinScriptParser.Ow_ruleContext RuleContext;

        private readonly bool IsGlobal;

        //private bool CreateInitialSkip = false;
        //private int SkipCountIndex = -1;

        public ParseRule(DeltinScriptParser.Ow_ruleContext ruleContext)
        {
            Rule = CreateRuleFromContext(ruleContext);
            RuleContext = ruleContext;
            IsGlobal = Rule.RuleEvent == RuleEvent.Ongoing_Global;
        }

        public void Parse()
        {
            // Parse conditions
            ParseConditions();
            
            // Parse actions
            ParseBlock(RuleContext.block());

            // Add an initial skip if it is required.
            /*
            if (CreateInitialSkip)
            {
                if (SkipCountIndex == -1)
                    throw new Exception($"{nameof(CreateInitialSkip)} cannot be true if {nameof(SkipCountIndex)} is -1!");

                Actions.Insert(0,
                    Element.Part<A_SkipIf>
                    (
                        // Condition
                        Element.Part<V_Compare>
                        (
                            GetIVarAtIndex(SkipCountIndex),
                            Operators.NotEqual,
                            new V_Number(0)
                        ),
                        // Number of actions
                        GetIVarAtIndex(SkipCountIndex)
                    )
                );
            }
            */

            Rule.Conditions = Conditions.ToArray();
            Rule.Actions = Actions.ToArray();
        }

        static Rule CreateRuleFromContext(DeltinScriptParser.Ow_ruleContext ruleContext)
        {
            string ruleName = ruleContext.STRINGLITERAL().GetText();
            ruleName = ruleName.Substring(1, ruleName.Length - 2);

            RuleEvent ruleEvent = RuleEvent.Ongoing_Global;
            TeamSelector team = TeamSelector.All;
            PlayerSelector player = PlayerSelector.All;

            {
                var additionalArgs = ruleContext.expr();
                foreach (var arg in additionalArgs)
                {
                    string name = arg.GetText().Split('.').ElementAtOrDefault(1);

                    if (name != null &&
                        !Enum.TryParse($"{name}", out ruleEvent) &&
                        !Enum.TryParse($"{name}", out team) &&
                        !Enum.TryParse($"{name}", out player))
                    {
                        throw new SyntaxErrorException("Unknown value in rule target parameters.", 0, 0);
                    }
                }
            }

            return new Rule(ruleName, ruleEvent, team, player);
        }

        void ParseConditions()
        {
            // Get the if contexts
            var ifContexts = RuleContext.rule_if();
            
            foreach(var @if in ifContexts)
            {
                Element parsedIf = ParseExpression(@if.expr());
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

        void ParseBlock(DeltinScriptParser.BlockContext blockContext)
        {
            var statements = blockContext.children
                .Where(v => v is DeltinScriptParser.StatementContext)
                .Cast<DeltinScriptParser.StatementContext>().ToArray();

            for (int i = 0; i < statements.Length; i++)
                ParseStatement(statements[i]);
        }

        void ParseStatement(DeltinScriptParser.StatementContext statementContext)
        {
            #region Method
            if (statementContext.GetChild(0) is DeltinScriptParser.MethodContext)
            {
                Actions.Add(ParseMethod(statementContext.GetChild(0) as DeltinScriptParser.MethodContext));
                return;
            }
            #endregion

            #region Custom Method
            if (statementContext.GetChild(0) is DeltinScriptParser.Custom_methodContext)
            {
                MethodResult result = ParseCustomMethod(statementContext.GetChild(0) as DeltinScriptParser.Custom_methodContext);

                if (result.MethodType != CustomMethodType.Action)
                    throw new SyntaxErrorException("Expected action method.", statementContext.start.Line, statementContext.start.Column);

                return;
            }
            #endregion

            #region Variable set. TODO: add support for += -= *= /=

            if (statementContext.STATEMENT_OPERATION() != null)
            {
                DefinedVar variable;
                Element target;
                string operation = statementContext.STATEMENT_OPERATION().GetText();

                Element value;

                if (statementContext.expr(1).GetChild(0) is DeltinScriptParser.Custom_methodContext)
                {
                    var result = ParseCustomMethod(statementContext.expr(1).GetChild(0) as DeltinScriptParser.Custom_methodContext);

                    if (result.MethodType != CustomMethodType.MultiAction_Value && result.MethodType != CustomMethodType.Value)
                        throw new SyntaxErrorException("Expected value method.", statementContext.start.Line, statementContext.start.Column);

                    if (result.Elements != null)
                        Actions.AddRange(result.Elements);

                    value = result.Result;
                }
                else
                    value = ParseExpression(statementContext.expr(1) as DeltinScriptParser.ExprContext);

                if (statementContext.expr(0).ChildCount == 3
                    && statementContext.expr(0).GetChild(1).GetText() == ".")
                {
                    variable = DefinedVar.GetVar(statementContext.expr(0).expr(1).GetChild(0).GetText());
                    target = ParseExpression(statementContext.expr(0).expr(0));
                }
                else
                {
                    variable = DefinedVar.GetVar(statementContext.expr(0).GetChild(0).GetText());
                    target = new V_EventPlayer();
                }

                switch (operation)
                {
                    case "+=":
                        value = Element.Part<V_Add>(variable.GetVariable(), value);
                        break;

                    case "-=":
                        value = Element.Part<V_Subtract>(variable.GetVariable(), value);
                        break;

                    case "*=":
                        value = Element.Part<V_Multiply>(variable.GetVariable(), value);
                        break;

                    case "/=":
                        value = Element.Part<V_Divide>(variable.GetVariable(), value);
                        break;

                    case "^=":
                        value = Element.Part<V_RaiseToPower>(variable.GetVariable(), value);
                        break;

                    case "%=":
                        value = Element.Part<V_Modulo>(variable.GetVariable(), value);
                        break;
                }

                Actions.Add(variable.SetVariable(value, target));
                return;
            }

            #endregion

            #region for

            if (statementContext.GetChild(0) is DeltinScriptParser.ForContext)
            {
                /*
                CreateInitialSkip = true;

                if (SkipCountIndex == -1)
                    SkipCountIndex = Assign();
                */

                // The action the for loop starts on.
                // +1 for the counter reset.
                int forActionStartIndex = Actions.Count() + 1;

                // The target array in the for statement.
                Element forArrayElement = ParseExpression(statementContext.@for().expr());

                // Use skipIndex with Get/SetIVarAtIndex to get the bool to determine if the loop is running.
                Var isBoolRunningSkipIf = Var.AssignVar(IsGlobal);
                // Insert the SkipIf at the start of the rule.
                Actions.Insert(0,
                    Element.Part<A_SkipIf>
                    (
                        // Condition
                        isBoolRunningSkipIf.GetVariable(),
                        // Number of actions
                        new V_Number(forActionStartIndex)
                    )
                );

                // Create the for's temporary variable.
                DefinedVar forTempVar = Var.AssignDefinedVar(
                    name    : statementContext.@for().PART().GetText(),
                    isGlobal: IsGlobal,
                    line    : statementContext.@for().start.Line,
                    column  : statementContext.@for().start.Column
                    );

                // Reset the counter.
                Actions.Add(forTempVar.SetVariable(new V_Number(0)));

                // Parse the for's block.
                ParseBlock(statementContext.@for().block());

                // Take the variable out of scope.
                forTempVar.OutOfScope();

                // Add the for's finishing elements
                //Actions.Add(SetIVarAtIndex(skipIndex, new V_Number(forActionStartIndex))); // Sets how many variables to skip in the next iteraction.
                Actions.Add(isBoolRunningSkipIf.SetVariable(new V_True())); // Enables the skip.

                Actions.Add(forTempVar.SetVariable( // Indent the index by 1.
                    Element.Part<V_Add>
                    (
                        forTempVar.GetVariable(),
                        new V_Number(1)
                    )
                ));

                Actions.Add(Element.Part<A_Wait>(new V_Number(0.06), WaitBehavior.IgnoreCondition)); // Add the Wait() required by the workshop.
                Actions.Add(Element.Part<A_LoopIf>( // Loop if the for condition is still true.
                    Element.Part<V_Compare>
                    (
                        forTempVar.GetVariable(),
                        Operators.LessThan,
                        Element.Part<V_CountOf>(forArrayElement)
                    )
                ));
                Actions.Add(isBoolRunningSkipIf.SetVariable(new V_False()));
                return;
            }

            #endregion

            #region if

            if (statementContext.GetChild(0) is DeltinScriptParser.IfContext)
            {
                /*
                Syntax after parse:

                If:
                    Skip If (Not (expr))
                    (body)
                    Skip - Only if there is if-else or else statements.
                Else if:
                    Skip If (Not (expr))
                    (body)
                    Skip - Only if there is more if-else or else statements.
                Else:
                    (body)

                */

                // Add dummy action, create after body is created.
                int skipIfIndex = Actions.Count();
                Actions.Add(null);

                // Parse the if body.
                ParseBlock(statementContext.@if().block());

                // Determines if the "Skip" action after the if block will be created.
                // Only if there is if-else or else statements.
                bool addIfSkip = statementContext.@if().else_if().Count() > 0 || statementContext.@if().@else() != null;

                // Create the inital "SkipIf" action now that we know how long the if's body is.
                // Add one to the body length if a Skip action is going to be added.
                Actions.RemoveAt(skipIfIndex);
                Actions.Insert(skipIfIndex, Element.Part<A_SkipIf>(Element.Part<V_Not>(ParseExpression(statementContext.@if().expr())), new V_Number(Actions.Count - skipIfIndex + (addIfSkip ? 1 : 0))));

                // Create the "Skip" dummy action.
                int skipIndex = -1;
                if (addIfSkip)
                {
                    skipIndex = Actions.Count();
                    Actions.Add(null);
                }

                // Parse else-ifs
                var skipIfContext = statementContext.@if().else_if();
                int[] skipIfData = new int[skipIfContext.Length]; // The index where the else if's "Skip" action is.
                for (int i = 0; i < skipIfContext.Length; i++)
                {
                    // Create the dummy action.
                    int skipIfElseIndex = Actions.Count();
                    Actions.Add(null);

                    // Parse the else-if body.
                    ParseBlock(skipIfContext[i].block());

                    // Determines if the "Skip" action after the else-if block will be created.
                    // Only if there is additional if-else or else statements.
                    bool addIfElseSkip = i < skipIfContext.Length - 1 || statementContext.@if().@else() != null;

                    // Create the "Skip If" action.
                    Actions.RemoveAt(skipIfElseIndex);
                    Actions.Insert(skipIfElseIndex, Element.Part<A_SkipIf>(Element.Part<V_Not>(ParseExpression(skipIfContext[i].expr())), new V_Number(Actions.Count - skipIfElseIndex + (addIfElseSkip ? 1 : 0))));

                    // Create the "Skip" dummy action.
                    if (addIfElseSkip)
                    {
                        skipIfData[i] = Actions.Count();
                        Actions.Add(null);
                    }
                }

                // Parse else body.
                if (statementContext.@if().@else() != null)
                    ParseBlock(statementContext.@if().@else().block());

                // Replace dummy skip with real skip now that we know the length of the if, if-else, and else's bodies.
                // Replace if's dummy.
                if (skipIndex != -1)
                {
                    Actions.RemoveAt(skipIndex);
                    Actions.Insert(skipIndex, Element.Part<A_Skip>(new V_Number(Actions.Count - skipIndex)));
                }

                // Replace else-if's dummy.
                for (int i = 0; i < skipIfData.Length; i++)
                    if (skipIfData[i] != 0)
                    {
                        Actions.RemoveAt(skipIfData[i]);
                        Actions.Insert(skipIfData[i], Element.Part<A_Skip>(new V_Number(Actions.Count - skipIfData[i])));
                    }

                return;
            }

            #endregion

            throw new Exception($"What's a {statementContext.GetChild(0)} ({statementContext.GetChild(0).GetType()})?");
        }

        Element ParseExpression(DeltinScriptParser.ExprContext context)
        {
            // If the expression is a(n)...

            #region Operation

            //   0       1      2
            // (expr operation expr)
            // count == 3
            if (context.ChildCount == 3 && new string[] { "^", "*", "/", "+", "-", "&", "|", "<", "<=", "==", ">=", ">", "!=" }.Contains(context.GetChild(1).GetText()))
            {
                Element left = ParseExpression(context.GetChild(0) as DeltinScriptParser.ExprContext);
                string operation = context.GetChild(1).GetText();
                Element right = ParseExpression(context.GetChild(2) as DeltinScriptParser.ExprContext);

                switch (operation)
                {
                    #region Math

                    case "^":
                        return Element.Part<V_RaiseToPower>(left, right);

                    case "*":
                        return Element.Part<V_Multiply>(left, right);

                    case "/":
                        return Element.Part<V_Divide>(left, right);

                    case "+":
                        return Element.Part<V_Add>(left, right);

                    case "-":
                        return Element.Part<V_Subtract>(left, right);

                    case "%":
                        return Element.Part<V_Modulo>(left, right);

                    #endregion

                    #region Bool compare

                    // COMPARE : '<' | '<=' | '==' | '>=' | '>' | '!=';

                    case "&":
                        return Element.Part<V_And>(left, right);

                    case "|":
                        return Element.Part<V_Or>(left, right);

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

                        #endregion
                }
            }

            #endregion

            #region Not

            if (context.GetChild(0) is DeltinScriptParser.NotContext)
                return Element.Part<V_Not>(ParseExpression(context.GetChild(1) as DeltinScriptParser.ExprContext));

            #endregion

            #region Number

            if (context.GetChild(0) is DeltinScriptParser.NumberContext)
            {
                var number = context.GetChild(0);

                double num = double.Parse(number.GetChild(0).GetText());
                /*
                // num will have the format expr(number(X)) if positive, expr(number(neg(X))) if negative.
                if (number.GetChild(0) is DeltinScriptParser.NegContext)
                    // Is negative, use '-' before int.parse to make it negative.
                    num = -double.Parse(number.GetChild(0).GetText());
                else
                    // Is positive
                    num = double.Parse(number.GetChild(0).GetText());
                */

                return new V_Number(num);
            }

            #endregion

            #region Boolean

            // True
            if (context.GetChild(0) is DeltinScriptParser.TrueContext)
                return new V_True();

            // False
            if (context.GetChild(0) is DeltinScriptParser.FalseContext)
                return new V_False();

            #endregion

            #region String

            if (context.GetChild(0) is DeltinScriptParser.StringContext)
            {
                return V_String.BuildString(
                    (context.GetChild(0) as DeltinScriptParser.StringContext)
                    .STRINGLITERAL().GetText()
                    // String will look like "hey this is the contents", trim the quotes.
                    .Trim('\"')
                );
            }

            #endregion

            #region null

            if (context.GetChild(0) is DeltinScriptParser.NullContext)
                return new V_Null();

            #endregion

            #region Group ( expr )

            if (context.ChildCount == 3 && context.GetChild(0).GetText() == "(" &&
                context.GetChild(0) is DeltinScriptParser.ExprContext &&
                context.GetChild(2).GetText() == ")")
            {
                Console.WriteLine("Group type:" + context.GetChild(0).GetType());
                return ParseExpression(context.GetChild(1) as DeltinScriptParser.ExprContext);
            }

            #endregion

            #region Method

            if (context.GetChild(0) is DeltinScriptParser.MethodContext)
                return ParseMethod(context.GetChild(0) as DeltinScriptParser.MethodContext);

            #endregion

            #region Variable

            if (context.GetChild(0) is DeltinScriptParser.VariableContext)
                return DefinedVar.GetVar((context.GetChild(0) as DeltinScriptParser.VariableContext).PART().GetText()).GetVariable(new V_EventPlayer());

            #endregion

            #region Array

            if (context.ChildCount == 4 && context.GetChild(1).GetText() == "[" && context.GetChild(3).GetText() == "]")
                return Element.Part<V_ValueInArray>(
                    ParseExpression(context.expr(0) as DeltinScriptParser.ExprContext),
                    ParseExpression(context.expr(1) as DeltinScriptParser.ExprContext));

            #endregion

            #region Create Array

            if (context.ChildCount >= 4 && context.GetChild(0).GetText() == "[")
            {
                var expressions = context.expr();
                V_AppendToArray prev = null;
                V_AppendToArray current = null;

                for (int i = 0; i < expressions.Length; i++)
                {
                    current = new V_AppendToArray();
                    current.ParameterValues = new object[2];

                    if (prev != null)
                        current.ParameterValues[0] = prev;
                    else
                        current.ParameterValues[0] = new V_EmptyArray();

                    current.ParameterValues[1] = ParseExpression(expressions[i]);
                    prev = current;
                }

                return current;
            }

            #endregion

            #region Seperator/enum

            if (context.ChildCount == 3 && context.GetChild(1).GetText() == ".")
            {
                Element left = ParseExpression(context.GetChild(0) as DeltinScriptParser.ExprContext);
                string variableName = context.GetChild(2).GetChild(0).GetText();

                DefinedVar var = DefinedVar.GetVar(variableName);
                if (var == null)
                    throw new SyntaxErrorException($"Variable {variableName} does not exist.", context.start.Line, context.start.Column);

                return var.GetVariable(left);
            }

            #endregion

            throw new Exception($"What's a {context.GetType().Name}?");
        }

        Element ParseMethod(DeltinScriptParser.MethodContext methodContext)
        {
            string methodName = methodContext.PART().GetText();
            Type methodType = Element.GetMethod(methodName);

            if (methodType == null)
                throw new SyntaxErrorException($"The method {methodName} does not exist.", methodContext.start.Line, methodContext.start.Column);

            // Parse parameters
            List<object> parameters = new List<object>();
            var parseParameters = methodContext.expr();
            foreach (var param in parseParameters)
                parameters.Add(ParseParameter(param));

            Element method = (Element)Activator.CreateInstance(methodType);
            method.ParameterValues = parameters.ToArray();

            return method;
        }

        MethodResult ParseCustomMethod(DeltinScriptParser.Custom_methodContext cmContext)
        {
            string methodName = cmContext.PART().GetText();

            var customMethod = CustomMethods.GetCustomMethod(methodName);

            if (customMethod == null)
                throw new SyntaxErrorException($"The custom method {methodName} does not exist.", cmContext.start.Line, cmContext.start.Column);

            var data = customMethod.GetCustomAttribute<CustomMethod>();

            object[] parameters = cmContext.expr().Select(v => ParseParameter(v)).ToArray();

            MethodResult result = (MethodResult)customMethod.Invoke(null, new object[] { IsGlobal, parameters });
            return result;
        }

        object ParseParameter(DeltinScriptParser.ExprContext context)
        {
            object value = null;

            if (context.GetChild(0) is DeltinScriptParser.EnumContext)
            {
                foreach (Type @enum in Constants.EnumParameters)
                {
                    try
                    {
                        value = Enum.Parse(@enum, context.GetText().Split('.').ElementAtOrDefault(1));
                    }
                    catch (Exception ex) when (ex is ArgumentNullException || ex is ArgumentException || ex is OverflowException) { }
                }

                if (value == null)
                    throw new SyntaxErrorException($"Could not parse enum parameter {context.GetText()}.", context.start.Line, context.start.Column);
            }

            else
                value = ParseExpression(context);

            if (value == null)
                throw new SyntaxErrorException("Could not parse parameter.", context.start.Line, context.start.Column);


            return value;
        }
    }

    class Var
    {
        public static Variable Global { get; private set; }
        public static Variable Player { get; private set; }

        private static int NextFreeGlobalIndex { get; set; }
        private static int NextFreePlayerIndex { get; set; }

        public static void Setup(Variable global, Variable player)
        {
            Global = global;
            Player = player;
        }

        public static int Assign(bool isGlobal)
        {
            if (isGlobal)
            {
                int index = NextFreeGlobalIndex;
                NextFreeGlobalIndex++;
                return index;
            }
            else
            {
                int index = NextFreePlayerIndex;
                NextFreePlayerIndex++;
                return index;
            }
        }

        private static Variable GetVar(bool isGlobal)
        {
            if (isGlobal)
                return Global;
            else
                return Player;
        }

        public static Var AssignVar(bool isGlobal)
        {
            return new Var(isGlobal, GetVar(isGlobal), Assign(isGlobal));
        }

        public static Var AssignVarRange(bool isGlobal, int count)
        {
            int start = Assign(isGlobal);
            for (int i = 0; i < count; i++)
                Assign(isGlobal);

            return new Var(isGlobal, GetVar(isGlobal), start, count);
        }

        public static DefinedVar AssignDefinedVar(bool isGlobal, string name, int line, int column)
        {
            return new DefinedVar(name, isGlobal, GetVar(isGlobal), Assign(isGlobal), line, column);
        }



        public bool IsGlobal { get; protected set; }
        public Variable Variable { get; protected set; }

        public bool IsInArray { get; protected set; }
        public int Index { get; protected set; }

        public bool IsSubArray { get; private set; }
        public int Start { get; protected set; }
        public int Count { get; protected set; }

        public Var(bool isGlobal, Variable variable, int index)
        {
            IsGlobal = isGlobal;
            Variable = variable;
            Index = index;
            IsInArray = index != -1;
        }

        public Var(bool isGlobal, Variable variable, int start, int count)
        {
            IsGlobal = isGlobal;
            Variable = Variable;

            IsSubArray = true;
            Start = start;
            Count = count;
        }

        protected Var()
        {}

        public Element GetVariable(Element targetPlayer = null)
        {
            Element element;

            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();

            if (IsInArray)
            {
                if (IsGlobal)
                    element = Element.Part<V_ValueInArray>(Element.Part<V_GlobalVariable>(Variable), new V_Number(Index));
                else
                    element = Element.Part<V_ValueInArray>(Element.Part<V_PlayerVariable>(targetPlayer, Variable), new V_Number(Index));
            }
            else if (IsSubArray)
            {
                if (IsGlobal)
                    element = Element.Part<V_ArraySlice>(Element.Part<V_GlobalVariable>(Variable), new V_Number(Start), new V_Number(Count));
                else
                    element = Element.Part<V_ArraySlice>(Element.Part<V_PlayerVariable>(targetPlayer, Variable), new V_Number(Start), new V_Number(Count));
            }
            else
            {
                if (IsGlobal)
                    element = Element.Part<V_GlobalVariable>(Variable);
                else
                    element = Element.Part<V_PlayerVariable>(targetPlayer, Variable);
            }

            return element;
        }

        public Element SetVariable(Element value, Element targetPlayer = null)
        {
            Element element;

            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();
            
            if (IsInArray)
            {
                if (IsGlobal)
                    element = Element.Part<A_SetGlobalVariableAtIndex>(Variable, new V_Number(Index), value);
                else
                    element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, Variable, new V_Number(Index), value);
            }
            else if (IsSubArray)
            {
                if (IsGlobal)
                    element = Element.Part<A_SetGlobalVariable>(
                        Variable, 
                        Element.Part<V_AppendToArray>(
                            Element.Part<V_AppendToArray>(
                                Element.Part<V_ArraySlice>(Element.Part<V_GlobalVariable>(Variable), new V_Number(0), new V_Number(Start)),
                                value
                            ),
                            Element.Part<V_ArraySlice>(Element.Part<V_GlobalVariable>(Variable), new V_Number(Start), Element.Part<V_CountOf>(value))
                        )
                    );
                else
                    element = Element.Part<A_SetPlayerVariable>(
                        targetPlayer,
                        Variable,
                        Element.Part<V_AppendToArray>(
                            Element.Part<V_AppendToArray>(
                                Element.Part<V_ArraySlice>(Element.Part<V_PlayerVariable>(targetPlayer, Variable), new V_Number(0), new V_Number(Start)),
                                value
                            ),
                            Element.Part<V_ArraySlice>(Element.Part<V_PlayerVariable>(targetPlayer, Variable), new V_Number(Start), Element.Part<V_CountOf>(value))
                        )
                    );
            }
            else
            {
                if (IsGlobal)
                    element = Element.Part<A_SetGlobalVariable>(Variable, value);
                else
                    element = Element.Part<A_SetPlayerVariable>(targetPlayer, Variable, value);
            }

            return element;

        }
    }

    class DefinedVar : Var
    {
        public string Name { get; protected set; }

        public static List<DefinedVar> VarCollection { get; private set; } = new List<DefinedVar>();

        public static bool IsVar(string name)
        {
            return VarCollection.Any(v => v.Name == name);
        }

        public static DefinedVar GetVar(string name)
        {
            return VarCollection.FirstOrDefault(v => v.Name == name);
        }

        public DefinedVar(DeltinScriptParser.VardefineContext vardefine)
        {
            IsGlobal = vardefine.GLOBAL() != null;
            string name = vardefine.PART(0).GetText();

            if (IsVar(name))
                throw new SyntaxErrorException($"The variable {name} was already defined.", vardefine.start.Line, vardefine.start.Column);

            Name = name;

            // Both can be null, or only one can have a value.
            string useVar = vardefine.PART(1)?.GetText();
            var useNumber = vardefine.number();

            // Auto assign
            if (useNumber == null && useVar == null)
            {
                Index = Var.Assign(IsGlobal);

                if (IsGlobal)
                    Variable = Var.Global;
                else
                    Variable = Var.Player;

                IsInArray = true;
            }
            else
            {
                if (useNumber != null)
                {
                    IsInArray = true;
                    string indexString = useNumber.GetText();
                    if (!int.TryParse(indexString, out int index))
                        throw new SyntaxErrorException("Expected number.", useNumber.start.Line, useNumber.start.Column);
                    Index = index;
                }

                if (useVar != null)
                {
                    if (!Enum.TryParse(useVar, out Variable var))
                        throw new SyntaxErrorException("Expected variable.", vardefine.start.Line, vardefine.start.Column);
                    Variable = var;
                }
            }

            VarCollection.Add(this);
        }

        public DefinedVar(string name, bool isGlobal, Variable variable, int index, int line, int column)
        {
            if (IsVar(name))
                throw new SyntaxErrorException($"The variable {name} was already defined.", line, column);

            Name = name;
            IsGlobal = isGlobal;
            Variable = variable;

            if (index != -1)
            {
                IsInArray = true;
                Index = index;
            }

            VarCollection.Add(this);
        }

        public void OutOfScope()
        {
            VarCollection.Remove(this);
        }
    }

    class Visitor : DeltinScriptBaseVisitor<object>
    {
    }
}
