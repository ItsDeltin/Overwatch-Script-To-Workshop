using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.FunctionBuilder;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public interface IScopeProvider
    {
        Scope GetObjectBasedScope();
        Scope GetStaticBasedScope();
        IMethod GetOverridenFunction(IMethodProvider provider);
    }

    public interface IScopeAppender
    {
        void AddObjectBasedScope(IMethod function);
        void AddStaticBasedScope(IMethod function);
    }

    public class DefinedMethodProvider : IElementProvider, IMethodProvider, IApplyBlock
    {
        public string Name => Context.Identifier.GetText();
        public LanguageServer.Location DefinedAt => new LanguageServer.Location(_parseInfo.Script.Uri, Context.Identifier.GetRange(Context.Range));

        public FunctionContext Context { get; }
        public ICodeTypeInitializer ContainingType { get; }

        public bool Static { get; }
        public bool IsSubroutine { get; }
        public string SubroutineName { get; }
        public AccessLevel AccessLevel { get; }
        public bool Recursive { get; }
        public bool SubroutineDefaultGlobal { get; }

        public AnonymousType[] GenericTypes { get; }
        public CodeType ReturnType { get; }
        public ParameterProvider[] ParameterProviders { get; }

        public IMethod OverridingFunction { get; }

        public CallInfo CallInfo { get; }

        public BlockAction Block { get; private set; }
        public bool MultiplePaths { get; private set; }
        public IExpression SingleReturnValue { get; private set; }

        public SubroutineInfo SubroutineInfo { get; set; }

        private readonly ParseInfo _parseInfo;
        private readonly Scope _containingScope;
        private readonly Scope _methodScope;
        private readonly List<IMethodProvider> _overriders = new List<IMethodProvider>();
        private readonly List<IOnBlockApplied> _listeners = new List<IOnBlockApplied>();
        private bool _wasApplied;

        private DefinedMethodProvider(ParseInfo parseInfo, IScopeProvider scopeProvider, FunctionContext context, ICodeTypeInitializer containingType)
        {
            _parseInfo = parseInfo;
            Context = context;
            ContainingType = containingType;
            DocRange nameRange = context.Identifier.Range;

            // Get the attributes.
            var attributeResult = new GenericAttributeAppender();
            var attributeGetter = new MethodAttributesGetter(context, attributeResult);
            attributeGetter.GetAttributes(parseInfo.Script.Diagnostics);

            // Set the attributes.
            Static = attributeResult.IsStatic;            
            IsSubroutine = attributeResult.IsSubroutine;            
            SubroutineName = attributeResult.SubroutineName;
            AccessLevel = attributeResult.AccessLevel;
            Recursive = attributeResult.IsRecursive;
            SubroutineDefaultGlobal = attributeResult.DefaultVariableType;

            // Setup the scope.
            _containingScope = Static ? scopeProvider.GetStaticBasedScope() : scopeProvider.GetObjectBasedScope();
            _containingScope.MethodContainer = true;
            _methodScope = _containingScope.Child();
            
            // Get the generics.
            GenericTypes = AnonymousType.GetGenerics(context.TypeArguments);

            foreach (var type in GenericTypes)
                _methodScope.AddType(new GenericCodeTypeInitializer(type));

            // Get the type.
            if (!context.Type.IsVoid)
                ReturnType = TypeFromContext.GetCodeTypeFromContext(parseInfo, _methodScope, context.Type);
            
            // Setup the parameters.
            ParameterProviders = ParameterProvider.GetParameterProviders(parseInfo, _methodScope, context.Parameters, IsSubroutine);

            // Override
            if (attributeResult.IsOverride)
                OverridingFunction = scopeProvider.GetOverridenFunction(this);

            // TODO Add the hover info.
            // parseInfo.Script.AddHover(nameRange, GetLabel(true));

            // if (Attributes.IsOverrideable)
            //     parseInfo.Script.AddCodeLensRange(new ImplementsCodeLensRange(this, parseInfo.Script, CodeLensSourceType.Function, nameRange));

            parseInfo.TranslateInfo.ApplyBlock(this);
        }

        public void SetupParameters() {}
        public void SetupBlock()
        {
            Block = new BlockAction(_parseInfo, _methodScope, Context.Block);

            // Validate returns.
            BlockTreeScan validation = new BlockTreeScan(_parseInfo, this);
            validation.ValidateReturns();
            MultiplePaths = validation.MultiplePaths;

            // If there is only one return statement, set SingleReturnValue.
            if (validation.Returns.Length == 1) SingleReturnValue = validation.Returns[0].ReturningValue;

            // If the return type is a constant type...
            if (ReturnType != null && ReturnType.IsConstant())
                // ... iterate through each return statement ...
                foreach (ReturnAction returnAction in validation.Returns)
                    // ... If the current return statement returns a value and that value does not implement the return type ...
                    if (returnAction.ReturningValue != null && (returnAction.ReturningValue.Type() == null || !returnAction.ReturningValue.Type().Implements(ReturnType)))
                        // ... then add a syntax error.
                        _parseInfo.Script.Diagnostics.Error("Must return a value of type '" + ReturnType.GetName() + "'.", returnAction.ErrorRange);
            
            _wasApplied = true;
            foreach (var listener in _listeners) listener.Applied();
        }

        public void OnBlockApply(IOnBlockApplied onBlockApplied)
        {
            if (_wasApplied) onBlockApplied.Applied();
            else _listeners.Add(onBlockApplied);
        }

        public string GetLabel(bool markdown) => throw new NotImplementedException();
    
        public IMethod GetDefaultInstance() => new DefinedMethodInstance(Name, this, new InstanceAnonymousTypeLinker(GenericTypes, GenericTypes));
        public void AddDefaultInstance(IScopeAppender scopeHandler)
        {
            // Get the instance.
            IMethod instance = GetDefaultInstance();
            
            // Add the function to the scope.
            if (Static)
                scopeHandler.AddStaticBasedScope(instance);
            else
                scopeHandler.AddObjectBasedScope(instance);
        }
        public IMethod GetInstance(GetInstanceInfo instanceInfo) => new DefinedMethodInstance(Name, this, new InstanceAnonymousTypeLinker(GenericTypes, instanceInfo.Generics));
        public void AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker)
        {
            // Get the instance.
            IMethod instance = new DefinedMethodInstance(Name, this, genericsLinker);
            
            // Add the function to the scope.
            if (Static)
                scopeHandler.AddStaticBasedScope(instance);
            else
                scopeHandler.AddObjectBasedScope(instance);
        }

        public SubroutineInfo GetSubroutineInfo()
        {
            if (!IsSubroutine) return null;
            if (SubroutineInfo == null)
            {
                var builder = new SubroutineBuilder(_parseInfo.TranslateInfo, new DefinedSubroutineContext(_parseInfo, this, GetOverrideFunctionHandlers()));
                builder.SetupSubroutine();
            }
            return SubroutineInfo;
        }

        public void Override(IMethodProvider overridenBy)
        {
            OverridingFunction?.GetProvider().Override(overridenBy);
            _overriders.Add(overridenBy);
        }

        public DefinedFunctionHandler[] GetOverrideFunctionHandlers()
            => _overriders.Select(op => new DefinedFunctionHandler((DefinedMethodProvider)op, false)).Prepend(new DefinedFunctionHandler(this, true)).ToArray();

        public static DefinedMethodProvider GetDefinedMethod(ParseInfo parseInfo, IScopeProvider scopeHandler, FunctionContext context, ICodeTypeInitializer containingType)
            => new DefinedMethodProvider(parseInfo, scopeHandler, context, containingType);
    }

    public class DefinedMethodInstance : IMethod
    {
        public string Name { get; }
        public string Documentation => null;
        public CodeType CodeType { get; }
        public CodeParameter[] Parameters { get; }
        public MethodAttributes Attributes { get; }
        public bool Static => Provider.Static;
        public bool WholeContext => true;
        public LanguageServer.Location DefinedAt => Provider.DefinedAt;
        public AccessLevel AccessLevel => Provider.AccessLevel;
        public DefinedMethodProvider Provider { get; }

        public DefinedMethodInstance(string name, DefinedMethodProvider provider, InstanceAnonymousTypeLinker instanceInfo)
        {
            Name = name;
            Provider = provider;
            CodeType = provider.ReturnType?.GetRealerType(instanceInfo);

            Parameters = new CodeParameter[provider.ParameterProviders.Length];
            for (int i = 0; i < Parameters.Length; i++)
                Parameters[i] = provider.ParameterProviders[i].GetInstance(instanceInfo);
        }

        IMethodProvider IMethod.GetProvider() => Provider;
        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
        public string GetLabel(bool markdown) => IMethod.GetLabel(this, true);

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());
            var controller = new FunctionBuildController(actionSet, methodCall, new DefaultGroupDeterminer(Provider.GetOverrideFunctionHandlers()));
            return controller.Call();
        }
    }
}