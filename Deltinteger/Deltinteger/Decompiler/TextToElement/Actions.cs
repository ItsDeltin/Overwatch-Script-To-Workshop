using System.Linq;
using Deltin.Deltinteger.Decompiler.ElementToCode;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    public interface ITTEAction
    {
        string Comment { get; set; }
        bool Disabled { get; set; }
        void Decompile(DecompileRule decompiler);
    }

    public class SetVariableAction : ITTEAction
    {
        public ITTEVariable Variable { get; }
        public string Operator { get; }
        public ITTEExpression Value { get; }
        public ITTEExpression Index { get; }
        public string Comment { get; set; }
        public bool Disabled { get; set; }

        public SetVariableAction(ITTEVariable variable, string op, ITTEExpression value, ITTEExpression index)
        {
            Variable = variable;
            Operator = op;
            Value = value;
            Index = index;
        }

        public override string ToString() => Variable.ToString() + (Index == null ? " " : "[" + Index.ToString() + "] ") + Operator + " " + Value.ToString();

        public void Decompile(DecompileRule decompiler)
        {
            decompiler.AddComment(this);

            if (Disabled) decompiler.Append("// ");
            Variable.Decompile(decompiler);

            if (Index != null)
            {
                decompiler.Append("[");
                Index.Decompile(decompiler);
                decompiler.Append("]");
            }

            decompiler.Append(" " + Operator + " ");
            Value.Decompile(decompiler);
            decompiler.EndAction();
        }
    }

    public class CallSubroutine : ITTEAction
    {
        public string SubroutineName { get; }
        public Parse.CallParallel Parallel { get; }
        public string Comment { get; set; }
        public bool Disabled { get; set; }

        public CallSubroutine(string name, Parse.CallParallel parallel)
        {
            SubroutineName = name;
            Parallel = parallel;
        }

        public void Decompile(DecompileRule decompiler)
        {
            decompiler.AddComment(this);

            switch (Parallel)
            {
                case Parse.CallParallel.NoParallel:
                    decompiler.Append(SubroutineName + "()");
                    break;

                case Parse.CallParallel.AlreadyRunning_DoNothing:
                    decompiler.Append("async! " + SubroutineName + "()");
                    break;

                case Parse.CallParallel.AlreadyRunning_RestartRule:
                    decompiler.Append("async " + SubroutineName + "()");
                    break;
            }
            decompiler.EndAction();
        }
    }
}