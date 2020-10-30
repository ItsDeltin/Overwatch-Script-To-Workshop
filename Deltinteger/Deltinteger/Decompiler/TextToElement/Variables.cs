using Deltin.Deltinteger.Decompiler.ElementToCode;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    public interface ITTEVariable
    {
        string Name { get; }
        void Decompile(DecompileRule decompiler);
    }

    public class GlobalVariableExpression : ITTEExpression, ITTEVariable
    {
        public string Name { get; }

        public GlobalVariableExpression(string name)
        {
            Name = name;
        }

        public override string ToString() => "Global." + Name;
        public void Decompile(DecompileRule decompiler) => decompiler.Append(decompiler.Decompiler.GetVariableName(Name, true));
    }

    public class AnonymousVariableExpression : ITTEExpression, ITTEVariable
    {
        public string Name { get; }
        public bool IsGlobal { get; }

        public AnonymousVariableExpression(string name, bool isGlobal)
        {
            Name = name;
            IsGlobal = isGlobal;
        }

        public override string ToString() => (IsGlobal ? "Global." : "Player.") + Name;
        public void Decompile(DecompileRule decompiler) => decompiler.Append(decompiler.Decompiler.GetVariableName(Name, IsGlobal));
    }

    public class PlayerVariableExpression : ITTEExpression, ITTEVariable
    {
        public string Name { get; }
        public ITTEExpression Player { get; }

        public PlayerVariableExpression(string name, ITTEExpression player)
        {
            Name = name;
            Player = player;
        }

        public override string ToString() => Player.ToString() + "." + Name;

        public void Decompile(DecompileRule decompiler)
        {
            Player.WritePlayerSeperator(decompiler);
            decompiler.Append(decompiler.Decompiler.GetVariableName(Name, false));
        }
    }
}