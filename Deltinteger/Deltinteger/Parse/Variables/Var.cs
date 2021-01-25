using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class Var : IIndexReferencer, IApplyBlock
    {
        private readonly ParseInfo _parseInfo;

        // IScopeable
        public string Name { get; }
        public MarkupBuilder Documentation { get; }
        public MarkupBuilder MarkupBuilder { get; }
        public AccessLevel AccessLevel { get; }
        public Location DefinedAt { get; }
        public bool WholeContext { get; }


        // Attributes
        public CodeType CodeType { get; private set; }
        ICodeTypeSolver IScopeable.CodeType => CodeType;
        public VariableType VariableType { get; private set; }
        public StoreType StoreType { get; private set; }
        public bool InExtendedCollection { get; }
        public int ID { get; }
        public bool Static { get; }
        public bool Recursive { get; }
        public Lambda.IBridgeInvocable BridgeInvocable { get; }
        public bool RequiresCapture { get; }
        private readonly SemanticTokenType _tokenType;
        private readonly TokenModifier[] _tokenModifiers;
        private readonly bool _handleRestrictedCalls;
        private readonly bool _inferType;
        private readonly VariableTypeHandler _variableTypeHandler;

        public bool WasCalled { get; private set; }

        /// <summary>The scope the variable and initial value will use.</summary>
        private readonly Scope _operationalScope;
        /// <summary>Determines when the initial value should be resolved.</summary>
        private readonly InitialValueResolve _initialValueResolve;
        /// <summary>Stores the context of the initial value.</summary>
        private readonly IParseExpression _initalValueContext;

        /// <summary>The resulting intial value. This will be null if there is no initial value.
        /// If _initialValueResolve is Instant, this will be set when the Var object is created.
        /// If it is ApplyBlock, this will be set when SetupBlock runs.</summary>
        public IExpression InitialValue { get; private set; }

        public CallInfo CallInfo => null;

        public Var(VarInfo varInfo)
        {
            Name = varInfo.Name;
            DefinedAt = varInfo.DefinedAt;
            _parseInfo = varInfo.ParseInfo;
            AccessLevel = varInfo.AccessLevel;
            WholeContext = varInfo.WholeContext;
            CodeType = varInfo.Type;
            InExtendedCollection = varInfo.InExtendedCollection;
            ID = varInfo.ID;
            Static = varInfo.Static;
            Recursive = varInfo.Recursive;
            BridgeInvocable = varInfo.BridgeInvocable;
            RequiresCapture = varInfo.RequiresCapture;
            _tokenType = varInfo.TokenType;
            _tokenModifiers = varInfo.TokenModifiers.ToArray();
            _handleRestrictedCalls = varInfo.HandleRestrictedCalls;
            _inferType = varInfo.InferType;
            _initalValueContext = varInfo.InitialValueContext;
            _initialValueResolve = varInfo.InitialValueResolve;
            _operationalScope = varInfo.OperationalScope;

            _variableTypeHandler = varInfo.VariableTypeHandler;
            if (!_inferType)
                AddScriptData();

            if (ID != -1)
            {
                if (VariableType == VariableType.Global)
                    _parseInfo.TranslateInfo.VarCollection.Reserve(ID, true, _parseInfo.Script.Diagnostics, DefinedAt.range);
                else if (VariableType == VariableType.Player)
                    _parseInfo.TranslateInfo.VarCollection.Reserve(ID, false, _parseInfo.Script.Diagnostics, DefinedAt.range);
            }

            if (_initialValueResolve == InitialValueResolve.Instant)
                GetInitialValue();
            else
                _parseInfo.TranslateInfo.ApplyBlock(this);
            
            _parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, _parseInfo, varInfo.CodeLensType, DefinedAt.range));
        }

        private void GetInitialValue()
        {
            // Get the initial value.
            if (_initalValueContext != null)
            {
                ParseInfo parseInfo = this._parseInfo;

                // Store the initial value's restricted calls.
                RestrictedCallList restrictedCalls = null;
                if (_handleRestrictedCalls)
                {
                    restrictedCalls = new RestrictedCallList();
                    parseInfo = parseInfo.SetRestrictedCallHandler(restrictedCalls);
                }

                // Parse the initial value.
                InitialValue = parseInfo.SetExpectingLambda(CodeType).GetExpression(_operationalScope, _initalValueContext);

                // Get the inferred type.
                if (_inferType)
                {
                    CodeType = InitialValue.Type();
                    _variableTypeHandler.SetType(CodeType);
                    AddScriptData();
                }

                // If the initial value's type is constant, make sure the constant type's implements the variable's type.
                if (InitialValue?.Type() != null && InitialValue.Type().IsConstant() && !InitialValue.Type().Implements(CodeType))
                    parseInfo.Script.Diagnostics.Error($"The type '{InitialValue.Type().Name}' cannot be stored.", _initalValueContext.Range);

                // If the variable's type is constant, make sure the value's type matches.
                else if (CodeType != null && CodeType.IsConstant() && (InitialValue.Type() == null || !InitialValue.Type().Implements(CodeType)))
                    parseInfo.Script.Diagnostics.Error($"Expected a value of type '" + CodeType.GetName() + "'", _initalValueContext.Range);

                // Check restricted calls.
                if (_handleRestrictedCalls)
                    foreach (RestrictedCall call in restrictedCalls)
                        // If the variable type is global, or the variable type is player and the restricted call type is not player...
                        if (VariableType == VariableType.Global ||
                            (VariableType == VariableType.Player && call.CallType != RestrictedCallType.EventPlayer))
                            // ... then add the error.
                            call.AddDiagnostic(parseInfo.Script.Diagnostics);
            }
        }

        private void AddScriptData()
        {
            VariableType = _variableTypeHandler.GetVariableType();
            StoreType = _variableTypeHandler.GetStoreType();

            if (DefinedAt.range != null)
            {
                _parseInfo.Script.AddToken(DefinedAt.range, _tokenType, _tokenModifiers);
                _parseInfo.Script.AddHover(DefinedAt.range, GetLabel(true));
                _parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);
            }
        }

        public bool Settable()
        {
            return (CodeType == null || !CodeType.IsConstant()) && (VariableType == VariableType.Global || VariableType == VariableType.Player || VariableType == VariableType.Dynamic);
        }

        // IExpression
        public Scope ReturningScope()
        {
            if (CodeType == null) return _parseInfo.TranslateInfo.PlayerVariableScope;
            else return CodeType.GetObjectScope();
        }
        public CodeType Type() => CodeType;

        // ICallable
        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            WasCalled = true;
            parseInfo.Script.AddToken(callRange, _tokenType, _tokenModifiers);
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.Script.AddHover(callRange, GetLabel(true));
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new Location(parseInfo.Script.Uri, callRange));
        }

        public IWorkshopTree Parse(ActionSet actionSet) => actionSet.IndexAssigner[this].GetVariable();

        public CompletionItem GetCompletion() => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.Variable,
            Detail = CodeType.GetName() + " " + Name
        };

        public string GetLabel(bool markdown)
        {
            string typeName = "define";
            if (CodeType != null) typeName = CodeType.GetName();
            return new MarkupBuilder().StartCodeLine().Add(typeName + " " + Name).EndCodeLine().ToString(markdown);
        }

        public void SetupParameters() { }

        public void SetupBlock()
        {
            GetInitialValue();
        }

        public void OnBlockApply(IOnBlockApplied onBlockApplied) => throw new NotImplementedException();

        public override string ToString()
        {
            string name = "[" + Name;

            if (CodeType != null) name += ", Type:" + CodeType.Name;
            name += ", Access:" + AccessLevel.ToString();
            name += ", Store:" + StoreType.ToString();
            name += "]";
            return name;
        }
    }

    public class VarInfo
    {
        public string Name { get; }
        public Location DefinedAt { get; }
        public ParseInfo ParseInfo { get; }
        public VariableTypeHandler VariableTypeHandler { get; }

        public CodeType Type = null;
        public bool InferType;
        public bool WholeContext = true;
        public bool Static = false;
        public bool InExtendedCollection = false;
        public int ID = -1;
        public IParseExpression InitialValueContext = null;
        public AccessLevel AccessLevel = AccessLevel.Private;
        public InitialValueResolve InitialValueResolve = InitialValueResolve.Instant;
        public Scope OperationalScope;
        public bool Recursive;
        public SemanticTokenType TokenType = SemanticTokenType.Variable;
        public List<TokenModifier> TokenModifiers = new List<TokenModifier>();
        public bool HandleRestrictedCalls;
        public CodeLensSourceType CodeLensType = CodeLensSourceType.Variable;
        public Lambda.IBridgeInvocable BridgeInvocable;
        public bool RequiresCapture;

        public VarInfo(string name, Location definedAt, ParseInfo parseInfo)
        {
            Name = name;
            DefinedAt = definedAt;
            ParseInfo = parseInfo;
            VariableTypeHandler = new VariableTypeHandler(this);
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