using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    class InterfaceLinkComponent : IComponent, IWorkshopInitComponent
    {
        public DeltinScript DeltinScript { get; set; }
        public List<InterfaceLink> Links { get; } = new List<InterfaceLink>();

        public void Init() {}

        public void Add(string variableName)
        {
            if (!Links.Any(link => link.VariableName == variableName)) Links.Add(new InterfaceLink(variableName));
        }

        public void WorkshopInit()
        {
            foreach (var link in Links)
                link.SetArrayStore(DeltinScript.VarCollection.Assign(link.VariableName, true, false));
        }

        public bool TryGet(string variableName, out IndexReference arrayStore)
        {
            foreach (var link in Links)
                if (variableName == link.VariableName)
                {
                    arrayStore = link.ArrayStore;
                    return true;
                }
            arrayStore = null;
            return false;
        }
    }

    class InterfaceLink
    {
        public string VariableName { get; }
        public IndexReference ArrayStore { get; private set; }

        public InterfaceLink(string variableName)
        {
            VariableName = variableName;
        }

        public void SetArrayStore(IndexReference indexReference)
        {
            ArrayStore = indexReference;
        }
    }

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
        protected ParseInfo ParseInfo { get; }

        public Interface(ParseInfo parseInfo, string name) : base(name)
        {
            ParseInfo = parseInfo;
            CanBeExtended = true;
            CanBeDeleted = true;
        }

        public void SetupImplementer(ClassType classType)
        {
            foreach (var variable in classType.ObjectVariables)
                if (variable.ArrayStore == null && ParseInfo.TranslateInfo.GetComponent<InterfaceLinkComponent>().TryGet(variable.Variable.Name, out IndexReference reference))
                    variable.SetArrayStore(reference);
            
            if (classType.Extends != null)
                SetupImplementer((ClassType)classType.Extends);
        }

        protected void AddVariable(InterfaceVariable variable, DocRange range)
        {
            Variables.Add(variable);
            ObjectScope.AddVariable(variable, ParseInfo.Script.Diagnostics, range);
            ParseInfo.TranslateInfo.GetComponent<InterfaceLinkComponent>().Add(variable.Name);
        }

        public override bool DoesImplement(CodeType type)
        {
            if (this == type) return true;
            foreach (Interface contract in Contracts)
                if (contract.DoesImplement(type))
                    return true;
            return false;
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
        private readonly DeltinScriptParser.InterfaceContext _context;
        private bool _elementsResolved = false;

        public DefinedInterface(ParseInfo parseInfo, DeltinScriptParser.InterfaceContext context) : base(parseInfo, context.name.Text)
        {
            _context = context;

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
            InheritContext.InheritFromContext(this, ParseInfo, _context.inherit());

            // Get interface variables.
            foreach (var variable in _context.interface_variable())
            {
                // Get the variable's type.
                CodeType variableType = CodeType.GetCodeTypeFromContext(ParseInfo, variable.code_type());

                // Interface variables cannot be constants.
                if (variableType != null && variableType.Constant() == TypeSettable.Constant)
                {
                    ParseInfo.Script.Diagnostics.Error("Interface variables cannot have a constant type.", DocRange.GetRange(variable.code_type()));
                    continue;
                }

                InterfaceVariable newVariable = new InterfaceVariable(variableType, variable.name.Text, new Location(ParseInfo.Script.Uri, DocRange.GetRange(variable.name)));
                AddVariable(newVariable, DocRange.GetRange(variable.name));
            }
        }

        public override void Call(ScriptFile script, DocRange callRange)
        {
            base.Call(script, callRange);
            script.AddDefinitionLink(callRange, DefinedAt);
            ParseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new LanguageServer.Location(script.Uri, callRange));
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