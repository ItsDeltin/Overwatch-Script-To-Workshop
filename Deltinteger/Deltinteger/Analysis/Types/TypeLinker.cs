using System;
using System.Collections.Generic;

namespace DS.Analysis.Types
{
    using Generics;

    class TypeLinker
    {
        Dictionary<CodeType, CodeType> map = new Dictionary<CodeType, CodeType>();

        /// <summary>Initializes an empty TypeLinker.</summary>
        public TypeLinker()
        {
        }

        /// <summary>Initializes a TypeLinker with type arguments and their respective values.</summary>
        /// <param name="arguments">The type arguments.</param>
        /// <param name="values">The type argument's values. Must be the same length as arguments.Count.</param>
        public TypeLinker(TypeArgCollection arguments, CodeType[] values)
        {
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));

            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (arguments.Count != values.Length)
                throw new ArgumentException(nameof(values), "values must be the same length as arguments");

            for (int i = 0; i < values.Length; i++)
                map.Add(arguments.TypeArgs[i].DataTypeProvider.Instance, values[i]);
        }

        /// <summary>Gets the CodeType that another CodeType is mapped to,</summary>
        /// <param name="value">The CodeType that is converted.</param>
        /// <returns>Returns `value` if the CodeType is not anonymous or isn't mapped. Otherwise returns the CodeType it is mapped to.</returns>
        public CodeType Convert(CodeType value)
        {
            if (map.TryGetValue(value, out CodeType subsitution))
                return subsitution;

            return value;
        }
    }
}