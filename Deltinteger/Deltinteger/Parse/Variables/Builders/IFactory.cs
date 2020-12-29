namespace Deltin.Deltinteger.Parse.Variables.Build
{
    public interface IVariableFactory
    {
        IVariable GetVariable(ISaveVariableResult saveResult, VariableComponentsCollection components, VarInfo varInfo);
    }

    class VariableFactory : IVariableFactory
    {
        public IVariable GetVariable(ISaveVariableResult saveResult, VariableComponentsCollection components, VarInfo varInfo)
        {
            IVariable result;
            // Macro
            if (components.IsComponent<MacroComponent>())
            {
                var macroVarProvider = new MacroVarProvider(varInfo);
                saveResult?.MacroVarProvider(macroVarProvider);
                result = macroVarProvider;
            }
            // Normal variable
            else
            {
                var var = new Var(varInfo);
                saveResult?.Var(var);
                result = var;
            }
            return result;
        }
    }
}