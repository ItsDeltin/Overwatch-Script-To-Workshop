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

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public abstract class ParameterBase : Attribute, ILanguageServerInfo
    {
        public string Name { get; private set; }

        protected ParameterBase(string name)
        {
            Name = name;
        }

        public virtual IWorkshopTree GetDefault()
        {
            return null;
        }

        public override string ToString()
        {
            return Name;
        }

        public abstract string GetLabel(bool markdown);

        public static string ParameterGroupToString(ParameterBase[] parameters, bool markdown)
        {
            return string.Join(", ", parameters.Select(p => p.GetLabel(markdown)));
        }
    }

    class Parameter : ParameterBase
    {
        public ValueType ReturnType { get; private set; }
        public Type DefaultType { get; private set; } // The value that the variable is set to use by default

        public Parameter(string name, ValueType returnType, Type defaultType) : base (name)
        {
            ReturnType = returnType;
            DefaultType = defaultType;
        }

        public override IWorkshopTree GetDefault()
        {
            if (DefaultType == null)
                return null;
            return (IWorkshopTree)Activator.CreateInstance(DefaultType);
        }

        public override string GetLabel(bool markdown)
        {
            if (!markdown)
                return ReturnType.ToString() + " " + Name;
            else
                return "**" + ReturnType.ToString() + "** " + Name;
        }
    }

    class EnumParameter : ParameterBase
    {
        public Type EnumType { get; private set; }
        public EnumData EnumData { get; private set; }

        public EnumParameter(string name, Type enumType) : base (name)
        {
            EnumType = enumType;
            EnumData = EnumData.GetEnum(enumType);
        }

        public override IWorkshopTree GetDefault()
        {
            return EnumData.Members[0];
        }

        public override string GetLabel(bool markdown)
        {
            if (!markdown)
                return EnumData.CodeName + " " + Name;
            else
                return "**" + EnumData.CodeName + "** " + Name;
        }
    }

    class VarRefParameter : ParameterBase 
    {
        public VarRefParameter(string name) : base(name) {}

        public override string GetLabel(bool markdown)
        {
            if (!markdown)
                return "ref " + Name;
            else
                return "**ref** " + Name;
        }
    }

    public abstract class Element : IWorkshopTree
    {
        private static ElementList[] Elements = GetElementList();
        private static ElementList[] GetElementList()
        {
            Type[] methodList = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<ElementData>() != null).ToArray();

            ElementList[] elements = new ElementList[methodList.Length];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = new ElementList(methodList[i]);
            
            return elements;
        }
        public static ElementList GetElement(string codeName)
        {
            return Elements.FirstOrDefault(e => e.Name == codeName);
        }
        public static CompletionItem[] GetCompletion(bool values, bool actions)
        {
            List<CompletionItem> completions = new List<CompletionItem>();
            foreach(ElementList element in Elements)
                if ((element.IsValue && values) || (!element.IsValue && actions))
                {
                    completions.Add(new CompletionItem(element.Name) {
                        detail = element.GetObject().ToString(),
                        kind = CompletionItem.Method,
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

        public Element(params IWorkshopTree[] parameterValues)
        {
            ElementList = Element.GetElement(GetType().Name.Substring(2));
            ElementData = GetType().GetCustomAttribute<ElementData>();
            ParameterData = ElementList.Parameters;
            ParameterValues = parameterValues;
        }

        public ElementList ElementList { get; private set; }
        public ElementData ElementData { get; private set; }
        public ParameterBase[] ParameterData { get; private set; }
        public string Name { get { return ElementList.Name; } }

        public string Comment { get; set; } = null;
        public IWorkshopTree[] ParameterValues { get; set; }
        public Deltin.Deltinteger.Parse.DefinedType SupportedType { get; set; }

        public override string ToString()
        {
            return ElementList.GetLabel(false);
        }

        public virtual void DebugPrint(Log log, int depth = 0)
        {
            if (ElementData.IsValue)
                log.Write(LogLevel.Verbose, new ColorMod(Extras.Indent(depth, false) + DebugInfo(), ConsoleColor.Cyan));
            else
                log.Write(LogLevel.Verbose, new ColorMod(Extras.Indent(depth, false) + DebugInfo(), ConsoleColor.White));

            for (int i = 0; i < ParameterData.Length; i++)
            {
                log.Write(LogLevel.Verbose, new ColorMod(Extras.Indent(depth, false) + ParameterData[i].Name + ":", ConsoleColor.Magenta));

                if (i < ParameterValues.Length)
                {
                    ParameterValues[i]?.DebugPrint(log, depth + 1);
                }
            }
        }
        protected virtual string DebugInfo() { return ElementData.ElementName; }

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
                (parameters.Count == 0 ? "" : "(" + string.Join(", ", parameters) + ")")
                + (!ElementData.IsValue ? (";" + (Comment != null ? " // " + Comment : "")) : "");
        }

        protected virtual string[] AdditionalParameters()
        {
            return new string[0];
        }

        // Converts an array of actions to a workshop
        public static string ToWorkshop(Element[] actions)
        {
            var builder = new TabStringBuilder(true);
            builder.AppendLine("actions");
            builder.AppendLine("{");
            builder.Indent = 1;
            foreach(Element action in actions)
            {
                builder.AppendLine(action.ToWorkshop());
            }
            builder.Indent = 0;
            builder.AppendLine("}");
            
            return builder.ToString();
        }

        // The estimated server load of the current element.
        public double ServerLoadWeight()
        {
            double weight = Weight();
            foreach (IWorkshopTree parameter in ParameterValues)
                weight += parameter.ServerLoadWeight();
            return weight;
        }
        protected virtual double Weight()
        {
            return 0;
        }

        // Creates an array from a list of values.
        public static Element CreateArray(params Element[] values)
        {
            Element array = new V_EmptyArray();
            for (int i = 0; i < values.Length; i++)
                array = Element.Part<V_Append>(array, values[i]);
            return array;
        }

        // Creates an ternary conditional that works in the workshop
        public static Element TernaryConditional(Element condition, Element consequent, Element alternative)
        {
            // This works by creating an array with the consequent (C) and the alternative (A): [C, A]
            // It creates an array that contains false and true: [false, true]
            // Then it gets the array value of the false/true array based on the condition result: IndexOfArrayValue(boolArray, condition)
            // The result is either 0 or 1. Use that index to get the value from the [C, A] array.
            return Element.Part<V_ValueInArray>(CreateArray(alternative, consequent), Element.Part<V_IndexOfArrayValue>(CreateArray(new V_False(), new V_True()), condition));

            // Another way to do it would be to add 0 to the boolean, however this won't work with truthey/falsey values that aren't booleans.
            // return Element.Part<V_ValueInArray>(CreateArray(alternative, consequent), Element.Part<V_Add>(condition, new V_Number(0)));
        }

        public static readonly Element DefaultElement = new V_Number(0);
    }

    public class ElementList : IMethod
    {
        public string Name { get; }
        public string WorkshopName { get; }
        public Type Type { get; }
        public bool IsValue { get; } 
        public ParameterBase[] Parameters { get; }
        public WikiMethod Wiki { get; }


        public string GetLabel(bool markdown)
        {
            return Name + "(" + Parameter.ParameterGroupToString(Parameters, markdown) + ")" 
            + (markdown && Wiki?.Description != null ? "\n\r" + Wiki.Description : "");
        }

        public ElementList(Type type)
        {
            ElementData data = type.GetCustomAttribute<ElementData>();
            Name = type.Name.Substring(2); 
            WorkshopName = data.ElementName;
            Type = type;
            IsValue = data.IsValue;
            Parameters = type.GetCustomAttributes<ParameterBase>().ToArray();
            Wiki = WorkshopWiki.Wiki.GetWikiMethod(WorkshopName);
        }

        public Element GetObject()
        {
            return (Element)Activator.CreateInstance(Type);
        }
    }
}
