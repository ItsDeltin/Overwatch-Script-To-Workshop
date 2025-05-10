namespace Deltin.Deltinteger.Elements
{
    public class Condition
    {
        public string Comment { get; set; }
        public bool Disabled { get; set; }
        public Element Element { get; }

        public Condition(Element element) => this.Element = element;
        public Condition(Element value1, Operator op, Element value2) : this(Element.Compare(value1, op, value2)) { }

        public void ToWorkshop(WorkshopBuilder builder)
        {
            string result = string.Empty;

            // Add a comment and newline
            if (Comment != null) builder.AppendLine($"\"{Comment}\"\n");

            if (Disabled) builder.AppendKeyword("disabled").Append(" ");

            Element.ToWorkshop(builder, ToWorkshopContext.ConditionValue);

            if (Element.Function.Name != "Compare")
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
        public int ElementCount() => Element.ElementCount();

        public Condition Optimized() => new Condition(Element.Optimized())
        {
            Comment = Comment,
            Disabled = Disabled
        };

        public static implicit operator Condition(Element element) => new Condition(element);
    }
}
