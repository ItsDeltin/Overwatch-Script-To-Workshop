namespace DS.Analysis.Utility
{
    using Types;
    using Types.Standard;

    /// <summary>Contains helper methods for DeltinScript analysis.</summary>
    static class Helper
    {
        /// <summary>Creates a new CodeType ObserverCollection initialized with StandardTypes.Unknown.Instance.</summary>
        /// <returns>The newly initialized CodeType ObserverCollection.</returns>
        public static ObserverCollection<CodeType> CreateTypeObserver() => new ValueObserverCollection<CodeType>(StandardTypes.Unknown.Instance);
    }
}