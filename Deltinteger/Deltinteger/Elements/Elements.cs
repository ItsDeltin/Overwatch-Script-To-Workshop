using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Models;
using Deltin.Deltinteger.I18n;
using Deltin.Deltinteger.Compiler;
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
        Players,
        Team,
        Map,
        Gamemode,
        Button,
        String,
        Effect,
        Text,
        Icon,
        WorldText
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

        public Element(ElementBaseJson function, params IWorkshopTree[] parameterValues)
        {
            Function = function;
            ParameterValues = parameterValues;
        }

        public virtual void ToWorkshop(WorkshopBuilder b, ToWorkshopContext context)
        {
            if (CustomConverters.Converters.TryGetValue(Function.Name, out var converter))
            {
                converter(b, this);
                return;
            }

            var action = Function as ElementJsonAction;
            if (action != null && action.Indentation == "outdent") b.Outdent();

            // Add a comment and newline
            if (Comment != null) b.Append($"\"{Comment}\"");

            // Add the disabled tag if the element is disabled.
            if (Function is ElementJsonAction && Disabled) b.AppendKeyword("disabled").Append(" ");

            // Add the name of the element.
            b.AppendKeyword(Function.Name);

            // Add the parameters.
            AddMissingParameters();
            if (ParameterValues.Length > 0)
            {
                b.Append("(");
                ParametersToWorkshop(b);
                b.Append(")");
            }

            if (action != null)
            {
                b.AppendLine(";");
                if (action.Indentation == "indent") b.Indent();
            }
        }

        protected void ParametersToWorkshop(WorkshopBuilder b)
        {
            for (int i = 0; i < ParameterValues.Length; i++)
            {
                ParameterValues[i].ToWorkshop(b, ToWorkshopContext.NestedValue);
                if (i != ParameterValues.Length - 1) b.Append(", ");
            }
        }

        /// <summary>Makes sure no parameter values are null.</summary>
        private void AddMissingParameters()
        {
            List<IWorkshopTree> parameters = new List<IWorkshopTree>();

            for (int i = 0; (Function.Parameters != null && i < Function.Parameters.Length) || (ParameterValues != null && i < ParameterValues.Length); i++)
                parameters.Add(ParameterValues?.ElementAtOrDefault(i) ?? Function.Parameters[i].GetDefaultValue() ?? throw new Exception("Null argument"));
            
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

            if (OptimizeElements.Optimizations.TryGetValue(Function.Name, out var optimizer))
                return optimizer(this);
            
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
            }
            else if (Function.Name == "Left") vertex = new Vertex(1, 0, 0);
            else if (Function.Name == "Right") vertex = new Vertex(-1, 0, 0);
            else if (Function.Name == "Up") vertex = new Vertex(0, 1, 0);
            else if (Function.Name == "Down") vertex = new Vertex(0, -1, 0);
            else if (Function.Name == "Forward") vertex = new Vertex(0, 0, 1);
            else if (Function.Name == "Backward") vertex = new Vertex(0, 0, -1);
			else if (Function.Name == "Subtract" 
					&& ParameterValues[0] is Element le && le.Function.Name == "Left"  
					&& ParameterValues[1] is Element re && re.Function.Name == "Left") vertex = new Vertex(0,0,0);
            else
            {   
                vertex = null;
                return false;
            }
            return true;
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
        public static Element Num(double value) => new NumberElement(value);
        public static Element Compare(IWorkshopTree a, Operator op, IWorkshopTree b) => Part("Compare", a, new OperatorElement(op), b);
        public static Element Vector(Element x, Element y, Element z) => Part("Vector", x, y, z);
        public static Element Vector(IWorkshopTree x, IWorkshopTree y, IWorkshopTree z) => Part("Vector", x, y, z);
        public static Element Not(IWorkshopTree a) => Part("Not", a);
        public static Element IndexOfArrayValue(Element array, Element value) => Part("Index Of Array Value", array, value);
        public static Element IndexOfArrayValue(IWorkshopTree array, IWorkshopTree value) => Part("Index Of Array Value", array, value);
        public static Element Append(Element array, Element value) => Part("Append To Array", array, value);
        public static Element Append(IWorkshopTree array, IWorkshopTree value) => Part("Append To Array", array, value);
        public static Element FirstOf(IWorkshopTree array) => Part("First Of", array);
        public static Element LastOf(IWorkshopTree array) => Part("Last Of", array);
        public static Element CountOf(IWorkshopTree array) => Part("Count Of", array);
        public static Element Contains(IWorkshopTree array, IWorkshopTree value) => Part("Array Contains", array, value);
        public static Element ValueInArray(IWorkshopTree array, IWorkshopTree index) => Part("Value In Array", array, index);
        public static Element Filter(IWorkshopTree array, IWorkshopTree condition) => Part("Filtered Array", array, condition);
        public static Element Sort(IWorkshopTree array, IWorkshopTree rank) => Part("Sorted Array", array, rank);
        public static Element All(IWorkshopTree array, IWorkshopTree condition) => Part("Is True For All", array, condition);
        public static Element Any(IWorkshopTree array, IWorkshopTree condition) => Part("Is True For Any", array, condition);
        public static Element Map(IWorkshopTree array, IWorkshopTree select) => Part("Mapped Array", array, select);
        public static Element Slice(IWorkshopTree array, IWorkshopTree start, IWorkshopTree count) => Part("Array Slice", array, start, count);
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
        public static Element TimeElapsed() => Part("Total Time Elapsed");
        public static Element Wait() => Part("Wait", Num(Constants.MINIMUM_WAIT), ElementRoot.Instance.GetEnumValueFromWorkshop("WaitBehavior", "Ignore Condition"));
        public static Element LoopIfConditionIsTrue() => Part("Loop If Condition Is True");
        public static Element XOf(IWorkshopTree expression) => Part("X Component Of", expression);
        public static Element YOf(IWorkshopTree expression) => Part("Y Component Of", expression);
        public static Element ZOf(IWorkshopTree expression) => Part("Z Component Of", expression);
        public static Element DistanceBetween(IWorkshopTree a, IWorkshopTree b) => Part("Distance Between", a, b);
        public static Element CrossProduct(IWorkshopTree a, IWorkshopTree b) => Part("Cross Product", a, b);
        public static Element DotProduct(IWorkshopTree a, IWorkshopTree b) => Part("Dot Product", a, b);
        public static Element Normalize(IWorkshopTree a) => Part("Normalize", a);
        public static Element DirectionTowards(IWorkshopTree a, IWorkshopTree b) => Part("Direction Towards", a, b);
        public static Element PositionOf(IWorkshopTree player) => Part("Position Of", player);
        public static Element EyePosition(IWorkshopTree player) => Part("Eye Position", player);
        public static Element FacingDirectionOf(IWorkshopTree player) => Part("Facing Direction Of", player);
        public static Element RoundToInt(IWorkshopTree value, Rounding rounding) => Part("Round To Integer", value, ElementRoot.Instance.GetEnumValueFromWorkshop("Rounding", rounding == Rounding.Down ? "Down" : rounding == Rounding.Up ? "Up" : "To Nearest"));
        public static Element CustomString(string value, params Element[] formats) => new StringElement(value, formats);
        public static Element LastEntity() => Part("Last Entity");
        public static Element RaycastPosition(IWorkshopTree start, IWorkshopTree end, IWorkshopTree playersToInclude = null, IWorkshopTree playersToExclude = null, IWorkshopTree includePlayerOwnedObjects = null)
            => Part("Ray Cast Hit Position", start ?? throw new ArgumentNullException(nameof(start)), end ?? throw new ArgumentNullException(nameof(end)), playersToInclude ?? Null(), playersToExclude ?? Null(), includePlayerOwnedObjects ?? False());
        public static Element CallSubroutine(Subroutine subroutine) => Element.Part("Call Subroutine", subroutine);
        public static Element StartRule(Subroutine subroutine, bool restartRule) => Element.Part("Start Rule", subroutine, ElementRoot.Instance.GetEnumValue("IfAlreadyRunning", restartRule ? "Restart Rule" : "Do Nothing"));
        public static Element SkipIf(Element condition, Element count) => Element.Part("Skip If", condition, count);

        public static Element Hud(
            IWorkshopTree players = null,
            IWorkshopTree header = null, IWorkshopTree subheader = null, IWorkshopTree text = null,
            string location = null, double? sortOrder = null,
            string headerColor = null, string subheaderColor = null, string textColor = null,
            string reevaluation = null, string spectators = null)
        =>
            Element.Part("Create HUD Text",
                players ?? Part("All Players"),
                header ?? Null(),
                subheader ?? Null(),
                text ?? Null(),
                ElementRoot.Instance.GetEnumValueFromWorkshop("Location", location ?? "Top"),
                Element.Num(sortOrder == null ? 0 : sortOrder.Value),
                ElementRoot.Instance.GetEnumValueFromWorkshop("Color", headerColor ?? "White"),
                ElementRoot.Instance.GetEnumValueFromWorkshop("Color", subheaderColor ?? "White"),
                ElementRoot.Instance.GetEnumValueFromWorkshop("Color", textColor ?? "White"),
                ElementRoot.Instance.GetEnumValueFromWorkshop("HudTextRev", reevaluation ?? "Visible To And String"),
                ElementRoot.Instance.GetEnumValueFromWorkshop("Spectators", spectators ?? "Default Visibility")
            );

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
        public static implicit operator Element(double number) => Num(number);
        public static implicit operator Element(int number) => Num(number);
        public static implicit operator Element(bool boolean) => Part(boolean.ToString());

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

        public void ToWorkshop(WorkshopBuilder b, ToWorkshopContext context) => b.Append(ToString());

        public override string ToString()
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

        public void ToWorkshop(WorkshopBuilder b, ToWorkshopContext context) => ElementRoot.Instance.GetEnumValue("Operation", Operation.ToString()).ToWorkshop(b, context);
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

        public override void ToWorkshop(WorkshopBuilder b, ToWorkshopContext context) => b.Append(Value.ToString());
        public override bool EqualTo(IWorkshopTree other) => base.EqualTo(other) && ((NumberElement)other).Value == Value;
    }
}
