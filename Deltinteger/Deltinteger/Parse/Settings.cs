#nullable enable

using System.Text.Json.Serialization;
using CompletionList = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionList;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using System.Reflection;
using Deltin.Deltinteger.Model;

namespace Deltin.Deltinteger.Parse.Settings
{
    public class DsTomlSettings
    {
        [JsonPropertyName("entry_point")]
        public string? EntryPoint { get; set; } = null;

        [JsonPropertyName("reset_nonpersistent")]
        public bool ResetNonpersistent { get; set; } = false;

        [JsonPropertyName("paste_check_is_extended")]
        public bool PasteCheckIsExtended { get; set; } = false;

        [JsonPropertyName("log_delete_reference_zero")]
        public bool LogDeleteReferenceZero { get; set; } = true;

        [JsonPropertyName("track_class_generations")]
        public bool TrackClassGenerations { get; set; } = false;

        [JsonPropertyName("global_reference_validation")]
        public bool GlobalReferenceValidation { get; set; } = false;

        [JsonPropertyName("reference_validation_type")]
        public ReferenceValidationType ReferenceValidationType { get; set; } = ReferenceValidationType.Subroutine;

        [JsonPropertyName("new_class_register_optimization")]
        public bool NewClassRegisterOptimization { get; set; } = true;

        [JsonPropertyName("abort_on_error")]
        public bool AbortOnError { get; set; } = true;

        [JsonPropertyName("c_style_workshop_output")]
        public bool CStyleWorkshopOutput { get; set; } = false;

        [JsonPropertyName("compile_miscellaneous_comments")]
        public bool CompileMiscellaneousComments { get; set; } = true;

        [JsonPropertyName("out_file")]
        public string? OutFile { get; set; } = null;

        [JsonPropertyName("variable_template")]
        public bool VariableTemplate { get; set; } = false;

        [JsonPropertyName("optimize_output")]
        public bool OptimizeOutput { get; set; } = true;

        [JsonPropertyName("use_tabs_in_workshop_output")]
        public bool UseTabsInWorkshopOutput { get; set; } = false;

        [JsonPropertyName("subroutine_stacks_are_extended")]
        public bool SubroutineStacksAreExtended { get; set; } = false;

        public static readonly DsTomlSettings Default = new DsTomlSettings();

        public static readonly CompletionList SettingsCompletionList = GetCompletionList();

        static CompletionList GetCompletionList()
        {
            var properties = typeof(DsTomlSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance).SelectWithoutNull(prop =>
            {
                // Find property name.
                var jsonPropertyName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
                if (jsonPropertyName is null)
                    return null;

                // Determine default value.
                var defaultValue = prop.GetValue(Default) switch
                {
                    true => "true",
                    false => "false",
                    null => "null",
                    ReferenceValidationType.Inline => "inline",
                    ReferenceValidationType.Subroutine => "subroutine",
                    _ => string.Empty,
                };
                string insertText = $"{jsonPropertyName} = {defaultValue}";

                return new CompletionItem()
                {
                    Label = jsonPropertyName,
                    Detail = new MarkupBuilder().StartCodeLine().Add(insertText).EndCodeLine(),
                    Documentation = GetDocumentation(jsonPropertyName),
                    Kind = CompletionItemKind.Field,
                    InsertText = insertText
                };
            });
            return new(properties);
        }

        static MarkupBuilder? GetDocumentation(string setting) => setting switch
        {
            "entry_point" => "The main OSTW file to begin analysis and compilation.",
            "reset_nonpersistent" => new MarkupBuilder("If true, OSTW will not assume that all variables are zero by default.")
                .NewLine().Add("This can be used to save & load games by copying the current global variable set as actions.")
                .NewLine().Add("Any variable with the ").Code("persist").Add(" attribute will not be reset. Add the ").Code("persist").Add(" attribute to any variable you want part of your save state."),
            "paste_check_is_extended" => new MarkupBuilder("Determines if the extra variable generated from reset_nonpersistent is placed in the extended collection."),
            "log_delete_reference_zero" => new MarkupBuilder("If true, logs to inspector when an invalid class reference is used with the ").Code("delete").Add(" statement."),
            "track_class_generations" => new MarkupBuilder("Improves class references so that every type of invalid pointer can be detected at runtime. This will increase element count a little."),
            "global_reference_validation" => new MarkupBuilder("Checks class pointers when they are accessed. If the reference is invalid, the file and line of the OSTW code is logged to the inspector and the rule is aborted.")
                .NewLine().Add("By default this will only detect null or 0 references, but with ").Code("track_class_generations").Add(" enabled it will detect any type of invalid pointer.")
                .NewLine().Add("Increases element count by a lot."),
            "reference_validation_type" => "Determines if the code generated by global_reference_validation is placed inline at every check or in a shared subroutine. Subroutine variant reduces element count from validation by 30%.",
            "new_class_register_optimization" => new MarkupBuilder("Code like ").Code("a = new MyClass()").Add(" will be optimized so that the 'a' value which is already being overridden is used to assign to the heap, rather than creating a new register."),
            "abort_on_error" => new MarkupBuilder("If an invalid class reference is found when ").Code("global_reference_validation").Add(" is enabled, this determines if the rule should be aborted."),
            "c_style_workshop_output" => new MarkupBuilder("Controls the workshop output format.")
                .NewLine().Add("Classic style (false):").NewLine().StartCodeLine().Add("Set Global Variable(A, Value In Array(Global Variable(B), 2));").EndCodeLine()
                .NewLine().Add("C style (true):").NewLine().StartCodeLine().Add("Global.A = Global.B[2];").EndCodeLine(),
            "compile_miscellaneous_comments" => "Toggles the extra comments generated by the OSTW compiler in the workshop output, like rule action count and extended collection names.",
            "out_file" => "The file to write the generated workshop code to.",
            "optimize_output" => "Controls if OSTW will optimize the generated workshop code.",
            "use_tabs_in_workshop_output" => "Uses tabs instead of spaces in the generated workshop code.",
            "subroutine_stacks_are_extended" => "Determines if the register used to store the object reference in class subroutine methods will be extended.",
            "variable_template" or _ => null,
        };
    }

    public enum ReferenceValidationType
    {
        [JsonPropertyName("inline")]
        Inline,
        [JsonPropertyName("subroutine")]
        Subroutine
    }
}