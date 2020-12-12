using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class CallMethodAction : IExpression, IStatement
    {
        public IExpression[] ParameterValues => Result?.ParameterValues;
        public IMethod CallingMethod => Result?.Function;
        public IInvokeResult Result { get; }
        private readonly ParseInfo _parseInfo;

        public CallMethodAction(ParseInfo parseInfo, Scope scope, FunctionExpression methodContext, bool usedAsExpression, Scope getter)
        {
            _parseInfo = parseInfo;

            // Get the invoke target.
            var resolveInvoke = new ResolveInvokeInfo();
            var target = parseInfo.SetInvokeInfo(resolveInvoke).GetExpression(scope, methodContext.Target);

            // Get the invoke info.
            IInvokeInfo invokeInfo = resolveInvoke.WasResolved ? resolveInvoke.InvokeInfo : target.Type()?.InvokeInfo;
            if (invokeInfo != null)
                Result = invokeInfo.Invoke(new InvokeData(parseInfo, methodContext, target, scope, getter, usedAsExpression));
            // If the target is not invocable and the target is not a missing element, add error.
            else if (target is MissingElementAction == false)
                parseInfo.Script.Diagnostics.Error("Method name expected", methodContext.Target.Range);
        }

        public Scope ReturningScope()
        {
            if (Result == null) return null;

            if (Result.ReturnType == null)
                return _parseInfo.TranslateInfo.PlayerVariableScope;
            else
                return Result.ReturnType.GetObjectScope();
        }

        public CodeType Type() => Result?.ReturnType;

        // IStatement
        public void Translate(ActionSet actionSet) => Result.Parse(actionSet);
        // IExpression
        public IWorkshopTree Parse(ActionSet actionSet) => Result.Parse(actionSet);

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment) => Result.SetComment(comment);

        public bool IsStatement() => true;
    }

    public enum CallParallel
    {
        NoParallel,
        AlreadyRunning_RestartRule,
        AlreadyRunning_DoNothing
    }
}