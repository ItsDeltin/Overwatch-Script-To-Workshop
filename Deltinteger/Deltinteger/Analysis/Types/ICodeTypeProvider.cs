namespace DS.Analysis.Types
{
    using Generics;
    using Scopes;

    // TODO: Make ITypeNodeManager into a field instead
    interface ICodeTypeProvider : ITypeNodeManager
    {
        string Name { get; }
        TypeArgCollection Generics { get; }
        IGetIdentifier GetIdentifier { get; }

        IDisposableTypeDirector CreateInstance(ProviderArguments arguments);

        ScopedElement CreateScopedElement();
    }
}