using System;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    interface IResolveElements {
        void ResolveElements();
    }

    public class NullType : CodeType
    {
        public static readonly NullType Instance = new NullType();

        private NullType() : base("?") {}

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }

    public class NumberType : CodeType, IResolveElements
    {
        private readonly ITypeSupplier _supplier;

        public NumberType(ITypeSupplier supplier) : base("Number")
        {
            CanBeExtended = false;
            _supplier = supplier;
        }

        public void ResolveElements()
        {
            Operations = new TypeOperation[] {
                new TypeOperation(TypeOperator.Add, this, this), // Number + number
                new TypeOperation(TypeOperator.Subtract, this, this), // Number - number
                new TypeOperation(TypeOperator.Multiply, this, this), // Number * number
                new TypeOperation(TypeOperator.Divide, this, this), // Number / number
                new TypeOperation(TypeOperator.Modulo, this, this), // Number % number
				new TypeOperation(TypeOperator.Pow, this, this),
                new TypeOperation(TypeOperator.Multiply, _supplier.Vector(), this), // Vector * number
				new TypeOperation(TypeOperator.LessThan, this, _supplier.Boolean()), // Number < number
                new TypeOperation(TypeOperator.LessThanOrEqual, this, _supplier.Boolean()), // Number <= number
                new TypeOperation(TypeOperator.GreaterThanOrEqual, this, _supplier.Boolean()), // Number >= number
                new TypeOperation(TypeOperator.GreaterThan, this, _supplier.Boolean()), // Number > number
            };
        }

        public override bool Implements(CodeType type) => base.Implements(type) || type.Implements(_supplier.Boolean());

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }

    public class BooleanType : CodeType
    {
        private readonly ITypeSupplier _supplier;

        public BooleanType(ITypeSupplier supplier) : base("Boolean")
        {
            CanBeExtended = false;
            _supplier = supplier;

            Operations = new TypeOperation[] {
                new TypeOperation(TypeOperator.And, this, this),
                new TypeOperation(TypeOperator.Or, this, this),
            };
        }

        public override bool Implements(CodeType type) => base.Implements(type) || type.Implements(_supplier.Number());

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }

    public class Positionable : CodeType
    {
        public static readonly Positionable Instance = new Positionable();

        private Positionable() : base("Positionable")
        {
            CanBeExtended = true;
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }
}
