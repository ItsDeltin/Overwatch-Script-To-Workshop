namespace Deltin.Deltinteger.Parse
{
    public interface IVariable : IElementProvider, IDeclarationKey
    {
        bool RequiresCapture => false;
        VariableType VariableType { get; }
        IVariableInstance GetInstance(CodeType definedIn, InstanceAnonymousTypeLinker genericsLinker);
        IVariableInstance GetDefaultInstance(CodeType definedIn);
    }
}