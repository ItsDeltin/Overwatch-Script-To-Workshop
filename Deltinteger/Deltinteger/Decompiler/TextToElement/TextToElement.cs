using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    class ConvertTextToElement
    {
        private readonly static char[] WHITESPACE = new char[] { '\r', '\n', '\t', ' ' };

        public string Content { get; }
        public int Position { get; private set; }
        public char Current => Content[Position];
        public bool ReachedEnd => Position >= Content.Length;

        private readonly Stack<TTEOperator> _operators = new Stack<TTEOperator>();
        private readonly Stack<ITTEExpression> _operands = new Stack<ITTEExpression>();

        private readonly string[] _actions;
        private readonly string[] _values;

        public List<WorkshopVariable> Variables { get; } = new List<WorkshopVariable>();
        public List<Subroutine> Subroutines { get; } = new List<Subroutine>();
        public List<TTERule> Rules { get; } = new List<TTERule>();

        public ConvertTextToElement(string content)
        {
            Content = content;
            _actions = ElementList.Elements.Where(e => !e.IsValue).Select(e => e.WorkshopName).OrderByDescending(e => e.Length).ToArray();
            _values = ElementList.Elements.Where(e => e.IsValue).Select(e => e.WorkshopName).OrderByDescending(e => e.Length).ToArray();
            _operators.Push(TTEOperator.Sentinel);
        }

        public void Get()
        {
            // Match variables and subroutines.
            MatchVariables();
            MatchSubroutines();
            // Match rules
            while (Rule(out TTERule rule)) Rules.Add(rule);
        }

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
        }

        bool Is(char character) => !ReachedEnd && Current == character;
        bool Is(int position, char character) => Position + position < Content.Length && Content[Position + position] == character;
        bool IsSymbol(int position) => Position + position < Content.Length && char.IsSymbol(Content[Position + position]);
        bool IsAny(params char[] characters) => !ReachedEnd && characters.Contains(Current);
        bool IsAny(string characters) => IsAny(characters.ToCharArray());
        bool IsNumeric() => IsAny("0123456789");
        bool IsAlpha() => IsAny("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        bool IsAlphaNumeric() => IsNumeric() || IsAlpha();

        bool Match(string str)
        {
            for (int i = 0; i < str.Length; i++)
                if (!Is(i, str[i]))
                    return false;
            
            if (IsSymbol(str.Length)) return false;

            Advance(str.Length);
            SkipWhitespace();
            return true;
        }

        // Commons
        // String
        bool MatchString(out string value)
        {
            if (!Match("\""))
            {
                value = null;
                return false;
            }

            value = "";
            bool escaped = false;
            do
            {
                value += Current;
                if (escaped) escaped = false;
                else if (Is(0, '"')) escaped = true;
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

        // Integer
        bool Integer(out int value)
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
        bool Double(out double number)
        {
            string str = "";

            if (Match("-")) str += "-";

            while (IsNumeric())
            {
                str += Current;
                Advance();
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

            if (str == "")
            {
                number = 0;
                return false;
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
            if (!Match("variables")) return false;
            Match("{");

            if (Match("global:")) MatchVariableList(true);
            if (Match("player:")) MatchVariableList(false);
            
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

        // Subroutines
        bool MatchSubroutines()
        {
            if (!Match("subroutines")) return false;
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
            if (!Match("rule"))
            {
                rule = null;
                return false;
            }

            Match("(");
            MatchString(out string ruleName);
            Match(")");
            Match("{");

            // Event
            Match("event");
            Match("{");
            EventInfo eventInfo = MatchEvent();
            Match("}");

            // Conditions
            List<ITTEExpression> conditions = new List<ITTEExpression>();
            if (Match("conditions"))
            {
                Match("{");
                while (Expression(out ITTEExpression condition))
                {
                    Match(";");
                    conditions.Add(condition);
                }
                Match("}");
            }

            // Actions
            List<ITTEAction> actions = new List<ITTEAction>(); 
            if (Match("actions"))
            {
                Match("{");
                while (Action(out ITTEAction action)) actions.Add(action);
                Match("}");
            }

            Match("}");

            rule = new TTERule(ruleName, eventInfo, conditions.ToArray(), actions.ToArray());
            return true;
        }

        EventInfo MatchEvent()
        {
            // Global
            if (Match("Ongoing - Global;"))
            {
                return new EventInfo();
            }
            // Subroutine
            else if (Match("Subroutine;"))
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
                    if (Match(eventNameInfo.Item1 + ";"))
                    {
                        ruleEvent = eventNameInfo.Item2;
                        break;
                    }

                // Get the player type.
                var player = PlayerSelector.All;
                foreach (var playerNameInfo in EventInfo.PlayerTypeNames)
                    if (Match(playerNameInfo.Item1 + ";"))
                    {
                        player = playerNameInfo.Item2;
                        break;
                    }

                // Get the team.
                var team = Team.All;
                if (Match("All;")) {}
                else if (Match("Team 1;"))
                    team = Team.Team1;
                else if (Match("Team 2;"))
                    team = Team.Team2;

                return new EventInfo(ruleEvent, player, team);
            }
        }

        // Actions
        bool Action(out ITTEAction action)
        {
            action = null;

            // Comment
            MatchString(out string comment);

            // Function.
            if (Function(true, out FunctionExpression func))
            {
                action = func;
            }
            // Set variable.
            else if (Expression(out ITTEExpression expr, getIndexer: false))
            {
                // Make sure the expression is a global or player variable.
                if (expr is ITTEVariable == false)
                    throw new Exception("Expression is not a variable.");
                
                // Get the index being set.
                VariableIndex(out var index);
                
                string op = null;
                string[] operators = new string[] { "=", "+=", "-=", "/=", "*=" };
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

        bool Function(string name, out FunctionExpression expr)
        {
            if (!Match(name))
            {
                expr = null;
                return false;
            }

            // Get the parameter values.
            List<ITTEExpression> values = new List<ITTEExpression>();
            if (Match("("))
            {
                _operators.Push(TTEOperator.Sentinel);
                do
                {
                    if (Expression(out ITTEExpression value))
                        values.Add(value);
                }
                while (Match(","));
                Match(")");
                _operators.Pop();
            }

            expr = new FunctionExpression(name, values.ToArray());
            return true;
        }

        // Expressions
        bool Expression(out ITTEExpression expr, bool root = true, bool getIndexer = true)
        {
            expr = null;

            // Group
            if (Match("("))
            {
                _operators.Push(TTEOperator.Sentinel);
                Expression(out expr);
                Match(")");
                _operators.Pop();
            }
            // Number
            else if (Number(out expr))
            {
            }
            // Variable
            else if (GlobalVariable(out expr))
            {
            }
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
            // Anonymous variable.
            else if (root && Identifier(out string identifier))
            {
                expr = new AnonymousVariableExpression(identifier);
                return true; // Anonymous variable should be a self contained expression.
            }
            // No matches
            else
            {
                return false;    
            }
            
            // Player variable
            if (MatchPlayerVariable(expr, out ITTEExpression playerVariable))
                expr = playerVariable;
            
            // Array index
            if (getIndexer)
            {
                while (VariableIndex(out ITTEExpression index))
                    expr = new IndexerExpression(expr, index);
            }

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

            // Ternary conditional
            if (Match("?"))
            {
                Expression(out ITTEExpression consequent);
                Match(":");
                Expression(out ITTEExpression alternative);
                expr = new TernaryExpression(expr, consequent, alternative);
            }

            return true;
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
            
            expr = new GlobalVariableExpression(name);
            return true;
        }

        bool MatchPlayerVariable(ITTEExpression parent, out ITTEExpression playerVariable)
        {
            int c = Position; // Revert

            string name = null;
            bool result = parent != null
                && Match(".")
                && Identifier(out name);
            
            if (!result)
            {
                playerVariable = null;
                Position = c;
                return false;
            }
            
            playerVariable = new PlayerVariableExpression(name, parent);
            return true;
        }

        bool VariableIndex(out ITTEExpression index)
        {
            if (!Match("["))
            {
                index = null;
                return false;
            }

            Expression(out index);
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
            while (_operators.Peek().Precedence > op.Precedence)
                PopOperator();
            _operators.Push(op);
        }

        void PopOperator()
        {
            var op = _operators.Pop();
            if (op.Binary)
            {
                var right = _operands.Pop();
                var left = _operands.Pop();
                _operands.Push(new BinaryOperatorExpression(left, right, op));
            }
            else
            {
                var value = _operands.Pop();
                _operands.Push(new UnaryOperatorExpression(value, op));
            }
        }
    }

    public class EventInfo
    {
        public static readonly (string, RuleEvent)[] PlayerEventNames = new (string, RuleEvent)[] {
            ("Ongoing - Each Player", RuleEvent.OngoingPlayer),
            ("Player Earned Elimination", RuleEvent.OnElimination),
            ("Player Dealt Final Blow", RuleEvent.OnFinalBlow),
            ("Player Dealt Damage", RuleEvent.OnDamageDealt),
            ("Player Took Damage", RuleEvent.OnDamageTaken),
            ("Player Died", RuleEvent.OnDeath),
            ("Player Dealt Healing", RuleEvent.OnHealingDealt),
            ("Player Received Healing", RuleEvent.OnHealingTaken),
            ("Player Joined Match", RuleEvent.OnPlayerJoin),
            ("Player Left Match", RuleEvent.OnPlayerLeave),
            ("Player Dealt Knockback", RuleEvent.PlayerDealtKnockback),
            ("Player Received Knockback", RuleEvent.PlayerReceivedKnockback)
        };
        public static readonly (string, PlayerSelector)[] PlayerTypeNames = new (string, PlayerSelector)[] {
            ("All", PlayerSelector.All),
            ("Ana", PlayerSelector.Ana),
            ("Ashe", PlayerSelector.Ashe),
            ("Baptiste", PlayerSelector.Baptiste),
            ("Bastion", PlayerSelector.Bastion),
            ("Brigitte", PlayerSelector.Brigitte),
            ("Doomfist", PlayerSelector.Doomfist),
            ("D.va", PlayerSelector.Dva),
            ("Echo", PlayerSelector.Echo),
            ("Genji", PlayerSelector.Genji),
            ("Hanzo", PlayerSelector.Hanzo),
            ("Junkrat", PlayerSelector.Junkrat),
            ("Lúcio", PlayerSelector.Lucio),
            ("Mccree", PlayerSelector.Mccree),
            ("Mei", PlayerSelector.Mei),
            ("Mercy", PlayerSelector.Mercy),
            ("Moira", PlayerSelector.Moira),
            ("Orisa", PlayerSelector.Orisa),
            ("Pharah", PlayerSelector.Pharah),
            ("Reaper", PlayerSelector.Reaper),
            ("Reinhardt", PlayerSelector.Reinhardt),
            ("Roadhog", PlayerSelector.Roadhog),
            ("Sigma", PlayerSelector.Sigma),
            ("Slot 0", PlayerSelector.Slot0),
            ("Slot 1", PlayerSelector.Slot1),
            ("Slot 2", PlayerSelector.Slot2),
            ("Slot 3", PlayerSelector.Slot3),
            ("Slot 4", PlayerSelector.Slot4),
            ("Slot 5", PlayerSelector.Slot5),
            ("Slot 6", PlayerSelector.Slot6),
            ("Slot 7", PlayerSelector.Slot7),
            ("Slot 8", PlayerSelector.Slot8),
            ("Slot 9", PlayerSelector.Slot9),
            ("Slot 10", PlayerSelector.Slot10),
            ("Slot 11", PlayerSelector.Slot11),
            ("Soldier: 76", PlayerSelector.Soldier76),
            ("Sombra", PlayerSelector.Sombra),
            ("Symmetra", PlayerSelector.Symmetra),
            ("Torbjörn", PlayerSelector.Torbjorn),
            ("Tracer", PlayerSelector.Tracer),
            ("Widowmaker", PlayerSelector.Widowmaker),
            ("Winston", PlayerSelector.Winston),
            ("Wrecking Ball", PlayerSelector.WreckingBall),
            ("Zarya", PlayerSelector.Zarya),
            ("Zenyatta", PlayerSelector.Zenyatta)
        };
        public RuleEvent Event { get; }
        public PlayerSelector Player { get; }
        public Team Team { get; }
        public string SubroutineName { get; }

        public EventInfo()
        {
        }
        public EventInfo(string subroutineName)
        {
            Event = RuleEvent.Subroutine;
            SubroutineName = subroutineName;
        }
        public EventInfo(RuleEvent ruleEvent, PlayerSelector player, Team team)
        {
            Event = ruleEvent;
            Player = player;
            Team = team;
        }
    }

    public class TTEOperator
    {
        public static TTEOperator Sentinel { get; } = new TTEOperator(0, null);
        // Unary
        public static TTEOperator Not { get; } = new TTEOperator(16, "!", false);
        // Compare
        public static TTEOperator Equal { get; } = new TTEOperator(2, "==");
        public static TTEOperator NotEqual { get; } = new TTEOperator(3, "!=");
        public static TTEOperator GreaterThan { get; } = new TTEOperator(4, ">");
        public static TTEOperator LessThan { get; } = new TTEOperator(5, "<");
        public static TTEOperator GreaterThanOrEqual { get; } = new TTEOperator(6, ">=");
        public static TTEOperator LessThanOrEqual { get; } = new TTEOperator(7, "<=");
        // Boolean
        public static TTEOperator And { get; } = new TTEOperator(8, "&&");
        public static TTEOperator Or { get; } = new TTEOperator(9, "||");
        // Math
        public static TTEOperator Subtract { get; } = new TTEOperator(10, "-");
        public static TTEOperator Add { get; } = new TTEOperator(11, "+");
        public static TTEOperator Modulo { get; } = new TTEOperator(12, "%");
        public static TTEOperator Divide { get; } = new TTEOperator(13, "/");
        public static TTEOperator Multiply { get; } = new TTEOperator(14, "*");
        public static TTEOperator Power { get; } = new TTEOperator(15, "^");

        public int Precedence { get; }
        public string Operator { get; }
        public bool Binary { get; }

        public TTEOperator(int precedence, string op, bool binary = true)
        {
            Precedence = precedence;
            Operator = op;
            Binary = binary;
        }
    }

    public class TTERule
    {
        public string Name { get; }
        public EventInfo EventInfo { get; }
        public ITTEExpression[] Conditions { get; }
        public ITTEAction[] Actions { get; }

        public TTERule(string name, EventInfo eventInfo, ITTEExpression[] conditions, ITTEAction[] actions)
        {
            Name = name;
            EventInfo = eventInfo;
            Conditions = conditions;
            Actions = actions;
        }
    }

    // Interfaces
    public interface ITTEExpression {}
    public interface ITTEAction {
        string Comment { get; set; }
    }
    public interface ITTEVariable
    {
        string Name { get; }
    }
    // Expressions
    public class NumberExpression : ITTEExpression
    {
        public double Value { get; }

        public NumberExpression(double value)
        {
            Value = value;
        }

        public override string ToString() => Value.ToString();
    }
    public class BinaryOperatorExpression : ITTEExpression
    {
        public ITTEExpression Left { get; }
        public ITTEExpression Right { get; }
        public TTEOperator Operator { get; }

        public BinaryOperatorExpression(ITTEExpression left, ITTEExpression right, TTEOperator op)
        {
            Left = left;
            Right = right;
            Operator = op;
        }

        public override string ToString() => Left.ToString() + " " + Operator.Operator + " " + Right.ToString(); 
    }
    public class UnaryOperatorExpression : ITTEExpression
    {
        public ITTEExpression Value { get; }
        public TTEOperator Operator { get; }

        public UnaryOperatorExpression(ITTEExpression value, TTEOperator op)
        {
            Value = value;
            Operator = op;
        }

        public override string ToString() => Operator.Operator + Value.ToString();
    }
    public class FunctionExpression : ITTEExpression, ITTEAction
    {
        public string Name { get; }
        public ITTEExpression[] Values { get; }
        public string Comment { get; set; }

        public FunctionExpression(string name, ITTEExpression[] values)
        {
            Name = name;
            Values = values;
        }

        public override string ToString() => Name + (Values.Length == 0 ? "" : "(" + string.Join(", ", Values.Select(v => v.ToString())) + ")");
    }
    public class GlobalVariableExpression : ITTEExpression, ITTEVariable
    {
        public string Name { get; }

        public GlobalVariableExpression(string name)
        {
            Name = name;
        }

        public override string ToString() => "Global." + Name;
    }
    public class AnonymousVariableExpression : ITTEExpression, ITTEVariable
    {
        public string Name { get; }

        public AnonymousVariableExpression(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;
    }
    public class PlayerVariableExpression : ITTEExpression, ITTEVariable
    {
        public string Name { get; }
        public ITTEExpression Player { get; }

        public PlayerVariableExpression(string name, ITTEExpression player)
        {
            Name = name;
            Player = player;
        }

        public override string ToString() => Player.ToString() + "." + Name;
    }
    public class IndexerExpression : ITTEExpression
    {
        public ITTEExpression Expression { get; }
        public ITTEExpression Index { get; }

        public IndexerExpression(ITTEExpression expression, ITTEExpression index)
        {
            Expression = expression;
            Index = index;
        }

        public override string ToString() => Expression.ToString() + "[" + Index.ToString() + "]";
    }
    public class TernaryExpression : ITTEExpression
    {
        public ITTEExpression Condition { get; }
        public ITTEExpression Consequent { get; }
        public ITTEExpression Alternative { get; }

        public TernaryExpression(ITTEExpression condition, ITTEExpression consequent, ITTEExpression alternative)
        {
            Condition = condition;
            Consequent = consequent;
            Alternative = alternative;
        }
    }
    // Actions
    public class SetVariableAction : ITTEAction
    {
        public ITTEVariable Variable { get; }
        public string Operator { get; }
        public ITTEExpression Value { get; }
        public ITTEExpression Index { get; }
        public string Comment { get; set; }

        public SetVariableAction(ITTEVariable variable, string op, ITTEExpression value, ITTEExpression index)
        {
            Variable = variable;
            Operator = op;
            Value = value;
            Index = index;
        }

        public override string ToString() => Variable.ToString() + (Index == null ? " " : " [" + Index.ToString() + "]") + Operator + " " + Value.ToString();
    }
}