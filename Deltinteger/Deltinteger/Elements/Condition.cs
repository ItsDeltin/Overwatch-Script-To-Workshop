using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger;

namespace Deltin.Deltinteger.Elements
{
    public class Condition : IWorkshopTree
    {
        public Element Value1 { get; private set; }
        public Operators CompareOperator { get; private set; }
        public Element Value2 { get; private set; }

        public Condition(Element value1, Operators compareOperator = Operators.Equal, Element value2 = null)
        {
            if (value2 == null)
                value2 = new V_True();

            if (value1.ElementData.ElementType != ElementType.Value)
                throw new ArgumentException("Method in condition must be a value method.", nameof(value1));

            if (value2.ElementData.ElementType != ElementType.Value)
                throw new ArgumentException("Method in condition must be a value method.", nameof(value2));

            Value1 = value1;
            CompareOperator = compareOperator;
            Value2 = value2;
        }
        
        public void DebugPrint(Log log, int depth = 0)
        {
            Value1.DebugPrint(log, depth);
            log.Write(LogLevel.Verbose, new ColorMod(new string(' ', depth * 4) + CompareOperator.ToString(), ConsoleColor.DarkYellow));
            Value2.DebugPrint(log, depth);
        }

        public string ToWorkshop()
        {
            return Value1.ToWorkshop() + " " + EnumValue.GetWorkshopName(CompareOperator) + " " + Value2.ToWorkshop();
        }
    }
}
