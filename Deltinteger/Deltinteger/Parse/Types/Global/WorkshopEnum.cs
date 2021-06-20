using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Parse.Workshop;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    class ValueGroupType : CodeType
    {
        public ElementEnum EnumData { get; }
        private readonly Scope _staticScope; 
        protected readonly Scope _objectScope;
        private List<EnumValuePair> _valuePairs = new List<EnumValuePair>();
        private bool _constant;

        public ValueGroupType(ElementEnum enumData, ITypeSupplier types, bool constant) : base(enumData.Name)
        {
            _staticScope = new Scope("enum " + Name);
            _objectScope = new Scope("enum " + Name);
            _constant = constant;
            EnumData = enumData;
            TokenType = SemanticTokenType.Enum;

            if (constant)
                TokenModifiers.Add(TokenModifier.Readonly);

            foreach (ElementEnumMember member in enumData.Members)
            {
                EnumValuePair newPair = new EnumValuePair(member, constant, this);
                _valuePairs.Add(newPair);
                _staticScope.AddNativeVariable(newPair);
            }

            Operations.DefaultAssignment = !constant;
        }

        public override bool IsConstant() => _constant;

        public override Scope GetObjectScope() => _objectScope;
        public override Scope ReturningScope() => _staticScope;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Enum
        };
        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            MarkupBuilder hoverContents = new MarkupBuilder()
                .StartCodeLine()
                .Add((_constant ? "constant " : "enum ") + Name)
                .EndCodeLine();
            
            if (_constant)
                hoverContents.NewSection().Add("Constant workshop types cannot be stored. Variables with this type cannot be changed from their initial value.");

            parseInfo.Script.AddHover(callRange, hoverContents.ToString());
            parseInfo.Script.AddToken(callRange, TokenType, TokenModifiers.ToArray());
        }

        public static ValueGroupType[] GetEnumTypes(ITypeSupplier supplier)
        {
            var enums = ElementRoot.Instance.Enumerators;
            var types = new List<ValueGroupType>();

            foreach (var enumerator in enums)
                if (!enumerator.Hidden)
                {
                    if (enumerator.Name == "Team")
                        types.Add(new TeamGroupType(supplier, enumerator));
                    else
                        types.Add(new ValueGroupType(enumerator, supplier, !enumerator.ConvertableToElement()));
                }

            return types.ToArray();
        }
    }

    class TeamGroupType : ValueGroupType
    {
        private readonly InternalVar Opposite;
        private readonly InternalVar Score;
        private readonly InternalVar OnDefense;
        private readonly InternalVar OnOffense;

        public TeamGroupType(ITypeSupplier typeSupplier, ElementEnum enumData) : base(enumData, typeSupplier, false)
        {
            Opposite = new InternalVar("Opposite", this, CompletionItemKind.Property) {
                Documentation = new MarkupBuilder()
                    .Add("The opposite team of the value. If the team value is ").Code("Team 1").Add(", Opposite will be ").Code("Team 2").Add(" and vice versa. If the value is ")
                    .Code("All").Add(", Opposite will still be ").Code("All").Add(".")
            };
            Score = new InternalVar("Score", typeSupplier.Number(), CompletionItemKind.Property) {
                Documentation = new MarkupBuilder()
                    .Add("The current score for the team. Results in 0 in free-for-all modes.")
            };
            OnDefense = new InternalVar("OnDefense", typeSupplier.Boolean(), CompletionItemKind.Property) {
                Documentation = new MarkupBuilder()
                    .Add("Whether the specified team is currently on defense. Results in False if the game mode is not Assault, Escort, or Assault/Escort.")
            };
            OnOffense = new InternalVar("OnOffense", typeSupplier.Boolean(), CompletionItemKind.Property) {
                Documentation = new MarkupBuilder()
                    .Add("Whether the specified team is currently on offense. Results in False if the game mode is not Assault, Escort, or Assault/Escort.")
            };

            _objectScope.AddNativeVariable(Opposite);
            _objectScope.AddNativeVariable(Score);
            _objectScope.AddNativeVariable(OnDefense);
            _objectScope.AddNativeVariable(OnOffense);
        }

        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, IWorkshopTree reference, VarIndexAssigner assigner)
        {
            assigner.Add(Opposite, Element.Part("Opposite Team Of", reference));
            assigner.Add(Score, Element.Part("Team Score", reference));
            assigner.Add(OnDefense, Element.Part("Is Team On Defense", reference));
            assigner.Add(OnOffense, Element.Part("Is Team On Offense", reference));
        }
    }

    class EnumValuePair : InternalVar
    {
        public ElementEnumMember Member { get; }
        readonly ValueGroupType _type;

        public EnumValuePair(ElementEnumMember member, bool constant, ValueGroupType type) : base(member.CodeName(), type, constant ? CompletionItemKind.Constant : CompletionItemKind.EnumMember)
        {
            Member = member;
            _type = type;
            Attributes = new VariableInstanceAttributes() {
                CanBeSet = false,
                UseDefaultVariableAssigner = false,
                CanBeIndexed = false,
                StoreType = StoreType.None
            };
            // todo: token type
            // TokenType = Deltin.Deltinteger.Parse.SemanticTokenType.EnumMember;
        }

        public override IWorkshopTree ToWorkshop(ActionSet actionSet) =>_type.IsConstant() ? Member : Member.ToElement();
    }
}