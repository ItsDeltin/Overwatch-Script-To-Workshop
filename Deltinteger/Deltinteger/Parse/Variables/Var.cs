using System;
using System.Collections.Generic;
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
        public bool Recursive { get; }
        public CodeParameter RelatedParameter { get; }
        private readonly TokenType _tokenType;
        private readonly TokenModifier[] _tokenModifiers;

        public bool WasCalled { get; private set; }

        /// <summary>The scope the variable and initial value will use.</summary>
        private readonly Scope _operationalScope;
        /// <summary>Determines when the initial value should be resolved.</summary>
        private readonly InitialValueResolve _initialValueResolve;
        /// <summary>Stores the context of the initial value.</summary>
        private readonly DeltinScriptParser.ExprContext _initalValueContext;

        /// <summary>The resulting intial value. This will be null if there is no initial value.
        /// If _initialValueResolve is Instant, this will be set when the Var object is created.
        /// If it is ApplyBlock, this will be set when SetupBlock runs.</summary>
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
            Recursive = varInfo.Recursive;
            RelatedParameter = varInfo.RelatedParameter;
            _tokenType = varInfo.TokenType;
            _tokenModifiers = varInfo.TokenModifiers.ToArray();
            _initalValueContext = varInfo.InitialValueContext;
            _initialValueResolve = varInfo.InitialValueResolve;
            _operationalScope = varInfo.OperationalScope;

            if (ID != -1)
            {
                if (VariableType == VariableType.Global)
                    parseInfo.TranslateInfo.VarCollection.Reserve(ID, true, parseInfo.Script.Diagnostics, DefinedAt.range);
                else if (VariableType == VariableType.Player)
                    parseInfo.TranslateInfo.VarCollection.Reserve(ID, false, parseInfo.Script.Diagnostics, DefinedAt.range);
            }

            // Add the variable to the scope.
            _operationalScope.AddVariable(this, parseInfo.Script.Diagnostics, DefinedAt.range);

            parseInfo.Script.AddToken(DefinedAt.range, _tokenType, _tokenModifiers);
            parseInfo.Script.AddHover(DefinedAt.range, GetLabel(true));
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);

            if (_initialValueResolve == InitialValueResolve.Instant)
                GetInitialValue();
            else
                parseInfo.TranslateInfo.ApplyBlock(this);
            
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, varInfo.CodeLensType, DefinedAt.range));
        }

        private void GetInitialValue()
        {
            // Get the initial value.
            if (_initalValueContext != null)
            {
                InitialValue = parseInfo.GetExpression(_operationalScope, _initalValueContext);
                if (InitialValue?.Type() != null && InitialValue.Type().IsConstant() && !InitialValue.Type().Implements(CodeType))
                    parseInfo.Script.Diagnostics.Error($"The type '{InitialValue.Type().Name}' cannot be stored.", DocRange.GetRange(_initalValueContext));
            }
        }

        public bool Settable()
        {
            return (CodeType == null || !CodeType.IsConstant()) && (VariableType == VariableType.Global || VariableType == VariableType.Player || VariableType == VariableType.Dynamic);
        }

        // IExpression
        public Scope ReturningScope()
        {
            if (CodeType == null) return parseInfo.TranslateInfo.PlayerVariableScope;
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
    
        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return actionSet.IndexAssigner[this].GetVariable();
        }
    
        public CompletionItem GetCompletion() => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.Variable,
            Detail = (CodeType?.Name ?? "define") + " " + Name
        };

        public string GetLabel(bool markdown)
        {
            string typeName = "define";
            if (CodeType != null) typeName = CodeType.GetName();
            return new MarkupBuilder().StartCodeLine().Add(typeName + " " + Name).EndCodeLine().ToString(markdown);
        }

        public void SetupParameters() {}

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
        public bool Recursive;
        public TokenType TokenType = TokenType.Variable;
        public List<TokenModifier> TokenModifiers = new List<TokenModifier>();
        public CodeLensSourceType CodeLensType = CodeLensSourceType.Variable;
        public CodeParameter RelatedParameter;

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