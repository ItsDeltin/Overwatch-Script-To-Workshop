using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Deltin.Deltinteger.Parse.Settings
{
    public struct ProjectSettings
    {
        [JsonProperty("compiler")]
        public CompileSettings Compiler;

        public static readonly ProjectSettings Default = default(ProjectSettings);
    }

    public struct CompileSettings
    {
        [JsonProperty("classVariableNames")]
        public string[] ClassVariableNames;

        [JsonProperty("deleteClassOutputStyle")]
        [JsonConverter(typeof(StringEnumConverter))]
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