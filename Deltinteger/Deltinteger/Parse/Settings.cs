using System.Runtime.Serialization;
using Deltin.Deltinteger.LanguageServer.Settings.TomlSettings;

namespace Deltin.Deltinteger.Parse.Settings
{
    public class ProjectSettings
    {
        public string EntryPoint { get; set; }
        public CompileSettings Compile { get; set; }
        public bool ResetNonpersistent { get; set; }

        public static readonly ProjectSettings Default = new ProjectSettings()
        {
            EntryPoint = null,
            Compile = default(CompileSettings),
            ResetNonpersistent = false
        };
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