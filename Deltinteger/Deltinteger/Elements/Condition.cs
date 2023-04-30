namespace Deltin.Deltinteger.Elements
{
    public class Condition
    {
        public string Comment { get; set; }
        readonly Element element;

        public Condition(Element element) => this.element = element;
        public Condition(Element value1, Operator op, Element value2) : this(Element.Compare(value1, op, value2)) { }

        public void ToWorkshop(WorkshopBuilder builder)
        {
            string result = string.Empty;

            // Add a comment and newline
            if (Comment != null) builder.AppendLine($"\"{Comment}\"\n");

            element.ToWorkshop(builder, ToWorkshopContext.ConditionValue);

            if (element.Function.Name != "Compare")
            {
                builder.Append(" ");
                new OperatorElement(Operator.Equal).ToWorkshop(builder, ToWorkshopContext.Other);
                builder.Append(" ");
                Element.True().ToWorkshop(builder, ToWorkshopContext.ConditionValue);
            }

            builder.Append(";");
            builder.AppendLine();
        }

        // todo: Since removing manual comparison, this is inaccurate.
        public int ElementCount() => element.ElementCount();

        public Condition Optimized() => new Condition(element.Optimized());

        public static implicit operator Condition(Element element) => new Condition(element);
    }
}
