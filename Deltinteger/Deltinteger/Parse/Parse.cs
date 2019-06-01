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
            Log.Write(LogLevel.Verbose, context.ToStringTree(parser));

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
                new DefinedVar(ScopeGroup.Root, vardefine[i]);

            // Get the user methods.
            var userMethods = context.user_method();

            for (int i = 0; i < userMethods.Length; i++)
                new UserMethod(userMethods[i]); 

            // Parse the rules.
            var rules = context.ow_rule();
            var compiledRules = new List<Rule>();

            for (int i = 0; i < rules.Length; i++)
            {
                ParseRule parsing = new ParseRule(rules[i]);

                Log.Write(LogLevel.Normal, $"Building rule: {parsing.Rule.Name}");
                parsing.Parse();
                Rule rule = parsing.Rule;

                compiledRules.Add(rule);
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

        private readonly List<A_Skip> ReturnSkips = new List<A_Skip>(); // Return statements whos skip count needs to be filled out.

        private ContinueSkip ContinueSkip; // Contains data about the wait/skip for continuing loops.

        public ParseRule(DeltinScriptParser.Ow_ruleContext ruleContext)
        {
            Rule = CreateRuleFromContext(ruleContext);
            RuleContext = ruleContext;
            IsGlobal = Rule.RuleEvent == RuleEvent.Ongoing_Global;
            ContinueSkip = new ContinueSkip(IsGlobal, Actions);
        }

        public void Parse()
        {
            // Parse conditions
            ParseConditions();
            
            // Parse actions
            ParseBlock(ScopeGroup.Root.Child(), RuleContext.block(), true);

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
            var conditions = RuleContext.rule_if()?.expr();
            
            if (conditions != null)
                foreach(var expr in conditions)
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

        Element ParseBlock(ScopeGroup scopeGroup, DeltinScriptParser.BlockContext blockContext, bool fulfillReturns)
        {
            int returnSkipStart = ReturnSkips.Count;

            Var returned = null;
            if (fulfillReturns)
                returned = Var.AssignVar(IsGlobal);

            var statements = blockContext.children
                .Where(v => v is DeltinScriptParser.StatementContext)
                .Cast<DeltinScriptParser.StatementContext>().ToArray();

            for (int i = 0; i < statements.Length; i++)
                ParseStatement(scopeGroup, statements[i], returned, i == statements.Length - 1);

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

        void ParseStatement(ScopeGroup scope, DeltinScriptParser.StatementContext statementContext, Var returned, bool isLast)
        {
            #region Method
            if (statementContext.GetChild(0) is DeltinScriptParser.MethodContext)
            {
                Element method = ParseMethod(scope, statementContext.GetChild(0) as DeltinScriptParser.MethodContext, false);
                if (method != null)
                    Actions.Add(method);
                return;
            }
            #endregion

            #region Variable set
            else if (statementContext.statement_operation() != null)
            {
                DefinedVar variable;
                Element target;
                Element index = null;
                string operation = statementContext.statement_operation().GetText();

                Element value;

                value = ParseExpression(scope, statementContext.expr(1) as DeltinScriptParser.ExprContext);

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

                    variable = scope.GetVar(statementContext.expr(0).expr(1).GetChild(0).GetText(),
                                                 statementContext.expr(0).expr(1).start);
                    target = ParseExpression(scope, statementContext.expr(0).expr(0));

                    // Get the index if the variable has []
                    var indexExpression = statementContext.expr(0).expr(1).expr(1);
                    if (indexExpression != null)
                        index = ParseExpression(scope, indexExpression);
                }
                else
                {
                    /*               .expr(0)             .expr(1)
                                        v                   v 
                        Statement (     v                   v  ) | Operation | Set to variable
                                   Variable to set (expr) | []
                    */
                    variable = scope.GetVar(statementContext.expr(0).GetChild(0).GetText(),
                                                 statementContext.expr(0).start);
                    target = new V_EventPlayer();

                    // Get the index if the variable has []
                    var indexExpression = statementContext.expr(0).expr(1);
                    if (indexExpression != null)
                        index = ParseExpression(scope, indexExpression);
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
            else if (statementContext.GetChild(0) is DeltinScriptParser.ForContext)
            {
                ContinueSkip.Setup();

                // The action the for loop starts on.
                // +1 for the counter reset.
                int forActionStartIndex = Actions.Count() - 1;

                // The target array in the for statement.
                Element forArrayElement = ParseExpression(scope, statementContext.@for().expr());

                ScopeGroup forGroup = scope.Child();

                // Create the for's temporary variable.
                DefinedVar forTempVar = Var.AssignDefinedVar(
                    scopeGroup: forGroup,
                    name      : statementContext.@for().PART().GetText(),
                    isGlobal  : IsGlobal,
                    token     : statementContext.@for().start
                    );

                // Reset the counter.
                Actions.Add(forTempVar.SetVariable(new V_Number(0)));

                // Parse the for's block.
                ParseBlock(forGroup, statementContext.@for().block(), false);

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

            #endregion

            #region if

            else if (statementContext.GetChild(0) is DeltinScriptParser.IfContext)
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

                // Add if's SkipIf action.
                A_SkipIf if_SkipIf = new A_SkipIf();
                Actions.Add(if_SkipIf);

                using (var ifScope = scope.Child())
                {
                    // Parse the if body.
                    ParseBlock(ifScope, statementContext.@if().block(), false);
                }

                // Determines if the "Skip" action after the if block will be created.
                // Only if there is if-else or else statements.
                bool addIfSkip = statementContext.@if().else_if().Count() > 0 || statementContext.@if().@else() != null;

                // Update the initial SkipIf's skip count now that we know the number of actions the if block has.
                // Add one to the body length if a Skip action is going to be added.
                if_SkipIf.ParameterValues = new object[]
                {
                    Element.Part<V_Not>(ParseExpression(scope, statementContext.@if().expr())),
                    new V_Number(Actions.Count - 1 - Actions.IndexOf(if_SkipIf) + (addIfSkip ? 1 : 0))
                };

                // Create the "Skip" action.
                A_Skip if_Skip = new A_Skip();
                if (addIfSkip)
                {
                    Actions.Add(if_Skip);
                }

                // Parse else-ifs
                var skipIfContext = statementContext.@if().else_if();
                A_Skip[] elseif_Skips = new A_Skip[skipIfContext.Length]; // The ElseIf's skips
                for (int i = 0; i < skipIfContext.Length; i++)
                {
                    // Create the SkipIf action for the else if.
                    A_SkipIf elseif_SkipIf = new A_SkipIf();
                    Actions.Add(elseif_SkipIf);

                    // Parse the else-if body.
                    using (var elseifScope = scope.Child())
                    {
                        ParseBlock(elseifScope, skipIfContext[i].block(), false);
                    }

                    // Determines if the "Skip" action after the else-if block will be created.
                    // Only if there is additional if-else or else statements.
                    bool addIfElseSkip = i < skipIfContext.Length - 1 || statementContext.@if().@else() != null;

                    // Set the SkipIf's parameters.
                    elseif_SkipIf.ParameterValues = new object[]
                    {
                        Element.Part<V_Not>(ParseExpression(scope, skipIfContext[i].expr())),
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
                if (statementContext.@if().@else() != null)
                    using (var elseScope = scope.Child())
                        ParseBlock(elseScope, statementContext.@if().@else().block(), false);

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

            #endregion

            #region return
            else if (statementContext.RETURN() != null)
            {
                // Will have a value if the statement is "return value;", will be null if the statement is "return;".
                var returnExpr = statementContext.expr()?.FirstOrDefault();

                if (returnExpr != null)
                {
                    Element result = ParseExpression(scope, returnExpr);
                    Actions.Add(returned.SetVariable(result));
                }

                if (!isLast)
                {
                    A_Skip returnSkip = new A_Skip();
                    Actions.Add(returnSkip);
                    ReturnSkips.Add(returnSkip);
                }

                return;
            }
            #endregion

            #region define
            else if (statementContext.define() != null)
            {
                string variableName = statementContext.define().PART().GetText();
                // var has 3 different meanings here, have fun!
                var var = Var.AssignDefinedVar(scope, IsGlobal, variableName, statementContext.start);

                // Set the defined variable if the variable is defined like "define var = 1"
                var setTo = statementContext.define().expr();
                if (setTo != null)
                    Actions.Add(var.SetVariable(ParseExpression(scope, setTo)));

                return;
            }
            #endregion

            throw new Exception($"What's a {statementContext.GetChild(0)} ({statementContext.GetChild(0).GetType()})?");
        }

        Element ParseExpression(ScopeGroup scope, DeltinScriptParser.ExprContext context)
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
                Element left = ParseExpression(scope, context.GetChild(0) as DeltinScriptParser.ExprContext);
                string operation = context.GetChild(1).GetText();
                Element right = ParseExpression(scope, context.GetChild(2) as DeltinScriptParser.ExprContext);

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
                return Element.Part<V_Not>(ParseExpression(scope, context.GetChild(1) as DeltinScriptParser.ExprContext));

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
                Element[] values = context.expr().Select(expr => ParseExpression(scope, expr)).ToArray();
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
                return ParseExpression(scope, context.GetChild(1) as DeltinScriptParser.ExprContext);

            #endregion

            #region Method

            if (context.GetChild(0) is DeltinScriptParser.MethodContext)
                return ParseMethod(scope, context.GetChild(0) as DeltinScriptParser.MethodContext, true);

            #endregion

            #region Variable

            if (context.GetChild(0) is DeltinScriptParser.VariableContext)
                return scope.GetVar((context.GetChild(0) as DeltinScriptParser.VariableContext).PART().GetText(), context.start).GetVariable(new V_EventPlayer());

            #endregion

            #region Array

            if (context.ChildCount == 4 && context.GetChild(1).GetText() == "[" && context.GetChild(3).GetText() == "]")
                return Element.Part<V_ValueInArray>(
                    ParseExpression(scope, context.expr(0) as DeltinScriptParser.ExprContext),
                    ParseExpression(scope, context.expr(1) as DeltinScriptParser.ExprContext));

            #endregion

            #region Create Array

            if (context.ChildCount >= 4 && context.GetChild(0).GetText() == "[")
            {
                var expressions = context.expr();
                V_Append prev = null;
                V_Append current = null;

                for (int i = 0; i < expressions.Length; i++)
                {
                    current = new V_Append()
                    {
                        ParameterValues = new object[2]
                    };

                    if (prev != null)
                        current.ParameterValues[0] = prev;
                    else
                        current.ParameterValues[0] = new V_EmptyArray();

                    current.ParameterValues[1] = ParseExpression(scope, expressions[i]);
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
                Element left = ParseExpression(scope, context.GetChild(0) as DeltinScriptParser.ExprContext);
                string variableName = context.GetChild(2).GetChild(0).GetText();

                DefinedVar var = scope.GetVar(variableName, context.start);

                return var.GetVariable(left);
            }

            #endregion

            throw new Exception($"Failed to parse element: {context.GetType().Name} at {context.start.Line}, {context.start.Column}");
        }

        Element ParseMethod(ScopeGroup scope, DeltinScriptParser.MethodContext methodContext, bool needsToBeValue)
        {
            // Get the method name
            string methodName = methodContext.PART().GetText();

            // Get the kind of method the method is (Method (Overwatch), Custom Method, or User Method.)
            var methodType = GetMethodType(methodName);
            if (methodType == null)
                throw new SyntaxErrorException($"The method {methodName} does not exist.", methodContext.start);

            // Get the parameters
            var parameters = methodContext.expr();

            Element method;

            switch (methodType)
            {
                case MethodType.Method:
                {
                    Type owMethod = Element.GetMethod(methodName);

                    method = (Element)Activator.CreateInstance(owMethod);
                    Parameter[] parameterData = owMethod.GetCustomAttributes<Parameter>().ToArray();
                    //object[] parsedParameters = new Element[parameterData.Length];
                    List<object> parsedParameters = new List<object>();

                    for (int i = 0; i < parameterData.Length; i++)
                    {
                        if (parameters.Length > i)
                        {
                            //parsedParameters[i] = ParseParameter(parameters[i], methodName, parameterData[i]);
                            parsedParameters.Add(ParseParameter(scope, parameters[i], methodName, parameterData[i]));
                        }
                        else 
                        {
                            if (parameterData[i].ParameterType == ParameterType.Value && parameterData[i].DefaultType == null)
                                throw new SyntaxErrorException($"Missing parameter \"{parameterData[i].Name}\" in the method \"{methodName}\" and no default type to fallback on.", 
                                    methodContext.start);
                            else
                                //parsedParameters[i] = parameterData[i].GetDefault();
                                parsedParameters.Add(parameterData[i].GetDefault());
                        }
                    }

                    method.ParameterValues = parsedParameters.ToArray();
                    break;
                }

                case MethodType.CustomMethod:
                {
                    MethodInfo customMethod = CustomMethods.GetCustomMethod(methodName);
                    Parameter[] parameterData = customMethod.GetCustomAttributes<Parameter>().ToArray();
                    object[] parsedParameters = new Element[parameterData.Length];

                    for (int i = 0; i < parameterData.Length; i++)
                        if (parameters.Length > i)
                            parsedParameters[i] = ParseParameter(scope, parameters[i], methodName, parameterData[i]);
                        else
                            throw new SyntaxErrorException($"Missing parameter \"{parameterData[i].Name}\" in the method \"{methodName}\" and no default type to fallback on.", 
                                methodContext.start);

                    MethodResult result = (MethodResult)customMethod.Invoke(null, new object[] { IsGlobal, parsedParameters });
                    switch (result.MethodType)
                    {
                        case CustomMethodType.Action:
                            if (needsToBeValue)
                                throw new IncorrectElementTypeException(methodName, true);
                            break;

                        case CustomMethodType.MultiAction_Value:
                        case CustomMethodType.Value:
                            if (!needsToBeValue)
                                throw new IncorrectElementTypeException(methodName, false);
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
                        UserMethod userMethod = UserMethod.GetUserMethod(methodName);

                        // Add the parameter variables to the scope.
                        DefinedVar[] parameterVars = new DefinedVar[userMethod.Parameters.Length];
                        for (int i = 0; i < parameterVars.Length; i++)
                        {
                            if (parameters.Length > i)
                            {
                                // Create a new variable using the parameter input.
                                parameterVars[i] = DefinedVar.AssignDefinedVar(methodScope, IsGlobal, userMethod.Parameters[i].Name, methodContext.start);
                                Actions.Add(parameterVars[i].SetVariable(ParseExpression(scope, parameters[i])));
                            }
                            else throw new SyntaxErrorException($"Missing parameter \"{userMethod.Parameters[i].Name}\" in the method \"{methodName}\".",
                                methodContext.start);
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

        object ParseParameter(ScopeGroup scope, DeltinScriptParser.ExprContext context, string methodName, Parameter parameterData)
        {
            object value = null;

            if (context.GetChild(0) is DeltinScriptParser.EnumContext)
            {
                if (parameterData.ParameterType != ParameterType.Enum)
                    throw new SyntaxErrorException($"Expected value type \"{parameterData.ValueType.ToString()}\" on {methodName}'s parameter \"{parameterData.Name}\"."
                        , context.start);

                string type = context.GetText().Split('.').ElementAtOrDefault(0);
                string enumValue = context.GetText().Split('.').ElementAtOrDefault(1);

                if (type != parameterData.EnumType.Name)
                    throw new SyntaxErrorException($"Expected enum type \"{parameterData.EnumType.ToString()}\" on {methodName}'s parameter \"{parameterData.Name}\"."
                        , context.start);

                try
                {
                    value = Enum.Parse(parameterData.EnumType, enumValue);
                }
                catch (Exception ex) when (ex is ArgumentNullException || ex is ArgumentException || ex is OverflowException)
                {
                    throw new SyntaxErrorException($"The value {enumValue} does not exist in the enum {type}."
                        , context.start);
                }

                if (value == null)
                    throw new SyntaxErrorException($"Could not parse enum parameter {context.GetText()}."
                        , context.start);
            }

            else
            {
                if (parameterData.ParameterType != ParameterType.Value)
                    throw new SyntaxErrorException($"Expected enum type \"{parameterData.EnumType.Name}\" on {methodName}'s parameter \"{parameterData.Name}\"."
                        , context.start);

                value = ParseExpression(scope, context);

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

        enum MethodType
        {
            Method,
            CustomMethod,
            UserMethod
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
