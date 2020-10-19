using System;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public interface IInitOperations
    {
        void InitOperations();
    }

    public class ObjectType : CodeType, IInitOperations
    {
        public static readonly ObjectType Instance = new ObjectType();

        private ObjectType() : base("Object")
        {
            CanBeExtended = true;
        }

        public void InitOperations()
        {
            Operations = new TypeOperation[] {
                new TypeOperation(TypeOperator.Equal, this, BooleanType.Instance, (l, r) => Element.Compare(l, Operator.Equal, r)),
                new TypeOperation(TypeOperator.NotEqual, this, BooleanType.Instance, (l, r) => Element.Compare(l, Operator.NotEqual, r))
            };
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }

    public class NullType : CodeType
    {
        public static readonly NullType Instance = new NullType();

        private NullType() : base("?") {}

        public override bool Implements(CodeType type) => type.Implements(ObjectType.Instance);
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }

    public class NumberType : CodeType, IInitOperations
    {
        public static readonly NumberType Instance = new NumberType();

        private NumberType() : base("Number")
        {
            CanBeExtended = false;
            Inherit(ObjectType.Instance, null, null);
        }

        public void InitOperations()
        {
            Operations = new TypeOperation[] {
                new TypeOperation(TypeOperator.Add, this, this), // Number + number
                new TypeOperation(TypeOperator.Subtract, this, this), // Number - number
                new TypeOperation(TypeOperator.Multiply, this, this), // Number * number
                new TypeOperation(TypeOperator.Divide, this, this), // Number / number
                new TypeOperation(TypeOperator.Modulo, this, this), // Number % number
				new TypeOperation(TypeOperator.Pow, this, this),
                new TypeOperation(TypeOperator.Multiply, this, VectorType.Instance), // Number * vector
                new TypeOperation(TypeOperator.Multiply, VectorType.Instance, this), // Vector * number
				new TypeOperation(TypeOperator.LessThan, this, BooleanType.Instance), // Number < number
                new TypeOperation(TypeOperator.LessThanOrEqual, this, BooleanType.Instance), // Number <= number
                new TypeOperation(TypeOperator.GreaterThanOrEqual, this, BooleanType.Instance), // Number >= number
                new TypeOperation(TypeOperator.GreaterThan, this, BooleanType.Instance), // Number > number
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
            Inherit(ObjectType.Instance, null, null);
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
            Inherit(ObjectType.Instance, null, null);

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
            Inherit(ObjectType.Instance, null, null);
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }
}
