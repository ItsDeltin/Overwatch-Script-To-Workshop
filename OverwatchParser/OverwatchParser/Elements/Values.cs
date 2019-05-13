using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace Deltin.OverwatchParser.Elements
{
    [ElementData("Absolute Value", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(Number))]
    public class AbsoluteValue : Element {}

    [ElementData("Add", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(Number))]
    [Parameter("Value", ValueType.Any, typeof(Number))]
    public class Add : Element {}

    [ElementData("And", ValueType.Boolean)]
    [Parameter("Value", ValueType.Boolean, typeof(True))]
    [Parameter("Value", ValueType.Boolean, typeof(True))]
    public class And : Element {}

    [ElementData("Append To Array", ValueType.Any)]
    [Parameter("Array", ValueType.Any, null)]
    [Parameter("Value", ValueType.Any, null)]
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

        protected override void AfterParameters()
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

    [ElementData("String", ValueType.String)]
    [Parameter("{0}", ValueType.Any, typeof(Number))]
    [Parameter("{1}", ValueType.Any, typeof(Number))]
    [Parameter("{2}", ValueType.Any, typeof(Number))]
    public class String : Element
    {
        public String(string text, params Element[] stringValues) : base(stringValues)
        {
            textID = Array.IndexOf(Constants.Strings, text);
            if (textID == -1)
                throw new Exception();
        }
        int textID;

        protected override void BeforeParameters()
        {
            Thread.Sleep(InputHandler.BigStep);

            // Select "string" option
            Program.Input.KeyPress(Keys.Down);
            Thread.Sleep(InputHandler.SmallStep);

            // Open the string list
            Program.Input.KeyPress(Keys.Space);
            Thread.Sleep(InputHandler.BigStep);

            // Leave the search field input
            Program.Input.KeyPress(Keys.Enter);
            Thread.Sleep(InputHandler.SmallStep);

            // Select the selected string by textID.
            for (int i = 0; i < textID; i++)
            {
                Program.Input.KeyPress(Keys.Down);
                Thread.Sleep(InputHandler.SmallStep);
            }

            // Select the string
            Program.Input.KeyPress(Keys.Space);
            Thread.Sleep(InputHandler.BigStep);
        }

        public static Element BuildString(params String[] strings)
        {
            if (strings.Length == 0)
                throw new ArgumentException($"There needs to be at least 1 string in the {nameof(strings)} array.");

            if (strings.Length == 1)
                return strings[0];

            if (strings.Length == 2)
                return new String("{0} {1}", strings);

            if (strings.Length == 3)
                return new String("{0} {1} {2}", strings);

            if (strings.Length > 3)
                return new String("{0} {1} {2}", strings[0], strings[1], BuildString(strings.Skip(2).Take(1).ToArray()));

            throw new Exception();
        }
    }

    [ElementData("True", ValueType.Boolean)]
    public class True : Element {}

    [ElementData("Vector", ValueType.Vector)]
    [Parameter("X", ValueType.Number, typeof(Number))]
    [Parameter("Y", ValueType.Number, typeof(Number))]
    [Parameter("Z", ValueType.Number, typeof(Number))]
    public class Vector : Element {}
}
