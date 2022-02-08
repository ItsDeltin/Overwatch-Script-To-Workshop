using System;
using Deltin.Deltinteger.Compiler;

namespace DS.Analysis.Methods.Overloads
{
    using Expressions;

    class PickyParameter : IDisposable
    {
        /// <summary>Determines if the parameter's name is specified.</summary>
        public bool Picky { get; }

        /// <summary>Determines if this parameter was filled by a default value.</summary>
        public bool Prefilled { get; }

        /// <summary>The name of the picky parameter. This will be null if `Picky` is false.</summary>
        public string Name { get; }

        /// <summary>The range of the picky name. This will be null if `Picky` is false.</summary>
        public DocRange NameRange { get; }

        /// <summary>The parameter's value.</summary>
        public Expression Value { get; }

        /// <summary>The range of the parameter's expression.</summary>
        public DocRange ValueRange { get; }


        public PickyParameter(bool prefilled)
        {
            Prefilled = prefilled;
        }

        public PickyParameter(Token name, Expression value, DocRange valueRange)
        {
            Prefilled = false;
            Picky = name;
            Name = name;
            NameRange = name;
            Value = value ?? throw new ArgumentNullException(nameof(value));
            ValueRange = valueRange ?? throw new ArgumentNullException(nameof(valueRange));
        }

        public void Dispose()
        {
            Value.Dispose();
        }
    }
}