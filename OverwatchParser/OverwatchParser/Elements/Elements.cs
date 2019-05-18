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
        Any = Number | Boolean | String | Hero | Vector | Player | Team ,
        VectorAndPlayer = Vector | Player, // Players can be subsituded as vectors, but not the other way around.
        Number = 1,
        Boolean = 2,
        String = 4,
        Hero = 8,
        Vector = 16,
        Player = 32,
        Team = 64
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
        private static Type[] MethodList = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<ElementData>() != null).ToArray();
        private static Type[] ActionList = MethodList.Where(t => t.GetCustomAttribute<ElementData>().ElementType == ElementType.Action).OrderBy(t => t.GetCustomAttribute<ElementData>().ElementName).ToArray(); // Actions in the method list.
        private static Type[] ValueList = MethodList.Where(t => t.GetCustomAttribute<ElementData>().ElementType == ElementType.Value).OrderBy(t => t.GetCustomAttribute<ElementData>().ElementName).ToArray(); // Values in the method list.

        private static Type[] FilteredValueList(ValueType parameterType)
        {
            return ValueList.Where(t =>
            {
                var valueType = t.GetCustomAttribute<ElementData>().ValueType;

                return parameterType.HasFlag(valueType) || parameterType == ValueType.Any || valueType == ValueType.Any;
            }).ToArray();
        }

        public static Type GetMethod(string name)
        {
            return MethodList.FirstOrDefault(m => name == m.Name.Substring(2));
        }

        public static T Part<T>(params object[] parameterValues) where T : Element, new()
        {
            T element = new T();
            element.ParameterValues = parameterValues;
            return element;
        }

        public Element(params object[] parameterValues)
        {
            ElementData = GetType().GetCustomAttribute<ElementData>();
            parameterData = GetType().GetCustomAttributes<Parameter>().ToArray();
            ParameterValues = parameterValues;
        }

        public ElementData ElementData { get; private set; }
        Parameter[] parameterData;

        public object[] ParameterValues;

        public void Input(Weight weight = null) => Input(false, ValueType.Any, null, 0, weight != null ? weight : new Weight());

        private void Input(bool isAlreadySet, ValueType valueType, Type defaultType, int depth, Weight weight)
        {
            // Make ParameterValues the same size as parameterData.
            if (ParameterValues == null)
                ParameterValues = new Element[0];
            Array.Resize(ref ParameterValues, parameterData.Length);

            Console.WriteLine($"{new string(' ', depth * 4)}{Info()}");

            // Add to the weight.
            weight.Add(GetWeight());

            /*
            // Vectors have an extra button that needs to be adjusted for.
            if (defaultType == typeof(V_Vector))
            {
                InputHandler.Input.KeyPress(Keys.Right);
                weight.Sleep(Wait.Small);
            }
            */

            // Select the option
            if (!isAlreadySet)
            {
                // Vectors have an extra button that needs to be adjusted for.
                if (defaultType == typeof(V_Vector))
                {
                    InputHandler.Input.KeyPress(Keys.Right);
                    weight.Sleep(Wait.Small);
                }

                // Open the menu.
                InputHandler.Input.KeyPress(Keys.Space);
                weight.Sleep(Wait.Long);

                int pos = -1; // The position of the menu button.

                if (ElementData.RowAfterSearch != -1)
                {
                    pos = ElementData.RowAfterSearch;
                    InputHandler.Input.TextInput(ElementData.ElementName);
                    weight.Sleep(Wait.Medium);
                }
                else
                {
                    // Get the position of the element
                    if (ElementData.ElementType == ElementType.Action)
                        pos = Array.IndexOf(ActionList, GetType());
                    else if (ElementData.ElementType == ElementType.Value)
                        pos = Array.IndexOf(FilteredValueList(valueType), GetType());
                }

                // Leave the input field
                InputHandler.Input.KeyPress(Keys.Tab);
                weight.Sleep(Wait.Medium);

                // Highlight the action/value.
                InputHandler.Input.RepeatKey(Keys.Down, pos);

                // Select it.
                InputHandler.Input.KeyPress(Keys.Space);
                weight.Sleep(Wait.Medium);
            }

            if (parameterData.Any(v => v.DefaultType == typeof(V_Vector)))
            {
                weight.Sleep(Wait.Long);
            }

            BeforeParameters(weight);

            // Do stuff with parameters
            for (int i = 0; i < parameterData.Length; i++)
            {
                // If the parameter is null, get the default variable.
                if (ParameterValues.ElementAtOrDefault(i) == null)
                    ParameterValues[i] = parameterData[i].GetDefault();

                // Select the parameter.
                InputHandler.Input.KeyPress(Keys.Down);
                weight.Sleep(Wait.Small);

                // Element input
                if (parameterData[i].ParameterType == ParameterType.Value)
                    ((Element)ParameterValues[i]).Input(
                        parameterData[i].DefaultType == ParameterValues[i].GetType(),
                        parameterData[i].ValueType, parameterData[i].DefaultType,
                        depth + 1,
                        weight);

                // Enum input
                else if (parameterData[i].ParameterType == ParameterType.Enum)
                {
                    Console.WriteLine($"{new string(' ', (depth + 1) * 4)}{ParameterValues[i]}");
                    InputHandler.Input.SelectEnumMenuOption(parameterData[i].EnumType, ParameterValues[i]);
                    weight.Add(1);
                }
            }

            AfterParameters(weight);

            if (depth == 0)
                Console.WriteLine();
        }

        protected virtual double GetWeight() { return 1; }
        protected virtual void BeforeParameters(Weight weight) { } // Executed before parameters are executed
        protected virtual void AfterParameters(Weight weight) { } // Executed after parameters are executed
        protected virtual string Info() { return ElementData.ElementName; }
    }

    public class Weight
    {
        public double TotalWeight { get; private set; } = 0;
        private double Scalar;
        private const int MSBuffer = 100;

        public Weight(double scalar = 0.4)
        {
            Scalar = scalar;
        }

        public void Add(double weight)
        {
            TotalWeight += weight;
        }

        public void Sleep(Wait wait)
        {
            int duration = 0;
            int max = 0;
            switch (wait)
            {
                case Wait.Small:
                    duration = InputHandler.SmallStep;
                    max = 2000;
                    break;

                case Wait.Medium:
                    duration = InputHandler.MediumStep;
                    max = 5000;
                    break;

                case Wait.Long:
                    duration = InputHandler.BigStep;
                    max = 10000;
                    break;
            }
            duration = Math.Min(Math.Max(duration, duration * ((int)Math.Round(TotalWeight * Scalar) - MSBuffer + 1)), max);
            Thread.Sleep(duration);
        }
    }
}
