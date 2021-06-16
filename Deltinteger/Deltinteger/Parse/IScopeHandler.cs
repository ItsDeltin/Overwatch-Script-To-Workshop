using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    public interface IScopeProvider
    {
        Scope GetObjectBasedScope();
        Scope GetStaticBasedScope();
        IMethod GetOverridenFunction(DeltinScript deltinScript, FunctionOverrideInfo provider);
        IVariableInstance GetOverridenVariable(string variableName);
        Scope GetScope(bool isStatic) => isStatic ? GetStaticBasedScope() : GetObjectBasedScope();
    }

    public interface IScopeAppender
    {
        void AddObjectBasedScope(IMethod function);
        void AddStaticBasedScope(IMethod function);
        void AddObjectBasedScope(IVariableInstance variable);
        void AddStaticBasedScope(IVariableInstance variable);

        void Add(IMethod function, bool isStatic)
        {
            if (isStatic) AddStaticBasedScope(function);
            else AddObjectBasedScope(function);
        }
        void Add(IVariableInstance variable, bool isStatic)
        {
            if (isStatic) AddStaticBasedScope(variable);
            else AddObjectBasedScope(variable);
        }

        CodeType DefinedIn() => this as CodeType;
    }

    public interface IConflictChecker
    {
        void CheckConflict(string elementName, FileDiagnostics diagnostics, DocRange range);
    }

    public interface IScopeHandler : IScopeProvider, IScopeAppender, IConflictChecker {}
}