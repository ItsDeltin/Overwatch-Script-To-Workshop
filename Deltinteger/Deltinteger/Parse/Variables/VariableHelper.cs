#nullable enable

namespace Deltin.Deltinteger.Parse.Variables;

/// <summary>
/// Helper class for compiling OSTW variables into the workshop.
/// </summary>
class VariableHelper
{
    /// <summary>
    /// Finds the <c>IGettable</c> assigned to variable <c>variableInstance</c> in the current context (<c>actionSet</c>).
    /// <para>This is the equivalent of:
    /// <code>
    /// actionSet.IndexAssigner.TryGet(variableInstance.Provider, out var result)
    /// </code></para>
    /// </summary>
    /// <param name="actionSet">The compiling context.</param>
    /// <param name="variableInstance">The variable to find the gettable from.</param>
    /// <returns>The gettable if it exists, otherwise <c>null</c>.</returns>
    public static IGettable? GetVariableIndexFromContext(ActionSet actionSet, IVariableInstance variableInstance)
    {
        actionSet.IndexAssigner.TryGet(variableInstance.Provider, out var result);
        return result;
    }

    /// <summary>
    /// Finds the given variable's initial value. The expression will be evaluated so it may generate actions.
    /// 
    /// <para>This is the equivalent of:
    /// <code>
    /// variableInstance.GetAssigner().GetInitialValue(new(actionSet), null).Value
    /// </code></para>
    /// </summary>
    /// <param name="actionSet">The compiling context.</param>
    /// <param name="variableInstance">The variable to find the initial value from.</param>
    /// <returns>The initial value if it exists, otherwise <c>null</c>.</returns>
    public static IWorkshopTree? GetInitialWorkshopValue(ActionSet actionSet, IVariableInstance variableInstance)
    {
        return variableInstance.GetAssigner().GetInitialValue(new(actionSet), null).Value;
    }

    /// <summary>
    /// Initializes a variable to it's default value. No actions will be generated if the variable is not linked
    /// to an index in the current compiling context, such as variables with only a getter ("macro variables").
    /// 
    /// No actions will be generated if the variable is not linked to an initial value, so it won't fall back to
    /// zero either. Consider other options if the variable needs a reset.
    /// </summary>
    /// <param name="actionSet">The compiling context.</param>
    /// <param name="variableInstance">The variable that will have it's value initialized.</param>
    public static void SetToInitialValue(ActionSet actionSet, IVariableInstance variableInstance)
    {
        var initialValue = GetInitialWorkshopValue(actionSet, variableInstance);
        if (initialValue is not null)
            GetVariableIndexFromContext(actionSet, variableInstance)
            ?.Set(actionSet, initialValue);
    }
}