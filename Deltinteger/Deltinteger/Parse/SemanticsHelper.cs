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

        public static void ErrorIfConflicts(
            ParseInfo parseInfo,
            CheckConflict identifier,
            string nameConflictMessage,
            string overloadConflictMessage,
            DocRange range,
            params Scope[] scopes)
        {
            if (parseInfo == null) throw new ArgumentNullException(nameof(parseInfo));
            if (identifier.Name == null) throw new ArgumentNullException(nameof(identifier) + "." + nameof(identifier.Name));
            if (nameConflictMessage == null) throw new ArgumentNullException(nameof(nameConflictMessage));
            if (overloadConflictMessage == null) throw new ArgumentNullException(nameof(overloadConflictMessage));
            if (range == null) throw new ArgumentNullException(nameof(range));
            if (scopes == null) throw new ArgumentNullException(nameof(scopes));

            foreach (var scope in scopes)
                switch (Conflicts(scope, identifier, parseInfo.TranslateInfo))
                {
                    // Name conflict
                    case ScopeConflict.NameConflict:
                        parseInfo.Script.Diagnostics.Error(nameConflictMessage, range);
                        return;
                    
                    // Overload conflict
                    case ScopeConflict.OverloadConflict:
                        parseInfo.Script.Diagnostics.Error(overloadConflictMessage, range);
                        return;
                }
        }

        public static ScopeConflict Conflicts(Scope scope, CheckConflict identifier, DeltinScript deltinScript)
        {
            while (scope != null)
            {
                // Variables
                foreach (var variable in scope.Variables)
                    if (variable.Name == identifier.Name)
                        return ScopeConflict.NameConflict;
                // Types
                foreach (var type in scope.Types)
                    if (type.Name == identifier.Name)
                        return ScopeConflict.NameConflict;
                // Methods
                foreach (var method in scope.Methods)
                    if (method.Name == identifier.Name)
                    {
                        // Not a method identifier.
                        if (identifier.ParameterTypes == null)
                            return ScopeConflict.NameConflict;
                        
                        // Make sure parameter lengths match.
                        if (identifier.ParameterTypes.Length == method.Parameters.Length)
                        {
                            bool conflicts = true;

                            for (int i = 0; i < identifier.ParameterTypes.Length; i++)
                                // If the parameter types do not match, there is no conflict.
                                if (!identifier.ParameterTypes[i].Is(method.Parameters[i].GetCodeType(deltinScript)))
                                {
                                    conflicts = false;
                                    break;
                                }
                            
                            if (conflicts)
                                return ScopeConflict.OverloadConflict;
                        }
                    }

                if (scope.CatchConflict) return ScopeConflict.NoConflict;
                scope = scope.Parent;
            }
            return ScopeConflict.NoConflict;
        }
    }

    public enum ScopeConflict
    {
        NoConflict,
        NameConflict,
        OverloadConflict,
    }
}