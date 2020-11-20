using System.Linq;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.Parse.Overload;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public interface IInvokeInfo
    {
        IInvokeResult Invoke(InvokeData invokeInfo);
    }

    class MethodGroupInvokeInfo : IInvokeInfo
    {
        public IInvokeResult Invoke(InvokeData invokeInfo)
        {
            var parseInfo = invokeInfo.ParseInfo;
            var groupCall = (CallMethodGroup)invokeInfo.Target;
            var group = groupCall.Group;

            // Make an OverloadChooser to choose an Overload.
            var overloadChooser = new OverloadChooser(group.Functions.Select(f => new MethodOverload(f)).ToArray(), parseInfo, invokeInfo.Scope, invokeInfo.Getter, invokeInfo.TargetRange, invokeInfo.CallRange, new OverloadError("method '" + group.Name + "'"));
            // Apply the parameters.
            overloadChooser.Apply(invokeInfo.Context.Parameters, groupCall.TypeArgs.Length > 0, groupCall.TypeArgs);
        
            // Get the best function.
            var callingMethod = (IMethod)overloadChooser.Overload;
            var result = new FunctionInvokeResult(parseInfo, invokeInfo.TargetRange, invokeInfo.UsedAsExpression, callingMethod, overloadChooser.AdditionalData, overloadChooser.Values, overloadChooser.AdditionalParameterData, overloadChooser.Match);
            var typeArgLinker = overloadChooser.Match?.TypeArgLinker;

            // CallingMethod may be null if no good functions are found.
            if (callingMethod != null)
            {
                result.ReturnType = callingMethod.CodeType?.GetRealerType(typeArgLinker);

                // If the function's block needs to be applied, check optional restricted calls when 'Applied()' runs.
                if (callingMethod is IApplyBlock applyBlock)
                    applyBlock.OnBlockApply(result);
                else // Otherwise, the optional restricted calls can be resolved right away.
                {
                    // Get optional parameter's restricted calls.
                    overloadChooser.Match?.CheckOptionalsRestrictedCalls(parseInfo, invokeInfo.TargetRange);
                }

                // Check if the function can be called in parallel.
                if (parseInfo.AsyncInfo != null)
                {
                    if (!callingMethod.Attributes.Parallelable)
                        parseInfo.AsyncInfo.Reject($"The method '{callingMethod.Name}' cannot be called in parallel");
                    else
                        parseInfo.AsyncInfo.Accept();
                }
                
                parseInfo.Script.AddHover(invokeInfo.Context.Range, IMethod.Hover(typeArgLinker, callingMethod.Name, callingMethod.CodeType, groupCall.TypeArgs, callingMethod.Parameters).ToString(true));
            }

            return result;
        }
    }

    class LambdaInvokeInfo : IInvokeInfo
    {
        private readonly PortableLambdaType _lambdaType;

        public LambdaInvokeInfo(PortableLambdaType lambdaType)
        {
            _lambdaType = lambdaType;
        }

        public IInvokeResult Invoke(InvokeData invokeInfo)
        {
            var parseInfo = invokeInfo.ParseInfo;
            
            // Create the overload chooser for the invoke function.
            var overloadChooser = new OverloadChooser(
                new MethodOverload[] { new MethodOverload(_lambdaType.InvokeFunction) },
                parseInfo,
                invokeInfo.Scope,
                invokeInfo.Getter,
                invokeInfo.TargetRange,
                invokeInfo.CallRange,
                new OverloadError("lambda '" + _lambdaType.GetName() + "'")
            );
            // Apply the parameters.
            overloadChooser.Apply(invokeInfo.Context.Parameters, false, null);

            var invoke = (LambdaInvoke)overloadChooser.Overload;
            invoke?.Call(parseInfo, invokeInfo.TargetRange);

            return new LambdaInvokeResult(invoke, overloadChooser.Values, invokeInfo.Target);
            // return new FunctionInvokeResult(parseInfo, invokeInfo.TargetRange, invokeInfo.UsedAsExpression, invoke, overloadChooser.Values, overloadChooser.AdditionalParameterData, overloadChooser.Match);
        }
    }

    public class InvokeData
    {
        public FunctionExpression Context { get; }
        public ParseInfo ParseInfo { get; }
        public IExpression Target { get; }
        public Scope Scope { get; }
        public Scope Getter { get; }
        public DocRange TargetRange { get; }
        public DocRange CallRange { get; }
        public bool UsedAsExpression { get; }

        public InvokeData(ParseInfo parseInfo, FunctionExpression context, IExpression target, Scope scope, Scope getter, bool usedAsExpression)
        {
            ParseInfo = parseInfo;
            Context = context;
            TargetRange = context.Target.Range;
            CallRange = context.Range;
            Target = target;
            Scope = scope;
            Getter = getter;
            UsedAsExpression = usedAsExpression;
        }
    }

    public interface IInvokeResult
    {
        IMethod Function { get; }
        IExpression[] ParameterValues { get; }
        object[] AdditionalParameterData { get; }
        CodeType ReturnType { get; }
        IWorkshopTree Parse(ActionSet actionSet);
        void SetComment(string comment);

        IWorkshopTree[] GetParameterValuesAsWorkshop(ActionSet actionSet)
        {
            if (ParameterValues == null) return new IWorkshopTree[0];

            IWorkshopTree[] parameterValues = new IWorkshopTree[ParameterValues.Length];
            for (int i = 0; i < ParameterValues.Length; i++)
                parameterValues[i] = Function.Parameters[i].Parse(actionSet, ParameterValues[i], AdditionalParameterData[i]);
            return parameterValues;
        }
    }

    class FunctionInvokeResult : IInvokeResult, IBlockListener, IOnBlockApplied
    {
        public CodeType ReturnType { get; set; }
        public IMethod Function { get; }
        public IExpression[] ParameterValues { get; }
        public object[] AdditionalParameterData { get; }
        private readonly object _additionalData;
        private readonly OverloadMatch _match;
        private readonly ParseInfo _parseInfo;
        private readonly DocRange _targetRange;
        private readonly bool _usedAsExpression;
        private readonly AsyncInfo _asyncInfo;
        private string _comment;

        public FunctionInvokeResult(ParseInfo parseInfo, DocRange targetRange, bool usedAsExpression, IMethod function, object additionalData, IExpression[] parameterValues, object[] additionalParameterData, OverloadMatch match)
        {
            Function = function;
            ParameterValues = parameterValues;
            AdditionalParameterData = additionalParameterData;
            _additionalData = additionalData;
            _match = match;
            _parseInfo = parseInfo;
            _targetRange = targetRange;
            _usedAsExpression = usedAsExpression;
            _asyncInfo = parseInfo.AsyncInfo;
        }

        public IWorkshopTree Parse(ActionSet actionSet) => Function.Parse(actionSet, new MethodCall(((IInvokeResult)this).GetParameterValuesAsWorkshop(actionSet), AdditionalParameterData) {
            ParallelMode = _asyncInfo?.ParallelMode ?? CallParallel.NoParallel,
            ActionComment = _comment,
            AdditionalData = _additionalData
        });

        public void OnBlockApply(IOnBlockApplied onBlockApplied)
        {
            // If the function being called is an IApplyBlock, bridge onBlockApply to it.
            if (Function is IApplyBlock applyBlock)
                applyBlock.OnBlockApply(onBlockApplied);
            // Otherwise, instantly apply.
            else
                onBlockApplied.Applied();
        }

        public void Applied()
        {
            if (_usedAsExpression && !Function.DoesReturnValue)
                _parseInfo.Script.Diagnostics.Error("The chosen overload for " + Function.Name + " does not return a value.", _targetRange);
            
            // Get optional parameter's restricted calls.
            _match?.CheckOptionalsRestrictedCalls(_parseInfo, _targetRange);
            
            // Check callinfo :)
            foreach (RestrictedCallType type in ((IApplyBlock)Function).CallInfo.GetRestrictedCallTypes())
                _parseInfo.RestrictedCallHandler.RestrictedCall(new RestrictedCall(type, _parseInfo.GetLocation(_targetRange), RestrictedCall.Message_FunctionCallsRestricted(Function.Name, type)));
        }

        public void SetComment(string comment) => _comment = comment;
    }

    class LambdaInvokeResult : IInvokeResult
    {
        public LambdaInvoke Function { get; }
        IMethod IInvokeResult.Function => this.Function;
        public CodeType ReturnType => Function.CodeType;
        public IExpression[] ParameterValues { get; }
        public object[] AdditionalParameterData { get; }
        private readonly IExpression _target;
        private string _comment;

        public LambdaInvokeResult(LambdaInvoke function, IExpression[] parameterValues, IExpression target)
        {
            Function = function;
            ParameterValues = parameterValues;
            AdditionalParameterData = new object[parameterValues?.Length ?? 0];
            _target = target;
        }

        public IWorkshopTree Parse(ActionSet actionSet) =>
            Function.Parse(
                actionSet.New(_target.Parse(actionSet)),
                new MethodCall(
                    ((IInvokeResult)this).GetParameterValuesAsWorkshop(actionSet),
                    AdditionalParameterData
                )
            );

        public void SetComment(string comment) => _comment = comment;
    }

    public class ResolveInvokeInfo
    {
        public IInvokeInfo InvokeInfo { get; private set; }
        public bool WasResolved { get; private set; }

        public void Resolve(IInvokeInfo invokeInfo)
        {
            InvokeInfo = invokeInfo;
            WasResolved = true;
        }
    }
}