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

            string errorMessage;

            // Can actually be extended.
            if (!extend.CanBeExtended)
                errorMessage = "Type '" + extend.Name + "' cannot be inherited.";

            // Self
            else if (extend.Is(type))
                errorMessage = "Cannot extend self.";

            // Circular
            else if (extend.Implements(type))
                errorMessage = $"The class {extend.Name} extends this class.";
            
            // Is class
            else if (extend is ClassType == false)
                errorMessage = $"Must override a class";
            
            // No errors
            else return true;

            // Add diagnostic then return false.
            diagnostics.Error(errorMessage, range);
            return false;
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
    }
}