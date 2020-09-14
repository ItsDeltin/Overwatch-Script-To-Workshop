using System.Linq;
using Deltin.Deltinteger.Decompiler.ElementToCode;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    public class FunctionExpression : ITTEExpression, ITTEAction
    {
        public ElementBaseJson Function { get; }
        public ITTEExpression[] Values { get; }
        public string Comment { get; set; }
        public bool Disabled { get; set; }

        public FunctionExpression(ElementBaseJson function, ITTEExpression[] values)
        {
            Function = function;
            Values = values;
        }

        public override string ToString() => Function.Name + (Values.Length == 0 ? "" : "(" + string.Join(", ", Values.Select(v => v.ToString())) + ")");

        void ITTEExpression.Decompile(DecompileRule decompiler) => Decompile(decompiler, false);
        void ITTEAction.Decompile(DecompileRule decompiler)
        {
            decompiler.AddComment(this);
            Decompile(decompiler, true);
        }

        public void Decompile(DecompileRule decompiler, bool end)
        {
            if (Disabled)
                decompiler.Append("// ");

            if (WorkshopFunctionDecompileHook.Convert.TryGetValue(Function.CodeName(), out var action))
                action.Invoke(decompiler, this);
            else
                Default(decompiler, end);
        }

        public void Default(DecompileRule decompiler, bool end)
        {
            decompiler.Append(Function.Name + "(");

            for (int i = 0; i < Values.Length; i++)
            {
                Values[i].Decompile(decompiler);
                if (i < Values.Length - 1)
                    decompiler.Append(", ");
            }

            decompiler.Append(")");

            // Finished
            if (end)
                decompiler.EndAction();
        }
    }
}