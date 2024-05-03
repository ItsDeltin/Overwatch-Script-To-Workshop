#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Model;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Emulator;

public abstract record EmulateValue
{
    public static readonly EmulateValue Default = 0;
    static readonly Vector ZeroVector = new(0, 0, 0);

    public abstract double AsNumber();
    public abstract bool AsBoolean();
    Vector AsVector() => this as Vector ?? ZeroVector;
    public virtual int CountOf() => 0;
    public virtual EmulateValue FirstOf() => 0;
    public virtual EmulateValue LastOf() => 0;
    public virtual EmulateValue[] Spread() => [this];

    public EmulateValue ValueInArray(double index)
    {
        var items = Spread();
        return index < items.Length ? items[Math.Max((int)index, 0)] : Default;
    }
    public EmulateValue IndexOf(EmulateValue value)
    {
        var array = Spread();
        for (int i = 0; i < array.Length; i++)
            if (array[i] == value)
                return i;
        return -1;
    }
    public EmulateValue Append(EmulateValue value) => From([.. Spread(), .. value.Spread()]);
    public EmulateValue RemoveByValue(EmulateValue value)
    {
        var array = Spread().ToList();
        array.Remove(value);
        return From(array);
    }
    public EmulateValue RemoveAtIndex(double index)
    {
        var array = Spread().ToList();

        if (index >= 0 && index < array.Count)
            array.RemoveAt((int)index);

        return From(array);
    }
    public EmulateValue SetAtIndex(double index, EmulateValue value)
    {
        var array = Spread().ToList();

        // Todo: what does the workshop do when doing "A[-1] = ..."?
        index = Math.Max(index, 0);

        while (array.Count <= index)
            array.Add(Default);

        array[(int)index] = value;

        return From(array);
    }
    public EmulateValue Modify(Operation operation, EmulateValue operand) => operation switch
    {
        Operation.Add => this + operand,
        Operation.Subtract => this - operand,
        Operation.Multiply => this * operand,
        Operation.Divide => this / operand,
        Operation.Modulo => this % operand,
        Operation.RaiseToPower => Pow(this, operand),
        Operation.Min => Min(this, operand),
        Operation.Max => Max(this, operand),
        Operation.AppendToArray => Append(operand),
        Operation.RemoveFromArrayByValue => RemoveByValue(operand),
        Operation.RemoveFromArrayByIndex => RemoveAtIndex(operand.AsNumber()),
        _ => throw new NotImplementedException(),
    };
    public EmulateValue ModifyAtIndex(double index, Operation operation, EmulateValue operand) =>
        SetAtIndex(index, ValueInArray(index).Modify(operation, operand));

    sealed record Number(double Value) : EmulateValue
    {
        public override double AsNumber() => Value;
        public override bool AsBoolean() => Value != 0;
        public override string ToString() => Value.ToString();
    }
    sealed record Boolean(bool Value) : EmulateValue
    {
        public override double AsNumber() => Value ? 1 : 0;
        public override bool AsBoolean() => Value;
        public override string ToString() => Value ? "True" : "False";
    }
    sealed record Array(EmulateValue[] Values) : EmulateValue
    {
        public override double AsNumber() => 0;
        public override bool AsBoolean() => Values.Length > 0;
        public override int CountOf() => Values.Length;
        public override EmulateValue FirstOf() => Values.Length == 0 ? Default : Values[0];
        public override EmulateValue LastOf() => Values.Length == 0 ? Default : Values[^1];
        public override EmulateValue[] Spread() => Values;
    }
    sealed record String(string Value) : EmulateValue
    {
        public override bool AsBoolean() => Value.Length > 0;
        public override double AsNumber() => 0;
        public override string ToString() => Value;
    }
    sealed record Vector(double X, double Y, double Z) : EmulateValue
    {
        public override bool AsBoolean() => X + Y + Z != 0;
        public override double AsNumber() => 0;
        public override string ToString() => $"Vector({X}, {Y}, {Z})";
    }

    public static implicit operator EmulateValue(double value) => new Number(value);
    public static implicit operator EmulateValue(int value) => new Number(value);
    public static implicit operator EmulateValue(bool value) => new Boolean(value);
    public static implicit operator EmulateValue(string value) => new String(value);
    public static implicit operator EmulateValue(EmulateValue[] values) => new Array(values);

    public static implicit operator double(EmulateValue emulateValue) => emulateValue.AsNumber();
    public static implicit operator bool(EmulateValue emulateValue) => emulateValue.AsBoolean();

    public static EmulateValue From(double value) => value;
    public static EmulateValue From(int value) => value;
    public static EmulateValue From(bool value) => value;
    public static EmulateValue From(string value) => value;
    public static EmulateValue From(EmulateValue[] values) => values;
    public static EmulateValue From(IEnumerable<EmulateValue> values) => values.ToArray();
    public static EmulateValue From(double X, double Y, double Z) => new Vector(X, Y, Z);

    public static EmulateValue operator +(EmulateValue left, EmulateValue right) => Add(left, right);
    public static EmulateValue operator -(EmulateValue left, EmulateValue right) => Subtract(left, right);
    public static EmulateValue operator *(EmulateValue left, EmulateValue right) => Multiply(left, right);
    public static EmulateValue operator /(EmulateValue left, EmulateValue right) => Divide(left, right);
    public static EmulateValue operator %(EmulateValue left, EmulateValue right) => Modulo(left, right);

    public static EmulateValue Add(EmulateValue left, EmulateValue right)
    {
        if ((left, right) is (Number a, Number b))
            return a.Value + b.Value;
        return Default;
    }
    public static EmulateValue Subtract(EmulateValue left, EmulateValue right)
    {
        if ((left, right) is (Number a, Number b))
            return a.Value - b.Value;
        return Default;
    }
    public static EmulateValue Multiply(EmulateValue left, EmulateValue right)
    {
        if ((left, right) is (Number a, Number b))
            return a.Value * b.Value;
        return Default;
    }
    public static EmulateValue Divide(EmulateValue left, EmulateValue right)
    {
        if ((left, right) is (Number a, Number b))
            return a.Value / b.Value;
        return Default;
    }
    public static EmulateValue Modulo(EmulateValue left, EmulateValue right) => NumOp(left, right, (a, b) => a % b) ?? Default;
    public static EmulateValue Pow(EmulateValue left, EmulateValue right) => NumOp(left, right, Math.Pow) ?? Default;
    public static EmulateValue Min(EmulateValue left, EmulateValue right) => NumOp(left, right, Math.Min) ?? Default;
    public static EmulateValue Max(EmulateValue left, EmulateValue right) => NumOp(left, right, Math.Max) ?? Default;

    static EmulateValue? NumOp(EmulateValue left, EmulateValue right, Func<double, double, double> op)
    {
        if ((left, right) is (Number a, Number b))
            return op(left, right);
        return default;
    }

    public static EmulateValue Compare(EmulateValue left, Operator op, EmulateValue right) => op switch
    {
        Operator.Equal => left == right,
        Operator.NotEqual => left != right,
        Operator.LessThan => left.AsNumber() < right.AsNumber(),
        Operator.LessThanOrEqual => left.AsNumber() <= right.AsNumber(),
        Operator.GreaterThan => left.AsNumber() > right.AsNumber(),
        Operator.GreaterThanOrEqual => left.AsNumber() >= right.AsNumber(),
        _ => throw new NotImplementedException(),
    };

    public static Result<EmulateValue, string> Evaluate(IWorkshopTree value, EmulateState state)
    {
        if (value is NumberElement number)
            return From(number.Value);
        else if (value is Element element)
        {
            var name = element.Function.Name;
            var p = element.ParameterValues;

            Result<EmulateValue, string> arithmetic(Func<EmulateValue, EmulateValue, EmulateValue> function)
            {
                return Evaluate(p[0], state).And(Evaluate(p[1], state)).MapValue(v => function(v.a, v.b));
            }

            Result<EmulateValue, string> eval(IWorkshopTree value) => Evaluate(value, state);

            Result<EmulateValue, string> one(Func<EmulateValue, EmulateValue> then) => eval(p[0]).MapValue(then);

            return (name, p.Length) switch
            {
                ("True", _) => From(true),
                ("False", _) => From(false),
                ("Add", 2) => arithmetic(Add),
                ("Subtract", 2) => arithmetic(Subtract),
                ("Multiply", 2) => arithmetic(Multiply),
                ("Divide", 2) => arithmetic(Divide),
                ("Modulo", 2) => arithmetic(Modulo),
                ("Compare", 3) => Evaluate(p[0], state).And(EmulateHelper.ExtractOperator(p[1])).And(Evaluate(p[2], state)).MapValue(ab_c => Compare(ab_c.a.a, ab_c.a.b, ab_c.b)),
                ("And", 2) => arithmetic((a, b) => a.AsBoolean() && b.AsBoolean()),
                ("Or", 2) => arithmetic((a, b) => a.AsBoolean() || b.AsBoolean()),
                ("Not", 1) => one(value => !value.AsBoolean()),
                ("If-Then-Else", 3) => eval(p[0]).And(eval(p[1])).And(eval(p[2])).MapValue(ab_c => ab_c.a.a.AsBoolean() ? ab_c.a.b : ab_c.b),
                ("Count Of", 1) => Evaluate(p[0], state).MapValue(v => From(v.CountOf())),
                ("Array", _) => p.SelectResult(v => Evaluate(v, state)).MapValue(arrayValues => From(arrayValues.ToArray())),
                ("Empty Array", _) => From(values: []),
                ("First Of", 1) => Evaluate(p[0], state).MapValue(v => v.FirstOf()),
                ("Last Of", 1) => Evaluate(p[0], state).MapValue(v => v.LastOf()),
                ("Global Variable", 1) => EmulateHelper.ExtractVariableName(p[0]).MapValue(name => state.GetGlobalVariable(name).Value),
                ("Value In Array", 2) => arithmetic((array, index) => array.ValueInArray(index)),
                ("Index Of Array Value", 2) => arithmetic((array, value) => array.IndexOf(value)),
                ("String" or "Custom String", _) => EvaluateString(element, state),
                ("Vector", 3) => eval(p[0]).And(eval(p[1])).And(eval(p[2])).MapValue(xy_z => From(xy_z.a.a, xy_z.a.b, xy_z.b)),
                ("X Component Of", 1) => eval(p[0]).MapValue(value => From(value.AsVector().X)),
                ("Y Component Of", 1) => eval(p[0]).MapValue(value => From(value.AsVector().Y)),
                ("Z Component Of", 1) => eval(p[0]).MapValue(value => From(value.AsVector().Z)),
                ("Null", _) => Default, // Do we need a dedicated null value? probably not
                (_, _) => $"Emulation for workshop function '{name}' (with {p.Length} parameters) is not supported"
            };
        }
        else if (value is DynamicSkip dynamicSkip)
        {
            return From(dynamicSkip.Value());
        }
        return $"Emulation for IWorkshopTree type '{value.GetType()}' is not supported";
    }

    static Result<EmulateValue, string> EvaluateString(Element element, EmulateState state)
    {
        if (element is not StringElement stringElement)
            return "Element is not a string";

        var str = stringElement.Value;
        return stringElement.ParameterValues.SelectResult(p => Evaluate(p, state)).MapValue(formats =>
        {
            var f = formats.ToArray();
            for (int i = 0; i < f.Length; i++)
                str = str.Replace($"{{{i}}}", f[i].ToString());

            return From(str);
        });
    }
}