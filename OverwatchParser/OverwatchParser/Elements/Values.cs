using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace OverwatchParser.Elements
{
    [ElementData("Absolute Value", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(Number))]
    public class AbsoluteValue : Element {}

    [ElementData("Add", ValueType.All)]
    [Parameter("Value", ValueType.All, typeof(Number))]
    [Parameter("Value", ValueType.All, typeof(Number))]
    public class Add : Element {}

    [ElementData("And", ValueType.Boolean)]
    [Parameter("Value", ValueType.Boolean, typeof(True))]
    [Parameter("Value", ValueType.Boolean, typeof(True))]
    public class And : Element {}

    [ElementData("Append To Array", ValueType.All)]
    [Parameter("Array", ValueType.All, null)]
    [Parameter("Value", ValueType.All, null)]
    public class AppendToArray : Element {}

    [ElementData("Event Player", ValueType.VectorAndPlayer)]
    public class EventPlayer : Element { }

    [ElementData("Number", ValueType.Number)]
    public class Number : Element
    {
        public Number(int value)
        {
            this.value = value;
        }
        int value;

        protected override void InputFinished()
        {
            Program.Input.KeyPress(Keys.Down);
            Thread.Sleep(InputHandler.SmallStep);

            var keys = InputHandler.GetNumberKeys(value);
            for (int i = 0; i < keys.Length; i++)
            {
                Program.Input.KeyDown(keys[i]);
                Thread.Sleep(InputHandler.SmallStep);
            }

            Program.Input.KeyPress(Keys.Enter);
            Thread.Sleep(InputHandler.SmallStep);
        }
    }

    [ElementData("True", ValueType.Boolean)]
    public class True : Element { }

    [ElementData("Vector", ValueType.Vector)]
    [Parameter("X", ValueType.Number, typeof(Number))]
    [Parameter("Y", ValueType.Number, typeof(Number))]
    [Parameter("Z", ValueType.Number, typeof(Number))]
    public class Vector : Element {}
}
