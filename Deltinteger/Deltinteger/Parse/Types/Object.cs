using System;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class ObjectType : CodeType
    {
        public static readonly ObjectType Instance = new ObjectType();

        private ObjectType() : base("Object")
        {
            CanBeExtended = true;
        }

        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => null;
    }

    public class NullType : CodeType
    {
        public static readonly NullType Instance = new NullType();

        private NullType() : base("?") {}

        public override bool Implements(CodeType type) => type.Implements(ObjectType.Instance);
        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => null;
    }

    public class NumberType : CodeType
    {
        public static readonly NumberType Instance = new NumberType();

        private NumberType() : base("Number")
        {
            CanBeExtended = false;
            Inherit(ObjectType.Instance, null, null);
        }

        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => null;
    }

    public class PlayerType : CodeType
    {
        public static readonly PlayerType Instance = new PlayerType();

        private InternalVar Team { get; } = new InternalVar("Team", CompletionItemKind.Property) {
            CodeType = TeamType.Instance,
            VariableType = VariableType.ElementReference
        };

        private PlayerType() : base("Player")
        {
            CanBeExtended = false;
            Inherit(ObjectType.Instance, null, null);
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(Team, Element.Part<V_TeamOf>(reference));
        }

        public override CompletionItem GetCompletion() => throw new NotImplementedException();
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

        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => null;
    }

    public class BooleanType : CodeType
    {
        public static readonly BooleanType Instance = new BooleanType();

        private BooleanType() : base("Boolean")
        {
            CanBeExtended = false;
            Inherit(ObjectType.Instance, null, null);
        }

        public override CompletionItem GetCompletion() => throw new NotImplementedException();
        public override Scope ReturningScope() => null;
    }
}