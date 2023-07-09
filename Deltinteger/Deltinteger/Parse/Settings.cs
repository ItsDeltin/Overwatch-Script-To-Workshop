using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Deltin.Deltinteger.Parse.Settings
{
    public class DsTomlSettings
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
        [JsonPropertyName("out_file")]
        public string OutFile { get; set; } = null;
        [JsonPropertyName("variable_template")]
        public bool VariableTemplate { get; set; } = false;
        [JsonPropertyName("optimize_output")]
        public bool OptimizeOutput { get; set; } = true;
        [JsonPropertyName("use_tabs_in_workshop_output")]
        public bool UseTabsInWorkshopOutput { get; set; } = false;
        [JsonPropertyName("subroutine_stacks_are_extended")]
        public bool SubroutineStacksAreExtended { get; set; } = false;

        public static readonly DsTomlSettings Default = new DsTomlSettings();
    }
}