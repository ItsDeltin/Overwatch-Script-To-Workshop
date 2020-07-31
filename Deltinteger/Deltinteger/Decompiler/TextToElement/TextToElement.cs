using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    public class ConvertTextToElement
    {
    }

    class WorkshopWalker
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

        public List<ITTEAction> Actions { get; } = new List<ITTEAction>();

        public WorkshopWalker(string content)
        {
            Content = content;
            _actions = ElementList.Elements.Where(e => !e.IsValue).Select(e => e.WorkshopName).OrderByDescending(e => e.Length).ToArray();
            _values = ElementList.Elements.Where(e => e.IsValue).Select(e => e.WorkshopName).OrderByDescending(e => e.Length).ToArray();
            _operators.Push(TTEOperator.Sentinel);
        }

        public void Get()
        {
            // Match rules
            while (Rule());
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
        bool Variables()
        {
            if (!Match("variables")) return false;
            Match("{");

            // TODO: match variables
            
            Match("}");
            return true;
        }

        // Rules
        bool Rule()
        {
            if (!Match("rule")) return false;

            Match("(");
            MatchString(out string ruleName);
            Match(")");
            Match("{");

            // Event
            Match("event");
            Match("{");
            Match("Ongoing - Global;");
            Match("}");

            // Actions
            Match("actions");
            Match("{");
            while (Action(out ITTEAction action)) Actions.Add(action);
            Match("}");

            Match("}");
            return true;
        }

        // Actions
        bool Action(out ITTEAction action)
        {
            action = null;
            // Action.
            if (Function(true, out FunctionExpression func))
            {
                action = func;
            }
            // Set variable.
            else if (Expression(out ITTEExpression expr))
            {
                // Make sure the expression is a global or player variable.
                if (expr is ITTEVariable == false)
                    throw new Exception("Expression is not a variable.");
                
                // TODO: +=, -=, etc. Also arrays
                Match("=");
                Expression(out ITTEExpression value);
                action = new SetVariableAction((ITTEVariable)expr, "=", value);
            }
            // Unknown.
            else
            {
                return false;
            }

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
                do
                {
                    if (Expression(out ITTEExpression value))
                        values.Add(value);
                }
                while (Match(","));
                Match(")");
            }

            expr = new FunctionExpression(name, values.ToArray());
            return true;
        }

        // Expressions
        bool Expression(out ITTEExpression expr, bool root = true)
        {
            expr = null;

            // Number
            if (Number(out expr))
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
            
            // No matches
            if (expr == null)
                return false;
            
            // Push the expression
            _operands.Push(expr);
            
            // Player variable
            if (MatchPlayerVariable(expr, out ITTEExpression playerVariable))
                expr = playerVariable;
            
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
    
        // Operators
        bool MatchOperator(out TTEOperator op)
        {
            if (Match("-"))
                op = TTEOperator.Subtract;
            else if (Match("+"))
                op = TTEOperator.Add;
            else if (Match("%"))
                op = TTEOperator.Modulo;
            else if (Match("/"))
                op = TTEOperator.Divide;
            else if (Match("*"))
                op = TTEOperator.Multiply;
            else if (Match("^"))
                op = TTEOperator.Power;
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
            var right = _operands.Pop();
            var left = _operands.Pop();
            var op = _operators.Pop();
            _operands.Push(new BinaryOperatorExpression(left, right, op));
        }
    }

    public class TTEOperator
    {
        public static TTEOperator Sentinel { get; } = new TTEOperator(0, null);
        public static TTEOperator Subtract { get; } = new TTEOperator(1, "-");
        public static TTEOperator Add { get; } = new TTEOperator(2, "+");
        public static TTEOperator Modulo { get; } = new TTEOperator(3, "%");
        public static TTEOperator Divide { get; } = new TTEOperator(4, "/");
        public static TTEOperator Multiply { get; } = new TTEOperator(5, "*");
        public static TTEOperator Power { get; } = new TTEOperator(6, "^");

        public int Precedence { get; }
        public string Operator { get; }

        public TTEOperator(int precedence, string op)
        {
            Precedence = precedence;
            Operator = op;
        }
    }

    // Interfaces
    public interface ITTEExpression {}
    public interface ITTEAction {}
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
    public class FunctionExpression : ITTEExpression, ITTEAction
    {
        public string Name { get; }
        public ITTEExpression[] Values { get; }

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
    // Actions
    public class SetVariableAction : ITTEAction
    {
        public ITTEVariable Variable { get; }
        public string Operator { get; }
        public ITTEExpression Value { get; }

        public SetVariableAction(ITTEVariable variable, string op, ITTEExpression value)
        {
            Variable = variable;
            Operator = op;
            Value = value;
        }

        public override string ToString() => Variable.ToString() + " " + Operator + " " + Value.ToString();
    }
}