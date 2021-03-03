namespace Deltin.Deltinteger.Parse
{
    public interface IVariable : IElementProvider
    {
        string Name { get; }
        bool RequiresCapture => false;
        VariableType VariableType { get; }
        IVariableInstance GetInstance(InstanceAnonymousTypeLinker genericsLinker);
        IVariableInstance GetDefaultInstance();
    }
}