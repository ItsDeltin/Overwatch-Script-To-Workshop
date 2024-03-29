namespace Deltin.Deltinteger.Parse
{
    public class TypeAttributes
    {
        /// <summary>Determines if this type is a parallel data type.</summary>
        public virtual bool IsStruct { get; set; }
        /// <summary>Determines if this type contains any generics.</summary>
        public virtual bool ContainsGenerics { get; set; }
        /// <summary>The number of variables required to assign to this data type.</summary>
        public virtual int StackLength { get; set; } = 1;
        /// <summary>Will the ExpressionTree update the target player when reading this type?</summary>
        public virtual bool UpdateTarget { get; set; } = true;

        public TypeAttributes() { }

        public TypeAttributes(bool isStruct, bool containsGenerics, bool updateTarget)
        {
            IsStruct = isStruct;
            ContainsGenerics = containsGenerics;
            UpdateTarget = updateTarget;
        }
    }
}