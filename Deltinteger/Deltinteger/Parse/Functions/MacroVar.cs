using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class MacroVarProvider : IVariable, ICallable, IApplyBlock
    {
        public string Name { get; }
        public CallInfo CallInfo { get; }
        public CodeType CodeType { get; }
        public VariableType VariableType => VariableType.ElementReference;
        public CodeType ContainingType { get; }
        public bool WholeContext { get; }
        public Location DefinedAt { get; }
        public AccessLevel AccessLevel { get; }
        public IExpression Value { get; private set; }

        private readonly IParseExpression _expressionContext;
        private readonly IRecursiveCallHandler _recursiveCallHandler;
        private readonly ApplyBlock _applyBlock = new ApplyBlock();
        private readonly ParseInfo _parseInfo;
        private readonly Scope _scope;
        private readonly bool _static;

        public MacroVarProvider(IMacroInfo macroInfo)
        {
            Name = macroInfo.Name;
            _parseInfo = macroInfo.ParseInfo;
            _expressionContext = macroInfo.InitialValueContext;
            _scope = macroInfo.Scope;
            _static = macroInfo.Static;
            WholeContext = macroInfo.WholeContext;
            ContainingType = macroInfo.BelongsTo;
            DefinedAt = macroInfo.DefinedAt;
            AccessLevel = macroInfo.AccessLevel;

            _recursiveCallHandler = new RecursiveCallHandler(this);
            CallInfo = new CallInfo(_recursiveCallHandler, macroInfo.ParseInfo.Script);

            macroInfo.ParseInfo.TranslateInfo.ApplyBlock(this);
        }

        public void SetupBlock()
        {
            Value = _parseInfo.GetExpression(_scope, _expressionContext);
            _applyBlock.Apply();
        }

        public string GetLabel(bool markdown)
        {
            throw new NotImplementedException();
        }

        public void OnBlockApply(IOnBlockApplied onBlockApplied) => _applyBlock.OnBlockApply(onBlockApplied);

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            throw new NotImplementedException();
        }

        public IVariableInstance GetInstance(InstanceAnonymousTypeLinker genericsLinker) => GetDefaultInstance();
        public IVariableInstance GetDefaultInstance() => new MacroVarInstance(this);
        public IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker)
        {
            var instance = GetDefaultInstance();
            scopeHandler.Add(instance, _static);
            return instance;
        }
        public void AddDefaultInstance(IScopeAppender scopeAppender) => AddInstance(scopeAppender, null);
    }

    public class MacroVarInstance : IVariableInstance
    {
        IVariable IVariableInstance.Provider => Provider;
        public MacroVarProvider Provider { get; }
        public string Name => Provider.Name;
        public CodeType CodeType => Provider.CodeType;
        public CodeType ContainingType => Provider.ContainingType;
        public bool WholeContext => Provider.WholeContext;
        public LanguageServer.Location DefinedAt => Provider.DefinedAt;
        public AccessLevel AccessLevel => Provider.AccessLevel;
        public MarkupBuilder Documentation { get; }
        public bool UseDefaultVariableAssigner => false;

        public MacroVarInstance(MacroVarProvider provider)
        {
            Provider = provider;
        }

        public IGettableAssigner GetAssigner() => new ConstantWorkshopValueAssigner(Provider.Value);
        public CompletionItem GetCompletion() => IVariableInstance.GetCompletion(this, CompletionItemKind.Variable);
        public IWorkshopTree ToWorkshop(ActionSet actionSet) => Provider.Value.Parse(actionSet);
    }
}