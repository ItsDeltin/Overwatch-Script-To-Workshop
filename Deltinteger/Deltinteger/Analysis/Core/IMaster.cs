namespace DS.Analysis.Core
{
    interface IMaster
    {
        void AddStaleObject(IUpdatable updatable);

        void RemoveObject(IUpdatable updatable);
    }
}