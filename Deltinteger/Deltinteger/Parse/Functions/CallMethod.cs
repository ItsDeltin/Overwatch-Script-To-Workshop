using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class CallMethodAction : IExpression, IStatement, IOnBlockApplied
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

            var options = scope.GetMethodsByName(methodName);
            if (options.Length == 0)
                parseInfo.Script.Diagnostics.Error($"No method by the name of '{methodName}' exists in the current context.", NameRange);
            else
            {
                OverloadChooser = new OverloadChooser(options, parseInfo, scope, getter, NameRange, DocRange.GetRange(methodContext), new OverloadError("method '" + methodName + "'"));

                if (methodContext.call_parameters() != null) OverloadChooser.SetContext(methodContext.call_parameters());
                else if (methodContext.picky_parameters() != null) OverloadChooser.SetContext(methodContext.picky_parameters());
                else OverloadChooser.SetContext();
            
                CallingMethod = (IMethod)OverloadChooser.Overload;
                ParameterValues = OverloadChooser.Values;

                if (CallingMethod != null)
                {
                    if (CallingMethod is DefinedFunction definedFunction)
                    {
                        definedFunction.OnBlockApply(this);
                        definedFunction.Call(parseInfo, NameRange);
                        parseInfo.CurrentCallInfo?.Call(definedFunction, NameRange);
                    }

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
    }

    public enum CallParallel
    {
        NoParallel,
        AlreadyRunning_RestartRule,
        AlreadyRunning_DoNothing
    }
}