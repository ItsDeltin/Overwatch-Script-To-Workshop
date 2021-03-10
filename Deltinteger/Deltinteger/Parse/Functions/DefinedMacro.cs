using System;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedMacroProvider : IElementProvider, IMethodProvider, ITypeArgTrackee, IApplyBlock
    {
        public string Name => Context.Identifier.Text;
        public AnonymousType[] GenericTypes { get; }
        public CodeType[] ParameterTypes { get; }
        public ParameterProvider[] ParameterProviders { get; }
        public CodeType ReturnType { get; }

        public IExpression Expression { get; private set; }

        public Location DefinedAt { get; }
        public MacroFunctionContext Context { get; }
        public GenericAttributeAppender Attributes { get; } = new GenericAttributeAppender();
        public IMethod Overriding { get; }

        public CallInfo CallInfo { get; }

        int ITypeArgTrackee.GenericsCount => GenericTypes.Length;
        ITypeArgTrackee IMethodExtensions.Tracker => this;

        private readonly RecursiveCallHandler _recursiveHandler;
        private readonly ApplyBlock _applyBlock = new ApplyBlock();

        private readonly ParseInfo _parseInfo;
        private readonly Scope _containingScope;
        private readonly Scope _methodScope;

        public DefinedMacroProvider(ParseInfo parseInfo, IScopeProvider scopeProvider, MacroFunctionContext context)
        {
            _parseInfo = parseInfo;
            Context = context;
            _recursiveHandler = new RecursiveCallHandler(this);
            CallInfo = new CallInfo(_recursiveHandler, parseInfo.Script);
            DefinedAt = parseInfo.Script.GetLocation(context.Identifier.Range);

            // Get the attributes.
            var attributeGetter = new AttributesGetter(context.Attributes, Attributes);
            attributeGetter.GetAttributes(parseInfo.Script.Diagnostics);

            // Setup the scope.
            _containingScope = Attributes.IsStatic ? scopeProvider.GetStaticBasedScope() : scopeProvider.GetObjectBasedScope();
            _methodScope = _containingScope.Child();
            
            // Get the generics.
            GenericTypes = AnonymousType.GetGenerics(context.TypeArguments, AnonymousTypeContext.Function);

            foreach (var type in GenericTypes)
                _methodScope.AddType(new GenericCodeTypeInitializer(type));
            
            // Get the type.
            ReturnType = TypeFromContext.GetCodeTypeFromContext(parseInfo, _methodScope, context.Type);

            // Setup the parameters.
            ParameterProviders = ParameterProvider.GetParameterProviders(parseInfo, _methodScope, context.Parameters, false);
            ParameterTypes = ParameterProviders.Select(p => p.Type).ToArray();

            // Override
            if (Attributes.IsOverride)
                Overriding = scopeProvider.GetOverridenFunction(parseInfo.TranslateInfo, new FunctionOverrideInfo(Name, ParameterTypes));

            // TODO: add hover info
            parseInfo.TranslateInfo.ApplyBlock(this);
        }

        public IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker)
        {
            var instance = new DefinedMacroInstance(this, genericsLinker);
            scopeHandler.Add(instance, Attributes.IsStatic);
            return instance;
        }
        public void AddDefaultInstance(IScopeAppender scopeAppender) => new DefinedMacroInstance(this, new InstanceAnonymousTypeLinker(GenericTypes, GenericTypes));
        public IMethod GetDefaultInstance() => new DefinedMacroInstance(this, new InstanceAnonymousTypeLinker(GenericTypes, GenericTypes));

        public void SetupBlock()
        {
            Expression = _parseInfo.SetCallInfo(CallInfo).SetExpectingLambda(ReturnType).GetExpression(_methodScope, Context.Expression);
            _applyBlock.Apply();
        }

        public void OnBlockApply(IOnBlockApplied onBlockApplied) => _applyBlock.OnBlockApply(onBlockApplied);

        MarkupBuilder ILabeled.GetLabel(DeltinScript deltinScript, LabelInfo labelInfo1) => ((IMethod)this).GetLabel(deltinScript, LabelInfo.Hover);
    }

    public class DefinedMacroInstance : IMethod
    {
        public string Name => Provider.Name;
        public DefinedMacroProvider Provider { get; }
        public CodeParameter[] Parameters { get; }
        public IVariableInstance[] ParameterVars { get; }
        public MarkupBuilder Documentation { get; }
        public ICodeTypeSolver CodeType { get; }
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public AccessLevel AccessLevel => Provider.Attributes.Accessor;
        public bool WholeContext => true;
        public LanguageServer.Location DefinedAt => Provider.DefinedAt;
        IMethodExtensions IMethod.MethodInfo { get; } = new MethodInfo();

        public DefinedMacroInstance(DefinedMacroProvider provider, InstanceAnonymousTypeLinker genericsLinker)
        {
            Provider = provider;
            CodeType = provider.ReturnType.GetRealType(genericsLinker);

            Parameters = new CodeParameter[provider.ParameterProviders.Length];
            ParameterVars = new IVariableInstance[provider.ParameterProviders.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                var parameterInstance = provider.ParameterProviders[i].GetInstance(genericsLinker);
                Parameters[i] = parameterInstance.Parameter;
                ParameterVars[i] = parameterInstance.Variable;
            }
        }

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            // Assign the parameters.
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained()).SetThisTypeLinker(methodCall.TypeArgs);
            return AbstractMacroBuilder.Call(actionSet, this, methodCall);
        }
    }
}