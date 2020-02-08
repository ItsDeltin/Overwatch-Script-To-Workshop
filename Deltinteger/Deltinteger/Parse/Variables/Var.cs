using System;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class Var : IIndexReferencer, IApplyBlock
    {
        private ParseInfo parseInfo { get; }

        // IScopeable
        public string Name { get; }
        public AccessLevel AccessLevel { get; }
        public Location DefinedAt { get; }
        public bool WholeContext { get; }


        // Attributes
        public CodeType CodeType { get; }
        public VariableType VariableType { get; }
        public StoreType StoreType { get; }
        public bool InExtendedCollection { get; }
        public int ID { get; }
        public bool Static { get; }

        private readonly Scope _operationalScope;
        private readonly InitialValueResolve _initialValueResolve;
        private readonly DeltinScriptParser.ExprContext _initalValueContext;
        private bool _finalized;

        public IExpression InitialValue { get; private set; }

        public CallInfo CallInfo => null;

        public Var(VarInfo varInfo)
        {
            Name = varInfo.Name;
            DefinedAt = varInfo.DefinedAt;
            parseInfo = varInfo.ParseInfo;
            AccessLevel = varInfo.AccessLevel;
            DefinedAt = varInfo.DefinedAt;
            WholeContext = varInfo.WholeContext;
            CodeType = varInfo.Type;
            VariableType = varInfo.VariableType;
            StoreType = varInfo.StoreType;
            InExtendedCollection = varInfo.InExtendedCollection;
            ID = varInfo.ID;
            Static = varInfo.Static;
            _initalValueContext = varInfo.InitialValueContext;
            _initialValueResolve = varInfo.InitialValueResolve;
            _operationalScope = varInfo.OperationalScope;

            // Add the variable to the scope.
            _operationalScope.AddVariable(this, parseInfo.Script.Diagnostics, DefinedAt.range);
            _finalized = true;

            parseInfo.Script.AddHover(DefinedAt.range, GetLabel(true));
            parseInfo.TranslateInfo.AddSymbolLink(this, DefinedAt);

            if (_initialValueResolve == InitialValueResolve.Instant)
                GetInitialValue();
            else
                parseInfo.TranslateInfo.ApplyBlock(this);
        }

        private void GetInitialValue()
        {
            // Get the initial value.
            if (_initalValueContext != null)
            {
                InitialValue = DeltinScript.GetExpression(parseInfo, _operationalScope, _initalValueContext);
                if (InitialValue?.Type() != null && InitialValue.Type().Constant() == TypeSettable.Constant && CodeType != InitialValue.Type())
                    parseInfo.Script.Diagnostics.Error($"The type '{InitialValue.Type().Name}' cannot be stored.", DocRange.GetRange(_initalValueContext));
            }
        }

        public bool Settable()
        {
            return (CodeType == null || CodeType.Constant() == TypeSettable.Normal) && (VariableType == VariableType.Global || VariableType == VariableType.Player || VariableType == VariableType.Dynamic);
        }

        // IExpression
        public Scope ReturningScope()
        {
            ThrowIfNotFinalized();
            if (CodeType == null) return parseInfo.TranslateInfo.PlayerVariableScope;
            else return CodeType.GetObjectScope();
        }
        public CodeType Type()
        {
            ThrowIfNotFinalized();
            return CodeType;
        }

        // ICallable
        public void Call(ScriptFile script, DocRange callRange)
        {
            ThrowIfNotFinalized();
            script.AddDefinitionLink(callRange, DefinedAt);
            script.AddHover(callRange, GetLabel(true));
            parseInfo.TranslateInfo.AddSymbolLink(this, new Location(script.Uri, callRange));
        }

        private void ThrowIfNotFinalized()
        {
            if (!_finalized) throw new Exception("Var not finalized.");
        }
    
        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            return actionSet.IndexAssigner[this].GetVariable();
        }
    
        public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = Name,
                Kind = CompletionItemKind.Variable
            };
        }

        public string GetLabel(bool markdown)
        {
            string typeName = "define";
            if (CodeType != null) typeName = CodeType.Name;
            return HoverHandler.Sectioned(typeName + " " + Name, null);
        }

        public void SetupParameters() {}

        public void SetupBlock()
        {
            GetInitialValue();
        }

        public void OnBlockApply(IOnBlockApplied onBlockApplied) => throw new NotImplementedException();
    }

    public class VarInfo
    {
        public string Name { get; }
        public Location DefinedAt { get; }
        public ParseInfo ParseInfo { get; }

        public CodeType Type = null;
        public bool WholeContext = true;
        public bool Static = false;
        public bool InExtendedCollection = false;
        public int ID = -1;
        public DeltinScriptParser.ExprContext InitialValueContext = null;
        public AccessLevel AccessLevel = AccessLevel.Private;
        public bool IsWorkshopReference = false;
        public VariableType VariableType = VariableType.Dynamic;
        public StoreType StoreType;
        public InitialValueResolve InitialValueResolve = InitialValueResolve.Instant;

        public Scope OperationalScope;

        public VarInfo(string name, Location definedAt, ParseInfo parseInfo)
        {
            Name = name;
            DefinedAt = definedAt;
            ParseInfo = parseInfo;
        }
    }

    public enum VariableType
    {
        // Dynamic variables are either global or player, depending on the rule it is defined in.
        Dynamic,
        // Global variable.
        Global,
        // Player variable.
        Player,
        // The variable references an element.
        ElementReference
    }

    public enum StoreType
    {
        None,
        FullVariable,
        Indexed
    }

    public enum InitialValueResolve
    {
        Instant,
        ApplyBlock
    }
}