using System;
using System.Linq;
using System.Reflection;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.WorkshopWiki;
using Deltin.Deltinteger.Parse;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Elements
{
    public class ElementList : IMethod
    {
        public static ElementList[] Elements { get; private set; }

        public static void InitElements()
        {
            Type[] methodList = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<ElementData>() != null).ToArray();

            Elements = new ElementList[methodList.Length];
            for (int i = 0; i < Elements.Length; i++)
                Elements[i] = new ElementList(methodList[i]);

            for (int i = 0; i < Elements.Length; i++)
                Elements[i].ApplyParameters();
        }
        public static ElementList GetElement(string codeName)
        {
            return Elements.FirstOrDefault(e => e.Name == codeName);
        }
        public static ElementList GetElement<T>() where T: Element
        {
            return Elements.FirstOrDefault(e => e.Type == typeof(T));
        }
        public static ElementList FromType(Type type)
        {
            return Elements.FirstOrDefault(element => element.Type == type);
        }

        public string Name { get; }
        public string WorkshopName { get; }
        public Type Type { get; }
        public bool IsValue { get; } 
        public bool Hidden { get; }
        public CodeParameter[] Parameters { get; private set; }
        public ParameterBase[] WorkshopParameters { get; }
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public UsageDiagnostic[] UsageDiagnostics { get; }
        public WikiMethod Wiki { get; }
        public string Documentation => Wiki?.Description;
        private ValueType ElementValueType { get; }

        // IScopeable defaults
        public LanguageServer.Location DefinedAt { get; } = null;
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public bool WholeContext { get; } = true;
        public bool Static => true;

        public CodeType ReturnType { get; private set; }

        public ElementList(Type type)
        {
            ElementData data = type.GetCustomAttribute<ElementData>();
            Name = type.Name.Substring(2); 
            WorkshopName = data.ElementName;
            Type = type;
            IsValue = data.IsValue;
            ElementValueType = data.ValueType;
            WorkshopParameters = type.GetCustomAttributes<ParameterBase>().ToArray();
            UsageDiagnostics = type.GetCustomAttributes<UsageDiagnostic>().ToArray();
            Hidden = type.GetCustomAttribute<HideElement>() != null;

            Wiki = WorkshopWiki.Wiki.GetWiki()?.GetMethod(WorkshopName);
        }

        public void ApplyParameters()
        {
            // Set the return type to the Vector class if the value returns a vector.
            if (ElementValueType == ValueType.Vector) ReturnType = VectorType.Instance;

            // Get the parameters.
            Parameters = new Parse.CodeParameter[WorkshopParameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                string name = WorkshopParameters[i].Name.Replace(" ", "");
                string description = Wiki?.GetWikiParameter(WorkshopParameters[i].Name)?.Description;

                if (WorkshopParameters[i] is VarRefParameter)
                {
                    Parameters[i] = new VariableParameter(
                        name,
                        description,
                        ((VarRefParameter)WorkshopParameters[i]).IsGlobal ? VariableType.Global : VariableType.Player,
                        new VariableResolveOptions() {
                            CanBeIndexed = false, FullVariable = true
                        }
                    );
                }
                else
                {
                    CodeType codeType = null;

                    // If the parameter is an enum, get the enum CodeType.
                    if (WorkshopParameters[i] is EnumParameter)
                        codeType = ValueGroupType.GetEnumType(((EnumParameter)WorkshopParameters[i]).EnumData);

                    var defaultValue = WorkshopParameters[i].GetDefault();

                    Parameters[i] = new CodeParameter(
                        name,
                        description,
                        codeType,
                        defaultValue == null ? null : new ExpressionOrWorkshopValue(defaultValue)
                    );
                }
            }
        }

        public Element GetObject()
        {
            return (Element)Activator.CreateInstance(Type);
        }

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            Element element = GetObject();
            element.ParameterValues = methodCall.ParameterValues;

            if (!IsValue)
            {
                actionSet.AddAction(element);
                return null;
            }
            else return element;
        }

        public string GetLabel(bool markdown) => MethodAttributes.DefaultLabel(this).ToString(markdown);

        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
    }
}