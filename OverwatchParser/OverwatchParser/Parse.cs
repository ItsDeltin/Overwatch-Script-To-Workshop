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

            Log.Write(context.ToStringTree());

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
            {
                ParseRule parsing = new ParseRule(rules[i], iv);

                Log.Write($"Building rule: {parsing.Rule.Name}");
                parsing.Parse();
                Rule rule = parsing.Rule;
                Log.Write($"Finished rule: {parsing.Rule.Name}");

                compiledRules.Add(rule);
            }

            Log.Write("Build succeeded.");

            // List all variables
            Log.Write("Variable Guide:");
            foreach (Var var in Var.VarCollection)
                Console.WriteLine($"{var.Name}: {var.Variable}{(var.IsArray ? $"[{var.Index}]" : "")}");

            return compiledRules.ToArray();
        }
    }

    class ParseRule
    {
        public Rule Rule { get; private set; }

        private readonly List<Element> Actions = new List<Element>();
        private readonly List<Condition> Conditions = new List<Condition>();

        private readonly InternalVars InternalVars;
        private DeltinScriptParser.Ow_ruleContext RuleContext;

        private bool IsGlobal;

        //private bool CreateInitialSkip = false;
        //private int SkipCountIndex = -1;

        public ParseRule(DeltinScriptParser.Ow_ruleContext ruleContext, InternalVars internalVars)
        {
            Rule = CreateRuleFromContext(ruleContext);
            RuleContext = ruleContext;
            IsGlobal = Rule.RuleEvent == RuleEvent.Ongoing_Global;
            InternalVars = internalVars;
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
                return Var.GetVar((context.GetChild(0) as DeltinScriptParser.VariableContext).PART().GetText()).GetVariable(new V_EventPlayer());

            #endregion

            #region Array

            if (context.ChildCount == 4 && context.GetChild(1).GetText() == "[" && context.GetChild(3).GetText() == "]")
                return Element.Part<V_ValueInArray>(
                    ParseExpression(context.expr(0) as DeltinScriptParser.ExprContext),
                    ParseExpression(context.expr(1) as DeltinScriptParser.ExprContext));

            #endregion

            #region Seperator/enum

            if (context.ChildCount == 3 && context.GetChild(1).GetText() == ".")
            {
                Element left = ParseExpression(context.GetChild(0) as DeltinScriptParser.ExprContext);
                string variableName = context.GetChild(2).GetChild(0).GetText();

                Var var = Var.GetVar(variableName);
                if (var == null)
                    throw new SyntaxErrorException($"Variable {variableName} does not exist.", context.start.Line, context.start.Column);

                return var.GetVariable(left);
            }

            #endregion

            throw new Exception($"What's a {context}?");
        }

        Element ParseMethod(DeltinScriptParser.MethodContext methodContext)
        {
            string methodName = methodContext.PART().GetText();
            Type methodType = Element.GetMethod(methodName);

            if (methodType == null)
                throw new SyntaxErrorException($"The method {methodName} does not exist.", methodContext.start.Line, methodContext.start.Column);

            List<object> parameters = new List<object>();
            var parseParameters = methodContext.expr();
            foreach (var param in parseParameters)
            {
                object value = null;

                /*
                if (!int.TryParse(param.GetText(), out _))
                    foreach (Type @enum in Constants.EnumParameters)
                    {
                        try
                        {
                            value = Enum.Parse(@enum, param.GetText());
                        }
                        catch (Exception ex) when (ex is ArgumentNullException || ex is ArgumentException || ex is OverflowException) {}
                    }
                */

                if (param.GetChild(0) is DeltinScriptParser.EnumContext)
                {
                    foreach (Type @enum in Constants.EnumParameters)
                    {
                        try
                        {
                            value = Enum.Parse(@enum, param.GetText().Split('.').ElementAtOrDefault(1));
                        }
                        catch (Exception ex) when (ex is ArgumentNullException || ex is ArgumentException || ex is OverflowException) { }
                    }

                    if (value == null)
                        throw new SyntaxErrorException($"Could not parse parameter {param.GetText()}.", param.start.Line, param.start.Column);
                }

                else if (value == null)
                    value = ParseExpression(param);

                else
                    throw new SyntaxErrorException("Could not parse parameter.", param.start.Line, param.start.Column);


                parameters.Add(value);
            }

            Element method = (Element)Activator.CreateInstance(methodType);
            method.ParameterValues = parameters.ToArray();

            return method;
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

            #region Variable set. TODO: add support for += -= *= /=

#warning TODO: add support for += -= *= /=
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
                int skipIndex = Assign();
                // Insert the SkipIf at the start of the rule.
                Actions.Insert(0,
                    Element.Part<A_SkipIf>
                    (
                        // Condition
                        GetIVarAtIndex(skipIndex),
                        // Number of actions
                        new V_Number(forActionStartIndex)
                    )
                );

                // Create the for's temporary variable.
                int forIndex = Assign();
                Var forTempVar = new Var(
                    name    : statementContext.@for().PART().GetText(),
                    isGlobal: IsGlobal,
                    variable: IsGlobal ? InternalVars.Global : InternalVars.Player,
                    index   : forIndex,
                    line    : statementContext.@for().start.Line,
                    column  : statementContext.@for().start.Column
                    );

                // Reset the counter.
                Actions.Add(SetIVarAtIndex(forIndex, new V_Number(0)));

                // Parse the for's block.
                ParseBlock(statementContext.@for().block());

                // Take the variable out of scope.
                forTempVar.OutOfScope();

                // Add the for's finishing elements
                //Actions.Add(SetIVarAtIndex(skipIndex, new V_Number(forActionStartIndex))); // Sets how many variables to skip in the next iteraction.
                Actions.Add(SetIVarAtIndex(skipIndex, new V_True())); // Enables the skip.

#warning Maybe change to use the Modify action when it is added to the action list?
                Actions.Add(SetIVarAtIndex( // Indent the index by 1.
                    forIndex, 
                    Element.Part<V_Add>
                    (
                        GetIVarAtIndex(forIndex), 
                        new V_Number(1)
                    )
                ));

                Actions.Add(Element.Part<A_Wait>(new V_Number(0.06), WaitBehavior.IgnoreCondition)); // Add the Wait() required by the workshop.
                Actions.Add(Element.Part<A_LoopIf>( // Loop if the for condition is still true.
                    Element.Part<V_Compare>
                    (
                        GetIVarAtIndex(forIndex),
                        Operators.LessThan,
                        Element.Part<V_CountOf>(forArrayElement)
                    )
                ));
                Actions.Add(SetIVarAtIndex(skipIndex, new V_False()));
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
                    ParseBlock(statementContext.@if().block());

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

        void ParseBlock(DeltinScriptParser.BlockContext blockContext)
        {
            var statements = blockContext.children
                .Where(v => v is DeltinScriptParser.StatementContext)
                .Cast<DeltinScriptParser.StatementContext>().ToArray();

            for (int i = 0; i < statements.Length; i++)
                ParseStatement(statements[i]);
        }

        Element SetIVarAtIndex(int index, Element value)
        {
            if (IsGlobal)
                return Element.Part<A_SetGlobalVariableAtIndex>(InternalVars.Global, new V_Number(index), value);
            else
                return Element.Part<A_SetPlayerVariableAtIndex>(new V_EventPlayer(), InternalVars.Player, new V_Number(index), value);
        }
        Element GetIVarAtIndex(int index)
        {
            if (IsGlobal)
                return Element.Part<V_ValueInArray>(Element.Part<V_GlobalVariable>(InternalVars.Global), new V_Number(index));
            else
                return Element.Part<V_ValueInArray>(Element.Part<V_PlayerVariable>(new V_EventPlayer(), InternalVars.Player), new V_Number(index));
        }

        private int Assign()
        {
            int index;
            if (IsGlobal)
                index = InternalVars.AssignGlobalIndex();
            else
                index = InternalVars.AssignPlayerIndex();

            return index;
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

        public Var(string name, bool isGlobal, Variable variable, int index, int line, int column)
        {
            if (IsVar(name))
                throw new SyntaxErrorException($"The variable {name} was already defined.", line, column);

            Name = name;
            IsGlobal = isGlobal;
            Variable = variable;

            if (index != -1)
            {
                IsArray = true;
                Index = index;
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
