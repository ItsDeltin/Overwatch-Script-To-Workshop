using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{    
    class ValueGroupType : CodeType
    {
        public EnumData EnumData { get; }
        private Scope Scope { get; } = new Scope();
        private List<EnumValuePair> ValuePairs { get; } = new List<EnumValuePair>();
        private bool Constant { get; }

        public ValueGroupType(EnumData enumData, bool constant) : base(enumData.CodeName)
        {
            Constant = constant;
            EnumData = enumData;
            foreach (EnumMember member in enumData.Members)
            {
                EnumValuePair newPair = new EnumValuePair(member, constant, this);
                ValuePairs.Add(newPair);
                Scope.AddNativeVariable(newPair);
            }
        }

        public override bool IsConstant() => Constant;
        public override void WorkshopInit(DeltinScript translateInfo)
        {
            foreach (EnumValuePair pair in ValuePairs)
            {
                if (Constant) translateInfo.DefaultIndexAssigner.Add(pair, pair.Member);
                else translateInfo.DefaultIndexAssigner.Add(pair, EnumData.ToElement(pair.Member));
            }
        }

        public override Scope ReturningScope() => Scope;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Enum
        };
        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            MarkupBuilder hoverContents = new MarkupBuilder()
                .StartCodeLine()
                .Add((Constant ? "constant " : "enum ") + Name)
                .EndCodeLine();
            
            if (Constant)
                hoverContents.NewSection().Add("Constant workshop types cannot be stored. Variables with this type cannot be changed from their initial value.");

            parseInfo.Script.AddHover(callRange, hoverContents.ToString());
            parseInfo.TranslateInfo.Types.CallType(this);
        }

        public static ValueGroupType GetEnumType(EnumData enumData) => (ValueGroupType)CodeType.DefaultTypes.First(t => t is ValueGroupType valueGroupType && valueGroupType.EnumData == enumData);
        public static ValueGroupType GetEnumType<T>() => GetEnumType(EnumData.GetEnum<T>());
    }

    class EnumValuePair : InternalVar
    {
        public EnumMember Member { get; }

        public EnumValuePair(EnumMember member, bool constant, CodeType type) : base(member.CodeName, constant ? CompletionItemKind.Constant : CompletionItemKind.EnumMember)
        {
            Member = member;
            CodeType = type;
        }
    }
}