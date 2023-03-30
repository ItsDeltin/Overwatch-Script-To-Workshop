using System.Runtime.Serialization;

namespace Deltin.Deltinteger.Parse.Settings
{
    public class ProjectSettings
    {
        public string EntryPoint { get; set; }
        public CompileSettings Compile { get; set; }

        public static readonly ProjectSettings Default = default(ProjectSettings);
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