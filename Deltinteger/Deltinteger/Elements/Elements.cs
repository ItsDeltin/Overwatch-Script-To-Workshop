using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    public enum ParameterType
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
            IsValue = false;
            ElementName = elementName;
        }

        // Value type == value
        public ElementData(string elementName, ValueType elementType)
        {
            IsValue = true;
            ElementName = elementName;
            ValueType = elementType;
        }

        public string ElementName { get; private set; }

        public bool IsValue { get; private set; }
        public ValueType ValueType { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true)]
    public class Parameter : Attribute
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

        public IWorkshopTree GetDefault()
        {
            if (ParameterType == ParameterType.Value)
            {
                if (DefaultType == null)
                    throw new Exception($"No default value to fallback on for parameter {Name}.");
                return (IWorkshopTree)Activator.CreateInstance(DefaultType);
            }

            if (ParameterType == ParameterType.Enum)
                return EnumData.GetEnum(EnumType).Members[0];

            return null;
        }

        public override string ToString()
        {
            return (ParameterType == ParameterType.Value ? ValueType.ToString() : EnumType.Name) + ": " + Name;
        }

        public static string ParameterGroupToString(Parameter[] parameters)
        {
            return string.Join(", ", parameters.Select(p => p.ToString()));
        }
    }

    public abstract class Element : IWorkshopTree
    {
        public static readonly Type[] MethodList = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<ElementData>() != null).ToArray();
        public static readonly Type[] ActionList = MethodList.Where(t => t.GetCustomAttribute<ElementData>().IsValue == false).OrderBy(t => t.GetCustomAttribute<ElementData>().ElementName).ToArray(); // Actions in the method list.
        public static readonly Type[] ValueList = MethodList.Where(t => t.GetCustomAttribute<ElementData>().IsValue).OrderBy(t => t.GetCustomAttribute<ElementData>().ElementName).ToArray(); // Values in the method list.

        public static Type GetMethod(string name)
        {
            return MethodList.FirstOrDefault(m => name == m.Name.Substring(2));
        }

        private static ElementList[] Elements = GetElementList();
        private static ElementList[] GetElementList()
        {
            ElementList[] elements = new ElementList[MethodList.Length];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = new ElementList(MethodList[i]);
            return elements;
        }
        public static ElementList GetElement(string codeName)
        {
            return Elements.FirstOrDefault(e => e.CodeName == codeName);
        }
        public static CompletionItem[] GetCompletion(bool values, bool actions)
        {
            List<CompletionItem> completions = new List<CompletionItem>();
            foreach(ElementList element in Elements)
                if ((element.IsValue && values) || (!element.IsValue && actions))
                {
                    completions.Add(new CompletionItem(element.CodeName) {
                        detail = element.GetObject().ToString(),
                        kind = CompletionItem.Method,
                        data = element.CodeName,
                        documentation = Wiki.GetWikiMethod(element.WorkshopName)?.Description
                    });
                }
            return completions.ToArray();
        }

        public static T Part<T>(params IWorkshopTree[] parameterValues) where T : Element, new()
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
            return $"{type.Name.Substring(2)}({Parameter.ParameterGroupToString(parameters)})";
        }

        public Element(params IWorkshopTree[] parameterValues)
        {
            ElementData = GetType().GetCustomAttribute<ElementData>();
            ParameterData = GetType().GetCustomAttributes<Parameter>().ToArray();
            ParameterValues = parameterValues;
        }

        public ElementData ElementData { get; private set; }
        public Parameter[] ParameterData { get; private set; }

        public IWorkshopTree[] ParameterValues;

        protected virtual string Info() { return ElementData.ElementName; }

        public override string ToString()
        {
            return GetName(this.GetType());
        }

        public string Name { get { return GetType().Name.Substring(2); } set {} }

        public virtual void DebugPrint(Log log, int depth = 0)
        {
            if (ElementData.IsValue)
                log.Write(LogLevel.Verbose, new ColorMod(Extras.Indent(depth, false) + Info(), ConsoleColor.Cyan));
            else
                log.Write(LogLevel.Verbose, new ColorMod(Extras.Indent(depth, false) + Info(), ConsoleColor.White));

            for (int i = 0; i < ParameterData.Length; i++)
            {
                log.Write(LogLevel.Verbose, new ColorMod(Extras.Indent(depth, false) + ParameterData[i].Name + ":", ConsoleColor.Magenta));

                if (i < ParameterValues.Length)
                {
                    ParameterValues[i]?.DebugPrint(log, depth + 1);
                }
            }
        }

        public virtual string ToWorkshop()
        {
            List<IWorkshopTree> elementParameters = new List<IWorkshopTree>();

            for (int i = 0; i < ParameterData.Length; i++)
            {
                IWorkshopTree parameter = ParameterValues?.ElementAtOrDefault(i);

                // If the parameter is null, get the default variable.
                if (parameter == null)
                    parameter = ParameterData[i].GetDefault();

                elementParameters.Add(parameter);
            }

            List<string> parameters = AdditionalParameters().ToList();

            parameters.AddRange(elementParameters.Select(p => p.ToWorkshop()));

            return ElementData.ElementName + 
                (parameters.Count == 0 ? "" :
                "(" + string.Join(", ", parameters) + ")");
        }

        protected virtual string[] AdditionalParameters()
        {
            return new string[0];
        }
    }

    public class ElementList
    {
        public string CodeName { get; private set; }
        public string WorkshopName { get; private set; }
        public Type Type { get; private set; }
        public bool IsValue { get; private set; } 
        public Parameter[] Parameters { get; private set; }

        public ElementList(Type type)
        {
            ElementData data = type.GetCustomAttribute<ElementData>();
            CodeName = type.Name.Substring(2); 
            WorkshopName = data.ElementName;
            Type = type;
            IsValue = data.IsValue;
            Parameters = type.GetCustomAttributes<Parameter>().ToArray();
        }

        public Element GetObject()
        {
            return (Element)Activator.CreateInstance(Type);
        }
    }
}
