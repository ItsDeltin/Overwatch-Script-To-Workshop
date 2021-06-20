namespace Deltin.Deltinteger.Elements
{
    public class Condition
    {
        public Element Value1 { get; private set; }
        public OperatorElement CompareOperator { get; private set; }
        public Element Value2 { get; private set; }
        public string Comment { get; set; }

        public Condition(Element value1, OperatorElement op, Element value2)
        {
            Value1 = value1;
            CompareOperator = op;
            Value2 = value2;
        }
        public Condition(Element value1, Operator op, Element value2) : this(value1, new OperatorElement(op), value2) {}
        public Condition(Element condition) : this(condition, Operator.Equal, Element.True()) {}

        public void ToWorkshop(WorkshopBuilder builder)
        {
            string result = string.Empty;

            // Add a comment and newline
            if (Comment != null) builder.AppendLine($"\"{Comment}\"\n");
            
            Value1.ToWorkshop(builder, ToWorkshopContext.ConditionValue);
            builder.Append(" ");
            CompareOperator.ToWorkshop(builder, ToWorkshopContext.Other);
            builder.Append(" ");
            Value2.ToWorkshop(builder, ToWorkshopContext.ConditionValue);
            builder.Append(";");
            builder.AppendLine();
        }

        public int ElementCount() => (Value1.ElementCount() + Value2.ElementCount()) - 1;

        public Condition Optimized() => new Condition(Value1.Optimized(), CompareOperator, Value2.Optimized());

        public static implicit operator Condition(Element element) => new Condition(element);
    }
}
