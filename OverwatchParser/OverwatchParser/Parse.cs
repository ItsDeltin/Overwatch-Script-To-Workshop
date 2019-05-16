using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Sharpen;
using OverwatchParser.Elements;

namespace OverwatchParser.Parse
{
    public class Parser
    {
        static Log Log = new Log("Parse");

        public static Rule[] ParseText(string text)
        {
            Console.WriteLine($"- Input:\n{text}\n-");

            AntlrInputStream inputStream = new AntlrInputStream(text);

            // Lexer
            DeltinScriptLexer speakLexer = new DeltinScriptLexer(inputStream);
            CommonTokenStream commonTokenStream = new CommonTokenStream(speakLexer);

            // Parse
            DeltinScriptParser speakParser = new DeltinScriptParser(commonTokenStream);

            // Get context
            DeltinScriptParser.RulesetContext context = speakParser.ruleset();
            Visitor visitor = new Visitor();
            visitor.Visit(context);

            Console.WriteLine(context.ToStringTree(speakParser));

            // Get the internal global variable to use.
            Variable useGlobalVar;
            if (!Enum.TryParse<Variable>(context.useGlobalVar().PART().ToString(), out useGlobalVar))
                throw new SyntaxErrorException("useGlobalVar must be a character.", 0, 0);

            // Get the internal player variable to use.
            Variable usePlayerVar;
            if (!Enum.TryParse<Variable>(context.usePlayerVar().PART().ToString(), out usePlayerVar))
                throw new SyntaxErrorException("usePlayerVar must be a character.", 0, 0);

            // Parse the rules.
            var rules = context.ow_rule();
            var compiledRules = new List<Rule>();

            for (int i = 0; i < rules.Length; i++)
                compiledRules.Add(ParseRule(rules[i]));

            return compiledRules.ToArray();
        }

        static Rule ParseRule(DeltinScriptParser.Ow_ruleContext ruleContext)
        {
            Rule rule = CreateRuleFromContext(ruleContext);

            // Determines if the starting skip if was created (used by for and goto)
            bool beginningSkipWasCreated = false;

            // The list of actions.
            List<Element> actions = new List<Element>();

            var statements = ruleContext.block().children;
            for (int i = 0; i < statements.Count; i++)
            {
                // If the statement is a method.
                if (statements[i].GetChild(0) is DeltinScriptParser.MethodContext)
                    actions.Add(ParseMethod(statements[i].GetChild(0) as DeltinScriptParser.MethodContext));
            }

            rule.Actions = actions.ToArray();

            return rule;
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

            Log.Write($"Rule: {ruleName}");

            return new Rule(ruleName, ruleEvent, team, player);
        }

        static Element ParseExpression(DeltinScriptParser.ExprContext context)
        {
            // If the expression is a(n)...

            #region Operation

            //   0       1      2
            // (expr operation expr)
            // count == 3
            if (context.ChildCount == 3)
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

                int num;
                // num will have the format expr(number(X)) if positive, expr(number(neg(X))) if negative.
                if (number.GetChild(0) is DeltinScriptParser.NegContext)
                    // Is negative, use '-' before int.parse to make it negative.
                    num = -int.Parse
                        ((number.GetChild(0).GetChild(0) as DeltinScriptParser.NegContext)
                        .NUMBER().GetText());
                else
                    // Is positive
                    num = int.Parse
                        ((number.GetChild(0) as DeltinScriptParser.NumberContext)
                        .NUMBER().GetText());

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

            throw new Exception($"What's a {context}?");
        }

        static Element ParseMethod(DeltinScriptParser.MethodContext methodContext)
        {
            string methodName = methodContext.PART().GetText();
            Type methodType = Element.GetMethod(methodName);

            if (methodType == null)
                throw new SyntaxErrorException($"The method {methodName} does not exist.", methodContext.start.Line, methodContext.start.Column);

            Element[] parameters = methodContext.expr().Select(v => ParseExpression(v))
                .ToArray();

            Element method = (Element)Activator.CreateInstance(methodType);
            method.ParameterValues = parameters;

            return method;
        }
    }

    class Visitor : DeltinScriptBaseVisitor<object>
    {
    }

    
}
