using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Functions.Builder;
using Deltin.Deltinteger.Parse.Functions.Builder.User;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class MethodGroup : IVariableInstance, IVariable
    {
        public string Name { get; }
        public MarkupBuilder Documentation { get; }
        public bool WholeContext => true;
        public Location DefinedAt => null; // Doesn't matter.
        public AccessLevel AccessLevel => AccessLevel.Public; // Doesn't matter.
        public ICodeTypeSolver CodeType => null;
        public IMethod[] Functions { get; }
        IVariable IVariableInstance.Provider => this;
        VariableType IVariable.VariableType => VariableType.ElementReference;
        IVariableInstanceAttributes IVariableInstance.Attributes { get; } = new VariableInstanceAttributes()
        {
            CanBeSet = false,
            StoreType = StoreType.None,
        };

        public MethodGroup(string name, IMethod[] functions)
        {
            Name = name;
            Functions = functions;
        }

        public CompletionItem GetCompletion(DeltinScript deltinScript) => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.Function,
            Documentation = new MarkupBuilder()
                .StartCodeLine()
                .Add(
                    Functions[0].GetLabel(deltinScript, LabelInfo.SignatureOverload) + (Functions.Length == 1 ? "" : " (+" + (Functions.Length - 1) + " overloads)")
                ).EndCodeLine().ToMarkup()
        };

        public IGettableAssigner GetAssigner(GetVariablesAssigner getAssigner) => throw new NotImplementedException();

        ICallVariable IVariableInstance.GetExpression(ParseInfo parseInfo, DocRange callRange, IExpression[] index, CodeType[] typeArgs)
            => new CallMethodGroup(parseInfo, callRange, this, typeArgs);

        IVariableInstance IVariable.GetInstance(CodeType definedIn, InstanceAnonymousTypeLinker genericsLinker) => this;
        IVariableInstance IVariable.GetDefaultInstance(CodeType definedIn) => this;
        IScopeable IElementProvider.AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker) => throw new NotImplementedException();

        void IVariableInstance.Call(ParseInfo parseInfo, DocRange callRange) { }
    }

    public class CallMethodGroup : ICallVariable, IExpression, ILambdaApplier, ILambdaInvocable, IWorkshopTree
    {
        public MethodGroup Group { get; }
        public CodeType[] TypeArgs { get; }
        private readonly ParseInfo _parseInfo;
        private readonly DocRange _range;
        private CodeType _type = new UnknownLambdaType(-1);
        public IMethod ChosenFunction { get; private set; }
        private IMethodGroupInvoker _constFunctionInvoker;
        public CallInfo CallInfo => ChosenFunction.Attributes.CallInfo;
        public IRecursiveCallHandler RecursiveCallHandler => CallInfo?.Function;
        public bool ResolvedSource => ChosenFunction != null;
        public IBridgeInvocable[] InvokedState { get; private set; }

        public CallMethodGroup(ParseInfo parseInfo, DocRange range, MethodGroup group, CodeType[] typeArgs)
        {
            _parseInfo = parseInfo;
            _range = range;
            Group = group;
            TypeArgs = typeArgs;
        }

        public void Accept()
        {
            _parseInfo.Script.AddToken(_range, SemanticTokenType.Function);

            if (_parseInfo.ResolveInvokeInfo != null)
                _parseInfo.ResolveInvokeInfo.Resolve(new MethodGroupInvokeInfo());
            else
                new CheckLambdaContext(
                    _parseInfo,
                    this,
                    "Cannot determine lambda in the current context",
                    _range,
                    ParameterState.Unknown
                ).Check();
        }

        public void GetLambdaContent(PortableLambdaType expecting)
        {
            if (SelectFunction(expecting))
                _type = TypeFromMethod(_parseInfo.TranslateInfo, ChosenFunction);
        }

        public void GetLambdaContent() => _parseInfo.Script.Diagnostics.Error("Cannot determine method group in the current context. Did you intend to invoke the method?", _range);

        public void Finalize(PortableLambdaType expecting)
        {
            if (expecting == null) return;

            _type = expecting;

            // If a compatible function was found, get the handler.
            if (ChosenFunction != null || SelectFunction(expecting))
            {
                _constFunctionInvoker = GetLambdaHandler(ChosenFunction);

                // Get the variable's invoke info from the parameters.
                InvokedState = new IBridgeInvocable[_constFunctionInvoker.ParameterCount()];
                for (int i = 0; i < _constFunctionInvoker.ParameterCount(); i++)
                    if (_constFunctionInvoker.GetParameterVar(i) is Var var)
                        InvokedState[i] = var.BridgeInvocable;
            }
            else
                _parseInfo.Script.Diagnostics.Error("No overload for '" + Group.Name + "' implements " + expecting.GetName(), _range);
        }

        bool SelectFunction(PortableLambdaType expecting)
        {
            foreach (var func in Group.Functions)
                if (FuncValid(func, expecting))
                {
                    ChosenFunction = func;
                    return true;
                }

            return false;
        }

        bool FuncValid(IMethod method, PortableLambdaType expecting)
        {
            if (expecting == null || method.Parameters.Length != expecting.Parameters.Length)
                return false;

            // Make sure the method implements the target lambda.
            for (int i = 0; i < method.Parameters.Length; i++)
            {
                var parameterType = method.Parameters[i].GetCodeType(_parseInfo.TranslateInfo);
                if (!parameterType.Implements(expecting.Parameters[i]))
                    return false;
            }

            return true;
        }

        CodeType TypeFromMethod(DeltinScript deltinScript, IMethod method) =>
            new PortableLambdaType(new PortableLambdaTypeBuilder(
                LambdaKind.Anonymous,
                method.Parameters.Select(p => p.GetCodeType(deltinScript)).ToArray(),
                method.CodeType?.GetCodeType(deltinScript)
            ));

        private static IMethodGroupInvoker GetLambdaHandler(IMethod function)
        {
            // If the chosen function is a DefinedMethod, use the DefinedFunctionHandler.
            if (function is DefinedMethodInstance definedMethod)
                return new DefinedMethodInvoker(definedMethod);

            // Otherwise, use the generic function handler.
            return new GenericFunctionInvoker(function);
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            if (_type.IsConstant())
                return this;
            return Lambda.Workshop.CaptureEncoder.Encode(actionSet, this);
        }

        public IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues) => _constFunctionInvoker.Invoke(actionSet, parameterValues);

        public MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo) => ChosenFunction.GetLabel(deltinScript, labelInfo);
        public Scope ReturningScope() => null;
        public CodeType Type() => _type;
        public IEnumerable<RestrictedCallType> GetRestrictedCallTypes() => ChosenFunction.Attributes.GetRestrictedCallTypes?.GetRestrictedCallTypes() ?? Enumerable.Empty<RestrictedCallType>();

        public void ToWorkshop(WorkshopBuilder builder, ToWorkshopContext context) => throw new NotImplementedException();
        public bool EqualTo(IWorkshopTree other) => throw new NotImplementedException();
    }

    interface IMethodGroupInvoker
    {
        int ParameterCount();
        IVariable GetParameterVar(int index);
        int GetIdentifier(ParseInfo parseInfo) => -1;
        IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues);
    }

    class DefinedMethodInvoker : IMethodGroupInvoker
    {
        readonly DefinedMethodInstance _method;

        public DefinedMethodInvoker(DefinedMethodInstance method)
        {
            _method = method;
        }

        public IVariable GetParameterVar(int index) => _method.ParameterVars[index].Provider;
        public int ParameterCount() => _method.ParameterVars.Length;

        // todo: macros
        public IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues) =>
            _method.Parse(actionSet, new MethodCall(parameterValues));
        // WorkshopFunctionBuilder.Call(
        //     actionSet,
        //     new Functions.Builder.CallInfo(parameterValues),
        //     new UserFunctionController(actionSet.ToWorkshop, _method, null));
    }

    class GenericFunctionInvoker : IMethodGroupInvoker
    {
        readonly IMethod _method;

        public GenericFunctionInvoker(IMethod method)
        {
            _method = method;
        }

        public IVariable GetParameterVar(int index) => null;
        public int ParameterCount() => _method.Parameters.Length;
        public IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues) => _method.Parse(actionSet, new MethodCall(parameterValues));
    }
}