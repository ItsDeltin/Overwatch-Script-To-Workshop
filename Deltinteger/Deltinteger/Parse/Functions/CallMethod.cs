using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Decompiler.Json;

namespace Deltin.Deltinteger.Parse
{
    public class CallMethodAction : IExpression, IStatement, IBlockListener, IOnBlockApplied
    {
        public IMethod CallingMethod { get; }
        private OverloadChooser OverloadChooser { get; }
        public IExpression[] ParameterValues { get; }
        public CallParallel Parallel { get; }

        private ParseInfo parseInfo { get; }
        private DocRange NameRange { get; }
        private bool UsedAsExpression { get; }

        private string Comment;

        public CallMethodAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.MethodContext methodContext, bool usedAsExpression, Scope getter)
        {
            this.parseInfo = parseInfo;
            string methodName = methodContext.PART().GetText();
            NameRange = DocRange.GetRange(methodContext.PART());

            UsedAsExpression = usedAsExpression;

            if (methodContext.ASYNC() != null)
            {
                if (methodContext.NOT() == null) Parallel = CallParallel.AlreadyRunning_RestartRule;
                else Parallel = CallParallel.AlreadyRunning_DoNothing;
            }

            // Get all functions with the same name in the current scope.
            var options = scope.GetMethodsByName(methodName);

            // If none are found, throw a syntax error.
            if (options.Length == 0)
                parseInfo.Script.Diagnostics.Error($"No method by the name of '{methodName}' exists in the current context.", NameRange);
            else
            {
                // Make an OverloadChooser to choose an Overload.
                OverloadChooser = new OverloadChooser(options, parseInfo, scope, getter, NameRange, DocRange.GetRange(methodContext), new OverloadError("method '" + methodName + "'"));
                // Apply the parameters.
                OverloadChooser.Apply(methodContext.call_parameters());
            
                // Get the best function.
                CallingMethod = (IMethod)OverloadChooser.Overload;
                ParameterValues = OverloadChooser.Values;

                // CallingMethod may be null if no good functions are found.
                if (CallingMethod != null)
                {
                    CallingMethod.Call(parseInfo, NameRange);

                    // If the function's block needs to be applied, check optional restricted calls when 'Applied()' runs.
                    if (CallingMethod is IApplyBlock applyBlock)
                        applyBlock.OnBlockApply(this);
                    else // Otherwise, the optional restricted calls can be resolved right away.
                    {
                        // Get optional parameter's restricted calls.
                        OverloadChooser.Match?.CheckOptionalsRestrictedCalls(parseInfo, NameRange);
                    }

                    // Check if the function can be called in parallel.
                    if (Parallel != CallParallel.NoParallel && !CallingMethod.Attributes.Parallelable)
                        parseInfo.Script.Diagnostics.Error($"The method '{CallingMethod.Name}' cannot be called in parallel.", NameRange);
                    
                    parseInfo.Script.AddHover(DocRange.GetRange(methodContext), CallingMethod.GetLabel(true));
                }
            }
        }

        public void Applied()
        {
            if (UsedAsExpression && !CallingMethod.DoesReturnValue)
                parseInfo.Script.Diagnostics.Error("The chosen overload for " + CallingMethod.Name + " does not return a value.", NameRange);
            
            // Get optional parameter's restricted calls.
            OverloadChooser.Match?.CheckOptionalsRestrictedCalls(parseInfo, NameRange);
            
            // Check callinfo :)
            foreach (RestrictedCallType type in ((IApplyBlock)CallingMethod).CallInfo.GetRestrictedCallTypes())
                parseInfo.RestrictedCallHandler.RestrictedCall(new RestrictedCall(type, parseInfo.GetLocation(NameRange), RestrictedCall.Message_FunctionCallsRestricted(CallingMethod.Name, type)));
        }

        public Scope ReturningScope()
        {
            if (CallingMethod == null) return null;

            if (CallingMethod.ReturnType == null)
                return parseInfo.TranslateInfo.PlayerVariableScope;
            else
                return CallingMethod.ReturnType.GetObjectScope();
        }

        public CodeType Type() => CallingMethod?.ReturnType;
    
        // IStatement
        public void Translate(ActionSet actionSet)
        {
            CallingMethod.Parse(actionSet.New(NameRange), GetMethodCall(actionSet));
        }

        // IExpression
        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return CallingMethod.Parse(actionSet.New(NameRange), GetMethodCall(actionSet));
        }

        private MethodCall GetMethodCall(ActionSet actionSet)
        {
            return new MethodCall(
                GetParameterValuesAsWorkshop(actionSet),
                OverloadChooser.AdditionalParameterData
            )
            {
                CallParallel = Parallel,
                ActionComment = Comment
            };
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment)
        {
            Comment = comment;
        }

        private IWorkshopTree[] GetParameterValuesAsWorkshop(ActionSet actionSet)
        {
            if (ParameterValues == null) return new IWorkshopTree[0];

            IWorkshopTree[] parameterValues = new IWorkshopTree[ParameterValues.Length];
            for (int i = 0; i < ParameterValues.Length; i++)
                parameterValues[i] = OverloadChooser.Overload.Parameters[i].Parse(actionSet, ParameterValues[i], OverloadChooser.AdditionalParameterData[i]);
            return parameterValues;
        }

        public void OnBlockApply(IOnBlockApplied onBlockApplied)
        {
            // If the function being called is an IApplyBlock, bridge onBlockApply to it.
            if (CallingMethod is IApplyBlock applyBlock)
                applyBlock.OnBlockApply(onBlockApplied);
            // Otherwise, instantly apply.
            else
                onBlockApplied.Applied();
        }

        public JsonAction ToJsonAction() => new JsonAction() { Function = new JsonCallFunction() {
            Name = CallingMethod.Name,
            Expressions = ParameterValues.Select(pv => pv.ToJson()).ToArray()
        }};
        public JsonExpression ToJson() => new JsonExpression() { Function = new JsonCallFunction() {
            Name = CallingMethod.Name,
            Expressions = ParameterValues.Select(pv => pv.ToJson()).ToArray()
        }};
    }

    public enum CallParallel
    {
        NoParallel,
        AlreadyRunning_RestartRule,
        AlreadyRunning_DoNothing
    }
}