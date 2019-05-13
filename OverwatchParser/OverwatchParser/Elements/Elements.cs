using System;
/*

Element
 L Condition
    L Value
       L Vector
          L Player
             L AllDeadPlayers

*/
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;

namespace OverwatchParser.Elements
{
    public struct OWEnum { }

    // The type of element the element is.
    enum ElementType
    {
        Action,
        Value
    }

    // Rule of thumb: Return values are restrictive (only 1), parameter values are loose (potentially multiple)
    [Flags]
    enum ValueType
    {
        All = Number | Boolean | Hero | Vector | Player,
        Number = 1,
        Boolean = 2,
        String = 4,
        Hero = 8,
        VectorAndPlayer = Vector | Player, // Vectors can use player variables too
        Vector = 16,
        Player = 32
    }

    enum Button
    {
        PrimaryFire,
        SecondaryFire,
        Ability1,
        Ability2,
        Ultimate,
        Interact,
        Jump,
        Crouch
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    class ElementData : Attribute
    {
        public ElementData(string elementName)
        {
            ElementType = ElementType.Action;
            ElementName = elementName;
        }

        public ElementData(string elementName, ValueType elementType)
        {
            ElementType = ElementType.Value;
            ElementName = elementName;
            ValueType = elementType;
        }

        public string ElementName { get; private set; }
        public ElementType ElementType { get; private set; }
        public ValueType ValueType { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    class Parameter : Attribute
    {
        public Parameter(string name, ValueType returnType, Type defaultType)
        {
            Name = name;
            ValueType = returnType;
            DefaultType = defaultType;
        }

        public string Name { get; private set; }

        public ValueType ValueType { get; private set; }

        public Type DefaultType { get; private set; } // The value that the variable is set to use by default
    }

    public abstract class Element
    {
        private static Type[] ActionList;
        private static Type[] ValueList;
        public static void LoadAllElements()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            ActionList = types.Where(t => t.GetCustomAttribute<ElementData>()?.ElementType == ElementType.Action).OrderBy(a => a.Name).ToArray();
            ValueList = types.Where(t => t.GetCustomAttribute<ElementData>()?.ElementType == ElementType.Value).OrderBy(a => a.Name).ToArray();
        }
        private static Type[] FilteredValueList(ValueType parameterType)
        {
            return ValueList.Where(t =>
            {
                var valueType = t.GetCustomAttribute<ElementData>().ValueType;

                return parameterType.HasFlag(valueType) || parameterType == ValueType.All || valueType == ValueType.All;
            }).ToArray();
        }

        public static T Part<T>(params Element[] parameterValues) where T : Element, new()
        {
            T element = new T();
            element.ParameterValues = parameterValues;
            return element;
        }

        public Element(params Element[] parameterValues)
        {
            elementData = GetType().GetCustomAttribute<ElementData>();
            parameterData = GetType().GetCustomAttributes<Parameter>().ToArray();
            ParameterValues = parameterValues;
        }

        ElementData elementData;
        Parameter[] parameterData;

        public Element[] ParameterValues;

        public void Input()
        {
            Input(false, ValueType.All);
        }

        private void Input(bool isAlreadySet, ValueType valueType)
        {
            if (ParameterValues == null)
                ParameterValues = new Element[0];

            if (ParameterValues.Length != parameterData.Length)
                throw new Exception($"Incorrect number of parameters for {elementData.ElementName}. Expected: {parameterData.Length}, got: {ParameterValues.Length}");

            Console.WriteLine($"{elementData.ElementName}:");

            int pos = -1;

            // Get the position of the element
            if (elementData.ElementType == ElementType.Action)
                pos = Array.IndexOf(ActionList, GetType());
            else if (elementData.ElementType == ElementType.Value)
            {
                Console.WriteLine($"    {valueType} list: {string.Join(", ", FilteredValueList(valueType).Select(v => v.Name))}");
                pos = Array.IndexOf(FilteredValueList(valueType), GetType());
            }

            Console.WriteLine($"    position: {pos}");

            // Select the option
            if (!isAlreadySet)
            {
                Program.Input.KeyPress(Keys.Space);
                Thread.Sleep(InputHandler.BigStep);

                // Leave the input field
                Program.Input.KeyPress(Keys.Enter);
                Thread.Sleep(InputHandler.BigStep);

                for (int i = 0; i < pos; i++)
                {
                    Program.Input.KeyPress(Keys.Down);
                    Thread.Sleep(InputHandler.SmallStep);
                }

                Program.Input.KeyPress(Keys.Space);
                Thread.Sleep(InputHandler.MediumStep);
            }

            Console.WriteLine();

            for (int i = 0; i < ParameterValues.Length; i++)
#warning remove ? later
            {
                Program.Input.KeyPress(Keys.Down);
                ParameterValues[i]?.Input(parameterData[i].DefaultType == ParameterValues[i].GetType(), parameterData[i].ValueType);
            }

            InputFinished();
        }
        protected virtual void InputFinished() { }
    }
}
