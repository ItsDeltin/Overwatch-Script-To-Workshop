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

namespace Deltin.OverwatchParser.Elements
{
    public struct OWEnum { }

    // The type of element the element is.
    public enum ElementType
    {
        Action,
        Value
    }

    enum ParameterType
    {
        Value,
        Enum
    }

    // Rule of thumb: Return values are restrictive (only 1), parameter values are loose (potentially multiple)
    [Flags]
    public enum ValueType
    {
        Any = Number | Boolean | Hero | Vector | Player | Team,
        Number = 1,
        Boolean = 2,
        String = 4,
        Hero = 8,
        VectorAndPlayer = Vector | Player, // Vectors can use player variables too
        Vector = 16,
        Player = 32,
        Team = 64
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
    public class ElementData : Attribute
    {
        public ElementData(string elementName, int rowAfterSearch = -1)
        {
            ElementType = ElementType.Action;
            ElementName = elementName;
            RowAfterSearch = rowAfterSearch;
        }

        public ElementData(string elementName, ValueType elementType, int rowAfterSearch = -1)
        {
            ElementType = ElementType.Value;
            ElementName = elementName;
            ValueType = elementType;
            RowAfterSearch = rowAfterSearch;
        }

        public string ElementName { get; private set; }
        public int RowAfterSearch { get; private set; }

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
            ParameterType = ParameterType.Value;
        }

        public Parameter(string name, Type enumType)
        {
            Name = name;
            EnumType = enumType;
            ParameterType = ParameterType.Enum;
        }

        public string Name { get; private set; }
        public ParameterType ParameterType { get; private set; }

        public ValueType ValueType { get; private set; }
        public Type DefaultType { get; private set; } // The value that the variable is set to use by default

        public Type EnumType { get; private set; }

        public object GetDefault()
        {
            if (ParameterType == ParameterType.Value)
            {
                if (DefaultType == null)
                    throw new Exception($"No default value to fallback on for parameter {Name}.");
                return Activator.CreateInstance(DefaultType);
            }

            if (ParameterType == ParameterType.Enum)
                return Enum.GetValues(EnumType).GetValue(0);

            return null;
        }
    }

    public abstract class Element
    {
        private static Type[] ActionList = null;
        private static Type[] ValueList = null;
        public static void LoadAllElements()
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            ActionList = types.Where(t => t.GetCustomAttribute<ElementData>()?.ElementType == ElementType.Action).OrderBy(a => a.GetCustomAttribute<ElementData>().ElementName).ToArray();
            ValueList = types.Where(t => t.GetCustomAttribute<ElementData>()?.ElementType == ElementType.Value).OrderBy(a => a.GetCustomAttribute<ElementData>().ElementName).ToArray();
        }
        private static Type[] FilteredValueList(ValueType parameterType)
        {
            return ValueList.Where(t =>
            {
                var valueType = t.GetCustomAttribute<ElementData>().ValueType;

                return parameterType.HasFlag(valueType) || parameterType == ValueType.Any || valueType == ValueType.Any;
            }).ToArray();
        }

        public static T Part<T>(params object[] parameterValues) where T : Element, new()
        {
            T element = new T();
            element.ParameterValues = parameterValues;
            return element;
        }

        public Element(params object[] parameterValues)
        {
            if (ActionList == null)
                LoadAllElements();

            ElementData = GetType().GetCustomAttribute<ElementData>();
            parameterData = GetType().GetCustomAttributes<Parameter>().ToArray();
            ParameterValues = parameterValues;
        }

        public ElementData ElementData { get; private set; }
        Parameter[] parameterData;

        public object[] ParameterValues;

        public void Input() => Input(false, ValueType.Any, null);

        private void Input(bool isAlreadySet, ValueType valueType, Type defaultType)
        {
            // Make ParameterValues the same size as parameterData.
            if (ParameterValues == null)
                ParameterValues = new Element[0];
            Array.Resize(ref ParameterValues, parameterData.Length);

            Console.WriteLine($"{ElementData.ElementName}:");

            // Select the option
            if (!isAlreadySet)
            {
                // Vectors have an extra button that needs to be adjusted for.
                if (defaultType == typeof(V_Vector))
                {
                    InputHandler.Input.KeyPress(Keys.Right);
                    Thread.Sleep(InputHandler.SmallStep);
                }

                // Open the menu.
                InputHandler.Input.KeyPress(Keys.Space);
                Thread.Sleep(InputHandler.BigStep);

                int pos = -1; // The position of the menu button.

                if (ElementData.RowAfterSearch != -1)
                {
                    pos = ElementData.RowAfterSearch;
                    InputHandler.Input.TextInput(ElementData.ElementName);
                    Thread.Sleep(InputHandler.MediumStep);
                }
                else
                {
                    // Get the position of the element
                    if (ElementData.ElementType == ElementType.Action)
                        pos = Array.IndexOf(ActionList, GetType());
                    else if (ElementData.ElementType == ElementType.Value)
                    {
                        Console.WriteLine($"    {valueType} list: {string.Join(", ", FilteredValueList(valueType).Select(v => v.Name))}");
                        pos = Array.IndexOf(FilteredValueList(valueType), GetType());
                    }
                }

                Console.WriteLine($"    position: {pos}");

                // Leave the input field
                InputHandler.Input.KeyPress(Keys.Enter);
                Thread.Sleep(InputHandler.MediumStep);

                for (int i = 0; i < pos; i++)
                {
                    InputHandler.Input.KeyPress(Keys.Down);
                    Thread.Sleep(InputHandler.SmallStep);
                }

                InputHandler.Input.KeyPress(Keys.Space);
                Thread.Sleep(InputHandler.MediumStep);
            }

            Console.WriteLine();

            BeforeParameters();

            for (int i = 0; i < parameterData.Length; i++)
            {
                if (ParameterValues.ElementAtOrDefault(i) == null)
                    ParameterValues[i] = parameterData[i].GetDefault();

                InputHandler.Input.KeyPress(Keys.Down);
                Thread.Sleep(InputHandler.SmallStep);

                if (parameterData[i].ParameterType == ParameterType.Value)
                    ((Element)ParameterValues[i]).Input(parameterData[i].DefaultType == ParameterValues[i].GetType(), parameterData[i].ValueType, parameterData[i].DefaultType);

                // Enum input
                else if (parameterData[i].ParameterType == ParameterType.Enum)
                    InputHandler.Input.SelectEnumMenuOption(parameterData[i].EnumType, ParameterValues[i]);

            }

            AfterParameters();
        }

        protected virtual void BeforeParameters() { }
        protected virtual void AfterParameters() { }
    }
}
