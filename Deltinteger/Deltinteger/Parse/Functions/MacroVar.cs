using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Parse.Functions;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class MacroVar : IVariable, IExpression, ICallable, IApplyBlock
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; set; }

        public bool Virtual { get; set; }
        public bool Override { get; set; }
        public bool IsOverridable => Virtual || Override;
        public CodeType ContainingType { get; }
        public List<MacroVar> Overriders { get; } = new List<MacroVar>();

        public LanguageServer.Location DefinedAt { get; }
        public bool Static { get; set; }
        public bool WholeContext => true;

        public IExpression Expression { get; private set; }
        public CodeType ReturnType { get; private set; }

        private readonly DeltinScriptParser.ExprContext _expressionToParse;
        private readonly Scope _scope;
        private readonly ParseInfo _parseInfo;
        private readonly DeltinScriptParser.Define_macroContext _context;
        private bool _wasApplied = false;

        public CallInfo CallInfo { get; }
        private readonly RecursiveCallHandler _recursiveCallHandler;

        public MacroVar(ParseInfo parseInfo, Scope objectScope, Scope staticScope, DeltinScriptParser.Define_macroContext macroContext, CodeType returnType)
        {
            _context = macroContext;

            Name = macroContext.name.Text;

            // Get the attributes.
            FunctionAttributesGetter attributeResult = new MacroAttributesGetter(macroContext, new MacroVarAttribute(this));
            attributeResult.GetAttributes(parseInfo.Script.Diagnostics);

            ContainingType = (Static ? staticScope : objectScope).This;
            DefinedAt = new Location(parseInfo.Script.Uri, DocRange.GetRange(macroContext.name));
            _recursiveCallHandler = new RecursiveCallHandler(this);
            CallInfo = new CallInfo(_recursiveCallHandler, parseInfo.Script);
            ReturnType = returnType;
            _expressionToParse = macroContext.expr();
            _scope = Static ? staticScope : objectScope;
            this._parseInfo = parseInfo;
            
            _scope.AddMacro(this, parseInfo.Script.Diagnostics, DocRange.GetRange(macroContext.name), !Override);
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);
            parseInfo.Script.AddHover(DocRange.GetRange(macroContext.name), GetLabel(true));
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.Variable, DefinedAt.range));

            DocRange nameRange = DocRange.GetRange(_context.name);

            if (Override)
            {
                MacroVar overriding = (MacroVar)objectScope.GetMacroOverload(Name, DefinedAt);

                if(overriding == this) {
                    parseInfo.Script.Diagnostics.Error("Overriding itself!", nameRange);
                }

                // No method with the name and parameters found.
                if (overriding == null) parseInfo.Script.Diagnostics.Error("Could not find a macro to override.", nameRange);
                else if (!overriding.IsOverridable) parseInfo.Script.Diagnostics.Error("The specified macro is not marked as virtual.", nameRange);
                else overriding.Overriders.Add(this);

                if (overriding != null && overriding.DefinedAt != null)
                {
                    // Make the override keyword go to the base method.
                    parseInfo.Script.AddDefinitionLink(
                        attributeResult.ObtainedAttributes.First(at => at.Type == MethodAttributeType.Override).Range,
                        overriding.DefinedAt
                    );
                }
            }
        }

        public void SetupParameters() {}

        public void SetupBlock()
        {
            if (_expressionToParse != null) Expression = _parseInfo.SetCallInfo(CallInfo).GetExpression(_scope.Child(), _expressionToParse);
            _wasApplied = true;
            foreach (var listener in listeners) listener.Applied();
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            return AbstractMacroBuilder.Call(actionSet, this);
        }

        public Scope ReturningScope() => ReturnType?.GetObjectScope() ?? _parseInfo.TranslateInfo.PlayerVariableScope;

        public CodeType Type() => ReturnType;

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.Script.AddHover(callRange, GetLabel(true));
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new Location(parseInfo.Script.Uri, callRange));
            parseInfo.CurrentCallInfo.Call(_recursiveCallHandler, callRange);
            OnBlockApply(new MacroVarRestrictedCallHandler(this, parseInfo.RestrictedCallHandler, parseInfo.GetLocation(callRange)));
        }

        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Property
        };

        public string GetLabel(bool markdown)
        {
            string name = (ReturnType?.GetName() ?? "define") + " " + Name;
            if (markdown) return HoverHandler.Sectioned(name, null);
            else return name;
        }

        public MacroVar[] AllMacroOverrideOptions()
        {
            List<MacroVar> options = new List<MacroVar>();
            options.AddRange(Overriders);
            foreach (var overrider in Overriders) options.AddRange(overrider.AllMacroOverrideOptions());
            return options.ToArray();
        }

        private List<IOnBlockApplied> listeners = new List<IOnBlockApplied>();
        public void OnBlockApply(IOnBlockApplied onBlockApplied)
        {
            if (_wasApplied) onBlockApplied.Applied();
            else listeners.Add(onBlockApplied);
        }
    }

    class MacroVarAttribute : IFunctionAppendResult
    {
        private readonly MacroVar _macro;

        public MacroVarAttribute(MacroVar macro)
        {
            _macro = macro;
        }

        public bool IsStatic() => _macro.Static;
        public bool IsVirtual() => _macro.Virtual;
        public void SetAccessLevel(AccessLevel accessLevel) => _macro.AccessLevel = accessLevel;
        public void SetOverride() => _macro.Override = true;
        public void SetStatic() => _macro.Static = true;
        public void SetVirtual() => _macro.Virtual = true;
        public void SetRecursive() => throw new NotImplementedException();
        public void SetSubroutine(string name) => throw new NotImplementedException();
    }

    /// <summary>When a macro is called, sometimes the macro's expression is not parsed yet so callers do not know if the macro has a restricted value.
    /// Once the expression is parsed, this will copy the restricted values to the caller's restricted value handler.</summary>
    class MacroVarRestrictedCallHandler : IOnBlockApplied
    {
        private readonly MacroVar _macroVar;
        private readonly IRestrictedCallHandler _callHandler;
        private readonly Location _callLocation;

        public MacroVarRestrictedCallHandler(MacroVar macroVar, IRestrictedCallHandler callHandler, Location callLocation)
        {
            _macroVar = macroVar;
            _callHandler = callHandler;
            _callLocation = callLocation;
        }

        public void Applied()
        {
            foreach (RestrictedCall restrictedCall in _macroVar.CallInfo.RestrictedCalls)
                _callHandler.RestrictedCall(new RestrictedCall(
                    restrictedCall.CallType,
                    _callLocation,
                    new CallStrategy($"The macro '{_macroVar.Name}' calls a restricted value of type '{RestrictedCall.StringFromCallType(restrictedCall.CallType)}'.")
                ));
        }
    }
}