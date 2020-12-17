using System.Text;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Deltin.Deltinteger.Decompiler.TextToElement;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;

namespace Deltin.Deltinteger.Decompiler.ElementToCode
{
    public class CodeFormattingOptions
    {
        public bool SameLineOpeningBrace = false;
        public bool IndentWithTabs = false;
        public int SpaceIndentCount = 4;
    }

    public class WorkshopDecompiler
    {
        public Workshop Workshop { get; }
        public CodeFormattingOptions Options { get; }
        public int IndentLevel { get; private set; }
        private readonly IDecompilerLobbySettingsResolver _settingsResolver;
        private readonly StringBuilder _builder = new StringBuilder();
        private bool _space = false;

        public WorkshopDecompiler(Workshop workshop, IDecompilerLobbySettingsResolver settingsResolver, CodeFormattingOptions options)
        {
            Workshop = workshop;
            Options = options;
            _settingsResolver = settingsResolver;
        }

        public string Decompile()
        {
            if (Workshop.Actions != null)
                new ActionTraveler(this, Workshop.Actions).Decompile();
            else if (Workshop.Conditions != null)
                new ConditionTraveler(this, Workshop.Conditions).Decompile();
            else
            {
                // Add settings import.
                if (_settingsResolver != null && Workshop.LobbySettings != null)
                {
                    string settingsFile = _settingsResolver.GetFile();
                    // If the resolved file is not null, add the import statement.
                    if (settingsFile != null)
                    {
                        Append("import \"" + settingsFile + "\";");
                        NewLine();
                        NewLine();
                    }
                }

                // Variables
                foreach (var variable in Workshop.Variables)
                {
                    Append((variable.IsGlobal ? "globalvar" : "playervar") + " define " + GetVariableName(variable.Name, variable.IsGlobal) + ";");
                    NewLine();
                }
                NewLine();

                // Rules
                foreach (var rule in Workshop.Rules)
                    new RuleTraveler(this, rule).Decompile();
            }

            return _builder.ToString().Trim();
        }

        public void AddBlock(bool startBlock = true)
        {
            if (Options.SameLineOpeningBrace)
            {
                if (startBlock) Append(" {");
                NewLine();
                Indent();
            }
            else
            {
                if (startBlock)
                {
                    NewLine();
                    Append("{");
                }
                NewLine();
                Indent();
            }
        }

        public void Indent() => IndentLevel++;
        public void Outdent() => IndentLevel--;

        public void Append(string text)
        {
            if (_space)
            {
                _space = false;
                _builder.Append(new string(Options.IndentWithTabs ? '\t' : ' ', IndentLevel * (Options.IndentWithTabs ? 1 : Options.SpaceIndentCount)));
            }
            _builder.Append(text);
        }

        public void NewLine()
        {
            _builder.AppendLine();
            _space = true;
        }

        public string GetVariableName(string baseName, bool isGlobal)
        {
            if (!isGlobal && Workshop.Variables.Any(v => v.IsGlobal && v.Name == baseName))
                baseName = "p_" + baseName;

            return baseName;
        }

        public override string ToString() => _builder.ToString();
    }

    public abstract class DecompileRule
    {
        public WorkshopDecompiler Decompiler { get; }
        public int CurrentAction { get; private set; }
        public bool IsFinished => CurrentAction >= ActionList.Length;
        public ITTEAction Current => ActionList[CurrentAction];
        public abstract ITTEAction[] ActionList { get; }
        public abstract void Decompile();

        protected DecompileRule(WorkshopDecompiler decompiler)
        {
            Decompiler = decompiler;
        }

        protected void DecompileActions()
        {
            while (CurrentAction < ActionList.Length)
                DecompileCurrentAction();
        }

        public void DecompileCurrentAction()
        {
            ActionList[CurrentAction].Decompile(this);
        }

        public void Append(string text) => Decompiler.Append(text);
        public void NewLine() => Decompiler.NewLine();
        public void AddBlock(bool startBlock = true) => Decompiler.AddBlock(startBlock);
        public void Outdent() => Decompiler.Outdent();
        public void Advance()
        {
            CurrentAction++;
        }
        public void EndAction()
        {
            Append(";");
            NewLine();
            Advance();
        }
        public void AddComment(ITTEAction action) => AddComment(action.Comment, action.Disabled);
        public void AddComment(string comment, bool disabled)
        {
            if (comment == null) return;

            var lines = comment.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (disabled) Append("// " + line);
                else Append("# " + line);
                NewLine();
            }
        }
    }

    public class RuleTraveler : DecompileRule
    {
        public TTERule Rule { get; }
        public override ITTEAction[] ActionList => Rule.Actions;

        public RuleTraveler(WorkshopDecompiler decompiler, TTERule rule) : base(decompiler)
        {
            Rule = rule;
        }

        public override void Decompile()
        {
            if (Rule.EventInfo.Event != RuleEvent.Subroutine)
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

                // Decompile conditions
                foreach (var condition in Rule.Conditions)
                    condition.Decompile(this);
            }
            else
            {
                Decompiler.Append("void " + Rule.EventInfo.SubroutineName + "() \"" + Rule.Name + "\"");
            }

            Decompiler.AddBlock();

            DecompileActions();

            Decompiler.Outdent();
            Decompiler.Append("}");
            Decompiler.NewLine();
            Decompiler.NewLine();
        }
    }

    public class ActionTraveler : DecompileRule
    {
        public override ITTEAction[] ActionList { get; }

        public ActionTraveler(WorkshopDecompiler decompiler, ITTEAction[] actions) : base(decompiler)
        {
            ActionList = actions;
        }

        public override void Decompile() => DecompileActions();
    }

    public class ConditionTraveler : DecompileRule
    {
        public override ITTEAction[] ActionList => throw new System.NotImplementedException();
        private readonly TTECondition[] _conditions;

        public ConditionTraveler(WorkshopDecompiler decompiler, TTECondition[] conditions) : base(decompiler)
        {
            _conditions = conditions;
        }

        public override void Decompile()
        {
            // Decompile conditions
            foreach (var condition in _conditions)
                condition.Decompile(this);
        }
    }

    public interface IDecompilerLobbySettingsResolver
    {
        string GetFile();
    }

    public class FileLobbySettingsResolver : IDecompilerLobbySettingsResolver
    {
        private readonly string _sourceFile;
        private readonly Ruleset _settings;

        public FileLobbySettingsResolver(string sourceFile, Ruleset settings)
        {
            _sourceFile = sourceFile;
            _settings = settings;
        }

        public string GetFile()
        {
            // Get the file name.
            string directory = Path.GetDirectoryName(_sourceFile);
            string file = Path.Join(directory, "customGameSettings.lobby");

            // Change file if the name already exists.
            int i = 0;
            while (File.Exists(file))
            {
                file = Path.Join(directory, "customGameSettings_" + i + ".lobby");
                i++;
            }

            // Create the file.
            using (var writer = File.CreateText(file))
                // Write to the settings file.
                writer.Write(JsonConvert.SerializeObject(_settings, new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                }));

            return Path.GetFileName(file);
        }
    }

    public class OmitLobbySettingsResolver : IDecompilerLobbySettingsResolver
    {
        public string GetFile() => null;
    }
}
