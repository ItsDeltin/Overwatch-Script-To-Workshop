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
        void CheckConflict(ParseInfo parseInfo, CheckConflict identifier, DocRange range);
    }

    public struct CheckConflict
    {
        public readonly string Name;
        public readonly CodeType[] ParameterTypes;

        public CheckConflict(string name)
        {
            Name = name;
            ParameterTypes = null;
        }

        public CheckConflict(string name, CodeType[] parameterTypes)
        {
            Name = name;
            ParameterTypes = parameterTypes;
        }

        public static string CreateNameConflictMessage(string typeName, string identifier) =>
            "The type '" + typeName + "' already contains a definition for '" + identifier + "'";
        public static string CreateOverloadConflictMessage(string typeName, string identifier) =>
            "The type '" + typeName + "' already contains a definition '" + identifier + "' with the same name and parameter types";
    }

    public interface IScopeHandler : IScopeProvider, IScopeAppender, IConflictChecker {}
}