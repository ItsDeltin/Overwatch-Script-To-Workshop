namespace DS.Analysis.Core
{
    interface IDependent
    {
        void MarkAsStale(string source);
    }
}