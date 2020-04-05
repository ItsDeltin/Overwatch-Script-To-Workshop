using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{    
    public class WorkshopEnumType : CodeType
    {
        private Scope EnumScope { get; } = new Scope();
        public EnumData EnumData { get; }

        public WorkshopEnumType(EnumData enumData) : base(enumData.CodeName)
        {
            EnumData = enumData;
            foreach (var member in enumData.Members)
            if (!member.IsHidden)
            {
                var scopedMember = new ScopedEnumMember(this, member);
                EnumScope.AddVariable(scopedMember, null, null);
            }
            EnumScope.ErrorName = "enum " + Name;
        }

        public override Scope ReturningScope() => EnumScope;
        public override bool IsConstant() => true;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = EnumData.CodeName,
            Kind = CompletionItemKind.Enum
        };
        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            MarkupBuilder hoverContents = new MarkupBuilder()
                .StartCodeLine()
                .Add("constant " + Name)
                .EndCodeLine()
                .NewSection()
                .Add("Constant workshop types cannot be stored. Variables with this type cannot be changed from their initial value.");

            parseInfo.Script.AddHover(callRange, hoverContents.ToString());
        }
    
        public static CodeType GetEnumType(EnumData enumData)
        {
            if (enumData.ConvertableToElement()) return CodeType.DefaultTypes.First(t => t is ValueGroupType valueGroupType && valueGroupType.EnumData == enumData);
            return (WorkshopEnumType)CodeType.DefaultTypes.First(t => t is WorkshopEnumType workshopEnum && workshopEnum.EnumData == enumData);
        }
        public static WorkshopEnumType GetEnumType<T>()
        {
            var enumData = EnumData.GetEnum<T>();
            return (WorkshopEnumType)CodeType.DefaultTypes.First(t => t is WorkshopEnumType && ((WorkshopEnumType)t).EnumData == enumData);
        }
    }

    public class ScopedEnumMember : IScopeable, IExpression
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public LanguageServer.Location DefinedAt { get; } = null;
        public bool WholeContext { get; } = true;
        public bool Static => true;
        
        public CodeType Enum { get; }
        public EnumMember EnumMember { get; }

        private Scope debugScope { get; } = new Scope();
        
        public ScopedEnumMember(CodeType parent, EnumMember enumMember)
        {
            Enum = parent;
            Name = enumMember.CodeName;
            EnumMember = enumMember;
            debugScope.ErrorName = "enum value " + Name;
        }

        public Scope ReturningScope()
        {
            return debugScope;
        }

        public CodeType Type() => Enum;

        public IWorkshopTree Parse(ActionSet actionSet) => EnumMember;

        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Constant
        };
    }

    class ValueGroupType : CodeType
    {
        public EnumData EnumData { get; }
        private Scope Scope { get; } = new Scope();
        private List<EnumValuePair> ValuePairs { get; } = new List<EnumValuePair>();

        public ValueGroupType(EnumData enumData) : base(enumData.CodeName)
        {
            EnumData = enumData;
            foreach (EnumMember member in enumData.Members)
            {
                EnumValuePair newPair = new EnumValuePair(member);
                ValuePairs.Add(newPair);
                Scope.AddNativeVariable(newPair);
            }
        }

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            foreach (EnumValuePair pair in ValuePairs)
                translateInfo.DefaultIndexAssigner.Add(pair, EnumData.ToElement(pair.Member));
        }

        public override Scope ReturningScope() => Scope;

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Enum
        };
    }

    class EnumValuePair : InternalVar
    {
        public EnumMember Member { get; }

        public EnumValuePair(EnumMember member) : base(member.CodeName, CompletionItemKind.EnumMember)
        {
            Member = member;
        }
    }
}