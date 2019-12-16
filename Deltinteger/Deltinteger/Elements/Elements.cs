using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.WorkshopWiki;
using Deltin.Deltinteger.Parse;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Elements
{
    [Flags]
    public enum ValueType
    {
        Any = Number | Boolean | Hero | Vector | Player | Team ,
        VectorAndPlayer = Vector | Player,
        Number = 1,
        Boolean = 2,
        Hero = 4,
        Vector = 8,
        Player = 16,
        Team = 32,
        Map = 64
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

    public abstract class Element : IWorkshopTree
    {
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
            ElementList = ElementList.FromType(GetType());
            ElementData = GetType().GetCustomAttribute<ElementData>();
            ParameterData = ElementList.WorkshopParameters;
            ParameterValues = parameterValues;
        }

        public ElementList ElementList { get; private set; }
        public ElementData ElementData { get; private set; }
        public ParameterBase[] ParameterData { get; private set; }
        public string Name { get { return ElementList.WorkshopName; } }

        public IWorkshopTree[] ParameterValues { get; set; }

        public override string ToString()
        {
            return ElementList.GetLabel(false);
        }
        
        public virtual string ToWorkshop(OutputLanguage language)
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

            parameters.AddRange(elementParameters.Select(p => p.ToWorkshop(language)));

            string result = I18n.I18n.Translate(language, Name);
            if (parameters.Count != 0) result += "(" + string.Join(", ", parameters) + ")";
            if (!ElementList.IsValue) result += ";";
            return result;
        }

        public virtual bool ConstantSupported<T>()
        {
            return false;
        }
        public virtual object GetConstant()
        {
            return null;
        }

        public virtual Element Optimize()
        {
            OptimizeChildren();
            return this;
        }

        protected void OptimizeChildren()
        {
            for (int i = 0; i < ParameterValues.Length; i++)
                if (ParameterValues[i] is Element)
                    ParameterValues[i] = ((Element)ParameterValues[i]).Optimize();
        }

        protected virtual string[] AdditionalParameters()
        {
            return new string[0];
        }

        // Creates an array from a list of values.
        public static Element CreateArray(params IWorkshopTree[] values)
        {
            Element array = new V_EmptyArray();
            for (int i = 0; i < values.Length; i++)
                array = Element.Part<V_Append>(array, values[i]);
            return array;
        }

        // Creates an ternary conditional that works in the workshop
        public static Element TernaryConditional(IWorkshopTree condition, IWorkshopTree consequent, IWorkshopTree alternative)
        {
            // This works by creating an array with the consequent (C) and the alternative (A): [C, A]
            // It creates an array that contains false and true: [false, true]
            // Then it gets the array value of the false/true array based on the condition result: IndexOfArrayValue(boolArray, condition)
            // The result is either 0 or 1. Use that index to get the value from the [C, A] array.
            return Element.Part<V_ValueInArray>(CreateArray(alternative, consequent), Element.Part<V_IndexOfArrayValue>(CreateArray(new V_False(), new V_True()), condition));

            // Another way to do it would be to add 0 to the boolean, however this won't work with truthey/falsey values that aren't booleans.
            // return Element.Part<V_ValueInArray>(CreateArray(alternative, consequent), Element.Part<V_Add>(condition, new V_Number(0)));
        }

        public static V_Number[] IntToElement(params int[] numbers)
        {
            V_Number[] elements = new V_Number[numbers?.Length ?? 0];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = new V_Number(numbers[i]);

            return elements;
        }

        public static Element operator +(Element a, Element b) => Element.Part<V_Add>(a, b);
        public static Element operator -(Element a, Element b) => Element.Part<V_Subtract>(a, b);
        public static Element operator *(Element a, Element b) => Element.Part<V_Multiply>(a, b);
        public static Element operator /(Element a, Element b) => Element.Part<V_Divide>(a, b);
        public static Element operator %(Element a, Element b) => Element.Part<V_Modulo>(a, b);
        public static Element operator <(Element a, Element b) => new V_Compare(a, Operators.LessThan, b);
        public static Element operator >(Element a, Element b) => new V_Compare(a, Operators.GreaterThan, b);
        public static Element operator <=(Element a, Element b) => new V_Compare(a, Operators.LessThanOrEqual, b);
        public static Element operator >=(Element a, Element b) => new V_Compare(a, Operators.GreaterThanOrEqual, b);
        public static Element operator !(Element a) => Element.Part<V_Not>(a);
        public static Element operator -(Element a) => a * -1;
        public Element this[Element i]
        {
            get { return Element.Part<V_ValueInArray>(this, i); }
            private set {}
        }
        public static implicit operator Element(double number) => new V_Number(number);
        public static implicit operator Element(int number) => new V_Number(number);

        public static readonly Element DefaultElement = 0;
    }

    public class ElementList : IMethod
    {
        public string Name { get; }
        public string WorkshopName { get; }
        public Type Type { get; }
        public bool IsValue { get; } 
        public Parse.CodeParameter[] Parameters { get; }
        public ParameterBase[] WorkshopParameters { get; }
        public UsageDiagnostic[] UsageDiagnostics { get; }
        public WikiMethod Wiki { get; }
        public StringOrMarkupContent Documentation => Wiki.Description;

        // IScopeable defaults
        public LanguageServer.Location DefinedAt { get; } = null;
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public string ScopeableType { get; } = "method";
        public bool WholeContext { get; } = true;

        public CodeType ReturnType { get; } = null;

        public static ElementList[] Elements { get; } = GetElementList();
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

        // public string GetLabel(bool markdown)
        // {
        //     return Name + "(" + Parameter.ParameterGroupToString(Parameters, markdown) + ")" 
        //         + (markdown && Wiki?.Description != null ? "\n\r" + Wiki.Description : "");
        // }

        public string GetLabel(bool markdown) => Name + CodeParameter.GetLabels(Parameters, markdown);

        public ElementList(Type type)
        {
            ElementData data = type.GetCustomAttribute<ElementData>();
            Name = type.Name.Substring(2); 
            WorkshopName = data.ElementName;
            Type = type;
            IsValue = data.IsValue;
            WorkshopParameters = type.GetCustomAttributes<ParameterBase>().ToArray();
            UsageDiagnostics = type.GetCustomAttributes<UsageDiagnostic>().ToArray();

            Wiki = WorkshopWiki.Wiki.GetWiki().GetMethod(WorkshopName);

            Parameters = new Parse.CodeParameter[WorkshopParameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                CodeType codeType = null;

                if (WorkshopParameters[i] is EnumParameter)
                    codeType = CodeType.DefaultTypes.First(t => t is WorkshopEnumType && ((WorkshopEnumType)t).EnumData == ((EnumParameter)WorkshopParameters[i]).EnumData);

                Parameters[i] = new CodeParameter(WorkshopParameters[i].Name, codeType, Wiki?.GetWikiParameter(WorkshopParameters[i].Name)?.Description);
            }
        }

        public Element GetObject()
        {
            return (Element)Activator.CreateInstance(Type);
        }

        public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] values)
        {
            Element element = GetObject();
            element.ParameterValues = values;

            if (!IsValue)
            {
                actionSet.AddAction(element);
                return null;
            }
            else return element;
        }

        public static ElementList FromType(Type type)
        {
            return Elements.FirstOrDefault(element => element.Type == type);
        }

        public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = Name,
                Kind = CompletionItemKind.Method
            };
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UsageDiagnostic : Attribute
    {
        public UsageDiagnostic(string message, int severity)
        {
            Message = message;
            Severity = severity;
        }

        public string Message { get; }
        public int Severity { get; }

        public LanguageServer.Diagnostic GetDiagnostic(DocRange range)
        {
            return new LanguageServer.Diagnostic(Message, range, Severity);
        }
    }
}