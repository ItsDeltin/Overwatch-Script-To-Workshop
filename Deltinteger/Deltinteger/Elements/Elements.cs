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
using Deltin.Deltinteger.Models;
using Deltin.Deltinteger.I18n;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Elements
{
    public enum ValueType
    {
        Any,
        VectorAndPlayer,
        Number,
        Boolean,
        Hero,
        Vector,
        Player,
        Team,
        Map,
        Gamemode,
        Button,
        String
    }

    public class Element : IWorkshopTree
    {
        public static Element Part(string name, params IWorkshopTree[] parameterValues)
            => new Element(ElementRoot.Instance.GetFunction(name), parameterValues);
        public static Element Part(ElementBaseJson function, params IWorkshopTree[] parameterValues)
            => new Element(function, parameterValues);

        public ElementBaseJson Function { get; }
        public IWorkshopTree[] ParameterValues { get; set; }
        public bool Disabled { get; set; }
        public string Comment { get; set; }

        public Element(ElementBaseJson function, IWorkshopTree[] parameterValues)
        {
            Function = function;
            ParameterValues = parameterValues;
        }

        public virtual string ToWorkshop(OutputLanguage language, ToWorkshopContext context)
        {            
            string result = "";

            // Add a comment and newline
            if (Comment != null) result += $"\"{Comment}\"\n";

            // Add the disabled tag if the element is disabled.
            if (Function is ElementJsonAction && Disabled) result += LanguageInfo.Translate(language, "disabled") + " ";

            // Add the name of the element.
            result += LanguageInfo.Translate(language, Function.Name);

            // Add the parameters.
            AddMissingParameters();
            var parameters = ParameterValues.Select(p => p.ToWorkshop(language, ToWorkshopContext.NestedValue));
            if (parameters.Count() != 0) result += "(" + string.Join(", ", parameters) + ")";

            return result;
        }

        /// <summary>Makes sure no parameter values are null.</summary>
        private void AddMissingParameters()
        {
            List<IWorkshopTree> parameters = new List<IWorkshopTree>();

            for (int i = 0; i < Function.Parameters.Length || i < ParameterValues.Length; i++)
                parameters.Add(ParameterValues?.ElementAtOrDefault(i) ?? GetDefaultParameter(Function.Parameters[i]));
            
            ParameterValues = parameters.ToArray();
        }

        public virtual bool EqualTo(IWorkshopTree other)
        {
            if (this.GetType() != other.GetType()) return false;

            Element bElement = (Element)other;
            if (Function != bElement.Function || ParameterValues.Length != bElement.ParameterValues.Length) return false;

            string[] createsRandom = new string[] {
                "Random Integer",
                "Randomized Array",
                "Random Real",
                "Random Value In Array"
            };

            for (int i = 0; i < ParameterValues.Length; i++)
            {
                if ((ParameterValues[i] == null) != (bElement.ParameterValues[i] == null))
                    return false;

                if (ParameterValues[i] != null && (!ParameterValues[i].EqualTo(bElement.ParameterValues[i]) || (ParameterValues[i] is Element element && createsRandom.Contains(element.Function.Name))))
                    return false;
            }
            
            return true;
        }

        public Element Optimize()
        {
            OptimizeChildren();
            return this;
        }

        protected void OptimizeChildren()
        {
            AddMissingParameters();
            for (int i = 0; i < ParameterValues.Length; i++)
                if (ParameterValues[i] is Element element)
                    ParameterValues[i] = element.Optimize();
        }

        public bool TryGetConstant(out Vertex vertex)
        {
            if (Function.Name == "Vector"
                && ParameterValues[0] is Element xe && xe.TryGetConstant(out double x)
                && ParameterValues[1] is Element ye && ye.TryGetConstant(out double y)
                && ParameterValues[2] is Element ze && ze.TryGetConstant(out double z))
            {
                vertex = new Vertex(x, y, z);
                return true;
            }
            vertex = null;
            return false;
        }

        public virtual bool TryGetConstant(out double number)
        {
            number = 0;
            return false;
        }

        public virtual bool TryGetConstant(out bool boolean)
        {
            if (Function.Name == "True")
            {
                boolean = true;
                return true;
            }
            else if (Function.Name == "False")
            {
                boolean = false;
                return true;
            }
            boolean = false;
            return false;
        }

        public virtual int ElementCount()
        {
            AddMissingParameters();
            int count = 1;
            
            foreach (var parameter in ParameterValues)
                count += parameter.ElementCount();
            
            return count;
        }

        public static Element True() => Part("True");
        public static Element False() => Part("False");
        public static Element EmptyArray() => Part("Empty Array");
        public static Element Null() => Part("Null");
        public static Element EventPlayer() => Part("Event Player");
        public static Element ArrayElement() => Part("Current Array Element");
        public static Element ArrayIndex() => Part("Current Array Index");
        public static Element Compare(IWorkshopTree a, Operator op, IWorkshopTree b) => Part("Compare", a, new OperatorElement(op), b);
        public static Element Vector(Element x, Element y, Element z) => Part("Vector", x, y, z);
        public static Element Not(IWorkshopTree a) => Part("Not", a);
        public static Element IndexOfArrayValue(Element array, Element value) => Part("Index Of Array Value", array, value);
        public static Element IndexOfArrayValue(IWorkshopTree array, IWorkshopTree value) => Part("Index Of Array Value", array, value);
        public static Element Append(Element array, Element value) => Part("Append To Array", array, value);
        public static Element Append(IWorkshopTree array, IWorkshopTree value) => Part("Append To Array", array, value);
        public static Element FirstOf(IWorkshopTree array) => Part("First Of", array);
        public static Element LastOf(IWorkshopTree array) => Part("Last Of", array);
        public static Element CountOf(IWorkshopTree array) => Part("Count Of", array);
        public static Element Contains(IWorkshopTree array, IWorkshopTree value) => Part("Array Contains", array);
        public static Element ValueInArray(IWorkshopTree array, IWorkshopTree index) => Part("Value In Array", array, index);
        public static Element Filter(IWorkshopTree array, IWorkshopTree condition) => Part("Filtered Array", array, condition);
        public static Element Pow(Element a, Element b) => Part("Raise To Power", a, b);
        public static Element Pow(IWorkshopTree a, IWorkshopTree b) => Part("Raise To Power", a, b);
        public static Element Multiply(IWorkshopTree a, IWorkshopTree b) => Part("Multiply", a, b);
        public static Element Divide(IWorkshopTree a, IWorkshopTree b) => Part("Divide", a, b);
        public static Element Modulo(IWorkshopTree a, IWorkshopTree b) => Part("Modulo", a, b);
        public static Element Add(IWorkshopTree a, IWorkshopTree b) => Part("Add", a, b);
        public static Element Subtract(IWorkshopTree a, IWorkshopTree b) => Part("Subtract", a, b);
        public static Element And(IWorkshopTree a, IWorkshopTree b) => Part("And", a, b);
        public static Element Or(IWorkshopTree a, IWorkshopTree b) => Part("Or", a, b);
        public static Element If(IWorkshopTree expression) => Part("If", expression);
        public static Element ElseIf(IWorkshopTree expression) => Part("Else If", expression);
        public static Element Else() => Part("Else");
        public static Element End() => Part("End");
        public static Element While(IWorkshopTree expression) => Part("While", expression);
        public static Element XOf(IWorkshopTree expression) => Part("X Component Of", expression);
        public static Element YOf(IWorkshopTree expression) => Part("Y Component Of", expression);
        public static Element ZOf(IWorkshopTree expression) => Part("Z Component Of", expression);
        public static Element DistanceBetween(IWorkshopTree a, IWorkshopTree b) => Part("Distance Between", a, b);
        public static Element CrossProduct(IWorkshopTree a, IWorkshopTree b) => Part("Cross Product", a, b);
        public static Element DotProduct(IWorkshopTree a, IWorkshopTree b) => Part("Dot Product", a, b);
        public static Element Normalize(IWorkshopTree a) => Part("Normalize", a);
        public static Element DirectionTowards(IWorkshopTree a, IWorkshopTree b) => Part("Direction Towards", a, b);
        public static Element PositionOf(IWorkshopTree player) => Part("Position Of", player);

        public static Element operator +(Element a, Element b) => Part("Add", a, b);
        public static Element operator -(Element a, Element b) => Part("Subtract", a, b);
        public static Element operator *(Element a, Element b) => Part("Multiply", a, b);
        public static Element operator /(Element a, Element b) => Part("Divide", a, b);
        public static Element operator %(Element a, Element b) => Part("Modulo", a, b);
        public static Element operator <(Element a, Element b) => Compare(a, Operator.LessThan, b);
        public static Element operator >(Element a, Element b) => Compare(a, Operator.GreaterThan, b);
        public static Element operator <=(Element a, Element b) => Compare(a, Operator.LessThanOrEqual, b);
        public static Element operator >=(Element a, Element b) => Compare(a, Operator.GreaterThanOrEqual, b);
        public static Element operator !(Element a) => Part("Not", a);
        public static Element operator -(Element a) => a * -1;
        public Element this[IWorkshopTree i]
        {
            get => Part("Value In Array", this, i);
            private set {}
        }
        public Element this[Element i]
        {
            get => Part("Value In Array", this, i);
            private set {}
        }
        public static implicit operator Element(double number) => new NumberElement(number);
        public static implicit operator Element(int number) => new NumberElement(number);
        public static implicit operator Element(bool boolean) => Part(boolean.ToString());

        public static IWorkshopTree GetDefaultParameter(ElementParameter parameter)
        {
            // todo
            return null;
        }

        // Creates an array from a list of values.
        public static Element CreateArray(params IWorkshopTree[] values)
        {
            if (values == null || values.Length == 0) return Part("Empty Array");
            return Part("Array", values);
        }

        public static Element CreateAppendArray(params IWorkshopTree[] values)
        {
            Element array = Part("Empty Array");
            for (int i = 0; i < values.Length; i++)
                array = Part("Append To Array", array, values[i]);
            return array;
        }

        // Creates an ternary conditional that works in the workshop
        public static Element TernaryConditional(IWorkshopTree condition, IWorkshopTree consequent, IWorkshopTree alternative) => Part("If-Then-Else", condition, consequent, alternative);
    }

    public class OperatorElement : IWorkshopTree
    {
        public Operator Operator { get; }

        public OperatorElement(Operator op)
        {
            Operator = op;
        }

        public bool EqualTo(IWorkshopTree other) => other is OperatorElement oe && oe.Operator == Operator;

        public string ToWorkshop(OutputLanguage language, ToWorkshopContext context)
        {
            switch (Operator)
            {
                case Operator.Equal: return "==";
                case Operator.GreaterThan: return ">";
                case Operator.GreaterThanOrEqual: return ">=";
                case Operator.LessThan: return "<";
                case Operator.LessThanOrEqual: return "<=";
                case Operator.NotEqual: return "!=";
                default: throw new NotImplementedException();
            }
        }
    }

    public class OperationElement : IWorkshopTree
    {
        public Operation Operation { get; }

        public OperationElement(Operation op)
        {
            Operation = op;
        }

        public bool EqualTo(IWorkshopTree other) => other is OperationElement oe && oe.Operation == Operation;

        public string ToWorkshop(OutputLanguage language, ToWorkshopContext context)
        {
            // todo
            throw new NotImplementedException();
        }
    }

    public class NumberElement : Element
    {
        public double Value { get; set; }

        public NumberElement(double value) : base(ElementRoot.Instance.GetFunction("Number"), null)
        {
            Value = value;
        }
        public NumberElement() : this(0) {}

        public override bool TryGetConstant(out double number)
        {
            number = Value;
            return true;
        }

        public override string ToWorkshop(OutputLanguage language, ToWorkshopContext context)
        {
            return base.ToWorkshop(language, context);
        }

        public override bool EqualTo(IWorkshopTree other) => base.EqualTo(other) && ((NumberElement)other).Value == Value;
    }

    public enum ElementIndent
    {
        Neutral,
        Increment,
        Decrement
    }

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
        public ValueType ElementValueType { get; }
        private RestrictedCallType? Restricted { get; }

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
            Restricted = type.GetCustomAttribute<RestrictedAttribute>()?.Type;

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

                    // If the default parameter value is an Element and the Element is restricted,
                    if (defaultValue is Element parameterElement && parameterElement.Function.Restricted != null)
                        // ...then add the restricted call type to the parameter's list of restricted call types.
                        Parameters[i].RestrictedCalls.Add((RestrictedCallType)parameterElement.Function.Restricted);
                }
            }
        }

        public Element GetObject()
        {
            return (Element)Activator.CreateInstance(Type);
        }

        public bool DoesReturnValue => IsValue;

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            Element element = GetObject();
            element.ParameterValues = methodCall.ParameterValues;
            element.Comment = methodCall.ActionComment;

            if (!IsValue)
            {
                actionSet.AddAction(element);
                return null;
            }
            else return element;
        }

        public string GetLabel(bool markdown) => HoverHandler.GetLabel(!IsValue ? null : ReturnType?.Name ?? "define", Name, Parameters, markdown, Wiki?.Description);

        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            if (Restricted != null)
                // If there is a restricted call type, add it.
                parseInfo.RestrictedCallHandler.RestrictedCall(new RestrictedCall(
                    (RestrictedCallType)Restricted,
                    parseInfo.GetLocation(callRange),
                    RestrictedCall.Message_Element((RestrictedCallType)Restricted)
                ));
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