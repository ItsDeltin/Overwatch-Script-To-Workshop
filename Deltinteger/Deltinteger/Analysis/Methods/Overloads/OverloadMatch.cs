namespace DS.Analysis.Methods.Overloads
{
    class OverloadMatch
    {
        /// <summary>The overload this OverloadMatch is for.</summary>
        public Overload Overload { get; }

        /// <summary>The input parameters ordered for the overload.</summary>
        public PickyParameter[] OrderedParameters { get; }
    }
}