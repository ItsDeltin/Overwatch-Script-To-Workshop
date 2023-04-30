using System;
using System.Linq;

namespace Deltin.Deltinteger.Elements.WorkshopConverter;

static class ElementWorkshopWriter
{
    /// <summary>Writes an element to the workshop output.</summary>
    /// <param name="b">The workshop output builder.</param>
    /// <param name="element">The element that is written to the output.</param>
    public static void ElementToWorkshop(WorkshopBuilder b, Element element)
    {
        if (b.CStyle)
        {
            // c-style workshop output is enabled.
            var simplified = ISimplifyExpression.FromElement(element);
            simplified.ToWorkshop(b, Operated.Root());
        }
        else
            // c-style workshop output is disabled,
            // write to workshop normally.
            FunctionToWorkshop(b, element);
    }

    static void FunctionToWorkshop(WorkshopBuilder b, Element element)
    {
        // Number
        if (element is NumberElement numberElement)
        {
            b.Append(((decimal)numberElement.Value).ToString());
            return;
        }
        // String
        else if (element is StringElement stringElement)
        {
            StringToWorkshop(b, stringElement);
            return;
        }
        // Team
        else if (element.Function.Name == "Team")
        {
            var enumerator = (ElementEnumMember)element.ParameterValues[0];
            // Team 1
            if (enumerator.Name == "Team1")
                b.AppendKeyword("Team 1");
            // Team 2
            else if (enumerator.Name == "Team2")
                b.AppendKeyword("Team 2");
            // All teams
            else
                b.AppendKeyword("All Teams");
            return;
        }

        var action = element.Function as ElementJsonAction;
        if (action != null && (action.Indentation == "outdent" || action.Indentation == "drop")) b.Outdent();

        // Add a comment and newline
        if (element.Comment != null) b.AppendLine($"\"{element.Comment}\"");

        // Add the disabled tag if the element is disabled.
        if (element.Function is ElementJsonAction && element.Disabled) b.AppendKeyword("disabled").Append(" ");

        // Add the parameters.
        element.AddMissingParameters();

        if (!b.CStyle || !CStyleOutput(b, element))
            WriteDefault(b, element);

        if (action != null)
        {
            b.AppendLine(";");
            if (action.Indentation == "indent" || action.Indentation == "drop") b.Indent();
        }
    }

    /// <summary>Typical element workshop format. Adds names and parameters.</summary>
    static void WriteDefault(WorkshopBuilder b, Element element)
    {
        // Add the name of the element.
        b.AppendKeyword(element.Function.Name);

        // Add parameters.
        if (element.ParameterValues.Length > 0)
        {
            b.Append("(");
            element.ParametersToWorkshop(b);
            b.Append(")");
        }
    }

    /// <summary>Writes a String or Custom String to the workshop output.</summary>
    static void StringToWorkshop(WorkshopBuilder b, StringElement stringElement)
    {
        b.AppendKeyword(stringElement.Localized ? "String" : "Custom String");
        b.Append("(\"" + (stringElement.Localized ? b.Kw(stringElement.Value) : stringElement.Value) + "\"");

        if (stringElement.ParameterValues.Length > 0 && stringElement.IndexOfLastNotNullParameter() != -1)
        {
            b.Append(", ");
            stringElement.ParametersToWorkshop(b, true);
        }

        b.Append(")");
    }

    /// <summary>Attempts to write the provided element using workshop's C style syntax.
    /// Returns true if successful.</summary>
    static bool CStyleOutput(WorkshopBuilder b, Element element)
    {
        if (element.Function.Name == "Global Variable")
        {
            b.Append("Global.");
            element.ParameterValues[0].ToWorkshop(b, ToWorkshopContext.NestedValue);
            return true;
        }
        else if (element.Function.Name == "Player Variable")
        {
            element.ParameterValues[0].ToWorkshop(b, ToWorkshopContext.NestedValue);
            b.Append(".");
            element.ParameterValues[1].ToWorkshop(b, ToWorkshopContext.NestedValue);
            return true;
        }
        else if (element.Function.Name == "Set Global Variable")
        {
            WriteAssignment(b, element, null, 0, null, null, 1);
            return true;
        }
        else if (element.Function.Name == "Set Player Variable")
        {
            WriteAssignment(b, element, 0, 1, null, null, 2);
            return true;
        }
        else if (element.Function.Name == "Set Global Variable At Index")
        {
            WriteAssignment(b, element, null, 0, 1, null, 2);
            return true;
        }
        else if (element.Function.Name == "Set Player Variable At Index")
        {
            WriteAssignment(b, element, 0, 1, 2, null, 3);
            return true;
        }
        else if (element.Function.Name == "Modify Global Variable")
        {
            WriteAssignment(b, element, null, 0, null, 1, 2);
            return true;
        }
        else if (element.Function.Name == "Modify Player Variable")
        {
            WriteAssignment(b, element, 0, 1, null, 2, 3);
            return true;
        }
        else if (element.Function.Name == "Modify Global Variable At Index")
        {
            WriteAssignment(b, element, null, 0, 1, 2, 3);
            return true;
        }
        else if (element.Function.Name == "Modify Player Variable At Index")
        {
            WriteAssignment(b, element, 0, 1, 2, 3, 4);
            return true;
        }
        return false;
    }

    /// <summary>Writes a c-style variable assignment to the workshop output.</summary>
    /// <param name="b">The workshop output builder.</param>
    /// <param name="element">The assignment action.</param>
    /// <param name="target">The index of the target parameter. If null, this is a global variable assignment.</param>
    /// <param name="variable">The index of the variable parameter.</param>
    /// <param name="index">The index of the index parameter. If null, this is not setting a global or player variable
    /// at an index.</param>
    /// <param name="assignmentOperator">The index of the parameter containing the Modify Variable operator.
    /// If null, this is a normal Set Variable.</param>
    /// <param name="value">The value the variable is being set to or modified by.</param>
    static void WriteAssignment(WorkshopBuilder b, Element element, int? target, int variable, int? index, int? assignmentOperator, int value)
    {
        string op = "=";
        if (assignmentOperator.HasValue)
        {
            op = ModifyOpStringFromElement(element, assignmentOperator.Value);
            if (op == null)
            {
                WriteDefault(b, element);
                return;
            }
        }

        if (target.HasValue)
            element.ParameterValues[target.Value].ToWorkshop(b, ToWorkshopContext.NestedValue);
        else
            b.AppendKeyword("Global");

        b.Append(".");
        element.ParameterValues[variable].ToWorkshop(b, ToWorkshopContext.NestedValue);
        if (index.HasValue)
        {
            b.Append("[");
            element.ParameterValues[index.Value].ToWorkshop(b, ToWorkshopContext.NestedValue);
            b.Append("]");
        }
        b.Append(" " + op + " ");
        element.ParameterValues[value].ToWorkshop(b, ToWorkshopContext.NestedValue);
    }

    /// <summary>Extracts an operator string from an Modify Variable action.
    /// Array and min/max modifications will return null.</summary>
    static string ModifyOpStringFromElement(Element element, int operationParameterIndex)
    {
        if (operationParameterIndex < element.ParameterValues.Length && element.ParameterValues[operationParameterIndex] is OperationElement operationElement)
        {
            switch (operationElement.Operation)
            {
                case Operation.Add: return "+=";
                case Operation.Divide: return "/=";
                case Operation.Modulo: return "%=";
                case Operation.Multiply: return "*=";
                case Operation.RaiseToPower: return "^=";
                case Operation.Subtract: return "-=";
            }
        }
        return null;
    }

    const int ARRAY_SUBSCRIPT = 0;
    const int UNARY = 1;
    const int POWER = 2;
    const int MULTIPLICATION_DIVISION_REMAINDER = 3;
    const int ADDITION_SUBTRACTION = 4;
    const int COMPARISON = 5;
    const int RELATIONAL = 6;
    const int LOGICAL = 7;
    const int TERNARY = 8;

    record struct WorkshopOperator(Op Op, int Precedence)
    {
        public string WorkshopSymbol()
        {
            switch (Op)
            {
                case Op.Power: return "^";
                case Op.Multiply: return "*";
                case Op.Divide: return "/";
                case Op.Modulo: return "%";
                case Op.Add: return "+";
                case Op.Subtract: return "-";
                case Op.GreaterThan: return ">";
                case Op.LessThan: return "<";
                case Op.GreaterThanOrEqual: return ">=";
                case Op.LessThanOrEqual: return "<=";
                case Op.Equal: return "==";
                case Op.NotEqual: return "!=";
                case Op.Or: return "||";
                case Op.And: return "&&";
            }
            throw new NotImplementedException(Op.ToString());
        }
    }

    enum Op
    {
        Power,
        Multiply,
        Divide,
        Modulo,
        Add,
        Subtract,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Equal,
        NotEqual,
        Or,
        And
    }

    static readonly WorkshopOperator[] Operators = new WorkshopOperator[] {
        new(Op.Power, POWER),
        new(Op.Multiply, MULTIPLICATION_DIVISION_REMAINDER),
        new(Op.Divide, MULTIPLICATION_DIVISION_REMAINDER),
        new(Op.Modulo, MULTIPLICATION_DIVISION_REMAINDER),
        new(Op.Add, ADDITION_SUBTRACTION),
        new(Op.Subtract, ADDITION_SUBTRACTION),
        new(Op.GreaterThan, COMPARISON),
        new(Op.LessThan, COMPARISON),
        new(Op.GreaterThanOrEqual, COMPARISON),
        new(Op.LessThanOrEqual, COMPARISON),
        new(Op.Equal, RELATIONAL),
        new(Op.NotEqual, RELATIONAL),
        new(Op.Or, LOGICAL),
        new(Op.And, LOGICAL),
    };

    static Op? OpFromWorkshopValue(string valueName)
    {
        switch (valueName)
        {
            case "Power": return Op.Power;
            case "Multiply": return Op.Multiply;
            case "Divide": return Op.Divide;
            case "Modulo": return Op.Modulo;
            case "Add": return Op.Add;
            case "Subtract": return Op.Subtract;
            case "Or": return Op.Or;
            case "And": return Op.And;
            default: return null;
        }
    }

    static WorkshopOperator WorkshopOperatorFromOp(Op op) => Operators.First(o => o.Op == op);

    /// <summary>Gets the math binary operator from an element.</summary>
    static WorkshopOperator? GetBinaryFromElement(Element element)
    {
        var workshopOp = OpFromWorkshopValue(element.Function.Name);
        return Operators.Cast<WorkshopOperator?>().FirstOrDefault(op => workshopOp == op.Value.Op);
    }

    /// <summary>Finds the operator a Compare workshop value is using.</summary>
    static WorkshopOperator? GetComparisonFromElement(Element element)
    {
        if (element.Function.Name != "Compare")
            return null;

        var elementOp = ((OperatorElement)element.ParameterValues[1]).Operator;
        switch (elementOp)
        {
            case Operator.Equal: return WorkshopOperatorFromOp(Op.Equal);
            case Operator.GreaterThan: return WorkshopOperatorFromOp(Op.GreaterThan);
            case Operator.GreaterThanOrEqual: return WorkshopOperatorFromOp(Op.GreaterThanOrEqual);
            case Operator.LessThan: return WorkshopOperatorFromOp(Op.LessThan);
            case Operator.LessThanOrEqual: return WorkshopOperatorFromOp(Op.LessThanOrEqual);
            case Operator.NotEqual: return WorkshopOperatorFromOp(Op.NotEqual);
            default: throw new NotImplementedException(elementOp.ToString());
        }
    }

    /// <summary>Writes a value to the workshop, optionally containing it in parentheses.</summary>
    static void Group(bool parentheses, WorkshopBuilder builder, Action action)
    {
        if (parentheses)
            builder.Append("(");

        action();

        if (parentheses)
            builder.Append(")");
    }

    interface ISimplifyExpression
    {
        void ToWorkshop(WorkshopBuilder builder, Operated operated);

        static ISimplifyExpression FromElement(Element element)
        {
            // Unary
            if (element.Function.Name == "Not")
            {
                var a = FromElement((Element)element.ParameterValues[0]);
                return new UnaryOperation(a);
            }

            // Binary
            var op = GetBinaryFromElement(element);
            if (op.HasValue)
            {
                var a = FromElement((Element)element.ParameterValues[0]);
                var b = FromElement((Element)element.ParameterValues[1]);
                return new BinaryOperation(a, b, op.Value);
            }

            // comparison
            var comparison = GetComparisonFromElement(element);
            if (comparison.HasValue)
            {
                var a = FromElement((Element)element.ParameterValues[0]);
                var b = FromElement((Element)element.ParameterValues[2]);
                return new BinaryOperation(a, b, comparison.Value);
            }

            // Ternary
            if (element.Function.Name == "If-Then-Else")
            {
                var a = FromElement((Element)element.ParameterValues[0]);
                var b = FromElement((Element)element.ParameterValues[1]);
                var c = FromElement((Element)element.ParameterValues[2]);
                return new TernaryOperation(a, b, c);
            }

            // Value in array
            if (element.Function.Name == "Value In Array")
            {
                var value = FromElement((Element)element.ParameterValues[0]);
                var index = (Element)element.ParameterValues[1];
                return new SimplifyValueInArray(value, index);
            }

            return new SimplifyElement(element);
        }
    }
    record UnaryOperation(ISimplifyExpression value) : ISimplifyExpression
    {
        public void ToWorkshop(WorkshopBuilder builder, Operated operated)
        {
            bool parentheses = operated.Match(
                (left, right) => false,
                precedence => precedence < UNARY,
                () => false
            );
            Group(parentheses, builder, () =>
            {
                builder.Append("!");
                value.ToWorkshop(builder, Operated.All(1));
            });
        }
    }
    record BinaryOperation(ISimplifyExpression a, ISimplifyExpression b, WorkshopOperator op) : ISimplifyExpression
    {
        bool NeedsParentheses(WorkshopOperator? left, WorkshopOperator? right)
        {
            return left.HasValue && left.Value.Precedence < op.Precedence
                || right.HasValue && right.Value.Precedence < op.Precedence;
        }

        public void ToWorkshop(WorkshopBuilder builder, Operated operated)
        {
            var (left, right, parentheses) = operated.Match(
                (left, right) =>
                {
                    if (NeedsParentheses(left, right))
                        return (null, null, true);
                    else
                        return (left, right, false);
                },
                precedence => (null, null, true),
                () => (null, null, false)
            );
            var aOp = Operated.Binary(left, op);
            var bOp = Operated.Binary(op, right);
            Group(parentheses, builder, () =>
            {
                a.ToWorkshop(builder, aOp);
                builder.Append(" " + op.WorkshopSymbol() + " ");
                b.ToWorkshop(builder, bOp);
            });
        }
    }
    record TernaryOperation(ISimplifyExpression a, ISimplifyExpression b, ISimplifyExpression c) : ISimplifyExpression
    {
        public void ToWorkshop(WorkshopBuilder builder, Operated operated)
        {
            bool parentheses = operated.Match(
                binary: (left, right) => true,
                all: precedence => true,
                root: () => false,
                // The workshop can process ternaryValue just fine without parentheses but
                // with them it matches how the workshop outputs the ternaries when
                // you copy a gamemode.
                // Plus this allows overpy to decompile chained ternaries without pasting
                // to and from the game.
                ternaryValue: () => true
            );
            Group(parentheses, builder, () =>
            {
                a.ToWorkshop(builder, Operated.Root());
                builder.Append(" ? ");
                b.ToWorkshop(builder, Operated.TernaryValue());
                builder.Append(" : ");
                c.ToWorkshop(builder, Operated.TernaryValue());
            });
        }
    }
    record SimplifyValueInArray(ISimplifyExpression value, Element index) : ISimplifyExpression
    {
        public void ToWorkshop(WorkshopBuilder builder, Operated operated)
        {
            bool parentheses = operated.Match(
                (left, right) => false,
                precedence => false,
                () => false
            );
            Group(parentheses, builder, () =>
            {
                value.ToWorkshop(builder, Operated.All(ARRAY_SUBSCRIPT));
                builder.Append("[");
                index.ToWorkshop(builder, ToWorkshopContext.NestedValue);
                builder.Append("]");
            });
        }
    }
    record struct SimplifyElement(Element element) : ISimplifyExpression
    {
        public void ToWorkshop(WorkshopBuilder builder, Operated operated) => FunctionToWorkshop(builder, element);
    }

    struct Operated
    {
        public T Match<T>(
            Func<WorkshopOperator?, WorkshopOperator?, T> binary,
            Func<int, T> all,
            Func<T> root,
            Func<T> ternaryValue = null
        )
        {
            switch (opType)
            {
                case OperatedType.Binary: return binary(left, right);
                case OperatedType.All: return all(precedence);
                case OperatedType.TernaryRhs: return (ternaryValue ?? root).Invoke();
                case OperatedType.Root: default: return root();
            }
        }

        OperatedType opType;
        WorkshopOperator? left;
        WorkshopOperator? right;
        int precedence;

        private Operated(OperatedType opType) => (this.opType, left, right, precedence) = (opType, default, default, default);
        private Operated(OperatedType opType, WorkshopOperator? left, WorkshopOperator? right, int precedence)
        {
            this.opType = opType;
            this.left = left;
            this.right = right;
            this.precedence = 0;
        }

        public static Operated Root() => new(OperatedType.Root);
        public static Operated All(int precedence) => new(OperatedType.All, default, default, precedence);
        public static Operated Binary(WorkshopOperator? left, WorkshopOperator? right)
        {
            // Either left or right should have a value.
            if (!left.HasValue && !right.HasValue)
                throw new Exception("Binary give no operation");
            return new(OperatedType.Binary, left, right, default);
        }
        public static Operated TernaryValue() => new Operated(OperatedType.TernaryRhs);

        enum OperatedType
        {
            Root,
            All,
            Binary,
            TernaryRhs
        }
    }
}