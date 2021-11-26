namespace DS.Analysis.Types.Components
{
    interface ITypeComparison
    {
        bool Is(CodeType other);

        bool Implements(CodeType other);

        bool CanBeAssignedTo(CodeType other);

        int GetTypeHashCode();
    }
}