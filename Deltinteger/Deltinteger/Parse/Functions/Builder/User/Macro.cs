using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Functions.Builder.Virtual;

namespace Deltin.Deltinteger.Parse.Functions.Builder.User
{
    static class MacroBuilder
    {
        /// <summary>Gets the workshop value of a macro.</summary>
        public static IWorkshopTree CallMacroFunction(ActionSet actionSet, DefinedMethodInstance macro, MethodCall methodCall)
        {
            actionSet = actionSet.ContainVariableAssigner().PackThis();

            // The list containing the macro and all of its overriders, recursively.
            var allOptions = new List<MacroOption>();
            allOptions.Add(new MacroOption(macro));

            // Add overriders to the list.
            var relations = new MethodClassRelations(actionSet.ToWorkshop, macro);
            if (relations.Overriders != null)
                allOptions.AddRange(relations.Overriders.Select(overrider => new MacroOption(overrider)));
            
            // Add parameters to the assigner.
            for (int i = 0; i < macro.ParameterVars.Length; i++)
            {
                // Origin parameter variables
                actionSet.IndexAssigner.Add(macro.ParameterVars[i].Provider, methodCall.ParameterValues[i]);

                // Overrider parameter variables.
                foreach (var overrider in relations.Overriders)
                    actionSet.IndexAssigner.Add(overrider.ParameterVars[i].Provider, methodCall.ParameterValues[i]);
            }

            // Create the virtual content builder and then return the resulting value.
            var virtualContentBuilder = new MacroContentBuilder(actionSet, allOptions);
            return virtualContentBuilder.Value;
        }

        // Adapter for the MacroContentBuilder to interface with DefinedMethodInstances.
        class MacroOption : IMacroOption
        {
            public DefinedMethodInstance Macro { get; }
            public MacroOption(DefinedMethodInstance macro) => Macro = macro;
            
            // The type that the macro was defined inside.
            public ClassType ContainingType() => (ClassType)Macro.DefinedInType;

            // Get the workshop value of the macro.
            public IWorkshopTree GetValue(ActionSet actionSet) => Macro.Provider.MacroValue.Parse(actionSet);
        }
    }
}