using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public static class CodeTypeHelpers
    {
        public static ICodeTypeInitializer[] TypesFromName(this Scope scope, string name)
        {
            var types = new List<ICodeTypeInitializer>();
            scope.IterateParents(scope =>
            {
                types.AddRange(scope.Types.Where(t => t.Name == name));
                return false;
            });
            return types.ToArray();
        }

        /// <summary>There should be a special CodeType for void rather than it being null.
        /// This is here to make it more clear when we are testing if a type is void.</summary>
        public static bool IsVoid(CodeType type) => type == null;
    }
}