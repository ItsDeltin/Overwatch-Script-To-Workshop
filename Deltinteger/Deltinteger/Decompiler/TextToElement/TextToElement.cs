using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Decompiler.ElementToCode;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    public class ConvertTextToElement
    {
        private readonly static char[] WHITESPACE = new char[] { '\r', '\n', '\t', ' ' };
        private readonly static string[] DEFAULT_VARIABLES = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };

        public string Content { get; }
        public int Position { get; private set; }
        public char Current => Content[Position];
        public bool ReachedEnd => Position >= Content.Length;
        public string LocalStream => Content.Substring(Position); // ! For debugging

        private readonly Stack<TTEOperator> _operators = new Stack<TTEOperator>();
        private readonly Stack<ITTEExpression> _operands = new Stack<ITTEExpression>();

        private readonly ElementJsonAction[] _actions;
        private readonly ElementJsonValue[] _values;

        public List<WorkshopVariable> Variables { get; } = new List<WorkshopVariable>();
        public List<Subroutine> Subroutines { get; } = new List<Subroutine>();
        public List<TTERule> Rules { get; } = new List<TTERule>();
        public Ruleset LobbySettings { get; private set; } = null;

        public ConvertTextToElement(string content)
        {
            Content = content;
            _actions = ElementRoot.Instance.Actions.OrderByDescending(e => e.Name.Length).ToArray();
            _values = ElementRoot.Instance.Values.OrderByDescending(e => e.Name.Length).ToArray();
            _operators.Push(TTEOperator.Sentinel);
        }

        public Workshop Get()
        {
            // Match lobby settings, variables, and subroutines.
            MatchSettings();
            MatchVariables();
            MatchSubroutines();

            // Match action copy
            if (ActionGroup(out var actions))
                return new Workshop(Variables.ToArray(), Subroutines.ToArray(), actions.ToArray());
            // Match condition copy
            else if (ConditionGroup(out var conditions))
                return new Workshop(Variables.ToArray(), Subroutines.ToArray(), conditions.ToArray());
            else
            {
                // Match rules
                while (Rule(out TTERule rule)) Rules.Add(rule);
                return new Workshop(Variables.ToArray(), Subroutines.ToArray(), Rules.ToArray(), LobbySettings);
            }
        }

        public Workshop GetActionList()
        {
            // Match variables and subroutines.
            MatchVariables();
            MatchSubroutines();

            // Match actions.
            if (ActionGroup(out var actions))
                return new Workshop(Variables.ToArray(), Subroutines.ToArray(), actions.ToArray());
            return null;
        }

        // TODO: Translate the english keyword to the specified language's keyword.
        public string Kw(string value) => value;

        void Advance()
        {
            if (!ReachedEnd)
                Position += 1;
        }

        void Advance(int length)
        {
            Position = Math.Min(Content.Length, Position + length);
        }

        void SkipWhitespace()
        {
            while (!ReachedEnd && WHITESPACE.Contains(Current))
                Advance();
            
            if (Match("//"))
            {
                while (!ReachedEnd && !Is('\n'))
                    Advance();
                
                SkipWhitespace();
            }
        }

        bool Is(char character) => !ReachedEnd && Current == character;
        bool Is(int position, char character) => Position + position < Content.Length && Content[Position + position] == character;
        bool IsInsensitive(int position, char character) => Position + position < Content.Length && Char.ToLower(Content[Position + position]) == Char.ToLower(character);
        bool IsSymbol(int position) => Position + position < Content.Length && char.IsSymbol(Content[Position + position]);
        bool IsAny(params char[] characters) => !ReachedEnd && characters.Contains(Current);
        bool IsAny(string characters) => IsAny(characters.ToCharArray());
        bool IsNumeric() => IsAny("0123456789");
        bool IsAlpha() => IsAny("_abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        bool IsAlphaNumeric() => IsNumeric() || IsAlpha();

        public bool Match(string str, bool caseSensitive = true, bool noSymbols = false)
        {
            for (int i = 0; i < str.Length; i++)
                if ((caseSensitive && !Is(i, str[i])) || !IsInsensitive(i, str[i]))
                    return false;
            
            if (!noSymbols && IsSymbol(str.Length)) return false;

            Advance(str.Length);
            SkipWhitespace();
            return true;
        }

        // Commons
        // String
        bool MatchString(out string value)
        {
            if (!Match("\"", noSymbols: true))
            {
                value = null;
                return false;
            }

            value = "";

            // Empty string
            if (Match("\"", noSymbols: true)) return true;

            bool escaped = false;
            do
            {
                value += Current;
                if (escaped) escaped = false;
                else if (Is(0, '\\')) escaped = true;
                Advance();
            }
            while (escaped || !Is(0, '"'));
            Advance();
            SkipWhitespace();

            return true;
        }

        // Identifier
        bool Identifier(out string identifier)
        {
            if (!IsAlpha())
            {
                identifier = null;
                return false;
            }

            identifier = "";
            while (IsAlphaNumeric())
            {
                identifier += Current;
                Advance();
            }
            SkipWhitespace();
            return true;
        }

        string CustomSettingName()
        {
            string name = "";
            while (!Is('{') && !Is('}') && !Is(':'))
            {
                name += Current;
                Advance();
            }
            SkipWhitespace();
            return name;
        }

        // Integer
        public bool Integer(out int value)
        {
            string str = "";

            if (Match("-")) str += "-";

            while (IsNumeric())
            {
                str += Current;
                Advance();
            }

            if (str == "")
            {
                value = 0;
                return false;
            }
            value = int.Parse(str);
            SkipWhitespace();
            return true;
        }

        // Double
        public bool Double(out double number)
        {
            string str = "";

            if (Match("-")) str += "-";

            while (IsNumeric())
            {
                str += Current;
                Advance();
            }

            if (str == "")
            {
                number = 0;
                return false;
            }

            if (Match("."))
            {
                str += ".";
                while (IsNumeric())
                {
                    str += Current;
                    Advance();
                }
            }

            number = double.Parse(str);
            SkipWhitespace();
            return true;
        }

        // Commons as ITTEExpression
        bool Number(out ITTEExpression numberExpression)
        {
            if (Double(out double value))
            {
                numberExpression = new NumberExpression(value);
                return true;
            }
            numberExpression = null;
            return false;
        }

        // Workshop Copy Structure
        // Variable list
        bool MatchVariables()
        {
            if (!Match(Kw("variables"))) return false;
            Match("{");

            if (Match(Kw("global") + ":")) MatchVariableList(true);
            if (Match(Kw("player") + ":")) MatchVariableList(false);
            
            Match("}");
            return true;
        }

        void MatchVariableList(bool isGlobal)
        {
            while (Integer(out int index))
            {
                Match(":");
                Identifier(out string name);
                Variables.Add(new WorkshopVariable(isGlobal, index, name));
            }
        }

        void AddIfOmitted(string variableName, bool isGlobal)
        {
            // Get the default index of the variable.
            int index = Array.IndexOf(DEFAULT_VARIABLES, variableName);
            // If the index was found and the variable was not added, add it.
            if (index != -1 && !Variables.Any(v => v.IsGlobal == isGlobal && v.Name == variableName))
                Variables.Add(new WorkshopVariable(isGlobal, index, variableName));
        }

        // Subroutines
        bool MatchSubroutines()
        {
            if (!Match(Kw("subroutines"))) return false;
            Match("{");

            // Subroutine list
            while (Integer(out int index))
            {
                Match(":");
                Identifier(out string name);
                Subroutines.Add(new Subroutine(index, name));
            } 

            Match("}");
            return true;
        }

        // Rules
        bool Rule(out TTERule rule)
        {
            bool disabled;
            if (Match(Kw("disabled")))
            {
                disabled = true;
                Match(Kw("rule"));
            }
            else if (Match(Kw("rule")))
            {
                disabled = false;
            }
            else
            {
                rule = null;
                return false;
            }

            Match("(");
            MatchString(out string ruleName);
            Match(")");
            Match("{");

            // Event
            Match(Kw("event"));
            Match("{");
            EventInfo eventInfo = MatchEvent();
            Match("}");

            // Conditions
            ConditionGroup(out var conditions);

            // Actions
            ActionGroup(out var actions);

            Match("}");

            rule = new TTERule(ruleName, eventInfo, conditions.ToArray(), actions.ToArray(), disabled);
            return true;
        }

        EventInfo MatchEvent()
        {
            // Global
            if (Match(Kw("Ongoing - Global") + ";"))
            {
                return new EventInfo();
            }
            // Subroutine
            else if (Match(Kw("Subroutine") + ";"))
            {
                Identifier(out string subroutineName);
                Match(";");
                return new EventInfo(subroutineName);
            }
            // Player event
            else
            {
                // Get the event type.
                var ruleEvent = RuleEvent.OngoingGlobal;
                foreach (var eventNameInfo in EventInfo.PlayerEventNames)
                    if (Match(Kw(eventNameInfo.Item1) + ";"))
                    {
                        ruleEvent = eventNameInfo.Item2;
                        break;
                    }

                // Get the team.
                var team = Team.All;
                if (Match(Kw("All") + ";")) {}
                else if (Match(Kw("Team 1") + ";"))
                    team = Team.Team1;
                else if (Match(Kw("Team 2") + ";"))
                    team = Team.Team2;
                
                // Get the player type.
                var player = PlayerSelector.All;
                foreach (var playerNameInfo in EventInfo.PlayerTypeNames)
                    if (Match(Kw(playerNameInfo.Item1) + ";"))
                    {
                        player = playerNameInfo.Item2;
                        break;
                    }

                return new EventInfo(ruleEvent, player, team);
            }
        }

        // Conditions
        bool ConditionGroup(out List<TTECondition> conditions)
        {
            conditions = new List<TTECondition>();
            if (!Match(Kw("conditions"))) return false;

            Match("{");
            while (Condition(out TTECondition condition))
            {
                Match(";");
                conditions.Add(condition);
            }
            Match("}");
            return true;
        }

        bool Condition(out TTECondition condition)
        {
            // Is the condition disabled?
            bool isDisabled = Match(Kw("disabled"));

            // Match the condition's expression.
            if (Expression(out ITTEExpression expression))
            {
                condition = new TTECondition(isDisabled, expression);
                return true;
            }

            // No expression matched; return false.
            condition = null;
            return false;
        }

        // Actions
        bool ActionGroup(out List<ITTEAction> actionList)
        {
            actionList = new List<ITTEAction>();
            if (!Match(Kw("actions"))) return false;

            Match("{");
            while (Action(out ITTEAction action)) actionList.Add(action);
            Match("}");
            return true;
        }

        bool Action(out ITTEAction action)
        {
            action = null;

            // Comment
            MatchString(out string comment);
            bool isDisabled = Match(Kw("disabled"));

            // Subroutine
            if (Match(Kw("Call Subroutine")))
            {
                Match("(");
                Identifier(out string name);
                Match(")");
                action = new CallSubroutine(name, Parse.CallParallel.NoParallel);
            }
            // Start Rule Subroutine
            else if (Match(Kw("Start Rule")))
            {
                Match("(");
                Identifier(out string name);
                Match(",");

                if (Match(Kw("Restart Rule")))
                {
                    Match(")");
                    action = new CallSubroutine(name, Parse.CallParallel.AlreadyRunning_RestartRule);
                }
                else if (Match(Kw("Do Nothing")))
                {
                    Match(")");
                    action = new CallSubroutine(name, Parse.CallParallel.AlreadyRunning_DoNothing);
                }
                else throw new Exception("Expected 'Restart Rule' or 'Do Nothing'.");
            }
            // Function.
            else if (Function(true, out FunctionExpression func))
            {
                action = func;
            }
            // Set variable.
            else if (Expression(out ITTEExpression expr))
            {
                // Unfold the index if required.
                ITTEExpression index = null;
                if (expr is IndexerExpression indexer)
                {
                    index = indexer.Index;
                    expr = indexer.Expression;
                }

                // Make sure the expression is a variable.
                if (expr is ITTEVariable == false)
                    throw new Exception("Expression is not a variable.");
                                
                string op = null;
                string[] operators = new string[] { "=", "+=", "-=", "/=", "*=", "%=", "^=" };
                foreach (string it in operators)
                    if (Match(it))
                    {
                        op = it;
                        break;
                    }
                
                Expression(out ITTEExpression value);
                action = new SetVariableAction((ITTEVariable)expr, op, value, index);
            }
            // Unknown.
            else
            {
                return false;
            }

            action.Disabled = isDisabled;
            action.Comment = comment;
            Match(";");
            return true;
        }

        // Functions
        bool Function(bool actions, out FunctionExpression expr)
        {
            if (actions)
                // Actions
                foreach (var action in _actions)
                {
                    if (Function(action, out expr))
                        return true;
                }
            else
                // Values
                foreach (var value in _values)
                {
                    if (Function(value, out expr))
                        return true;
                }
            
            // Nope
            expr = null;
            return false;
        }

        bool Function(ElementBaseJson func, out FunctionExpression expr)
        {
            if (!Match(Kw(func.Name), false))
            {
                expr = null;
                return false;
            }

            // Get the parameter values.
            List<ITTEExpression> values = new List<ITTEExpression>();
            if (Match("("))
            {
                int currentParameter = 0;
                do
                {
                    ElementParameter parameter = null;
                    if (func.Parameters != null && currentParameter < func.Parameters.Length)
                        parameter = func.Parameters[currentParameter];

                    // Variable reference
                    if (parameter != null && parameter.IsVariableReference)
                    {
                        // Match the variable parameter.
                        if (!Identifier(out string identifier))
                            throw new Exception("Failed to retrieve identifier of variable parameter.");
                        
                        AddIfOmitted(identifier, parameter.VariableReferenceIsGlobal.Value);
                        values.Add(new AnonymousVariableExpression(identifier, parameter.VariableReferenceIsGlobal.Value));
                    }
                    // Enumerator
                    else if (parameter?.Type != null && ElementRoot.Instance.TryGetEnum(parameter.Type, out var enumerator))
                    {
                        // Match enum member
                        foreach (var member in enumerator.Members.OrderByDescending(m => m.Name.Length))
                            if (Match(Kw(member.DecompileName()), false))
                            {
                                values.Add(new ConstantEnumeratorExpression(member));
                                break;
                            }
                    }
                    // Normal parameter
                    else
                    {
                        if (ContainExpression(out ITTEExpression value)) values.Add(value);
                    }

                    // Increment the current parameter.
                    currentParameter++;
                }
                while (Match(","));
                Match(")");
            }

            expr = new FunctionExpression(func, values.ToArray());
            return true;
        }

        // Expressions
        bool Expression(out ITTEExpression expr, bool root = true)
        {
            expr = null;

            // Group
            if (Match("("))
            {
                ContainExpression(out expr);
                Match(")");
            }
            // Number
            else if (Number(out expr)) {}
            // String
            else if (WorkshopString(out expr)) {}
            // Enum value
            else if (EnumeratorValue(out expr)) {}
            // Variable
            else if (GlobalVariable(out expr)) {}
            // Legacy
            else if (LegacyExpression(out expr)) {}
            // Function
            else if (Function(false, out FunctionExpression value))
            {
                expr = value;
            }
            // Unary operator
            else if (MatchUnary(out TTEOperator unaryOperator))
            {
                PushOperator(unaryOperator);
                Expression(out expr);
            }
            // No matches
            else
            {
                return false;    
            }

            // Array index
            while (VariableIndex(out ITTEExpression index))
                expr = new IndexerExpression(expr, index);
            
            // Player variable
            if (MatchPlayerVariable(expr, out ITTEExpression playerVariable))
                expr = playerVariable;

            // Push the expression
            _operands.Push(expr);
            
            // Binary operator
            while (MatchOperator(out TTEOperator op))
            {
                PushOperator(op);
                Expression(out ITTEExpression right, false);
            }
            while (_operators.Peek().Precedence > 0)
                PopOperator();
            
            // If this is the root, return the top operand.
            if (root) expr = _operands.Pop();

            return true;
        }

        bool ContainExpression(out ITTEExpression expr)
        {
            _operators.Push(TTEOperator.Sentinel);
            bool result = Expression(out expr);
            _operators.Pop();
            return result;
        }

        bool LegacyExpression(out ITTEExpression expr)
        {
            if (LegacyOperator(out expr))
                return true;
            else if (Match("Value In Array"))
            {
                Match("(");
                ContainExpression(out var array);
                Match(",");
                ContainExpression(out var index);
                Match(")");

                expr = new IndexerExpression(array, index);
                return true;
            }
            return false;
        }

        bool LegacyOperator(out ITTEExpression expr)
        {
            TTEOperator op = null;

            if (Match(Kw("Add"))) op = TTEOperator.Add;
            else if (Match(Kw("Subtract"))) op = TTEOperator.Subtract;
            else if (Match(Kw("Multiply"))) op = TTEOperator.Multiply;
            else if (Match(Kw("Divide"))) op = TTEOperator.Divide;
            else if (Match(Kw("Modulo"))) op = TTEOperator.Modulo;
            else if (Match(Kw("Raise To Power"))) op = TTEOperator.Power;
            else if (Match(Kw("And"))) op = TTEOperator.And;
            else if (Match(Kw("Or"))) op = TTEOperator.Or;
            else if (Match(Kw("Compare")))
            {
                Match("(");
                ContainExpression(out ITTEExpression compareLeft);
                Match(",");

                if (Match("==")) op = TTEOperator.Equal;
                else if (Match("!=")) op = TTEOperator.NotEqual;
                else if (Match(">=")) op = TTEOperator.GreaterThanOrEqual;
                else if (Match("<=")) op = TTEOperator.LessThanOrEqual;
                else if (Match(">")) op = TTEOperator.GreaterThan;
                else if (Match("<")) op = TTEOperator.LessThan;

                Match(",");
                ContainExpression(out ITTEExpression compareRight);
                Match(")");

                expr = new BinaryOperatorExpression(compareLeft, compareRight, op);
                return true;
            }

            if (op == null)
            {
                expr = null;
                return false;
            }

            Match("(");
            ContainExpression(out ITTEExpression left);
            Match(",");
            ContainExpression(out ITTEExpression right);
            Match(")");

            expr = new BinaryOperatorExpression(left, right, op);
            return true;
        }

        // Workshop string function
        bool WorkshopString(out ITTEExpression expr)
        {
            bool localized; // Determines if the string is localized.

            // Custom string
            if (Match(Kw("Custom String")))
                localized = false;
            // Localized string
            else if (Match(Kw("String")))
                localized = true;
            else
            {
                // Not a string
                expr = null;
                return false;
            }

            Match("(");

            // Get the actual string.
            MatchString(out string str);

            // Get the format parameters.
            List<ITTEExpression> formats = new List<ITTEExpression>();
            while (Match(","))
            {
                if (ContainExpression(out ITTEExpression value)) formats.Add(value);
            }

            Match(")");

            expr = new StringExpression(str, formats.ToArray(), localized);
            return true;
        }

        // Enumerator Values
        bool EnumeratorValue(out ITTEExpression expr)
        {
            if (Match(Kw("All Teams")))
            {
                expr = new ConstantEnumeratorExpression(ElementEnumMember.Team(Team.All));
                return true;
            }
            if (Match(Kw("Team 1")))
            {
                expr = new ConstantEnumeratorExpression(ElementEnumMember.Team(Team.Team1));
                return true;
            }
            if (Match(Kw("Team 2")))
            {
                expr = new ConstantEnumeratorExpression(ElementEnumMember.Team(Team.Team2));
                return true;
            }
            // TODO: Gamemode, map, button, etc

            expr = null;
            return false;
        }

        // Variables
        bool GlobalVariable(out ITTEExpression expr)
        {
            int c = Position; // Revert

            string name = null;
            bool result = Match("Global")
                && Match(".")
                && Identifier(out name);

            if (!result)
            {
                expr = null;
                Position = c;
                return false;
            }
            
            AddIfOmitted(name, true);
            expr = new GlobalVariableExpression(name);
            return true;
        }

        bool MatchPlayerVariable(ITTEExpression parent, out ITTEExpression playerVariable)
        {
            playerVariable = parent;
            bool matched = false;

            while (Match("."))
            {
                matched = true;
                Identifier(out string name);
                AddIfOmitted(name, false);
                playerVariable = new PlayerVariableExpression(name, playerVariable);

                // Array index
                while (VariableIndex(out ITTEExpression index))
                    playerVariable = new IndexerExpression(playerVariable, index);
            }
            
            return matched;
        }

        bool VariableIndex(out ITTEExpression index)
        {
            if (!Match("["))
            {
                index = null;
                return false;
            }

            ContainExpression(out index);

            Match("]");
            return true;
        }
    
        // Operators
        bool MatchOperator(out TTEOperator op)
        {
            if (Match("&&")) op = TTEOperator.And;
            else if (Match("||")) op = TTEOperator.Or;
            else if (Match("-")) op = TTEOperator.Subtract;
            else if (Match("+")) op = TTEOperator.Add;
            else if (Match("%")) op = TTEOperator.Modulo;
            else if (Match("/")) op = TTEOperator.Divide;
            else if (Match("*")) op = TTEOperator.Multiply;
            else if (Match("^")) op = TTEOperator.Power;
            else if (Match("==")) op = TTEOperator.Equal;
            else if (Match("!=")) op = TTEOperator.NotEqual;
            else if (Match(">=")) op = TTEOperator.GreaterThanOrEqual;
            else if (Match("<=")) op = TTEOperator.LessThanOrEqual;
            else if (Match(">")) op = TTEOperator.GreaterThan;
            else if (Match("<")) op = TTEOperator.LessThan;
            else if (Match("?")) op = TTEOperator.Ternary;
            else if (Match(":")) op = TTEOperator.RhsTernary;
            else
            {
                op = null;
                return false;
            }
            return true;
        }

        bool MatchUnary(out TTEOperator op)
        {
            if (Match("!")) op = TTEOperator.Not;
            else
            {
                op = null;
                return false;
            }
            return true;
        }

        void PushOperator(TTEOperator op)
        {
            // while (_operators.Peek().Precedence > op.Precedence)
            while (TTEOperator.Compare(_operators.Peek(), op))
                PopOperator();
            _operators.Push(op);
        }

        void PopOperator()
        {
            var op = _operators.Pop();
            if (op.Type == OperatorType.Binary)
            {
                // Binary
                if (op.Contain == ContainGroup.Left)
                {
                     var right = _operands.Pop();
                    var left = _operands.Pop();
                    _operands.Push(new BinaryOperatorExpression(left, right, op));
                    return;
                }

                Stack<ITTEExpression> exprs = new Stack<ITTEExpression>();
                exprs.Push(_operands.Pop());
                exprs.Push(_operands.Pop());

                while (_operators.Peek() == op)
                {
                    _operators.Pop();
                    exprs.Push(_operands.Pop());
                }

                ITTEExpression result = exprs.Pop();
                while (exprs.Count != 0)
                    result = new BinaryOperatorExpression(result, exprs.Pop(), op);
                
                _operands.Push(result);
            }
            else if (op.Type == OperatorType.Unary)
            {
                // Unary
                var value = _operands.Pop();
                _operands.Push(new UnaryOperatorExpression(value, op));
            }
            else
            {
                // Ternary
                var op2 = _operators.Pop();
                var rhs = _operands.Pop();
                var middle = _operands.Pop();
                var lhs = _operands.Pop();
                _operands.Push(new TernaryExpression(lhs, middle, rhs));
            }
        }
    
        // Settings
        bool MatchSettings()
        {
            if (!Match(Kw("settings"))) return false;

            Ruleset ruleset = new Ruleset();

            Match("{"); // Start settings section.

            // Main settings
            if (Match(Kw("main")))
            {
                Match("{"); // Start main section.

                // Description
                if (Match(Kw("Description") + ":"))
                {
                    MatchString(out string description);
                    ruleset.Description = description;
                }

                Match("}"); // End main section.
            }

            // General lobby settings
            if (Match(Kw("lobby")))
            {
                ruleset.Lobby = new WorkshopValuePair();
                Match("{"); // Start lobby section.
                GroupSettings(ruleset.Lobby, Ruleset.LobbySettings); // Match the settings and value pairs.
                Match("}"); // End lobby section.
            }

            // Modes
            if (Match(Kw("modes")))
            {
                ruleset.Modes = new ModesRoot();
                Match("{"); // Start modes section.

                // Match the mode settings.
                while (LobbyModes(ruleset));

                Match("}"); // End modes section.
            }

            // Heroes
            if (Match(Kw("heroes")))
            {
                ruleset.Heroes = new HeroesRoot();
                Match("{"); // Start heroes section.

                // Match the hero settings.
                while (HeroSettingsGroup(ruleset));

                Match("}"); // End heroes section.
            }

            // Custom workshop settings
            if (Match(Kw("workshop")))
            {
                ruleset.Workshop = new WorkshopValuePair();
                Match("{"); // Start workshop section.

                // Match settings.
                while(!Match("}"))
                {
                    string identifier = CustomSettingName();
                    Match(":");

                    object value = "?";

                    // Boolean: On
                    if (Match(Kw("On")))
                        value = true;
                    // Boolean: Off
                    else if (Match(Kw("Off")))
                        value = false;
                    // Number
                    else if (Double(out double num))
                        value = num;
                    // Combo
                    else if (Match("["))
                    {
                        Double(out double comboIndex);
                        value = comboIndex;
                        Match("]");
                    }
                    // Match hero names.
                    else if (MatchHeroNames(out var hero))
                        value = hero.HeroName;
                    
                    // Add the custom setting.
                    ruleset.Workshop.Add(identifier, value);
                }
            }

            Match("}"); // End settings section.
            LobbySettings = ruleset;
            return true;
        }

        bool LobbyModes(Ruleset ruleset)
        {
            // Match general
            if (Match(Kw("General")))
            {
                ruleset.Modes.All = new WorkshopValuePair(); // Init settings dictionary.
                Match("{"); // Start general settings section.
                GroupSettings(ruleset.Modes.All, ModeSettingCollection.AllModeSettings.First(modeSettings => modeSettings.ModeName == "All").ToArray()); // Match settings.
                Match("}"); // End general settings section.
                return true;
            }

            // Disabled
            bool disabled = Match(Kw("disabled"));

            foreach (var mode in ModeSettingCollection.AllModeSettings)
            // Match the mode name.
            if (Match(Kw(mode.ModeName)))
            {
                ModeSettings relatedModeSettings = ruleset.Modes.SettingsFromModeCollection(mode); // Get the related mode settings from the matched mode.
                Match("{"); // Start specific mode settings section.
                // Match the value pairs.
                GroupSettings(relatedModeSettings.Settings, mode.ToArray(), () => {
                    bool matchingEnabledMaps; // Determines if the map group is matching enabled or disabled maps.
                    // Match enabled maps
                    if (Match(Kw("enabled maps"))) matchingEnabledMaps = true;
                    // Match disabled maps
                    else if (Match(Kw("disabled maps"))) matchingEnabledMaps = false;
                    // End
                    else return false;

                    Match("{"); // Start map section.

                    List<string> maps = new List<string>(); // Matched maps.

                    // Match map names.
                    bool matched = true;
                    while (matched)
                    {
                        matched = false;
                        // Only match maps related to the current mode.
                        foreach (var map in LobbyMap.AllMaps.Where(m => m.GameModes.Any(mapMode => mapMode.ToLower() == mode.ModeName.ToLower())).OrderByDescending(map => map.GetWorkshopName().Length))
                            // Match the map.
                            if (Match(Kw(map.GetWorkshopName()), false))
                            {
                                // Add the map.
                                maps.Add(map.Name);

                                // Indicate that a map was matched in this iteration.
                                matched = true;
                                break;
                            }
                    }

                    Match("}"); // End map section.

                    // Add the maps to the mode's settings.
                    if (matchingEnabledMaps) relatedModeSettings.EnabledMaps = maps.ToArray();
                    else relatedModeSettings.DisabledMaps = maps.ToArray();

                    return true;
                });
                Match("}"); // End specific mode settings section.

                if (disabled) relatedModeSettings.Settings.Add("Enabled", !disabled);
                return true;
            }
            return false;
        }

        bool HeroSettingsGroup(Ruleset ruleset)
        {
            // Matched settings will be added to this list.
            HeroList list = new HeroList();
            list.Settings = new Dictionary<string, object>();

            // Match hero settings group name.
            if (Match(Kw("General"))) ruleset.Heroes.General = list;   // General
            else if (Match(Kw("Team 1"))) ruleset.Heroes.Team1 = list; // Team 1
            else if (Match(Kw("Team 2"))) ruleset.Heroes.Team2 = list; // Team 2
            else return false;

            Match("{"); // Start hero settings section.

            // Match general settings.
            GroupSettings(list.Settings, HeroSettingCollection.AllHeroSettings.First(hero => hero.HeroName == "General").ToArray(), () => {
                // Match hero names.
                if (MatchHeroNames(out var hero))
                {
                    WorkshopValuePair heroSettings = new WorkshopValuePair();
                    list.Settings.Add(hero.HeroName, heroSettings);

                    Match("{"); // Start specific hero settings section.

                    // Match settings.
                    GroupSettings(heroSettings, hero.ToArray());
                    
                    Match("}"); // End specific hero settings section.
                    return true;
                }
                
                bool enabledHeroes; // Determines if the hero group is matching enabled or disabled heroes.
                // Enabled heroes
                if (Match(Kw("enabled heroes"))) enabledHeroes = true;
                // Disabled heroes
                else if (Match(Kw("disabled heroes"))) enabledHeroes = false;
                // No heroes
                else return false;

                var heroes = new List<string>(); // The list of heroes in the collection.

                Match("{"); // Start the enabled heroes section.
                while (MatchHero(out string heroName)) heroes.Add(heroName); // Match heroes.
                Match("}"); // End the enabled heroes section.

                // Apply the hero list.
                if (enabledHeroes) list.EnabledHeroes = heroes.ToArray();
                else list.DisabledHeroes = heroes.ToArray();

                // Done
                return true;
            });

            Match("}"); // End hero settings section.
            return true;
        }

        bool MatchHeroNames(out HeroSettingCollection collection)
        {
            foreach (var hero in HeroSettingCollection.AllHeroSettings.Where(heroSettings => heroSettings.HeroName != "General"))
                if (Match(Kw(hero.HeroName), false))
                {
                    collection = hero;
                    return true;
                }
            collection = null;
            return false;
        }

        bool MatchHero(out string heroName)
        {
            // Iterate through all hero names.
            foreach (var hero in HeroSettingCollection.AllHeroSettings)
                // If a hero name is matched, return true.
                if (Match(Kw(hero.HeroName), false))
                {
                    heroName = hero.HeroName;
                    return true;
                }
            // Otherwise, return false.
            heroName = null;
            return false;
        }

        void GroupSettings(Dictionary<string, object> collection, LobbySetting[] settings, Func<Boolean> onInterupt = null)
        {
            var orderedSettings = settings.OrderByDescending(s => s.Name.Length); // Order the settings so longer names are matched first.

            bool matched = true;
            while (matched)
            {
                matched = false;

                // Test hook.
                if (onInterupt != null && onInterupt.Invoke())
                {
                    // If the hook handled the match, break.
                    matched = true;
                    break;
                }

                foreach (var lobbySetting in orderedSettings)
                {
                    // Match the setting name.
                    if (MatchLobbySetting(collection, lobbySetting))
                    {
                        // Indicate that a setting was matched.
                        matched = true;
                        break;
                    }
                }
            }
        }

        bool MatchLobbySetting(Dictionary<string, object> collection, LobbySetting setting)
        {
            // Match the setting name.
            if (Match(Kw(setting.Workshop), false))
            {
                Match(":"); // Match the value seperator.
                setting.Match(this, out object value); // Match the setting value.

                // Add the setting.
                collection.Add(setting.Name, value);
                return true;
            }
            return false;
        }
    }
}
