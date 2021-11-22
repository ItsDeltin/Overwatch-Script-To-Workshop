namespace DS.Analysis.Types.Components
{
    interface IAssignableTo
    {
        bool CanBeAssignedTo(CodeType other);
    }
}