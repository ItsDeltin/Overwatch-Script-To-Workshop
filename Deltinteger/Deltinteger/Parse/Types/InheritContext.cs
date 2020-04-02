using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    class InheritContext
    {
        public static void InheritFromContext(IImplementer type, ParseInfo parseInfo, DeltinScriptParser.InheritContext context)
        {
            if (context == null) return;

            // If there is no type name, error.
            if (context.first == null)
                parseInfo.Script.Diagnostics.Error("Expected type name.", DocRange.GetRange(context.TERNARY_ELSE()));
            else
            {
                foreach (var inherit in context.PART())
                {
                    DocRange range = DocRange.GetRange(inherit);

                    // Get the type being inherited.
                    CodeType inheriting = parseInfo.TranslateInfo.Types.GetCodeType(inherit.GetText(), parseInfo.Script.Diagnostics, range);

                    // GetCodeType will return null if the type is not found.
                    if (inheriting == null) continue;

                    inheriting.Call(parseInfo.Script, range);

                    string errorMessage = null;
                    if (!inheriting.CanBeExtended) errorMessage = "Type '" + inheriting.Name + "' cannot be inherited.";
                    else if (inheriting == type) errorMessage = "Cannot extend self.";
                    else if (inheriting.DoesImplement((CodeType)type)) errorMessage = $"The class {inheriting.Name} extends this class.";
                    else if (type.Extends != null) errorMessage = $"{type.Name} is already extending the class {type.Extends}.";
                    else if (type is Interface && inherit is Interface == false) errorMessage = "Interfaces can only implement interfaces.";

                    if (errorMessage != null)
                    {
                        parseInfo.Script.Diagnostics.Error(errorMessage, range);
                        continue;
                    }

                    if (inheriting is Interface contract)
                        type.Contracts.Add(contract);
                    else
                        type.Extends = inheriting;

                    inheriting.ResolveElements();
                }
            }

            ExtendCompletion(parseInfo, context);
        }

        static void ExtendCompletion(ParseInfo parseInfo, DeltinScriptParser.InheritContext context)
        {
            parseInfo.Script.AddCompletionRange(new CompletionRange(
                // Get the completion items of all types.
                parseInfo.TranslateInfo.Types.AllTypes
                    .Where(t => t is ClassType ct && ct.CanBeExtended)
                    .Select(t => t.GetCompletion())
                    .ToArray(),
                // Get the completion range.
                DocRange.GetRange(context.TERNARY_ELSE(), parseInfo.Script.NextToken(context.TERNARY_ELSE())),
                // This completion takes priority.
                CompletionRangeKind.ClearRest
            ));
        }
    }
}