using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Functions.Builder;
using Deltin.Deltinteger.Parse.Functions.Builder.User;
using Deltin.Deltinteger.Parse.Vanilla;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedMethodProvider : IElementProvider, IMethodProvider, ITypeArgTrackee, IApplyBlock, IDeclarationKey, IGetContent
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
        public bool Ref { get; }
        public bool SubroutineDefaultGlobal { get; }
        public VanillaSubroutine? TargetVanillaSubroutine { get; }

        public AnonymousType[] GenericTypes { get; }
        public CodeType ReturnType { get; }
        public ParameterProvider[] ParameterProviders { get; }
        public CodeType[] ParameterTypes { get; }

        public DefinedMethodInstance OverridingFunction { get; }

        public CallInfo CallInfo { get; }

        public BlockAction Block { get; private set; }
        public bool MultiplePaths { get; private set; }
        public IExpression SingleReturnValue { get; private set; }
        public IExpression MacroValue { get; private set; }
        public ValueSolveSource ContentReady { get; } = new ValueSolveSource();
        public ParsedMetaComment MetaComment { get; }

        ITypeArgTrackee IMethodExtensions.Tracker => this;
        int ITypeArgTrackee.GenericsCount => GenericTypes.Length;

        private readonly ParseInfo _parseInfo;
        private readonly Scope _methodScope;

        private DefinedMethodProvider(ParseInfo parseInfo, IScopeHandler scopeProvider, FunctionContext context, IDefinedTypeInitializer containingType)
        {
            _parseInfo = parseInfo;
            Context = context;
            ContainingType = containingType;
            CallInfo = new CallInfo(new RecursiveCallHandler(this, context.Subroutine is not null || context.Attributes.Recursive), parseInfo.Script, ContentReady);

            DocRange nameRange = context.Identifier.Range;

            // Get the attributes.
            var attributes = new GenericAttributeAppender(AttributeType.In, AttributeType.GlobalVar, AttributeType.PlayerVar);
            AttributesGetter.GetAttributes(parseInfo.Script.Diagnostics, context.Attributes, attributes);

            // Set the attributes.
            Static = attributes.IsStatic;
            Recursive = attributes.IsRecursive;
            Virtual = attributes.IsVirtual;
            AccessLevel = attributes.Accessor;
            Recursive = attributes.IsRecursive;
            Ref = attributes.Ref;

            // Get subroutine info.
            if (context.Subroutine?.Name is not null)
            {
                IsSubroutine = true;
                SubroutineName = context.Subroutine.Name.Text.RemoveQuotes();
                SubroutineDefaultGlobal = !context.PlayerVar;

                if (context.Subroutine.Target)
                {
                    // Do not allow targetting a workshop subroutine if there are type arguments.
                    if (context.TypeArguments.Count != 0)
                        parseInfo.Error($"Cannot target a workshop subroutine with type arguments", context.Subroutine.Target);
                    else // Ensure that the subroutine exists.
                    {
                        var targetName = context.Subroutine.Target.Text.RemoveQuotes();

                        // Find vanilla subroutine in scope.
                        TargetVanillaSubroutine = parseInfo.ScopedVanillaVariables?.GetSubroutine(targetName);
                        if (TargetVanillaSubroutine is null)
                            parseInfo.Error($"No workshop subroutine named '{targetName}' is in the current scope", context.Subroutine.Target);
                    }
                }

                if (Ref)
                {
                    parseInfo.Error("Ref functions cannot be subroutines", nameRange);
                }
            }

            // Setup the scope.
            var containingScope = scopeProvider.GetScope(Static);
            containingScope.MethodContainer = true;
            _methodScope = containingScope.Child(true);

            // Get the generics.
            GenericTypes = AnonymousType.GetGenerics(parseInfo, context.TypeArguments, this);
            foreach (var type in GenericTypes)
                _methodScope.AddType(new GenericCodeTypeInitializer(type));

            // Get the type.
            if (!context.Type.IsVoid)
                ReturnType = TypeFromContext.GetCodeTypeFromContext(parseInfo, _methodScope, context.Type);

            // Get the function description.
            MetaComment = ParsedMetaComment.FromMetaComment(context.MetaComment);

            // Setup the parameters.
            ParameterProviders = ParameterProvider.GetParameterProviders(parseInfo, _methodScope, context.Parameters, IsSubroutine, SubroutineDefaultGlobal, MetaComment);
            ParameterTypes = ParameterProviders.Select(p => p.Type).ToArray();

            // Override
            if (attributes.IsOverride)
            {
                OverridingFunction = (DefinedMethodInstance)scopeProvider.GetOverridenFunction(parseInfo.TranslateInfo, new FunctionOverrideInfo(Name, ParameterTypes));
                if (OverridingFunction == null)
                    SemanticsHelper.CouldNotOverride(parseInfo, nameRange, "method");
            }

            // Check conflicts and add to scope.
            scopeProvider.CheckConflict(parseInfo, new(Name, ParameterTypes), nameRange);
            scopeProvider.Add(GetDefaultInstance(scopeProvider.DefinedIn()), Static);

            // Add LSP elements
            // Hover
            parseInfo.Script.AddHover(nameRange, GetLabel(parseInfo.TranslateInfo, LabelInfo.Hover));
            // 'references' code lens
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.Function, DefinedAt.range));
            // Rename & go-to-definition
            parseInfo.Script.Elements.AddDeclarationCall(this, new DeclarationCall(nameRange, true));
            // todo: 'override' code lens
            // if (Attributes.IsOverrideable)
            //     parseInfo.Script.AddCodeLensRange(new ImplementsCodeLensRange(this, parseInfo.Script, CodeLensSourceType.Function, nameRange));

            // Add the CallInfo to the recursion check.
            parseInfo.TranslateInfo.GetComponent<RecursionCheckComponent>().AddCheck(CallInfo);

            // Queue content for staged initiation.
            parseInfo.TranslateInfo.StagedInitiation.On(this);
        }

        public void GetContent()
        {
            var parseInfo = _parseInfo
                .SetReturnType(ReturnType)
                .SetThisType(ContainingType)
                .SetCallInfo(CallInfo)
                .SetIsInRefFunction(Ref);

            // Ignore struct variable settability errors if this is a Ref method.
            if (Ref)
                parseInfo = parseInfo.SetContextualModifierGroup(null);

            if (Context.Block != null)
            {
                var returnTracker = new ReturnTracker();
                Block = new BlockAction(parseInfo.SetReturnTracker(returnTracker), _methodScope, Context.Block);

                MultiplePaths = returnTracker.IsMultiplePaths;

                // todo: there should be smarter return branch checking like earlier versions of ostw had,
                // this will have to do for now to prevent compiling without a return.
                if (!CodeTypeHelpers.IsVoid(ReturnType) && returnTracker.Returns.Count == 0)
                    _parseInfo.Script.Diagnostics.Error("Method must return a value", Context.Identifier);

                // If there is only one return statement, set SingleReturnValue.
                if (returnTracker.Returns.Count == 1) SingleReturnValue = returnTracker.Returns[0].ReturningValue;
            }
            else if (Context.MacroValue != null)
            {
                MacroValue = SingleReturnValue = parseInfo.SetExpectType(ReturnType).GetExpression(_methodScope, Context.MacroValue);
                parseInfo.CreateExpressionCompletion(_methodScope, Context.Colon.Range.End + Context.EndToken.Range.Start);

                SemanticsHelper.ExpectValueType(parseInfo, MacroValue, ReturnType, Context.MacroValue.Range);
            }

            ContentReady.Set();

            if (IsSubroutine && GenericTypes.Length == 0 && (ContainingType == null || ContainingType.WorkingInstance.Generics.Length == 0))
            {
                parseInfo.TranslateInfo.GetComponent<AutoCompileSubroutine>().AddSubroutine(this);
            }
        }

        public IMethod GetDefaultInstance(CodeType definedIn) => new DefinedMethodInstance(this, new InstanceAnonymousTypeLinker(GenericTypes, GenericTypes), definedIn ?? ContainingType?.WorkingInstance);
        public IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker)
        {
            // Get the instance.
            IMethod instance = new DefinedMethodInstance(this, genericsLinker, scopeHandler.DefinedIn());
            scopeHandler.Add(instance, Static);
            return instance;
        }

        public static DefinedMethodProvider GetDefinedMethod(ParseInfo parseInfo, IScopeHandler scopeHandler, FunctionContext context, IDefinedTypeInitializer containingType)
            => new DefinedMethodProvider(parseInfo, scopeHandler, context, containingType);

        public MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo) => labelInfo.MakeFunctionLabel(deltinScript, ReturnType, Name, ParameterProviders, GenericTypes, MetaComment);
    }

    public class DefinedMethodInstance : IMethod
    {
        public string Name => Provider.Name;
        public MarkupBuilder Documentation { get; }
        public ICodeTypeSolver CodeType { get; }
        public IVariableInstance[] ParameterVars { get; }
        public CodeParameter[] Parameters { get; }
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public DefinedMethodProvider Provider { get; }
        public InstanceAnonymousTypeLinker InstanceInfo { get; }
        public bool WholeContext => true;
        public LanguageServer.Location DefinedAt => Provider.DefinedAt;
        public AccessLevel AccessLevel => Provider.AccessLevel;
        IMethodExtensions IMethod.MethodInfo => Provider;

        readonly CodeType definedInType;

        public DefinedMethodInstance(DefinedMethodProvider provider, InstanceAnonymousTypeLinker instanceInfo, CodeType definedIn)
        {
            Provider = provider;
            CodeType = provider.ReturnType?.GetRealType(instanceInfo);
            InstanceInfo = instanceInfo;
            definedInType = Attributes.ContainingType = definedIn;
            Attributes.Parallelable = provider.IsSubroutine;
            Attributes.Recursive = provider.Recursive;

            Parameters = new CodeParameter[provider.ParameterProviders.Length];
            ParameterVars = new IVariableInstance[Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                var parameterInstance = provider.ParameterProviders[i].GetInstance(instanceInfo);
                ParameterVars[i] = parameterInstance.Variable;
                Parameters[i] = parameterInstance.Parameter;
            }

            Attributes.CallInfo = Provider.CallInfo;
            Attributes.GetRestrictedCallTypes = Provider.CallInfo;
            Documentation = Provider.MetaComment?.Description;
        }

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            // Used to GetRealType on the method's type.
            var calleeThisTypeLinker = actionSet.ThisTypeLinker;

            actionSet = actionSet
                .MergeTypeLinker(methodCall.TypeArgs)
                .MergeTypeLinker(InstanceInfo);

            if (Provider.Block != null)
                return WorkshopFunctionBuilder.Call(actionSet, methodCall, new UserFunctionController(actionSet.ToWorkshop, this, actionSet.ThisTypeLinker, calleeThisTypeLinker));
            else
                return MacroBuilder.CallMacroFunction(actionSet, this, methodCall, calleeThisTypeLinker);
        }

        public object Call(ParseInfo parseInfo, DocRange callRange)
        {
            // LSP
            parseInfo.Script.Elements.AddDeclarationCall(Provider, new DeclarationCall(callRange, false));
            parseInfo.Script.AddDefinitionLink(callRange, Provider.DefinedAt);

            // Add method to call tracker.
            parseInfo.CurrentCallInfo.Call(Provider.CallInfo.Function, callRange);

            // If this is a Ref function, ensure the source is a settable variable.
            if (Provider.Ref)
            {
                // Calling function from another value.
                if (parseInfo.SourceExpression is not null)
                    SourceVariableResolver.GetSourceVariable(parseInfo, callRange);
                // Calling function from the current this object.
                else if (!parseInfo.IsInRefFunction)
                    parseInfo.Error("Cannot call ref function in a non-ref function", callRange);
            }

            return null;
        }

        public CodeType GetContainingType(InstanceAnonymousTypeLinker typeLinker) => definedInType?.GetRealType(typeLinker);
        public bool HasContainingType() => definedInType is not null;
    }
}