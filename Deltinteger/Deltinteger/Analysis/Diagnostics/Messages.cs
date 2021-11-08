namespace DS.Analysis.Diagnostics
{
    static class Messages
    {
        public static string TypeNameNotFound(string typeName) => $"No types with the name '{typeName}' exists in the current context";

        public static string GenericCountMismatch(string typeName, int provided, int expected) => $"Generic type '{typeName}' requires {expected} type arguments, got {provided}";

        public static string ElementNonexistentInSource(string elementName, string sourceName) => $"'{elementName}' does not exist in the {sourceName}";
    }
}