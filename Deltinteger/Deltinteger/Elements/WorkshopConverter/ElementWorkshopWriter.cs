using System;
using System.Linq;

namespace Deltin.Deltinteger.Elements.WorkshopConverter;

static class ElementWorkshopWriter
{
    /// <summary>Writes an element to the workshop output.</summary>
    /// <param name="b">The workshop output builder.</param>
    /// <param name="element">The element that is written to the output.</param>
    /// <param name="isCondition">Is the element a condition in a rule?</param>
    public static void ElementToWorkshop(WorkshopBuilder b, Element element, bool isCondition)
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
            FunctionToWorkshop(b, element, isCondition);
    }

    static void FunctionToWorkshop(WorkshopBuilder b, Element element, bool isCondition)
    {
        // Add the parameters.
        element.AddMissingParameters();

        // Number
        if (element is NumberElement numberElement)
        {
            b.Append(DoubleToWorkshop.ToWorkshop(numberElement.Value));
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
        // Special compare output for conditions.
        else if (element.Function.Name == "Compare" && isCondition)
        {
            element.ParameterValues[0].ToWorkshop(b, ToWorkshopContext.NestedValue);
            b.Append(" ");
            element.ParameterValues[1].ToWorkshop(b, ToWorkshopContext.NestedValue);
            b.Append(" ");
            element.ParameterValues[2].ToWorkshop(b, ToWorkshopContext.NestedValue);
            return;
        }

        var action = element.Function as ElementJsonAction;
        if (action != null && (action.Indentation == "outdent" || action.Indentation == "drop")) b.Outdent(2);

        // Add a comment and newline
        if (element.Comment != null) b.AppendLine($"\"{element.Comment}\"");

        // Add the disabled tag if the element is disabled.
        if (element.Function is ElementJsonAction && element.Disabled) b.AppendKeyword("disabled").Append(" ");

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

    /// <summary>Gets the math binary operator from an element.
    /// Returns null if the element is not a math value.</summary>
    static Op? GetBinaryFromElement(Element element)
    {
        switch (element.Function.Name)
        {
            case "Raise To Power": return Op.Power;
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

    /// <summary>Finds the operator a Compare workshop value is using.
    /// Returns null if the element is not a Compare value.</summary>
    static Op? GetComparisonFromElement(Element element)
    {
        if (element.Function.Name != "Compare")
            return null;

        var elementOp = ((OperatorElement)element.ParameterValues[1]).Operator;
        switch (elementOp)
        {
            case Operator.Equal: return Op.Equal;
            case Operator.GreaterThan: return Op.GreaterThan;
            case Operator.GreaterThanOrEqual: return Op.GreaterThanOrEqual;
            case Operator.LessThan: return Op.LessThan;
            case Operator.LessThanOrEqual: return Op.LessThanOrEqual;
            case Operator.NotEqual: return Op.NotEqual;
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

    /// <summary>ISimplifyExpression takes a graph of Elements to export in c-style syntax.</summary>
    interface ISimplifyExpression
    {
        /// <summary>Writes the simplification graph to the workshop.</summary>
        /// <param name="builder">The builder generating the workshop output.</param>
        /// <param name="operated">Contains details about how this value in the graph is being
        /// operated on. Implementations will determine using this data if their output will
        /// need to be parenthesized.</param>
        void ToWorkshop(WorkshopBuilder builder, Operated operated);

        /// <summary>Generates the simplification graph from an element.</summary>
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
    /// <summary>A unary operation in the workshop. This only deals with the Not expression.</summary>
    record UnaryOperation(ISimplifyExpression value) : ISimplifyExpression
    {
        public void ToWorkshop(WorkshopBuilder builder, Operated operated)
        {
            bool parentheses = operated.Match(
                (left, right) => false,
                precedence => precedence < WorkshopOperators.UNARY,
                () => false
            );
            Group(parentheses, builder, () =>
            {
                builder.Append("!");
                value.ToWorkshop(builder, Operated.All(1));
            });
        }
    }
    /// <summary>A mathematical or comparing expression.</summary>
    record BinaryOperation(ISimplifyExpression a, ISimplifyExpression b, Op op) : ISimplifyExpression
    {
        bool NeedsParentheses(Op? left, Op? right)
        {
            int precedence = WorkshopOperators.GetPrecedence(op);
            return left.HasValue && WorkshopOperators.GetPrecedence(left.Value) < precedence
                || right.HasValue && WorkshopOperators.GetPrecedence(right.Value) < precedence
                || left.HasValue && left == op && WorkshopOperators.IsNonAssociative(op);
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
                builder.Append(" " + WorkshopOperators.WorkshopSymbolFromOp(op) + " ");
                b.ToWorkshop(builder, bOp);
            });
        }
    }
    /// <summary>If-Then-Else</summary>
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
    /// <summary>Value In Array</summary>
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
                value.ToWorkshop(builder, Operated.All(WorkshopOperators.ARRAY_SUBSCRIPT));
                builder.Append("[");
                index.ToWorkshop(builder, ToWorkshopContext.NestedValue);
                builder.Append("]");
            });
        }
    }
    /// <summary>A normal workshop value. The end of a branch in the simplification graph.</summary>
    record struct SimplifyElement(Element element) : ISimplifyExpression
    {
        public void ToWorkshop(WorkshopBuilder builder, Operated operated) => FunctionToWorkshop(builder, element, false);
    }

    /// <summary>This is passed to an ISimplifyExpression to describe how that expression is being used.</summary>
    struct Operated
    {
        /// <summary>The titular method which exhaustively matches each way an expression may be operated on.</summary>
        /// <typeparam name="T">An arbritrary type that is returned from each of the matches.</typeparam>
        /// <param name="binary">The expression is being added or compared to. The first parameter is the operator
        /// on the left, the second parameter is the operator on the right. One or both of the parameters will not be null.</param>
        /// <param name="all">Similiar to binary, but denotes that the entire expression is being operated on rather than
        /// modifications from either direction. Ie: Value In Array.</param>
        /// <param name="root">This is the root value in the Operated tree, which means it is not being operated on.
        /// Nothing special should happen.</param>
        /// <param name="ternaryValue">A special condition that an expression is being used in an If-Then-Else.
        /// If not provided with a value, the `root` func parameter is called instead. This exists so that nested
        /// If-Then-Else values can wrap themselves in parentheses. The workshop can actually read these just fine
        /// without the parentheses, but it's consistent with the workshop code copied back from the game. This also allows
        /// allows nested If-Then-Elses in the c-syntax to be decompiled by overpy.</param>
        /// <returns>The value returned by the chosen match.</returns>
        public T Match<T>(
            Func<Op?, Op?, T> binary,
            Func<int, T> all,
            Func<T> root,
            Func<T> ternaryValue = null
        )
        {
            switch (opType)
            {
                case OperatedType.Binary: return binary(left, right);
                case OperatedType.All: return all(precedence);
                case OperatedType.Ternary: return (ternaryValue ?? root).Invoke();
                case OperatedType.Root: default: return root();
            }
        }

        /// <summary>Denotes that an expression is not being operated on and is actually the root
        /// expression in the simplification tree.</summary>
        public static Operated Root() => new(OperatedType.Root);
        /// <summary>An operation is occuring on the entirety of the subgraph.</summary>
        /// <param name="precedence">The precedence of the operation.</param>
        public static Operated All(int precedence) => new(OperatedType.All, default, default, precedence);
        /// <summary>An binary operation from the left and/or right is occuring to a value in the graph.</summary>
        /// <param name="left">The operation from the left-hand side.</param>
        /// <param name="right">The operation from the right-hand size.</param>
        /// <exception cref="ArgumentException">An exception is thrown if both left and right parameters are null.</exception>
        public static Operated Binary(Op? left, Op? right)
        {
            // Either left or right should have a value.
            if (!left.HasValue && !right.HasValue)
                throw new ArgumentException("Binary give no operation");
            return new(OperatedType.Binary, left, right, default);
        }
        /// <summary>A expression is being used as the result or alternative of an If-Then-Else.</summary>
        public static Operated TernaryValue() => new Operated(OperatedType.Ternary);

        OperatedType opType;
        // 'left' and 'right' are only used if OperatedType is Binary.
        Op? left;
        Op? right;
        // 'precedence' is only used if OperatedType is All.
        int precedence;

        private Operated(OperatedType opType) => (this.opType, left, right, precedence) = (opType, default, default, default);
        private Operated(OperatedType opType, Op? left, Op? right, int precedence)
        {
            this.opType = opType;
            this.left = left;
            this.right = right;
            this.precedence = 0;
        }

        enum OperatedType
        {
            Root,
            All,
            Binary,
            Ternary
        }
    }
}