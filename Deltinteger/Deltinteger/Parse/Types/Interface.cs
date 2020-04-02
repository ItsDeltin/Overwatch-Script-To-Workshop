using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public interface IImplementer : INamed
    {
        List<Interface> Contracts { get; }
        CodeType Extends { get; set; }
    }

    public abstract class Interface : CodeType, IImplementer
    {
        public List<Interface> Contracts { get; } = new List<Interface>();
        protected Scope ObjectScope { get; } = new Scope();
        protected List<InterfaceVariable> Variables { get; } = new List<InterfaceVariable>();

        public Interface(string name) : base(name)
        {
            CanBeExtended = true;
            CanBeDeleted = true;
        }

        public override Scope GetObjectScope() => ObjectScope;
        public override Scope ReturningScope() => null;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Detail = "interface " + Name,
            Kind = CompletionItemKind.Interface
        };
    }

    public class DefinedInterface : Interface
    {
        public LanguageServer.Location DefinedAt { get; }
        private readonly ParseInfo _parseInfo;
        private readonly DeltinScriptParser.InterfaceContext _context;
        private bool _elementsResolved = false;

        public DefinedInterface(ParseInfo parseInfo, DeltinScriptParser.InterfaceContext context) : base(context.name.Text)
        {
            _context = context;
            _parseInfo = parseInfo;

            if (parseInfo.TranslateInfo.Types.IsCodeType(Name))
                parseInfo.Script.Diagnostics.Error($"A type with the name '{Name}' already exists.", DocRange.GetRange(context.name));
            
            DefinedAt = new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(context.name));
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);
        }

        public override void ResolveElements()
        {
            if (_elementsResolved) return;
            _elementsResolved = true;

            // Get implementing interfaces.
            InheritContext.InheritFromContext(this, _parseInfo, _context.inherit());

            // Get interface variables.
            foreach (var variable in _context.interface_variable())
            {
                // Get the variable's type.
                CodeType variableType = CodeType.GetCodeTypeFromContext(_parseInfo, variable.code_type());

                // Interface variables cannot be constants.
                if (variableType.Constant() == TypeSettable.Constant)
                {
                    _parseInfo.Script.Diagnostics.Error("Interface variables cannot have a constant type.", DocRange.GetRange(variable.code_type()));
                    continue;
                }

                InterfaceVariable newVariable = new InterfaceVariable(variableType, variable.name.Text, new Location(_parseInfo.Script.Uri, DocRange.GetRange(variable.name)));
                Variables.Add(newVariable);
            }
        }

        public override void WorkshopInit(DeltinScript translateInfo)
        {

        }

        public override void Call(ScriptFile script, DocRange callRange)
        {
            base.Call(script, callRange);
            script.AddDefinitionLink(callRange, DefinedAt);
            _parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new LanguageServer.Location(script.Uri, callRange));
        }
    }

    public class InterfaceVariable : IIndexReferencer
    {
        public CodeType CodeType { get; }
        public string Name { get; }
        public LanguageServer.Location DefinedAt { get; }
        public VariableType VariableType => VariableType.Global;
        public bool Static => false;
        public bool WholeContext => true;
        public AccessLevel AccessLevel => AccessLevel.Public;

        public InterfaceVariable(CodeType type, string name, Location definedAt)
        {
            CodeType = type;
            Name = name;
            DefinedAt = definedAt;
        }

        public bool Settable() => true;
        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Variable
        };
        public Scope ReturningScope() => Type().GetObjectScope();
        public CodeType Type() => CodeType;
        public IWorkshopTree Parse(ActionSet actionSet) => throw new NotImplementedException();

        public void Call(ScriptFile script, DocRange callRange)
        {
            //todo
        }

        public string GetLabel(bool markdown) => "todo";
    }
}