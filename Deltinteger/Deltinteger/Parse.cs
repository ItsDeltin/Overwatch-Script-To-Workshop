using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Antlr4;
using Antlr4.Runtime;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class Parser
    {
        static Log Log = new Log("Parse");

        public static Rule[] ParseText(string text)
        {
            AntlrInputStream inputStream = new AntlrInputStream(text);

            // Lexer
            DeltinScriptLexer lexer = new DeltinScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);

            // Parse
            DeltinScriptParser parser = new DeltinScriptParser(commonTokenStream);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ErrorListener());

            // Get context
            DeltinScriptParser.RulesetContext context = parser.ruleset();

            //PrintContext(context);
            Console.WriteLine(context.ToStringTree(parser));

            Visitor visitor = new Visitor();
            visitor.Visit(context);

            {
                // Get the internal global variable to use.
                if (!Enum.TryParse(context.useGlobalVar().PART().ToString(), out Variable useGlobalVar))
                    throw new SyntaxErrorException("useGlobalVar must be a character.", context.useGlobalVar().start);

                // Get the internal player variable to use.
                if (!Enum.TryParse(context.usePlayerVar().PART().ToString(), out Variable usePlayerVar))
                    throw new SyntaxErrorException("usePlayerVar must be a character.", context.usePlayerVar().start);

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

        static void PrintContext(ParserRuleContext context)
        {
            if (context == null)
                return;
            Log.Write($"{new string(' ', (context.Depth() - 1) * 4)}{context.GetType().Name} [{context.start.Line}, {context.start.Column}] {context.GetText()}");
            foreach (var child in context.children)
                PrintContext(child as ParserRuleContext);
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
                    string type = arg.GetText().Split('.').ElementAtOrDefault(0);
                    string name = arg.GetText().Split('.').ElementAtOrDefault(1);

                    if (type == "Event")
                    {
                        if (Enum.TryParse(name, out RuleEvent setEvent))
                            ruleEvent = setEvent;
                        else
                            throw new SyntaxErrorException($"Unknown event type \"{arg.GetText()}\".", arg.start);
                    }
                    else if (type == "Team")
                    {
                        if (Enum.TryParse(name, out TeamSelector setTeam))
                            team = setTeam;
                        else
                            throw new SyntaxErrorException($"Unknown team type \"{arg.GetText()}\".", arg.start);
                    }
                    else if (type == "Player")
                    {
                        if (Enum.TryParse(name, out PlayerSelector setPlayer))
                            player = setPlayer;
                        else
                            throw new SyntaxErrorException($"Unknown player type \"{arg.GetText()}\".", arg.start);
                    }
                    else
                        throw new SyntaxErrorException($"Unknown rule argument \"{arg.GetText()}\".", arg.start);
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
                Actions.Add(ParseMethod(statementContext.GetChild(0) as DeltinScriptParser.MethodContext, false));
                return;
            }
            #endregion

            #region Variable set

            if (statementContext.STATEMENT_OPERATION() != null)
            {
                DefinedVar variable;
                Element target;
                Element index = null;
                string operation = statementContext.STATEMENT_OPERATION().GetText();

                Element value;

                value = ParseExpression(statementContext.expr(1) as DeltinScriptParser.ExprContext);

                /*  Format if the variable has an expression beforehand (sets the target player)
                                 expr(0)           .ChildCount
                                   v                   v
                    Statement (    v                   v          ) | Operation | Set to variable
                               Variable to set (       v         )
                                   ^            expr | . | expr
                                   ^                   ^
                                 expr(0)          .GetChild(1) == '.'                               */
                if (statementContext.expr(0).ChildCount == 3
                    && statementContext.expr(0).GetChild(1).GetText() == ".")
                {
                    /*  Get Variable:  .expr(0)              .expr(1)
                                         v                     v  .expr(1) (if the value to be set is an array)
                        Statement (      v                     v      v    ) | Operation | Set to variable
                                   Variable to set (           v      v  )
                                         ^          expr | . | expr | []
                                         ^           ^
                        Get  Target:  .expr(0)    .expr(0)                                            */

                    variable = DefinedVar.GetVar(statementContext.expr(0).expr(1).GetChild(0).GetText(),
                                                 statementContext.expr(0).expr(1).start);
                    target = ParseExpression(statementContext.expr(0).expr(0));

                    // Get the index if the variable has []
                    var indexExpression = statementContext.expr(0).expr(1).expr(1);
                    if (indexExpression != null)
                        index = ParseExpression(indexExpression);
                }
                else
                {
                    /*               .expr(0)             .expr(1)
                                        v                   v 
                        Statement (     v                   v  ) | Operation | Set to variable
                                   Variable to set (expr) | []
                    */
                    variable = DefinedVar.GetVar(statementContext.expr(0).GetChild(0).GetText(),
                                                 statementContext.expr(0).start);
                    target = new V_EventPlayer();

                    // Get the index if the variable has []
                    var indexExpression = statementContext.expr(0).expr(1);
                    if (indexExpression != null)
                        index = ParseExpression(indexExpression);
                }

                switch (operation)
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
                    token    : statementContext.@for().start
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
            if (context.ChildCount == 3
                &&(Constants.   MathOperations.Contains(context.GetChild(1).GetText())
                || Constants.CompareOperations.Contains(context.GetChild(1).GetText())
                || Constants.   BoolOperations.Contains(context.GetChild(1).GetText())))
            {
                Element left = ParseExpression(context.GetChild(0) as DeltinScriptParser.ExprContext);
                string operation = context.GetChild(1).GetText();
                Element right = ParseExpression(context.GetChild(2) as DeltinScriptParser.ExprContext);

                if (Constants.BoolOperations.Contains(context.GetChild(1).GetText()))
                {
                    if (left.ElementData.ValueType != Elements.ValueType.Any && left.ElementData.ValueType != Elements.ValueType.Boolean)
                        throw new SyntaxErrorException($"Expected boolean datatype, got {left .ElementData.ValueType.ToString()} instead.", context.start);
                    if (right.ElementData.ValueType != Elements.ValueType.Any && right.ElementData.ValueType != Elements.ValueType.Boolean)
                        throw new SyntaxErrorException($"Expected boolean datatype, got {right.ElementData.ValueType.ToString()} instead.", context.start);
                }

                switch (operation)
                {
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
                return V_String.ParseString(
                    context.start,
                    // String will look like "hey this is the contents", trim the quotes.
                    (context.GetChild(0) as DeltinScriptParser.StringContext).STRINGLITERAL().GetText().Trim('\"'),
                    null
                );
            }

            #endregion

            #region Formatted String

            if (context.GetChild(1) is DeltinScriptParser.StringContext)
            {
                Element[] values = context.expr().Select(expr => ParseExpression(expr)).ToArray();
                return V_String.ParseString(
                    context.start,
                    (context.GetChild(1) as DeltinScriptParser.StringContext).STRINGLITERAL().GetText().Trim('\"'),
                    values
                    );
            }

            #endregion

            #region null

            if (context.GetChild(0) is DeltinScriptParser.NullContext)
                return new V_Null();

            #endregion

            #region Group ( expr )

            if (context.ChildCount == 3 && context.GetChild(0).GetText() == "(" &&
                context.GetChild(1) is DeltinScriptParser.ExprContext &&
                context.GetChild(2).GetText() == ")")
            {
                Console.WriteLine("Group type:" + context.GetChild(0).GetType());
                return ParseExpression(context.GetChild(1) as DeltinScriptParser.ExprContext);
            }

            #endregion

            #region Method

            if (context.GetChild(0) is DeltinScriptParser.MethodContext)
                return ParseMethod(context.GetChild(0) as DeltinScriptParser.MethodContext, true);

            #endregion

            #region Variable

            if (context.GetChild(0) is DeltinScriptParser.VariableContext)
                return DefinedVar.GetVar((context.GetChild(0) as DeltinScriptParser.VariableContext).PART().GetText(), context.start).GetVariable(new V_EventPlayer());

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
                    current = new V_AppendToArray()
                    {
                        ParameterValues = new object[2]
                    };

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

            #region Empty Array

            if (context.ChildCount == 2 && context.GetText() == "[]")
                return Element.Part<V_EmptyArray>();

            #endregion

            #region Seperator/enum

            if (context.ChildCount == 3 && context.GetChild(1).GetText() == ".")
            {
                Element left = ParseExpression(context.GetChild(0) as DeltinScriptParser.ExprContext);
                string variableName = context.GetChild(2).GetChild(0).GetText();

                DefinedVar var = DefinedVar.GetVar(variableName, context.start);

                return var.GetVariable(left);
            }

            #endregion

            throw new Exception($"What's a {context.GetType().Name}?");
        }

        Element ParseMethod(DeltinScriptParser.MethodContext methodContext, bool needsToBeValue)
        {
            // Get the method name
            string methodName = methodContext.PART().GetText();

            // Get the method type.
            Type methodType = Element.GetMethod(methodName);
            MethodInfo customMethod = CustomMethods.GetCustomMethod(methodName);

            if (methodType != null && customMethod != null)
                throw new Exception("Conflicting Overwatch method and custom method.");

            if (methodType == null && customMethod == null)
                throw new SyntaxErrorException($"The method {methodName} does not exist.", methodContext.start);

            bool isCustomMethod = methodType == null;

            // Parse parameters
            var parseParameters = methodContext.expr();
            Parameter[] parameterData;

            string fullMethodName;

            Element method = null;
            if (!isCustomMethod)
            {
                parameterData = methodType.GetCustomAttributes<Parameter>().ToArray();
                method = (Element)Activator.CreateInstance(methodType);
                fullMethodName = method.ToString();
            }
            else
            {
                parameterData = customMethod.GetCustomAttributes<Parameter>().ToArray();
                fullMethodName = CustomMethods.GetName(customMethod);
            }

            if (parseParameters.Length > parameterData.Length)
                throw new SyntaxErrorException($"Too many arguments in the method {methodName} which only takes {parameterData.Length} parameters.", methodContext.start);

            List<object> finalParameters = new List<object>();
            for (int i = 0; i < parseParameters.Length; i++)
                finalParameters.Add(ParseParameter(parseParameters[i], fullMethodName, parameterData[i]));

            if (isCustomMethod)
            {
                MethodResult result = (MethodResult)customMethod.Invoke(null, new object[] { IsGlobal, finalParameters.ToArray() });

                switch (result.MethodType)
                {
                    case CustomMethodType.Action:
                        if (needsToBeValue)
                            throw new IncorrectElementTypeException(fullMethodName, true);
                        break;

                    case CustomMethodType.MultiAction_Value:
                    case CustomMethodType.Value:
                        if (!needsToBeValue)
                            throw new IncorrectElementTypeException(fullMethodName, false);
                        break;
                }

                if (result.Elements != null)
                    Actions.AddRange(result.Elements);
                finalParameters = null;
                method = result.Result;
            }
            else
                method.ParameterValues = finalParameters?.ToArray();

            return method;
        }

        object ParseParameter(DeltinScriptParser.ExprContext context, string methodName, Parameter parameterData)
        {
            object value = null;

            if (context.GetChild(0) is DeltinScriptParser.EnumContext)
            {
                if (parameterData.ParameterType != ParameterType.Enum)
                    throw new SyntaxErrorException($"Expected value type \"{parameterData.ValueType.ToString()}\" on {methodName}'s parameter \"{parameterData.Name}\"."
                        , context.start);

                bool invalidType = context.GetText().Split('.').ElementAtOrDefault(0) != parameterData.EnumType.Name;
                if (!invalidType)
                    try
                    {
                        value = Enum.Parse(parameterData.EnumType, context.GetText().Split('.').ElementAtOrDefault(1));
                    }
                    catch (Exception ex) when (ex is ArgumentNullException || ex is ArgumentException || ex is OverflowException)
                    {
                        invalidType = true;
                    }

                if (invalidType)
                    throw new SyntaxErrorException($"Expected enum type \"{parameterData.EnumType.ToString()}\" on {methodName}'s parameter \"{parameterData.Name}\"."
                        , context.start);

                if (value == null)
                    throw new SyntaxErrorException($"Could not parse enum parameter {context.GetText()}."
                        , context.start);
            }

            else
            {
                if (parameterData.ParameterType != ParameterType.Value)
                    throw new SyntaxErrorException($"Expected enum type \"{parameterData.EnumType.Name}\" on {methodName}'s parameter \"{parameterData.Name}\"."
                        , context.start);

                value = ParseExpression(context);

                Element element = value as Element;
                ElementData elementData = element.GetType().GetCustomAttribute<ElementData>();

                if (elementData.ValueType != Elements.ValueType.Any &&
                    !parameterData.ValueType.HasFlag(elementData.ValueType))
                    throw new SyntaxErrorException($"Expected value type \"{parameterData.ValueType.ToString()}\" on {methodName}'s parameter \"{parameterData.Name}\", got \"{elementData.ValueType.ToString()}\" instead."
                        , context.start);
            }

            if (value == null)
                throw new SyntaxErrorException("Could not parse parameter.", context.start);


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

        public static DefinedVar AssignDefinedVar(bool isGlobal, string name, IToken token)
        {
            return new DefinedVar(name, isGlobal, GetVar(isGlobal), Assign(isGlobal), token);
        }



        public bool IsGlobal { get; protected set; }
        public Variable Variable { get; protected set; }

        public bool IsInArray { get; protected set; }
        public int Index { get; protected set; }

        public Var(bool isGlobal, Variable variable, int index)
        {
            IsGlobal = isGlobal;
            Variable = variable;
            Index = index;
            IsInArray = index != -1;
        }

        protected Var()
        {}

        public Element GetVariable(Element targetPlayer = null, Element getAiIndex = null)
        {
            Element element;

            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();

            if (getAiIndex == null)
            {
                if (IsInArray)
                {
                    if (IsGlobal)
                        element = Element.Part<V_ValueInArray>(Element.Part<V_GlobalVariable>(Variable), new V_Number(Index));
                    else
                        element = Element.Part<V_ValueInArray>(Element.Part<V_PlayerVariable>(targetPlayer, Variable), new V_Number(Index));
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<V_GlobalVariable>(Variable);
                    else
                        element = Element.Part<V_PlayerVariable>(targetPlayer, Variable);
                }
            }
            else
            {
                if (IsInArray)
                {
                    if (IsGlobal)
                        element = Element.Part<V_ValueInArray>(Element.Part<V_ValueInArray>(Element.Part<V_GlobalVariable>(Variable)), new V_Number(Index));
                    else
                        element = Element.Part<V_ValueInArray>(Element.Part<V_ValueInArray>(Element.Part<V_PlayerVariable>(targetPlayer, Variable)), new V_Number(Index));
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<V_ValueInArray>(Element.Part<V_GlobalVariable>(Variable), getAiIndex);
                    else
                        element = Element.Part<V_ValueInArray>(Element.Part<V_PlayerVariable>(targetPlayer, Variable), getAiIndex);
                }
            }

            return element;
        }

        public Element SetVariable(Element value, Element targetPlayer = null, Element setAtIndex = null)
        {
            Element element;

            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();

            if (setAtIndex == null)
            {
                if (IsInArray)
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(Variable, new V_Number(Index), value);
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, Variable, new V_Number(Index), value);
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariable>(Variable, value);
                    else
                        element = Element.Part<A_SetPlayerVariable>(targetPlayer, Variable, value);
                }
            }
            else
            {
                if (IsInArray)
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(Variable, new V_Number(Index), 
                            Element.Part<V_AppendToArray>(
                                Element.Part<V_AppendToArray>(
                                    Element.Part<V_ArraySlice>(GetVariable(targetPlayer), new V_Number(0), setAtIndex), 
                                    value),
                            Element.Part<V_ArraySlice>(GetVariable(targetPlayer), Element.Part<V_Add>(setAtIndex, new V_Number(1)), new V_Number(9999))));
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, Variable, new V_Number(Index),
                            Element.Part<V_AppendToArray>(
                                Element.Part<V_AppendToArray>(
                                    Element.Part<V_ArraySlice>(GetVariable(targetPlayer), new V_Number(0), setAtIndex),
                                    value),
                            Element.Part<V_ArraySlice>(GetVariable(targetPlayer), Element.Part<V_Add>(setAtIndex, new V_Number(1)), new V_Number(9999))));
                }
                else
                {
                    if (IsGlobal)
                        element = Element.Part<A_SetGlobalVariableAtIndex>(Variable, setAtIndex, value);
                    else
                        element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, Variable, setAtIndex, value);
                }
            }

            return element;

        }
    }

    class DefinedVar : Var
    {
        public string Name { get; protected set; }

        public static List<DefinedVar> VarCollection { get; private set; } = new List<DefinedVar>();

        private static bool IsVar(string name)
        {
            return VarCollection.Any(v => v.Name == name);
        }

        public static DefinedVar GetVar(string name, IToken token)
        {
            DefinedVar var = VarCollection.FirstOrDefault(v => v.Name == name);

            if (var == null)
                throw new SyntaxErrorException($"The variable {name} does not exist.", token);

            return var;
        }

        public DefinedVar(DeltinScriptParser.VardefineContext vardefine)
        {
            IsGlobal = vardefine.GLOBAL() != null;
            string name = vardefine.PART(0).GetText();

            if (IsVar(name))
                throw new SyntaxErrorException($"The variable {name} was already defined.", vardefine.start);

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
                        throw new SyntaxErrorException("Expected number.", useNumber.start);
                    Index = index;
                }

                if (useVar != null)
                {
                    if (!Enum.TryParse(useVar, out Variable var))
                        throw new SyntaxErrorException("Expected variable.", vardefine.start);
                    Variable = var;
                }
            }

            VarCollection.Add(this);
        }

        public DefinedVar(string name, bool isGlobal, Variable variable, int index, IToken token)
        {
            if (IsVar(name))
                throw new SyntaxErrorException($"The variable {name} was already defined.", token);

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

    public class ErrorListener : BaseErrorListener
    {
        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            throw new SyntaxErrorException(msg, offendingSymbol);
        }
    }

    class Visitor : DeltinScriptBaseVisitor<object>
    {
    }
}
