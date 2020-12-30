using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Parse
{
    public abstract class DefinedFunction : IMethod, ICallable, IApplyBlock, ISymbolLink
    {
        public string Name { get; }
        public CodeType CodeType { get; protected set; }
        public CodeParameter[] Parameters { get; private set; }
        public AccessLevel AccessLevel { get; protected set; }
        public Location DefinedAt { get; }
        public bool WholeContext { get; } = true;
        public MarkupBuilder Documentation { get; } = null;
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public bool Static { get; protected set; }

        protected ParseInfo parseInfo { get; }
        protected Scope methodScope { get; private set; }
        protected Scope containingScope { get; private set; }
        public Var[] ParameterVars { get; private set; }

        public CallInfo CallInfo { get; }

        protected bool WasApplied = false;

        private readonly RecursiveCallHandler _recursiveCallHandler;

        public DefinedFunction(ParseInfo parseInfo, string name, Location definedAt)
        {
            Name = name;
            DefinedAt = definedAt;
            this.parseInfo = parseInfo;
            _recursiveCallHandler = new RecursiveCallHandler(this);
            CallInfo = new CallInfo(_recursiveCallHandler, parseInfo.Script);

            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, definedAt, true);
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.Function, DefinedAt.range));
        }

        protected void SetupScope(Scope chosenScope)
        {
            methodScope = chosenScope.Child();
            methodScope.CatchConflict = true;
            containingScope = chosenScope;
        }

        // IApplyBlock
        public virtual void SetupParameters() { }
        public abstract void SetupBlock();

        protected void SetupParameters(List<VariableDeclaration> context, bool subroutineParameter)
        {
            var parameterInfo = CodeParameter.GetParameters(parseInfo, methodScope, context, subroutineParameter);
            Parameters = parameterInfo.Parameters;
            ParameterVars = parameterInfo.Variables;
        }

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new Location(parseInfo.Script.Uri, callRange));
            parseInfo.CurrentCallInfo?.Call(_recursiveCallHandler, callRange);
        }
        
        public string GetLabel(bool markdown) => IMethod.DefaultLabel(markdown, this).ToString(markdown);

        public abstract IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall);

        public CompletionItem GetCompletion() => IMethod.GetFunctionCompletion(this);

        protected List<IOnBlockApplied> listeners = new List<IOnBlockApplied>();
        public void OnBlockApply(IOnBlockApplied onBlockApplied)
        {
            if (WasApplied) onBlockApplied.Applied();
            else listeners.Add(onBlockApplied);
        }

        public override string ToString()
        {
            string name = GetLabel(false);
            if (Attributes.ContainingType != null) name = Attributes.ContainingType.Name + "." + name;
            return name;
        }

        public Var[] VirtualVarGroup(int i)
        {
            List<Var> parameters = new List<Var>();
            foreach (var overrider in Attributes.AllOverrideOptions()) parameters.Add(((DefinedFunction)overrider).ParameterVars[i]);
            return parameters.ToArray();
        }
    }
}