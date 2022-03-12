namespace DS.Analysis.Types
{
    using Generics;

    interface ICodeTypeProvider : ITypePartHandler
    {
        string Name { get; }
        TypeArgCollection Generics { get; }
        IGetIdentifier GetIdentifier { get; }

        IDisposableTypeDirector CreateInstance(ProviderArguments arguments);
    }
}