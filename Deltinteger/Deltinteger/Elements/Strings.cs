using System;
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Elements
{
    public class WorkshopCustomString : Element
    {
        public string Text { get; }

        public WorkshopCustomString(string text, params IWorkshopTree[] format) : base(ElementRoot.Instance.GetFunction("Custom String"), format)
        {
            Text = text;
        }
        public WorkshopCustomString() : this("") {}

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
                    list[0] = new WorkshopCustomString(join3, list[0], list[1], list[2]);
                    list.RemoveRange(1, 2);
                }
                else if (list.Count >= 2)
                {
                    list[0] = new WorkshopCustomString(join2, list[0], list[1], Element.Null());
                    list.RemoveAt(1);
                }
                else throw new Exception();
            }
            return list[0];
        }

        public override bool EqualTo(IWorkshopTree other) => base.EqualTo(other) && ((WorkshopCustomString)other).Text == Text;

        public override string ToWorkshop(OutputLanguage language, ToWorkshopContext context)
        {
            string toWorkshop = Function.Name + "(\"" + Text + "\"";

            if (ParameterValues != null && ParameterValues.Length > 0)
                toWorkshop += ", " + string.Join(", ", ParameterValues.Select(p => p.ToWorkshop(language, ToWorkshopContext.NestedValue)));
            
            toWorkshop += ")";
            return toWorkshop;
        }
    }

    public class WorkshopString : Element
    {
        public string Text { get; }

        public WorkshopString(string text, params Element[] stringValues) : base(ElementRoot.Instance.GetFunction("String"), stringValues)
        {
            Text = text;
        }
        public WorkshopString() : this("") {}

        public override bool EqualTo(IWorkshopTree other) => base.EqualTo(other) && ((WorkshopString)other).Text == Text;

        public override string ToWorkshop(OutputLanguage language, ToWorkshopContext context)
        {
            string toWorkshop = Function.Name + "(\"" + Text + "\"";

            if (ParameterValues != null && ParameterValues.Length > 0)
                toWorkshop += ", " + string.Join(", ", ParameterValues.Select(p => p.ToWorkshop(language, ToWorkshopContext.NestedValue)));
            
            toWorkshop += ")";
            return toWorkshop;
        }
    }
}