using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OverwatchParser.Elements
{
    public class Condition
    {
        public Element Value1 { get; private set; }
        public Operators CompareOperator { get; private set; }
        public Element Value2 { get; private set; }

        public Condition(Element value1, Operators compareOperator = Operators.Equal, Element value2 = null)
        {
            if (value2 == null)
                value2 = new V_True();

            if (value1.ElementData.ElementType != ElementType.Value)
                throw new IncorrectElementTypeException(nameof(value1), true);

            if (value2.ElementData.ElementType != ElementType.Value)
                throw new IncorrectElementTypeException(nameof(value2), true);

            Value1 = value1;
            CompareOperator = compareOperator;
            Value2 = value2;
        }

        public void Input()
        {
            // Open the "Create Condition" menu.
            InputHandler.Input.KeyPress(Keys.Space);
            Thread.Sleep(InputHandler.BigStep);

            // Setup control spot
            InputHandler.Input.KeyPress(Keys.Tab);
            Thread.Sleep(InputHandler.SmallStep);
            // The spot will be at the bottom when tab is pressed. 
            // Pressing up once will select the operator value, up another time will select the first value paramerer.
            InputHandler.Input.RepeatKey(Keys.Up, 2);

            // Input value1.
            Value1.Input();

            // Set the operator.
            InputHandler.Input.KeyPress(Keys.Down);
            Thread.Sleep(InputHandler.SmallStep);
            InputHandler.Input.SelectEnumMenuOption(CompareOperator);

            // Input value2.
            InputHandler.Input.KeyPress(Keys.Down);
            Thread.Sleep(InputHandler.SmallStep);
            Value2.Input();

            // Close the Create Condition menu.
            InputHandler.Input.KeyPress(Keys.Escape);
            Thread.Sleep(InputHandler.BigStep);
        }
    }
}
