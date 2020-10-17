using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public static class CodeTypeHelpers
    {
        public static bool CanExtend(CodeType type, CodeType extend, FileDiagnostics diagnostics, DocRange range)
        {
            if (extend == null) throw new ArgumentNullException(nameof(extend));

            string errorMessage = null;

            // Can actually be extended.
            if (!extend.CanBeExtended)
                errorMessage = "Type '" + extend.Name + "' cannot be inherited.";
            // TODO: Replace with extend.Is
            else if (extend == type)
                errorMessage = "Cannot extend self.";
            // Circular
            else if (extend.Implements(type))
                errorMessage = $"The class {extend.Name} extends this class.";

            // Add diagnostic if there is an error.
            if (errorMessage != null)
            {
                diagnostics.Error(errorMessage, range);
                return false;
            }
            return true;
        }

        public static ICodeTypeInitializer[] TypesFromName(this Scope scope, string name)
        {
            var types = new List<ICodeTypeInitializer>();
            scope.IterateParents(scope => {
                types.AddRange(scope.Types.Where(t => t.Name == name));
                return false;
            });
            return types.ToArray();
        }
        
        public static ICodeTypeInitializer GetInitializer(this Scope scope, string name) => scope.Types.FirstOrDefault(type => type.Name == name);

        public static ICodeTypeInitializer GetInitializer(this Scope scope, string name, FileDiagnostics diagnostics, DocRange range)
        {
            var initializer = scope.Types.FirstOrDefault(type => type.Name == name);
            if (initializer == null)
                diagnostics.Error("No type by the name of '" + name + "' exists in the current context", range);
            return initializer;
        }
    }
}