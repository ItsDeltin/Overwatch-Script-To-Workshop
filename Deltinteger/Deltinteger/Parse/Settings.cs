using System.Runtime.Serialization;

namespace Deltin.Deltinteger.Parse.Settings
{
    public class ProjectSettings
    {
        public string EntryPoint { get; set; }
        public CompileSettings Compile { get; set; }
        public bool ResetUnpersistent { get; set; }

        public static readonly ProjectSettings Default = new ProjectSettings()
        {
            EntryPoint = null,
            Compile = default(CompileSettings),
            ResetUnpersistent = false
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