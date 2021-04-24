namespace Deltin.Deltinteger.Parse
{
    public class VariableResolveOptions
    {
        /// <summary>Determines if a variables needs to be an entire workshop variable.</summary>
        public bool FullVariable = false;
        /// <summary>Determines if the variable can be set to a value in an array.</summary>
        public bool CanBeIndexed = true;
        /// <summary>Determines if the variable should be settable.</summary>
        public bool ShouldBeSettable = true;
    }
}