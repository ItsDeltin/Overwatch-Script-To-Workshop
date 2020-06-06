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

        private List<DefinedEnumMember> members = new List<DefinedEnumMember>();


        public DefinedEnum(ParseInfo parseInfo, DeltinScriptParser.Enum_defineContext enumContext) : base(enumContext.name.Text)
        {
            CanBeExtended = false;
            CanBeDeleted = false;
            Kind = "enum";

            if (parseInfo.TranslateInfo.Types.IsCodeType(Name))
                parseInfo.Script.Diagnostics.Error($"A type with the name '{Name}' already exists.", DocRange.GetRange(enumContext.name));
            
            _translateInfo = parseInfo.TranslateInfo;
            Scope = new Scope("enum " + Name);
            
            DefinedAt = new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(enumContext.name));
            _translateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);

            // Get the enum members.
            if (enumContext.firstMember != null)
            {
                var expression = (enumContext.expr() != null) ? new ExpressionOrWorkshopValue(parseInfo.GetExpression(Scope, enumContext.expr())):null;
                members.Add(new DefinedEnumMember(parseInfo, this, enumContext.firstMember.Text, 0, new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(enumContext.firstMember)), expression));

                if (enumContext.enum_element() != null)
                    for (int i = 0; i < enumContext.enum_element().Length; i++)
                    {
                        expression = (enumContext.enum_element(i).expr() != null) ? new ExpressionOrWorkshopValue(parseInfo.GetExpression(Scope, enumContext.enum_element(i).expr())) : null;
                        members.Add(
                            new DefinedEnumMember(
                                parseInfo, this, enumContext.enum_element(i).PART().GetText(), i + 1, new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(enumContext.enum_element(i).PART())), expression
                            )
                        );
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

    class DefinedEnumMember : IVariable, ICallable, IExpression
    {
        public new string Name { get; }
        public LanguageServer.Location DefinedAt { get; }
        public DefinedEnum Enum { get; }
        public int ID { get; }

        public AccessLevel AccessLevel => AccessLevel.Public;
        public bool Static => true;
        public bool WholeContext => true;
        public bool CanBeIndexed => false;

        public ExpressionOrWorkshopValue ValueExpression { get; private set; }
        public IWorkshopTree Value { get; private set; }

        private DeltinScript _translateInfo { get; }

        public InternalVar ValueVar { get; }
        //public InternalVar Var { get; }

        private Scope scope;

        public DefinedEnumMember(ParseInfo parseInfo, DefinedEnum type, string name, int id, LanguageServer.Location definedAt, ExpressionOrWorkshopValue value)
        {
            Enum = type;
            Name = name;
            DefinedAt = definedAt;
            ID = id;
            _translateInfo = parseInfo.TranslateInfo;
            ValueExpression = (value == null) ? new ExpressionOrWorkshopValue(new V_Number(ID)): value;

            _translateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, definedAt, true);
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.EnumValue, DefinedAt.range));

            //Var = new InternalVar(name, AccessLevel.Public, CompletionItemKind.EnumMember);
            //Var.CodeType = this;
            ValueVar = new InternalVar("value", AccessLevel.Public, CompletionItemKind.Property);

            //type.ReturningScope().AddNativeVariable(Var);
            scope = type.ReturningScope().Child(name);

            scope.AddNativeVariable(ValueVar);
        }

        public CodeType Type() => Enum;
        //public Scope ReturningScope() => scope;



        public IWorkshopTree Parse(ActionSet actionSet)
        {
            Value = ValueExpression.Parse(actionSet);
            //actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());
            //actionSet.IndexAssigner.Add(ValueVar, Value);
            return Value;
        }

        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.EnumMember
        };

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            _translateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new LanguageServer.Location(parseInfo.Script.Uri, callRange));
        }

        public Scope ReturningScope() => scope;
    }
}


//class EnumVar : InternalVar
//{
//    public JsonVar(string name) : base(name, CompletionItemKind.Property) { }
//    public override string GetLabel(bool markdown)
//    {
//        if (!markdown) return base.GetLabel(false);
//        return Documentation;
//    }
//}