using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Deltin.Deltinteger.Elements
{
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
    [Serializable]
    public class ElementData : Attribute
    {
        public ElementData(string elementName, int rowAfterSearch)
        {
            ElementType = ElementType.Action;
            ElementName = elementName;
            RowAfterSearch = rowAfterSearch;
        }

        public ElementData(string elementName, ValueType elementType, int rowAfterSearch)
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

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true)]
    [Serializable]
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

    [Serializable]
    public abstract class Element : IEquatable<Element>
    {
        public static readonly Type[] MethodList = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<ElementData>() != null).ToArray();
        public static readonly Type[] ActionList = MethodList.Where(t => t.GetCustomAttribute<ElementData>().ElementType == ElementType.Action).OrderBy(t => t.GetCustomAttribute<ElementData>().ElementName).ToArray(); // Actions in the method list.
        public static readonly Type[] ValueList = MethodList.Where(t => t.GetCustomAttribute<ElementData>().ElementType == ElementType.Value).OrderBy(t => t.GetCustomAttribute<ElementData>().ElementName).ToArray(); // Values in the method list.

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
            T element = new T()
            {
                ParameterValues = parameterValues
            };
            return element;
        }

        public static string GetName(Type type)
        {
            ElementData elementData = type.GetCustomAttribute<ElementData>();
            Parameter[] parameters = type.GetCustomAttributes<Parameter>().ToArray();
            return $"{elementData.ElementName}({string.Join(", ", parameters.Select(v => $"{(v.ParameterType == ParameterType.Value ? v.ValueType.ToString() : v.EnumType.Name)}: {v.Name}"))})";
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

        public void Input() => Input(false, ValueType.Any, null, 0);

        private void Input(bool isAlreadySet, ValueType valueType, Type defaultType, int depth)
        {
            Console.WriteLine($"{new string(' ', depth * 4)}{Info()}");

            // Select the option
            if (!isAlreadySet)
            {
                // Vectors have an extra button that needs to be adjusted for.
                if (defaultType == typeof(V_Vector))
                {
                    InputSim.Press(Keys.Right, Wait.Short);
                }

                // Open the menu.
                InputSim.Press(Keys.Space, Wait.Long);

                int pos = -1; // The position of the menu button.

                if (ElementData.RowAfterSearch != -1)
                {
                    pos = ElementData.RowAfterSearch;
                    InputSim.TextInput(ElementData.ElementName, Wait.Medium);
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
                InputSim.Press(Keys.Tab, Wait.Medium);

                // Highlight the action/value.
                InputSim.Press(Keys.Down, Wait.Short, pos);

                // Select it.
                InputSim.Press(Keys.Space, Wait.Medium);
            }

            BeforeParameters();

            // Do stuff with parameters
            for (int i = 0; i < parameterData.Length; i++)
            {
                object parameter = ParameterValues?.ElementAtOrDefault(i);

                // If the parameter is null, get the default variable.
                if (parameter == null)
                    parameter = parameterData[i].GetDefault();

                // Select the parameter.
                InputSim.Press(Keys.Down, Wait.Short);

                // Element input
                if (parameterData[i].ParameterType == ParameterType.Value)
                    ((Element)parameter).Input(
                        parameterData[i].DefaultType == parameter.GetType(),
                        parameterData[i].ValueType, parameterData[i].DefaultType,
                        depth + 1);

                // Enum input
                else if (parameterData[i].ParameterType == ParameterType.Enum)
                {
                    Console.WriteLine($"{new string(' ', (depth + 1) * 4)}{parameter}");
                    InputSim.SelectEnumMenuOption(parameterData[i].EnumType, parameter);
                }
            }

            AfterParameters();

            if (depth == 0)
                Console.WriteLine();
        }

        protected virtual void BeforeParameters() { } // Executed before parameters are executed
        protected virtual void AfterParameters() { } // Executed after parameters are executed
        protected virtual string Info() { return ElementData.ElementName; }

        public bool Equals(Element other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            if (ParameterValues.Length != other.ParameterValues.Length)
                return false;

            if (!AdditionalEquals(other))
                return false;

            for (int i = 0; i < ParameterValues.Length; i++)
            {
                if (ParameterValues[i].GetType() != other.ParameterValues[i].GetType())
                    return false;
                if (ParameterValues[i] is Element)
                {
                    if (!(ParameterValues[i] as Element).Equals(other.ParameterValues[i] as Element))
                        return false;
                }
                else if (!ParameterValues[i].Equals(other.ParameterValues[i]))
                    return false;
            }

            return true;
        }
        protected virtual bool AdditionalEquals(Element other) { return true; }

        public override bool Equals(object obj)
        {
            return Equals(obj as Rule);
        }

        public override int GetHashCode()
        {
            return (GetType(), ElementData, ParameterValues, AdditionalGetHashCode()).GetHashCode();
        }
        protected virtual int AdditionalGetHashCode() { return 0; }

        public override string ToString()
        {
            return GetName(this.GetType());
        }

        public void Print(int depth = 0)
        {
            Console.WriteLine(new string(' ', depth * 4) + Info());
            foreach (var param in ParameterValues)
                if (param is Element)
                    (param as Element).Print(depth + 1);
                else
                    Console.WriteLine(new string(' ', (depth + 1) * 4) + param);
        }
    }
}
