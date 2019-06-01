using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
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
        Any = Number | Boolean | Hero | Vector | Player | Team ,
        VectorAndPlayer = Vector | Player, // Players can be subsituded as vectors, but not the other way around.
        Number = 1,
        Boolean = 2,
        //String = 4,
        Hero = 4,
        Vector = 8,
        Player = 16,
        Team = 32,
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ElementData : Attribute
    {
        // No value type == action
        public ElementData(string elementName)
        {
            ElementType = ElementType.Action;
            ElementName = elementName;
        }

        // Value type == value
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

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true)]
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

    public abstract class Element : IWorkshopTree
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

        protected virtual string Info() { return ElementData.ElementName; }

        public override string ToString()
        {
            return GetName(this.GetType());
        }

        public virtual void DebugPrint(Log log, int depth = 0)
        {
            if (ElementData.ElementType == ElementType.Action)
                log.Write(LogLevel.Verbose, new ColorMod(new string(' ', depth * 4) + Info(), ConsoleColor.Cyan));
            else
                log.Write(LogLevel.Verbose, new ColorMod(new string(' ', depth * 4) + Info(), ConsoleColor.White));

            for (int i = 0; i < parameterData.Length; i++)
            {
                log.Write(LogLevel.Verbose, new ColorMod(new string(' ', (depth + 1) * 4) + parameterData[i].Name + ":", ConsoleColor.Magenta));

                if (i < ParameterValues.Length)
                {
                    if (ParameterValues[i] is Element)
                        (ParameterValues[i] as Element).DebugPrint(log, depth + 1);
                    else
                        log.Write(LogLevel.Verbose, new string(' ', (depth + 1) * 4) + ParameterValues[i]);
                }
            }
        }

        public virtual string ToWorkshop()
        {
            List<object> elementParameters = new List<object>();

            for (int i = 0; i < parameterData.Length; i++)
            {
                object parameter = ParameterValues?.ElementAtOrDefault(i);

                // If the parameter is null, get the default variable.
                if (parameter == null)
                    parameter = parameterData[i].GetDefault();

                elementParameters.Add(parameter);
            }

            List<string> parameters = AdditionalParameters().ToList();

            parameters.AddRange(
                elementParameters.Select(p => p is Element ?
                    (p as Element).ToWorkshop() :
                    EnumValue.GetWorkshopName(p as Enum) 
                ));


            return ElementData.ElementName + 
                (parameters.Count == 0 ? "" : 
                "(" + string.Join(", ", parameters) + ")");
        }

        protected virtual string[] AdditionalParameters()
        {
            return new string[0];
        }
    }
}
