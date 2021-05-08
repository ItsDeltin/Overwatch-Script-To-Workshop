using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Functions.Builder;
using Deltin.Deltinteger.Parse.Functions.Builder.User;

namespace Deltin.Deltinteger.Parse
{
    public interface IScopeProvider
    {
        Scope GetObjectBasedScope();
        Scope GetStaticBasedScope();
        IMethod GetOverridenFunction(DeltinScript deltinScript, FunctionOverrideInfo provider);
        IVariableInstance GetOverridenVariable(string variableName);
        Scope GetScope(bool isStatic) => isStatic ? GetStaticBasedScope() : GetObjectBasedScope();
    }

    public interface IScopeAppender
    {
        void AddObjectBasedScope(IMethod function);
        void AddStaticBasedScope(IMethod function);
        void AddObjectBasedScope(IVariableInstance variable);
        void AddStaticBasedScope(IVariableInstance variable);

        void Add(IMethod function, bool isStatic)
        {
            if (isStatic) AddStaticBasedScope(function);
            else AddObjectBasedScope(function);
        }
        void Add(IVariableInstance variable, bool isStatic)
        {
            if (isStatic) AddStaticBasedScope(variable);
            else AddObjectBasedScope(variable);
        }

        CodeType DefinedIn() => this as CodeType;
    }

    public interface IScopeHandler : IScopeProvider, IScopeAppender {}

    public class DefinedMethodProvider : IElementProvider, IMethodProvider, ITypeArgTrackee, IApplyBlock
    {
        public string Name => Context.Identifier.GetText();
        public LanguageServer.Location DefinedAt => new LanguageServer.Location(_parseInfo.Script.Uri, Context.Identifier.GetRange(Context.Range));

        public FunctionContext Context { get; }
        public IDefinedTypeInitializer ContainingType { get; }

        public bool Static { get; }
        public bool IsSubroutine { get; }
        public string SubroutineName { get; }
        public AccessLevel AccessLevel { get; }
        public bool Recursive { get; }
        public bool Virtual { get; }
        public bool SubroutineDefaultGlobal { get; }

        public AnonymousType[] GenericTypes { get; }
        public CodeType ReturnType { get; }
        public ParameterProvider[] ParameterProviders { get; }
        public CodeType[] ParameterTypes { get; }

        public DefinedMethodInstance OverridingFunction { get; }

        public CallInfo CallInfo { get; }

        public BlockAction Block { get; private set; }
        public bool MultiplePaths { get; private set; }
        public IExpression SingleReturnValue { get; private set; }

        ITypeArgTrackee IMethodExtensions.Tracker => this;
        int ITypeArgTrackee.GenericsCount => GenericTypes.Length;
        IMethod IMethodProvider.Overriding => OverridingFunction;

        private readonly ParseInfo _parseInfo;
        private readonly Scope _containingScope;
        private readonly Scope _methodScope;
        private readonly List<IOnBlockApplied> _listeners = new List<IOnBlockApplied>();
        private bool _wasApplied;

        private DefinedMethodProvider(ParseInfo parseInfo, IScopeProvider scopeProvider, FunctionContext context, IDefinedTypeInitializer containingType)
        {
            _parseInfo = parseInfo;
            Context = context;
            ContainingType = containingType;
            DocRange nameRange = context.Identifier.Range;

            // Get the attributes.
            var attributeResult = new GenericAttributeAppender(AttributeType.Ref, AttributeType.In, AttributeType.GlobalVar, AttributeType.PlayerVar);
            var attributeGetter = new AttributesGetter(context.Attributes, attributeResult);
            attributeGetter.GetAttributes(parseInfo.Script.Diagnostics);

            // Set the attributes.
            Static = attributeResult.IsStatic;            
            Recursive = attributeResult.IsRecursive;
            Virtual = attributeResult.IsVirtual;
            AccessLevel = attributeResult.Accessor;

            // Get subroutine info.
            if (context.Subroutine)
            {
                IsSubroutine = true;
                SubroutineName = context.Subroutine.Text.RemoveQuotes();
                SubroutineDefaultGlobal = !context.PlayerVar;
            }

            // Setup the scope.
            _containingScope = Static ? scopeProvider.GetStaticBasedScope() : scopeProvider.GetObjectBasedScope();
            _containingScope.MethodContainer = true;
            _methodScope = _containingScope.Child();
            
            // Get the generics.
            GenericTypes = AnonymousType.GetGenerics(context.TypeArguments, AnonymousTypeContext.Function);

            foreach (var type in GenericTypes)
                _methodScope.AddType(new GenericCodeTypeInitializer(type));

            // Get the type.
            if (!context.Type.IsVoid)
                ReturnType = TypeFromContext.GetCodeTypeFromContext(parseInfo, _methodScope, context.Type);
            
            // Setup the parameters.
            ParameterProviders = ParameterProvider.GetParameterProviders(parseInfo, _methodScope, context.Parameters, IsSubroutine);
            ParameterTypes = ParameterProviders.Select(p => p.Type).ToArray();

            // Override
            if (attributeResult.IsOverride)
                OverridingFunction = (DefinedMethodInstance)scopeProvider.GetOverridenFunction(parseInfo.TranslateInfo, new FunctionOverrideInfo(Name, ParameterTypes));

            // TODO Add the hover info.
            // parseInfo.Script.AddHover(nameRange, GetLabel(true));

            // if (Attributes.IsOverrideable)
            //     parseInfo.Script.AddCodeLensRange(new ImplementsCodeLensRange(this, parseInfo.Script, CodeLensSourceType.Function, nameRange));

            parseInfo.TranslateInfo.ApplyBlock(this);
            parseInfo.Script.Elements.AddMethodDeclaration(this);
        }

        public void SetupParameters() {}
        public void SetupBlock()
        {
            _methodScope.This = ContainingType?.WorkingInstance;
            Block = new BlockAction(_parseInfo.SetReturnType(ReturnType), _methodScope, Context.Block);

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
    
        public IMethod GetDefaultInstance(CodeType definedIn) => new DefinedMethodInstance(this, new InstanceAnonymousTypeLinker(GenericTypes, GenericTypes), definedIn);
        public DefinedMethodInstance CreateInstance(InstanceAnonymousTypeLinker genericsLinker, CodeType definedIn) => new DefinedMethodInstance(this, genericsLinker, definedIn);
        public void AddDefaultInstance(IScopeAppender scopeHandler) => scopeHandler.Add(GetDefaultInstance(scopeHandler.DefinedIn()), Static);
        public IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker)
        {
            // Get the instance.
            IMethod instance = new DefinedMethodInstance(this, genericsLinker, scopeHandler.DefinedIn());
            
            // Add the function to the scope.
            if (Static)
                scopeHandler.AddStaticBasedScope(instance);
            else
                scopeHandler.AddObjectBasedScope(instance);
            
            return instance;
        }

        public static DefinedMethodProvider GetDefinedMethod(ParseInfo parseInfo, IScopeProvider scopeHandler, FunctionContext context, IDefinedTypeInitializer containingType)
            => new DefinedMethodProvider(parseInfo, scopeHandler, context, containingType);

        public MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo) => labelInfo.MakeFunctionLabel(deltinScript, ReturnType, Name, ParameterProviders);
    }

    public class DefinedMethodInstance : IMethod
    {
        public string Name => Provider.Name;
        public MarkupBuilder Documentation => null;
        public ICodeTypeSolver CodeType { get; }
        public IVariableInstance[] ParameterVars { get; }
        public CodeParameter[] Parameters { get; }
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public DefinedMethodProvider Provider { get; }
        public InstanceAnonymousTypeLinker InstanceInfo { get; }
        public CodeType DefinedInType { get; }
        public bool WholeContext => true;
        public LanguageServer.Location DefinedAt => Provider.DefinedAt;
        public AccessLevel AccessLevel => Provider.AccessLevel;
        IMethodExtensions IMethod.MethodInfo => Provider;

        public DefinedMethodInstance(DefinedMethodProvider provider, InstanceAnonymousTypeLinker instanceInfo, CodeType definedIn)
        {
            Provider = provider;
            CodeType = provider.ReturnType?.GetRealType(instanceInfo);
            InstanceInfo = instanceInfo;
            DefinedInType = definedIn;

            Parameters = new CodeParameter[provider.ParameterProviders.Length];
            ParameterVars = new IVariableInstance[Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                var parameterInstance = provider.ParameterProviders[i].GetInstance(instanceInfo);
                ParameterVars[i] = parameterInstance.Variable;
                Parameters[i] = parameterInstance.Parameter;
            }
        }

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained()).SetThisTypeLinker(methodCall.TypeArgs);
            return WorkshopFunctionBuilder.Call(actionSet, methodCall, new UserFunctionController(actionSet.ToWorkshop, this, methodCall.TypeArgs));
        }
    }
}