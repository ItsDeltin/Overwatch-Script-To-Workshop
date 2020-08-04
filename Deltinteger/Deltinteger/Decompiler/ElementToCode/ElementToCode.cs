using System.Text;
using Deltin.Deltinteger.Decompiler.TextToElement;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Decompiler.ElementToCode
{
    public class CodeFormattingOptions
    {
        public bool SameLineOpeningBrace = false;
        public bool IndentWithTabs = false;
    }

    public class WorkshopDecompiler
    {
        public Workshop Workshop { get; }
        public CodeFormattingOptions Options { get; }
        public int IndentLevel { get; private set; }
        private readonly StringBuilder _builder = new StringBuilder();
        private bool space = false;

        public WorkshopDecompiler(Workshop workshop, CodeFormattingOptions options)
        {
            Workshop = workshop;
            Options = options;
        }

        public void AddBlock()
        {
            if (Options.SameLineOpeningBrace) Append(" {");
            else
            {
                NewLine();
                Append("{");
                NewLine();
                Indent();
            }
        }

        public void Indent() => IndentLevel++;
        public void Outdent() => IndentLevel--;

        public void Append(string text)
        {
            if (space)
            {
                space = false;
                _builder.Append(new string(Options.IndentWithTabs ? '\t' : ' ', IndentLevel * (Options.IndentWithTabs ? 1 : 4)));
            }
            _builder.Append(text);
        }

        public void NewLine()
        {
            _builder.AppendLine();
            space = true;
        }

        public string Decompile()
        {
            // Variables
            foreach (var variable in Workshop.Variables)
            {
                Append((variable.IsGlobal ? "globalvar" : "playervar") + " define " + variable.Name + ";");
                NewLine();
            }
            NewLine();
            
            // Rules
            foreach (var rule in Workshop.Rules)
                new DecompileRule(this, rule).Decompile();
            
            return _builder.ToString();
        }

        public override string ToString() => _builder.ToString();
    }

    public class DecompileRule
    {
        public WorkshopDecompiler Decompiler { get; }
        public TTERule Rule { get; }
        public int CurrentAction { get; private set; }
        public bool IsFinished => CurrentAction >= Rule.Actions.Length;
        public ITTEAction Current => Rule.Actions[CurrentAction];

        public DecompileRule(WorkshopDecompiler decompiler, TTERule rule)
        {
            Decompiler = decompiler;
            Rule = rule;
        }
        
        public void Decompile()
        {
            if (Rule.Disabled) Decompiler.Append("disabled ");
            Decompiler.Append("rule: \"" + Rule.Name + "\"");

            if (Rule.EventInfo.Event != RuleEvent.OngoingGlobal)
            {
                Decompiler.NewLine();
                Decompiler.Append("Event." + EnumData.GetEnumValue(Rule.EventInfo.Event).CodeName);
                // Write the event.
                if (Rule.EventInfo.Team != Team.All)
                {
                    Decompiler.NewLine();
                    Decompiler.Append("Team." + EnumData.GetEnumValue(Rule.EventInfo.Team).CodeName);
                }
                // Write the player.
                if (Rule.EventInfo.Player != PlayerSelector.All)
                {
                    Decompiler.NewLine();
                    Decompiler.Append("Player." + EnumData.GetEnumValue(Rule.EventInfo.Player).CodeName);
                }
            }

            Decompiler.AddBlock();

            while (CurrentAction < Rule.Actions.Length)
                DecompileCurrentAction();

            Decompiler.Outdent();
            Decompiler.Append("}");
            Decompiler.NewLine();
            Decompiler.NewLine();
        }

        public void DecompileCurrentAction()
        {
            Rule.Actions[CurrentAction].Decompile(this);
            CurrentAction++;
        }

        public void Append(string text) => Decompiler.Append(text);
        public void NewLine() => Decompiler.NewLine();
        public void AddBlock() => Decompiler.AddBlock();
        public void Outdent() => Decompiler.Outdent();
        public void Advance() {
            CurrentAction++;
        }
    }
}