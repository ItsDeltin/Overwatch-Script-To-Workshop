using Deltin.Deltinteger.Decompiler.ElementToCode;

namespace Deltin.Deltinteger.Decompiler.TextToElement
{
    public class TTERule
    {
        public string Name { get; }
        public EventInfo EventInfo { get; }
        public TTECondition[] Conditions { get; }
        public ITTEAction[] Actions { get; }
        public bool Disabled { get; }

        public TTERule(string name, EventInfo eventInfo, TTECondition[] conditions, ITTEAction[] actions, bool disabled)
        {
            Name = name;
            EventInfo = eventInfo;
            Conditions = conditions;
            Actions = actions;
            Disabled = disabled;
        }

        public override string ToString() => Name + " [" + Actions.Length + " actions]";
    }

    public class TTECondition
    {
        public bool Disabled { get; }
        public ITTEExpression Expression { get; }

        public TTECondition(bool disabled, ITTEExpression expression)
        {
            Disabled = disabled;
            Expression = expression;
        }

        public void Decompile(DecompileRule decompiler)
        {
            decompiler.NewLine();

            // Make the condition a comment if it is disabled.
            if (Disabled) decompiler.Append("// ");

            // Add the condition.
            decompiler.Append("if (");
            Expression.Decompile(decompiler);
            decompiler.Append(")");
        }
    }
}