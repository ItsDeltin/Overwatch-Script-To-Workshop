using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger
{
    public class MethodAttributes
    {
        /// <summary>The type the method belongs to.</summary>
        public CodeType ContainingType { get; set; }

        ///<summary>If true, the method can be called asynchronously.</summary>
        public bool Parallelable { get; set; } = false;

        ///<summary>If true, the method can be overriden.</summary>
        public bool Virtual { get; set; } = false;

        ///<summary>If true, the method must be overriden.</summary>
        public bool Abstract { get; set; } = false;

        /// <summary>Determines if the method can be called recursively.</summary>
        public bool Recursive { get; set; }

        public CallInfo CallInfo { get; set; }
    }
}