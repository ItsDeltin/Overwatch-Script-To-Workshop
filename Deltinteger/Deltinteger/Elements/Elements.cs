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

        public Deltin.Deltinteger.Parse.IndexedVar SupportedType { get; set; }

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

        // Creates an array from a list of values.
        public static Element CreateArray(params Element[] values)
        {
            Element array = new V_EmptyArray();
            for (int i = 0; i < values.Length; i++)
                array = Element.Part<V_Append>(array, values[i]);
            return array;
        }

        // Creates an ternary conditional that works in the workshop
        public static Element TernaryConditional(Element condition, Element consequent, Element alternative, bool supportTruthyFalsey)
        {
            // This works by creating an array with the consequent (C) and the alternative (A): [C, A]
            // It creates an array that contains false and true: [false, true]
            // Then it gets the array value of the false/true array based on the condition result: IndexOfArrayValue(boolArray, condition)
            // The result is either 0 or 1. Use that index to get the value from the [C, A] array.
            if (supportTruthyFalsey)
                return Element.Part<V_ValueInArray>(CreateArray(alternative, consequent), Element.Part<V_IndexOfArrayValue>(CreateArray(new V_False(), new V_True()), condition));
            else 
                return Element.Part<V_ValueInArray>(CreateArray(alternative, consequent), Element.Part<V_Add>(condition, new V_Number(0)));

            // Another way to do it would be to add 0 to the boolean, however this won't work with truthey/falsey values that aren't booleans.
            // return Element.Part<V_ValueInArray>(CreateArray(alternative, consequent), Element.Part<V_Add>(condition, new V_Number(0)));
        }

        public static Element[] While(ContinueSkip continueSkip, Element condition, Element[] actions)
        {
            List<Element> result = new List<Element>();

            continueSkip.Setup();
            int whileStartIndex = continueSkip.GetSkipCount() + 1;

            A_SkipIf skipCondition = new A_SkipIf() { ParameterValues = new IWorkshopTree[2] };
            skipCondition.ParameterValues[0] = !(condition);
            result.Add(skipCondition);
            result.AddRange(actions);
            result.AddRange(continueSkip.SetSkipCountActions(whileStartIndex));
            skipCondition.ParameterValues[1] = new V_Number(result.Count);
            result.Add(new A_Loop());
            result.AddRange(continueSkip.ResetSkipActions());

            return result.ToArray();
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
        public ParameterBase[] Parameters { get; }
        public UsageDiagnostic[] UsageDiagnostics { get; }
        public WikiMethod Wiki { get; }

        public Element Parse(TranslateRule context, bool needsToBeValue, ScopeGroup scope, MethodNode methodNode, IWorkshopTree[] parameters)
        {
            TranslateRule.CheckMethodType(needsToBeValue, IsValue ? CustomMethodType.Value : CustomMethodType.Action, methodNode.Name, methodNode.Location);

            Element element = GetObject();
            element.ParameterValues = parameters;

            Element result;

            if (element.ElementData.IsValue)
                result = element;
            else
            {
                context.Actions.Add(element);
                result = null;
            }

            foreach (var usageDiagnostic in UsageDiagnostics)
                context.ParserData.Diagnostics.AddDiagnostic(methodNode.Location.uri, usageDiagnostic.GetDiagnostic(methodNode.Location.range));
            
            return result;
        }

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
            UsageDiagnostics = type.GetCustomAttributes<UsageDiagnostic>().ToArray();
        }

        public Element GetObject()
        {
            return (Element)Activator.CreateInstance(Type);
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

        public Diagnostic GetDiagnostic(DocRange range)
        {
            return new Diagnostic(Message, range) { severity = Severity };
        }
    }
}