using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.FunctionBuilder;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class MethodGroup : IVariable
    {
        public string Name { get; }
        public bool WholeContext => true;
        public bool CanBeIndexed => false;
        public bool Static => false; // Doesn't matter.
        public Location DefinedAt => null; // Doesn't matter.
        public AccessLevel AccessLevel => AccessLevel.Public; // Doesn't matter.
        public CodeType CodeType => null;
        public List<IMethod> Functions { get; } = new List<IMethod>();

        public MethodGroup(string name)
        {
            Name = name;
        }

        public bool MethodIsValid(IMethod method) => method.Name == Name;
        public void AddMethod(IMethod method) => Functions.Add(method);

        public CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Function,
            Documentation = new MarkupBuilder()
                .StartCodeLine()
                .Add(
                    (Functions[0].DoesReturnValue ? (Functions[0].CodeType == null ? "define" : Functions[0].CodeType.GetName()) : "void") + " " +
                    Functions[0].GetLabel(false) + (Functions.Count == 1 ? "" : " (+" + (Functions.Count - 1) + " overloads)")
                ).EndCodeLine().ToMarkup()
        };
    }

    public class CallMethodGroup : IExpression, ILambdaApplier, ILambdaInvocable, IWorkshopTree
    {
        public MethodGroup Group { get; }
        private readonly ParseInfo _parseInfo;
        private readonly DocRange _range;
        private PortableLambdaType _type = new PortableLambdaType(LambdaKind.Anonymous);
        private IMethod _chosenFunction;
        private int _identifier;
        private IMethodGroupInvoker _functionInvoker;
        public CallInfo CallInfo => (_chosenFunction as IApplyBlock)?.CallInfo;
        public IRecursiveCallHandler RecursiveCallHandler => CallInfo?.Function;
        public bool ResolvedSource => _chosenFunction != null;
        public IBridgeInvocable[] InvokedState { get; private set; }

        public CallMethodGroup(ParseInfo parseInfo, DocRange range, MethodGroup group)
        {
            _parseInfo = parseInfo;
            _range = range;
            Group = group;
        }

        public void Accept()
        {
            _parseInfo.Script.AddToken(_range, TokenType.Function);

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

        public void GetLambdaStatement(PortableLambdaType expecting)
        {
            bool found = false;
            _type = expecting;
            foreach (var func in Group.Functions)
            {
                if (func.Parameters.Length == expecting.Parameters.Length)
                {
                    // Make sure the method implements the target lambda.
                    for (int i = 0; i < func.Parameters.Length; i++)
                        if (func.Parameters[i].Type != null && !func.Parameters[i].Type.Implements(expecting.Parameters[i]))
                            continue;
                    
                    _chosenFunction = func;
                    found = true;
                    break;
                }
            }

            // If a compatible function was found, get the handler.
            if (found)
            {
                _functionInvoker = GetLambdaHandler(_chosenFunction);
                if (!_type.IsConstant())
                    _identifier = _functionInvoker.GetIdentifier(_parseInfo);

                // Get the variable's invoke info from the parameters.
                InvokedState = new IBridgeInvocable[_functionInvoker.ParameterCount()];
                for (int i = 0; i < _functionInvoker.ParameterCount(); i++)
                    if (_functionInvoker.GetParameterVar(i) is Var var)
                        InvokedState[i] = var.BridgeInvocable;
            }
            else
                _parseInfo.Script.Diagnostics.Error("No overload for '" + Group.Name + "' implements " + expecting.GetName(), _range);
        }

        public void GetLambdaStatement() => _parseInfo.Script.Diagnostics.Error("Cannot determine method group in the current context. Did you intend to invoke the method?", _range);

        private static IMethodGroupInvoker GetLambdaHandler(IMethod function)
        {
            // If the chosen function is a DefinedMethod, use the DefinedFunctionHandler.
            if (function is DefinedMethod definedMethod)
                return new FunctionMethodGroupInvoker(new DefinedFunctionHandler(definedMethod, false));
            
            // If the chosen function is a macro.
            if (function is DefinedMacro definedMacro)
                return new MacroMethodGroupInvoker(definedMacro);
            
            // Otherwise, use the generic function handler.
            return new FunctionMethodGroupInvoker(new GenericMethodHandler(function));
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            if (_type.IsConstant())
                return this;
            return Element.CreateArray(Element.Num(_identifier), actionSet.This ?? Element.Null());
        }

        public IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues) => _functionInvoker.Invoke(actionSet, parameterValues);

        public string GetLabel(bool markdown) => _chosenFunction.GetLabel(markdown);
        public Scope ReturningScope() => null;
        public CodeType Type() => _type;
        
        public void ToWorkshop(WorkshopBuilder builder, ToWorkshopContext context) => throw new NotImplementedException();
        public bool EqualTo(IWorkshopTree other) => throw new NotImplementedException();
    }

    interface IMethodGroupInvoker
    {
        int ParameterCount();
        IIndexReferencer GetParameterVar(int index);
        int GetIdentifier(ParseInfo parseInfo) => -1;
        IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues);
    }

    class FunctionMethodGroupInvoker : IMethodGroupInvoker
    {
        private readonly IFunctionHandler _functionHandler;

        public FunctionMethodGroupInvoker(IFunctionHandler functionHandler)
        {
            _functionHandler = functionHandler;
        }

        public int GetIdentifier(ParseInfo parseInfo) => parseInfo.TranslateInfo.GetComponent<LambdaGroup>().Add(_functionHandler);
        public IIndexReferencer GetParameterVar(int index) => _functionHandler.GetParameterVar(index);
        public int ParameterCount() => _functionHandler.ParameterCount();

        public IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues)
        {
            var buildController = new FunctionBuildController(actionSet, new CallHandler(parameterValues), new DefaultGroupDeterminer(new IFunctionHandler[] { _functionHandler }));
            return buildController.Build();
        }
    }

    class MacroMethodGroupInvoker : IMethodGroupInvoker
    {
        private readonly DefinedMacro _macro;

        public MacroMethodGroupInvoker(DefinedMacro macro)
        {
            _macro = macro;
        }

        public IIndexReferencer GetParameterVar(int index) => _macro.ParameterVars[index];
        public int ParameterCount() => _macro.Parameters.Length;
        public IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues) => _macro.Parse(actionSet, new MethodCall(parameterValues));
    }

    class GenericMethodHandler : IFunctionHandler
    {
        public CodeType ContainingType => null;
        private readonly IMethod _method;
        private readonly IIndexReferencer[] _parameterSavers;

        public GenericMethodHandler(IMethod method)
        {
            _method = method;

            _parameterSavers = new IIndexReferencer[_method.Parameters.Length];
            for (int i = 0; i < _parameterSavers.Length; i++)
                _parameterSavers[i] = new IndexReferencer(_method.Parameters[i].Name);
        }

        public string GetName() => _method.Name;
        public bool DoesReturnValue() => _method.DoesReturnValue;
        public IIndexReferencer GetParameterVar(int index) => _parameterSavers[index];
        public SubroutineInfo GetSubroutineInfo() => throw new NotImplementedException();
        public bool IsObject() => false;
        public bool IsRecursive() => false;
        public bool IsSubroutine() => false;
        public bool MultiplePaths() => false;
        public int ParameterCount() => _method.Parameters.Length;

        public void ParseInner(ActionSet actionSet)
        {
            var parameterValues = new IWorkshopTree[_parameterSavers.Length];
            for (int i = 0; i < _parameterSavers.Length; i++)   
                parameterValues[i] = actionSet.IndexAssigner[_parameterSavers[i]].GetVariable();

            var result = _method.Parse(actionSet, new MethodCall(parameterValues, new object[parameterValues.Length]));
            if (_method.DoesReturnValue)
                actionSet.ReturnHandler.ReturnValue(result);
        }
        public object UniqueIdentifier() => _method;
    }
}