using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class AsyncInfo
    {
        public CallParallel ParallelMode { get; }
        private bool _canBeCalledInParallel = false;
        private string _message;

        public AsyncInfo(CallParallel parallelMode)
        {
            ParallelMode = parallelMode;
        }

        public void Accept() => _canBeCalledInParallel = true;
        public void Reject(string message) => _message = message;

        public static IExpression ParseAsync(ParseInfo parseInfo, Scope scope, AsyncContext context, bool usedAsValue)
        {
            AsyncInfo asyncInfo = new AsyncInfo(context.IgnoreIfRunning ? CallParallel.AlreadyRunning_DoNothing : CallParallel.AlreadyRunning_RestartRule);
            var result = parseInfo.SetAsyncInfo(asyncInfo).GetExpression(scope, context.Expression, usedAsValue: usedAsValue);

            if (!asyncInfo._canBeCalledInParallel)
                parseInfo.Script.Diagnostics.Error(asyncInfo._message ?? "This expression cannot be executed asynchronously", context.AsyncToken.Range);

            return result;
        }
    }
}