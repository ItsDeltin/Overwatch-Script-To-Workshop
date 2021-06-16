using System;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    static class SemanticsHelper
    {
        public static bool ExpectValueType(ParseInfo parseInfo, IExpression expression, CodeType expectType, DocRange range)
        {
            if (!expression.Type().Implements(expectType))
            {
                parseInfo.Script.Diagnostics.Error("Expected a value of type '" + expectType.GetName() + "'", range);
                return false;
            }
            return true;
        }

        public static void CouldNotOverride(ParseInfo parseInfo, DocRange range, string elementTypeName) =>
            parseInfo.Script.Diagnostics.Error("No overridable " + elementTypeName + " found in parent classes", range);
        

        public static bool AccessLevelMatches(AccessLevel targetAccessLevel, CodeType targetContainer, CodeType thisContainer) =>
            targetContainer == null || AccessLevelMatches(targetAccessLevel, targetContainer.LowestAccessLevel(thisContainer));
        
        public static bool AccessLevelMatches(AccessLevel target, AccessLevel lowest) => target switch {
            AccessLevel.Public => true, // Target is public
            AccessLevel.Protected => lowest == AccessLevel.Private || lowest == AccessLevel.Protected,
            AccessLevel.Private => lowest == AccessLevel.Private,
            _ => throw new NotImplementedException(target.ToString())
        };

        public static void ErrorIfConflicts(string name, string errorMessage, FileDiagnostics diagnostics, DocRange range, params Scope[] scopes)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (errorMessage == null) throw new ArgumentNullException(nameof(errorMessage));
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            if (range == null) throw new ArgumentNullException(nameof(range));
            if (scopes == null) throw new ArgumentNullException(nameof(scopes));

            foreach (var scope in scopes)
                if (scope.Conflicts(name))
                {
                    diagnostics.Error(errorMessage, range);
                    return;
                }
        }
    }
}