namespace Deltin.Deltinteger.Parse
{
    public struct LabelInfo
    {
        public bool IncludeReturnType;
        public bool IncludeParameterTypes;
        public bool IncludeParameterNames;
        public bool IncludeDocumentation;

        public static readonly LabelInfo Hover = new LabelInfo() {
            IncludeDocumentation = true,
            IncludeReturnType = true,
            IncludeParameterTypes = true,
            IncludeParameterNames = true
        };

        public static readonly LabelInfo SignatureOverload = new LabelInfo() {
            IncludeDocumentation = false,
            IncludeReturnType = true,
            IncludeParameterTypes = true,
            IncludeParameterNames = true
        };

        public static readonly LabelInfo OverloadError = new LabelInfo() {
            IncludeDocumentation = false,
            IncludeReturnType = false,
            IncludeParameterTypes = false,
            IncludeParameterNames = true
        };

        public static readonly LabelInfo RecursionError = new LabelInfo() {
            IncludeDocumentation = false,
            IncludeReturnType = false,
            IncludeParameterTypes = true,
            IncludeParameterNames = false
        };
    }
}