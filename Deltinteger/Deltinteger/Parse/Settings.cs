using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Deltin.Deltinteger.LanguageServer.Settings.TomlSettings;

namespace Deltin.Deltinteger.Parse.Settings
{
    public class ProjectSettings
    {
        [JsonPropertyName("entry_point")]
        public string EntryPoint { get; set; } = null;
        [JsonPropertyName("reset_nonpersistent")]
        public bool ResetNonpersistent { get; set; } = false;
        [JsonPropertyName("paste_check_is_extended")]
        public bool PasteCheckIsExtended { get; set; } = false;
        [JsonPropertyName("log_delete_reference_zero")]
        public bool LogDeleteReferenceZero { get; set; } = true;
        [JsonPropertyName("c_style_workshop_output")]
        public bool CStyleWorkshopOutput { get; set; } = false;
        [JsonPropertyName("compile_miscellaneous_comments")]
        public bool CompileMiscellaneousComments { get; set; } = true;

        public static readonly ProjectSettings Default = new ProjectSettings();
    }

    public enum DeleteClassStyle
    {
        [EnumMember(Value = "inline")]
        Inline,
        [EnumMember(Value = "subroutine")]
        Subroutine
    }
}