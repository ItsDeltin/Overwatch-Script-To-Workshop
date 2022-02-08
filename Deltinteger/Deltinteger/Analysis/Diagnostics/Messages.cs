namespace DS.Analysis.Diagnostics
{
    static class Messages
    {
        public static string TypeNameNotFound(string typeName) => $"No types with the name '{typeName}' exists in the current context";

        public static string GenericCountMismatch(string typeName, int provided, int expected) => $"Generic type '{typeName}' requires {expected} type arguments";

        public static string ElementNonexistentInSource(string elementName, string sourceName) => $"'{elementName}' does not exist in the {sourceName}";

        public static string IdentifierDoesNotExist(string name) => $"'{name}' does not exist in the current context";

        public static string NotAssignable(string from, string to) => $"The type '{from}' cannot be assigned to '{to}'";

        public static string ModuleHasTypeArgs() => $"Modules cannot have type arguments";

        public static string ExpectedVariable() => "Assignment expression must be a variable";

        public static string PickyParameterAlreadySet(string name) => $"The parameter '{name}' was already set";
    }
}