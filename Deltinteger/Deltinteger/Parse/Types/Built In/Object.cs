using System;
using System.Linq.Expressions;
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
                new TypeOperation(TypeOperator.Equal, this, BooleanType.Instance, null, (l,r,a) => new V_Compare(l.GetVariable(), Operators.Equal, r.GetVariable())),
                new TypeOperation(TypeOperator.NotEqual, this, BooleanType.Instance, null, (l,r,a) => new V_Compare(l.GetVariable(), Operators.NotEqual, r.GetVariable()))
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
                new TypeOperation(TypeOperator.Add, this, this, null, TypeOperation.Add), // Number + number
                new TypeOperation(TypeOperator.Subtract, this, this, null, TypeOperation.Subtract), // Number - number
                new TypeOperation(TypeOperator.Multiply, this, this, null, TypeOperation.Multiply), // Number * number
                new TypeOperation(TypeOperator.Divide, this, this, null, TypeOperation.Divide), // Number / number
                new TypeOperation(TypeOperator.Modulo, this, this, null, TypeOperation.Modulo), // Number % number
                new TypeOperation(TypeOperator.Multiply, VectorType.Instance, VectorType.Instance, null, TypeOperation.Multiply), // Number * vector
                new TypeOperation(TypeOperator.LessThan, this, BooleanType.Instance, null, (l,r,a) => new V_Compare(l.GetVariable(), Operators.LessThan, r.GetVariable())), // Number < number
                new TypeOperation(TypeOperator.LessThanOrEqual, this, BooleanType.Instance, null, (l,r,a) => new V_Compare(l.GetVariable(), Operators.LessThanOrEqual, r.GetVariable())), // Number <= number
                new TypeOperation(TypeOperator.GreaterThanOrEqual, this, BooleanType.Instance, null, (l,r,a) => new V_Compare(l.GetVariable(), Operators.GreaterThanOrEqual, r.GetVariable())), // Number >= number
                new TypeOperation(TypeOperator.GreaterThan, this, BooleanType.Instance, null, (l,r,a) => new V_Compare(l.GetVariable(), Operators.GreaterThan, r.GetVariable())), // Number > number
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
                new TypeOperation(TypeOperator.And, this, this, null, (l,r,a) => Element.Part<V_And>(l.GetVariable(), r.GetVariable())),
                new TypeOperation(TypeOperator.Or, this, this, null, (l,r,a) => Element.Part<V_Or>(l.GetVariable(), r.GetVariable())),
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