using Newtonsoft.Json;

namespace Deltin.Deltinteger.LanguageServer
{
    class PathmapEditorResult
    {
        [JsonProperty("success")]
        public bool Success { get; }

        [JsonProperty("reason")]
        public string Reason { get; }

        public PathmapEditorResult()
        {
            Success = true;
        }

        public PathmapEditorResult(string reason)
        {
            Success = false;
            Reason = reason;
        }
    }
}