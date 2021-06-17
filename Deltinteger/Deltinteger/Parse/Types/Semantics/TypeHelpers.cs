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
            scope.IterateParents(scope => {
                types.AddRange(scope.Types.Where(t => t.Name == name));
                return false;
            });
            return types.ToArray();
        }
    }
}