using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    public class Var : IVariable, IElementProvider, IDeclarationKey
    {
        private readonly ParseInfo _parseInfo;

        // IScopeable
        public string Name { get; }
        public AccessLevel AccessLevel { get; }
        public Location DefinedAt { get; }
        public bool WholeContext { get; }

        // Attributes
        public CodeType CodeType { get; private set; }
        public VariableType VariableType { get; private set; }
        public StoreType StoreType { get; private set; }
        public bool InExtendedCollection { get; }
        public bool Ref { get; }
        public int ID { get; }
        public bool Static { get; }
        public bool Recursive { get; }
        public Lambda.IBridgeInvocable BridgeInvocable { get; }
        public bool RequiresCapture { get; }
        public bool IsMacro { get; }
        public bool Virtual { get; }
        public bool Override { get; }
        public bool Persist { get; }
        private readonly SemanticTokenType _tokenType;
        private readonly TokenModifier[] _tokenModifiers;
        private readonly bool _handleRestrictedCalls;
        private readonly bool _inferType;
        private readonly VariableTypeHandler _variableTypeHandler;
        public MarkupBuilder Documentation { get; }

        /// <summary>The scope the variable and initial value will use.</summary>
        private readonly Scope _operationalScope;
        /// <summary>Determines when the initial value should be resolved.</summary>
        private readonly InitialValueResolve _initialValueResolve;
        /// <summary>Stores the context of the initial value.</summary>
        private readonly IParseExpression _initialValueContext;

        /// <summary>The resulting intial value. This will be null if there is no initial value.
        /// If _initialValueResolve is Instant, this will be set when the Var object is created.
        /// If it is ApplyBlock, this will be set when SetupBlock runs.</summary>
        public IExpression InitialValue { get; private set; }

        public ValueSolveSource ValueReady { get; } = new ValueSolveSource();

        public Var(VarInfo varInfo)
        {
            Name = varInfo.Name;
            DefinedAt = varInfo.DefinedAt;
            _parseInfo = varInfo.ParseInfo;
            AccessLevel = varInfo.AccessLevel;
            WholeContext = varInfo.WholeContext;
            CodeType = varInfo.Type;
            InExtendedCollection = varInfo.InExtendedCollection;
            Ref = varInfo.Ref;
            ID = varInfo.ID;
            Static = varInfo.Static;
            Recursive = varInfo.Recursive;
            BridgeInvocable = varInfo.BridgeInvocable;
            RequiresCapture = varInfo.RequiresCapture;
            IsMacro = varInfo.IsMacro;
            Virtual = varInfo.Virtual;
            Override = varInfo.Override;
            Persist = varInfo.Persist;
            _tokenType = varInfo.TokenType;
            _tokenModifiers = varInfo.TokenModifiers.ToArray();
            _handleRestrictedCalls = varInfo.HandleRestrictedCalls;
            _inferType = varInfo.InferType;
            Documentation = varInfo.Documentation;
            _initialValueContext = varInfo.InitialValueContext;
            _initialValueResolve = varInfo.InitialValueResolve;
            _operationalScope = varInfo.Scope;

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

            // Get the initial value.
            if (_initialValueResolve == InitialValueResolve.Instant)
                GetInitialValue();
            else
                _parseInfo.TranslateInfo.StagedInitiation.On(InitiationStage.Content, GetInitialValue);

            if (DefinedAt != null)
            {
                _parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, _parseInfo, varInfo.CodeLensType, DefinedAt.range));
                _parseInfo.Script.Elements.AddDeclarationCall(this, new DeclarationCall(DefinedAt.range, true));
            }
        }

        private void GetInitialValue()
        {
            // Get the initial value.
            if (_initialValueContext != null)
            {
                ParseInfo parseInfo = this._parseInfo;

                // Store the initial value's restricted calls.
                RestrictedCallList restrictedCalls = null;
                if (_handleRestrictedCalls)
                {
                    restrictedCalls = new RestrictedCallList();
                    parseInfo = parseInfo.SetRestrictedCallHandler(restrictedCalls);
                }

                // The parseInfo used to get the variable's value.
                ParseInfo initialValueParseInfo = parseInfo.SetIsUsedAsValue(true);

                // If the variable type is known, set the expected type.
                if (CodeType != null)
                    initialValueParseInfo = initialValueParseInfo.SetExpectType(CodeType);

                if (parseInfo.CurrentCallInfo == null)
                {
                    CallInfo callInfo = new CallInfo(parseInfo.Script);
                    initialValueParseInfo = initialValueParseInfo.SetCallInfo(callInfo);
                }

                // Parse the initial value.
                InitialValue = initialValueParseInfo.GetExpression(_operationalScope, _initialValueContext);

                // Get the inferred type.
                if (_inferType)
                {
                    CodeType = InitialValue.Type();
                    _variableTypeHandler.SetType(CodeType);
                    AddScriptData();
                }

                // If the initial value's type is constant, make sure the constant type's implements the variable's type.
                if (InitialValue?.Type() != null && InitialValue.Type().IsConstant() && !InitialValue.Type().Implements(CodeType))
                    parseInfo.Script.Diagnostics.Error($"The type '{InitialValue.Type().Name}' cannot be stored.", _initialValueContext.Range);

                // If the variable's type is constant, make sure the value's type matches.
                else SemanticsHelper.ExpectValueType(parseInfo, InitialValue, CodeType, _initialValueContext.Range);

                // Check restricted calls.
                if (_handleRestrictedCalls)
                    foreach (RestrictedCall call in restrictedCalls)
                        // If the variable type is global, or the variable type is player and the restricted call type is not player...
                        if (VariableType == VariableType.Global ||
                            (VariableType == VariableType.Player && call.CallType != RestrictedCallType.EventPlayer))
                            // ... then add the error.
                            call.AddDiagnostic(parseInfo.Script.Diagnostics);
            }

            else if (_inferType && CodeType == null)
            {
                CodeType = _parseInfo.Types.Any();
                _variableTypeHandler.SetType(CodeType);
                AddScriptData();
            }

            ValueReady.Set();
        }

        private void AddScriptData()
        {
            VariableType = _variableTypeHandler.GetVariableType();
            StoreType = _variableTypeHandler.GetStoreType();

            if (DefinedAt != null)
            {
                _parseInfo.Script.AddToken(DefinedAt.range, _tokenType, _tokenModifiers);
                _parseInfo.Script.AddHover(DefinedAt.range, GetLabel());
            }
        }

        public MarkupBuilder GetLabel()
        {
            string typeName = "define";
            if (CodeType != null) typeName = CodeType.GetName();

            var builder = new MarkupBuilder().StartCodeLine().Add(typeName + " " + Name).EndCodeLine();

            if (Documentation != null)
                builder.NewSection().Add(Documentation);

            return builder;
        }

        public override string ToString()
        {
            string name = "[" + Name;

            if (CodeType != null) name += ", Type:" + CodeType.Name;
            name += ", Access:" + AccessLevel.ToString();
            name += ", Store:" + StoreType.ToString();
            name += "]";
            return name;
        }

        public IVariableInstance GetDefaultInstance(CodeType definedIn) => new VariableInstance(this, InstanceAnonymousTypeLinker.Empty, definedIn);
        public IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker)
        {
            var instance = new VariableInstance(this, genericsLinker, scopeHandler.DefinedIn());
            scopeHandler.Add(instance, Static);
            return instance;
        }
        public void AddDefaultInstance(IScopeAppender scopeHandler) => scopeHandler.Add(GetDefaultInstance(scopeHandler.DefinedIn()), Static);
        public IVariableInstance GetInstance(CodeType definedIn, InstanceAnonymousTypeLinker genericsLinker) => new VariableInstance(this, genericsLinker, definedIn);
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