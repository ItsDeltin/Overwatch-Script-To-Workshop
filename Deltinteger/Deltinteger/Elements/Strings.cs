using System;
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Elements
{
    public class StringElement : Element
    {
        public string Value { get; set; }
        public bool Localized { get; set; }

        public StringElement(string value, bool localized, params IWorkshopTree[] formats) : base(ElementRoot.Instance.GetFunction("Custom String"), formats)
        {
            Value = value;
            Localized = localized;
        }
        public StringElement(string value, params IWorkshopTree[] formats) : this(value, false, formats) {}
        public StringElement() : this(null, false) {}

        public override void ToWorkshop(WorkshopBuilder b, ToWorkshopContext context)
        {
            b.AppendKeyword(Localized ? "String" : "Custom String");
            b.Append("(\"" + (Localized ? b.Kw(Value) : Value) + "\"");

            if (ParameterValues.Length > 0)
            {
                b.Append(", ");
                ParametersToWorkshop(b);
            }
            
            b.Append(")");
        }

        public override bool EqualTo(IWorkshopTree other) => base.EqualTo(other) && ((StringElement)other).Value == Value && ((StringElement)other).Localized == Localized;

        public static IWorkshopTree Join(params IWorkshopTree[] elements)
        {
            if (elements.Length == 0) throw new Exception();

            const string join2 = "{0}{1}";
            const string join3 = "{0}{1}{2}";

            List<IWorkshopTree> list = elements.ToList();
            while (list.Count > 1)
            {
                if (list.Count >= 3)
                {
                    list[0] = new StringElement(join3, list[0], list[1], list[2]);
                    list.RemoveRange(1, 2);
                }
                else if (list.Count >= 2)
                {
                    list[0] = new StringElement(join2, list[0], list[1], Element.Null());
                    list.RemoveAt(1);
                }
                else throw new Exception();
            }
            return list[0];
        }
    }
}