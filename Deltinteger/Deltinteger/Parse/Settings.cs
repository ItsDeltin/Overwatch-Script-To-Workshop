using System.Runtime.Serialization;
using Deltin.Deltinteger.LanguageServer.Settings.TomlSettings;

namespace Deltin.Deltinteger.Parse.Settings
{
    public class ProjectSettings
    {
        public string EntryPoint { get; set; } = null;
        public CompileSettings Compile { get; set; } = default(CompileSettings);
        public bool ResetNonpersistent { get; set; } = false;
        public bool PasteCheckIsExtended { get; set; } = false;
        public bool LogDeleteReferenceZero { get; set; } = true;

        public static readonly ProjectSettings Default = new ProjectSettings();
    }

    public struct CompileSettings
    {
        public string[] ClassVariableNames;
        public DeleteClassStyle DeleteClassOutputStyle;
    }

    public enum DeleteClassStyle
    {
        [EnumMember(Value = "inline")]
        Inline,
        [EnumMember(Value = "subroutine")]
        Subroutine
    }
}