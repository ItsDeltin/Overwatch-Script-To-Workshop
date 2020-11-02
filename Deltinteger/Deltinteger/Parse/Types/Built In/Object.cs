using System;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class NullType : CodeType
    {
        public static readonly NullType Instance = new NullType();

        private NullType() : base("?") {}

        // public override bool Implements(CodeType type) => type.Implements(ObjectType.Instance);
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
            // Inherit(ObjectType.Instance, null, null);
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

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }

    public class TeamType : CodeType
    {
        public static readonly TeamType Instance = new TeamType();

        private TeamType() : base("Team")
        {
            CanBeExtended = false;
            // Inherit(ObjectType.Instance, null, null);
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }

    public class BooleanType : CodeType
    {
        public static readonly BooleanType Instance = new BooleanType();

        private BooleanType() : base("Boolean")
        {
            CanBeExtended = false;
            // Inherit(ObjectType.Instance, null, null);

            Operations = new TypeOperation[] {
                new TypeOperation(TypeOperator.And, this, this),
                new TypeOperation(TypeOperator.Or, this, this),
            };
        }

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
            // Inherit(ObjectType.Instance, null, null);
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }
}
