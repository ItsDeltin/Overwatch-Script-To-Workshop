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

            InternalVars iv;
            {
                // Get the internal global variable to use.
                Variable useGlobalVar;
                if (!Enum.TryParse<Variable>(context.useGlobalVar().PART().ToString(), out useGlobalVar))
                    throw new SyntaxErrorException("useGlobalVar must be a character.", 0, 0);

                // Get the internal player variable to use.
                Variable usePlayerVar;
                if (!Enum.TryParse<Variable>(context.usePlayerVar().PART().ToString(), out usePlayerVar))
                    throw new SyntaxErrorException("usePlayerVar must be a character.", 0, 0);

                iv = new InternalVars(useGlobalVar, usePlayerVar);
            }

            // Get the defined variables.
            var vardefine = context.vardefine();

            for (int i = 0; i < vardefine.Length; i++)
                // The new var is stored in Var.VarCollection
                new Var(vardefine[i], iv);

            // Parse the rules.
            var rules = context.ow_rule();
            var compiledRules = new List<Rule>();

            for (int i = 0; i < rules.Length; i++)
                compiledRules.Add(ParseRule(rules[i]));

            Log.Write("Build succeeded.");

            return compiledRules.ToArray();
        }

        static Rule ParseRule(DeltinScriptParser.Ow_ruleContext ruleContext)
        {
            Rule rule = CreateRuleFromContext(ruleContext);
            Log.Write($"Building rule: {rule.Name}");

            // Determines if the starting skip if was created (used by for and goto)
            bool beginningSkipWasCreated = false;

            // The list of actions.
            List<Element> actions = new List<Element>();

            var statements = ruleContext.block().children
                .Where(v => v is DeltinScriptParser.StatementContext)
                .Cast<DeltinScriptParser.StatementContext>().ToArray();

            for (int i = 0; i < statements.Length; i++)
                actions.Add(ParseStatement(statements[i]));

            rule.Actions = actions.ToArray();

            Log.Write($"Finished rule: {rule.Name}");
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

            return new Rule(ruleName, ruleEvent, team, player);
        }

        static Element ParseExpression(DeltinScriptParser.ExprContext context)
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

                double num;
                // num will have the format expr(number(X)) if positive, expr(number(neg(X))) if negative.
                if (number.GetChild(0) is DeltinScriptParser.NegContext)
                    // Is negative, use '-' before int.parse to make it negative.
                    num = -double.Parse(number.GetChild(0).GetText());
                else
                    // Is positive
                    num = double.Parse(number.GetChild(0).GetText());

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
                return Var.GetVar((context.GetChild(0) as DeltinScriptParser.VariableContext).PART().GetText()).GetVariable(new V_EventPlayer());

            #endregion

            #region Array

            if (context.ChildCount == 4 && context.GetChild(1).GetText() == "[" && context.GetChild(3).GetText() == "]")
                return Element.Part<V_ValueInArray>(
                    ParseExpression(context.expr(0) as DeltinScriptParser.ExprContext),
                    ParseExpression(context.expr(1) as DeltinScriptParser.ExprContext));

            #endregion

            #region Seperator

            if (context.ChildCount == 3 && context.GetChild(1).GetText() == ".")
            {
                Element left = ParseExpression(context.GetChild(0) as DeltinScriptParser.ExprContext);
                // (left) . (expr(variable(yeet)))
                string variableName = context.GetChild(2).GetChild(0).GetText();

                Var var = Var.GetVar(variableName);
                if (var == null)
                    throw new SyntaxErrorException($"Variable {variableName} does not exist.", context.start.Line, context.start.Column);

                return var.GetVariable(left);
            }

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

        static Element ParseStatement(DeltinScriptParser.StatementContext statementContext)
        {
            // If the statement is a method.
            if (statementContext.GetChild(0) is DeltinScriptParser.MethodContext)
                return ParseMethod(statementContext.GetChild(0) as DeltinScriptParser.MethodContext);

            if (statementContext.STATEMENT_OPERATION() != null)
            {
                Var variable;
                Element target;

                string operation = statementContext.STATEMENT_OPERATION().GetText();

                Element value = ParseExpression(statementContext.expr(1) as DeltinScriptParser.ExprContext);

                if (statementContext.expr(0).ChildCount == 3
                    && statementContext.expr(0).GetChild(1).GetText() == ".")
                {
                    variable = Var.GetVar(statementContext.expr(0).expr(1).GetChild(0).GetText());
                    target = ParseExpression(statementContext.expr(0).expr(0));
                }
                else
                {
                    variable = Var.GetVar(statementContext.expr(0).GetChild(0).GetText());
                    target = new V_EventPlayer();
                }

                return variable.SetVariable(value, target);
            }

            throw new Exception($"What's a {statementContext}?");
        }
    }

    class InternalVars
    {
        public InternalVars(Variable global, Variable player)
        {
            Global = global;
            Player = player;
        }
        public Variable Global { get; private set; }
        public Variable Player { get; private set; }

        public int NextFreeGlobalIndex { get; private set; }
        public int NextFreePlayerIndex { get; private set; }

        public int AssignGlobalIndex()
        {
            int index = NextFreeGlobalIndex;
            NextFreeGlobalIndex++;
            return index;
        }

        public int AssignPlayerIndex()
        {
            int index = NextFreePlayerIndex;
            NextFreePlayerIndex++;
            return index;
        }
    }

    class Var
    {
        public static List<Var> VarCollection { get; private set; } = new List<Var>();

        public static bool IsVar(string name)
        {
            return VarCollection.Any(v => v.Name == name);
        }

        public static Var GetVar(string name)
        {
            return VarCollection.FirstOrDefault(v => v.Name == name);
        }

        public bool IsGlobal { get; private set; }
        public bool IsArray { get; private set; }
        public string Name { get; private set; }
        public Variable Variable { get; private set; }
        public int Index { get; private set; }

        public Var(DeltinScriptParser.VardefineContext vardefine, 
            InternalVars iv)
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
                if (IsGlobal)
                {
                    Variable = iv.Global;
                    Index = iv.AssignGlobalIndex();
                }
                else
                {
                    Variable = iv.Player;
                    Index = iv.AssignPlayerIndex();
                }
                IsArray = true;
            }
            else
            {
                if (useNumber != null)
                {
                    IsArray = true;
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

        public Element GetVariable(Element targetPlayer = null)
        {
            Element element;

            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();

            if (!IsArray)
            {
                if (IsGlobal)
                    element = Element.Part<V_GlobalVariable>(Variable);
                else
                    element = Element.Part<V_PlayerVariable>(targetPlayer, Variable);
            }
            else
            {
                element = new V_ValueInArray();
                List<object> parameterValues = new List<object>();
                Element arrayParameter;

                if (IsGlobal)
                    arrayParameter = Element.Part<V_GlobalVariable>(Variable);
                else
                    arrayParameter = Element.Part<V_PlayerVariable>(targetPlayer, Variable);

                parameterValues.Add(arrayParameter);
                parameterValues.Add(new V_Number(Index));
                element.ParameterValues = parameterValues.ToArray();
            }

            
            return element;
        }

        public Element SetVariable(Element value, Element targetPlayer = null)
        {
            Element element;

            if (targetPlayer == null)
                targetPlayer = new V_EventPlayer();

            if (!IsArray)
            {
                if (IsGlobal)
                    element = Element.Part<A_SetGlobalVariable>(Variable, value);
                else
                    element = Element.Part<A_SetPlayerVariable>(targetPlayer, Variable, value);
            }
            else
            {
                if (IsGlobal)
                    element = Element.Part<A_SetGlobalVariableAtIndex>(Variable, new V_Number(Index), value);
                else
                    element = Element.Part<A_SetPlayerVariableAtIndex>(targetPlayer, Variable, new V_Number(Index), value);
            }

            return element;

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
