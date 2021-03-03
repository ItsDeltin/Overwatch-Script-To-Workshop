using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedEnum : CodeType, ISymbolLink
    {
        public LanguageServer.Location DefinedAt { get; }
        private Scope Scope { get; }
        private DeltinScript _translateInfo { get; }

        public DefinedEnum(ParseInfo parseInfo, EnumContext enumContext) : base(enumContext.Identifier.Text)
        {
            CanBeExtended = false;
            CanBeDeleted = false;
            Kind = TypeKind.Enum;

            // Check if a type with the same name already exists.
            // todo
            // if (parseInfo.TranslateInfo.Types.IsCodeType(Name))
            //     parseInfo.Script.Diagnostics.Error($"A type with the name '{Name}' already exists.", enumContext.Identifier.Range);
            
            _translateInfo = parseInfo.TranslateInfo;
            Scope = new Scope("enum " + Name);

            // Set location and symbol link.
            DefinedAt = new Location(parseInfo.Script.Uri, enumContext.Identifier.Range);
            _translateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);

            // Get the enum members.
            for (int i = 0; i < enumContext.Values.Count; i++)
                if (enumContext.Values[i].Identifier)
                {
                    var expression = enumContext.Values[i].Value != null
                        ? new ExpressionOrWorkshopValue(parseInfo.GetExpression(Scope, enumContext.Values[i].Value))
                        : new ExpressionOrWorkshopValue(Element.Num(i));
                    
                    var newMember = new DefinedEnumMember(parseInfo, this, enumContext.Values[i].Identifier.Text, new Location(parseInfo.Script.Uri, enumContext.Values[i].Identifier.Range), expression);
                    Scope.AddVariable(newMember, parseInfo.Script.Diagnostics, newMember.DefinedAt.range);
                }
        }

        public override Scope ReturningScope() => Scope;

        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            base.Call(parseInfo, callRange);
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            _translateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new LanguageServer.Location(parseInfo.Script.Uri, callRange));
        }

        public override CompletionItem GetCompletion() => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.Enum
        };
    }

    class DefinedEnumMember : IVariable, IVariableInstance, IExpression, ICallable, ISymbolLink
    {
        public string Name { get; }
        public MarkupBuilder Documentation { get; }
        public LanguageServer.Location DefinedAt { get; }
        public DefinedEnum Enum { get; }
        public IVariableInstanceAttributes Attributes { get; } = new VariableInstanceAttributes() { CanBeSet = false, StoreType = StoreType.None };

        public AccessLevel AccessLevel => AccessLevel.Public;
        public bool WholeContext => true;
        public ICodeTypeSolver CodeType => null;

        public ExpressionOrWorkshopValue ValueExpression { get; private set; }

        readonly DeltinScript _deltinScript;
        public VariableType VariableType => VariableType.Dynamic;
        public IVariable Provider => this;

        public DefinedEnumMember(ParseInfo parseInfo, DefinedEnum type, string name, LanguageServer.Location definedAt, ExpressionOrWorkshopValue value)
        {
            Enum = type;
            Name = name;
            DefinedAt = definedAt;
            _deltinScript = parseInfo.TranslateInfo;
            ValueExpression = value;

            _deltinScript.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, definedAt, true);
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.EnumValue, DefinedAt.range));
        }

        public CodeType Type() => Enum;

        public IWorkshopTree Parse(ActionSet actionSet) => ValueExpression.Parse(actionSet);

        public CompletionItem GetCompletion() => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.EnumMember
        };

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            _deltinScript.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new Location(parseInfo.Script.Uri, callRange));
        }

        public Scope ReturningScope() => null;

        public IVariableInstance GetInstance(InstanceAnonymousTypeLinker genericsLinker) => this;
        public IGettableAssigner GetAssigner(ActionSet actionSet) => throw new NotImplementedException();
        public IExpression GetExpression(ParseInfo parseInfo, DocRange callRange, IExpression[] index, CodeType[] typeArgs) => this;
        public IVariableInstance GetDefaultInstance() => this;
        public IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker) => throw new NotImplementedException();
        public void AddDefaultInstance(IScopeAppender scopeAppender) => throw new NotImplementedException();
    }
}