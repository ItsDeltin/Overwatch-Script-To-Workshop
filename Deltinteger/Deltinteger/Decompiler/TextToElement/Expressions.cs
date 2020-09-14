using Deltin.Deltinteger.Decompiler.ElementToCode;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    // Interfaces
    public interface ITTEExpression
    {
        void Decompile(DecompileRule decompiler);
        bool IsEventPlayer() => this is FunctionExpression func && func.Function.Name == "Event Player";
        bool RequiresContainment() => this is UnaryOperatorExpression || this is BinaryOperatorExpression || this is TernaryExpression;
        void WritePlayerSeperator(DecompileRule decompiler)
        {
            if (IsEventPlayer()) return;

            if (RequiresContainment()) decompiler.Append("(");
            Decompile(decompiler);
            if (RequiresContainment()) decompiler.Append(")");
            
            decompiler.Append(".");
        }
    }

    public class NumberExpression : ITTEExpression
    {
        public double Value { get; }

        public NumberExpression(double value)
        {
            Value = value;
        }

        public override string ToString() => Value.ToString();
        public void Decompile(DecompileRule decompiler) => decompiler.Append(Value.ToString());
    }

    public class StringExpression : ITTEExpression
    {
        public string Value { get; }
        public ITTEExpression[] Formats { get; }
        public bool IsLocalized { get; }

        public StringExpression(string str, ITTEExpression[] formats, bool isLocalized)
        {
            Value = str;
            Formats = formats;
            IsLocalized = isLocalized;
        }

        public override string ToString() => (IsLocalized ? "@" : "") + "\"" + Value + "\"";

        public void Decompile(DecompileRule decompiler)
        {
            string str = (IsLocalized ? "@\"" : "\"") + Value + "\"";
            if (Formats == null || Formats.Length == 0)
                decompiler.Append(str);
            else
            {
                decompiler.Append("<" + str + ", ");
                for (int i = 0; i < Formats.Length; i++)
                {
                    Formats[i].Decompile(decompiler);
                    if (i < Formats.Length - 1)
                        decompiler.Append(", ");
                }
                decompiler.Append(">");
            }
        }
    }

    public class IndexerExpression : ITTEExpression
    {
        public ITTEExpression Expression { get; }
        public ITTEExpression Index { get; }

        public IndexerExpression(ITTEExpression expression, ITTEExpression index)
        {
            Expression = expression;
            Index = index;
        }

        public override string ToString() => Expression.ToString() + "[" + Index.ToString() + "]";

        public void Decompile(DecompileRule decompiler)
        {
            if (Expression.RequiresContainment()) decompiler.Append("(");
            Expression.Decompile(decompiler);
            if (Expression.RequiresContainment()) decompiler.Append(")");

            decompiler.Append("[");
            Index.Decompile(decompiler);
            decompiler.Append("]");
        }
    }

    public class ConstantEnumeratorExpression : ITTEExpression
    {
        public ElementEnumMember Member { get; }

        public ConstantEnumeratorExpression(ElementEnumMember member)
        {
            Member = member;
        }

        public override string ToString() => Member.Name;

        public void Decompile(DecompileRule decompiler)
        {
            decompiler.Append(Member.Enum.Name + "." + Member.CodeName());
        }
    }
}