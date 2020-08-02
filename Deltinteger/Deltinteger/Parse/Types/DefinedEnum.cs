using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedEnum : CodeType
    {
        public LanguageServer.Location DefinedAt { get; }
        private Scope Scope { get; }
        private DeltinScript _translateInfo { get; }


        public DefinedEnum(ParseInfo parseInfo, DeltinScriptParser.Enum_defineContext enumContext) : base(enumContext.name.Text)
        {
            CanBeExtended = false;
            CanBeDeleted = false;
            Kind = "enum";

            if (parseInfo.TranslateInfo.Types.IsCodeType(Name))
                parseInfo.Script.Diagnostics.Error($"A type with the name '{Name}' already exists.", DocRange.GetRange(enumContext.name));
            
            _translateInfo = parseInfo.TranslateInfo;
            Scope = new Scope("enum " + Name);
            
            DefinedAt = new Location(parseInfo.Script.Uri, DocRange.GetRange(enumContext.name));
            _translateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);

            List<DefinedEnumMember> members = new List<DefinedEnumMember>();

            // Get the enum members.
            if (enumContext.firstMember != null)
            {
                var firstExpression = (enumContext.expr() != null) ? new ExpressionOrWorkshopValue(parseInfo.GetExpression(Scope, enumContext.expr())) : new ExpressionOrWorkshopValue(new V_Number(0));
                members.Add(new DefinedEnumMember(parseInfo, this, enumContext.firstMember.Text, new Location(parseInfo.Script.Uri, DocRange.GetRange(enumContext.firstMember)), firstExpression));

                if (enumContext.enum_element() != null)
                    for (int i = 0; i < enumContext.enum_element().Length; i++)
                    {
                        var expression = enumContext.enum_element(i).expr() != null
                            ? new ExpressionOrWorkshopValue(parseInfo.GetExpression(Scope, enumContext.enum_element(i).expr()))
                            : new ExpressionOrWorkshopValue(new NumberElement(i + 1));
                        
                        members.Add(new DefinedEnumMember(parseInfo, this, enumContext.enum_element(i).PART().GetText(), new Location(parseInfo.Script.Uri, DocRange.GetRange(enumContext.enum_element(i).PART())), expression));
                    }
            }

            foreach (var member in members) Scope.AddVariable(member, parseInfo.Script.Diagnostics, member.DefinedAt.range);
        }

        public override Scope ReturningScope() => Scope;

        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            base.Call(parseInfo, callRange);
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            _translateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new LanguageServer.Location(parseInfo.Script.Uri, callRange));
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Enum
        };
    }

    class DefinedEnumMember : IVariable, IExpression, ICallable
    {
        public string Name { get; }
        public LanguageServer.Location DefinedAt { get; }
        public DefinedEnum Enum { get; }

        public AccessLevel AccessLevel => AccessLevel.Public;
        public bool Static => true;
        public bool WholeContext => true;
        public bool CanBeIndexed => false;

        public ExpressionOrWorkshopValue ValueExpression { get; private set; }

        private DeltinScript _translateInfo { get; }

        public DefinedEnumMember(ParseInfo parseInfo, DefinedEnum type, string name, LanguageServer.Location definedAt, ExpressionOrWorkshopValue value)
        {
            Enum = type;
            Name = name;
            DefinedAt = definedAt;
            _translateInfo = parseInfo.TranslateInfo;
            ValueExpression = value;

            _translateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, definedAt, true);
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.EnumValue, DefinedAt.range));
        }

        public CodeType Type() => Enum;

        public IWorkshopTree Parse(ActionSet actionSet) => ValueExpression.Parse(actionSet);

        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.EnumMember
        };

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            _translateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new Location(parseInfo.Script.Uri, callRange));
        }

        public Scope ReturningScope() => null;
    }
}