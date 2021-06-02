namespace Deltin.Deltinteger.Parse
{
    public interface IVariable : IElementProvider, IDeclarationKey
    {
        bool RequiresCapture => false;
        VariableType VariableType { get; }
        IVariableInstance GetInstance(InstanceAnonymousTypeLinker genericsLinker);
        IVariableInstance GetDefaultInstance();
    }
}