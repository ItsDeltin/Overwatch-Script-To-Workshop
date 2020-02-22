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
        public override TypeSettable Constant() => EnumData.ConvertableToElement() ? TypeSettable.Convertable : TypeSettable.Constant;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = EnumData.CodeName,
            Kind = CompletionItemKind.Enum
        };
        public override void Call(ScriptFile script, DocRange callRange)
        {
            MarkupBuilder hoverContents = new MarkupBuilder();

            if (Constant() == TypeSettable.Convertable)
            {
                hoverContents
                    .StartCodeLine()
                    .Add("enum " + Name)
                    .EndCodeLine();
            }
            else if (Constant() == TypeSettable.Constant)
            {
                hoverContents
                    .StartCodeLine()
                    .Add("constant " + Name)
                    .EndCodeLine()
                    .NewSection()
                    .Add("Constant workshop types cannot be stored. Variables with this type cannot be changed from their initial value.");
            }

            script.AddHover(callRange, hoverContents.ToString());
        }
    
        public static WorkshopEnumType GetEnumType(EnumData enumData)
        {
            return (WorkshopEnumType)CodeType.DefaultTypes.First(t => t is WorkshopEnumType && ((WorkshopEnumType)t).EnumData == enumData);
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

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            if (asElement) return EnumData.ToElement(EnumMember) ?? (IWorkshopTree)EnumMember;
            return (IWorkshopTree)EnumMember;
        }

        public CompletionItem GetCompletion() => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.EnumMember
        };
    }
}