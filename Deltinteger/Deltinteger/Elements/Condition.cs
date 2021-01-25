using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger;

namespace Deltin.Deltinteger.Elements
{
    public class Condition
    {
        public Element Value1 { get; private set; }
        public OperatorElement CompareOperator { get; private set; }
        public Element Value2 { get; private set; }
        public string Comment { get; set; }

        public Condition(Element value1, Operator op, Element value2)
        {
            Value1 = value1;
            CompareOperator = new OperatorElement(op);
            Value2 = value2;
        }
        public Condition(Element condition) : this(condition, Operator.Equal, Element.True()) {}

        public void ToWorkshop(WorkshopBuilder builder, bool optimize)
        {
            string result = string.Empty;

            // Add a comment and newline
            if (Comment != null) builder.AppendLine($"\"{Comment}\"\n");

            Element a = Value1;
            Element b = Value2;
            if (optimize)
            {
                a = a.Optimize();
                b = b.Optimize();
            }
            
            a.ToWorkshop(builder, ToWorkshopContext.ConditionValue);
            builder.Append(" ");
            CompareOperator.ToWorkshop(builder, ToWorkshopContext.Other);
            builder.Append(" ");
            b.ToWorkshop(builder, ToWorkshopContext.ConditionValue);
            builder.Append(";");
            builder.AppendLine();
        }

        public int ElementCount(bool optimized)
        {
            if (optimized)
                return 1 + Value1.Optimize().ElementCount() + Value2.Optimize().ElementCount();
            else
                return 1 + Value1.ElementCount() + Value2.ElementCount();
        }

        public static implicit operator Condition(Element element) => new Condition(element);
    }
}
