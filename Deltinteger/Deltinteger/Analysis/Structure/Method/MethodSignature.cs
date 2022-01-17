namespace DS.Analysis.Methods
{
    using System;
    using System.Collections.Generic;
    using Types;

    struct MethodSignature
    {
        public string Name { get; }
        public CodeType[] ArgumentTypes { get; }

        public override bool Equals(object obj)
        {
            // Ensure obj is a MethodSignature.
            if (obj is not MethodSignature other)
                return false;

            // Name and ArgumentType's length should be equal.
            if (other.Name != Name || ArgumentTypes.Length != other.ArgumentTypes.Length)
                return false;

            // Argument type equality
            for (int i = 0; i < ArgumentTypes.Length; i++)
                if (!ArgumentTypes[i].Comparison.Is(ArgumentTypes[i]))
                    return false;

            // The elements are equal.
            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Name);

            foreach (var argumentType in ArgumentTypes)
                hashCode.Add(argumentType);

            return hashCode.ToHashCode();
        }
    }
}