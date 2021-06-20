using System;

namespace Deltin.Deltinteger.Parse
{
    abstract class ConstantExpressionResolver
    {
        public virtual void On(IExpression expression)
        {
            // todo: do something if the expression has a GetMeta or GetContent.
            switch (expression)
            {
                // Expression is a CallVariableAction, get the initial value.
                case CallVariableAction callVariableAction:
                    OnVariable(callVariableAction);
                    break;
                
                // Expression is a CallMethodAction, get the method's single return value.
                case CallMethodAction callMethodAction:
                    OnMethod(callMethodAction);
                    break;
                
                // If the expression is an ExpressionTree, get the last value.
                case ExpressionTree expressionTree:
                    ContinueIfExists(expressionTree, expressionTree.Result);
                    break;
                
                // Nothing compatible found, finish.
                default:
                    Complete(expression);
                    break;
            }
        }

        protected virtual void OnVariable(CallVariableAction variableCall)
        {
            if (variableCall.Calling is VariableInstance var)
                var.Var.ValueReady.OnReady(() => ContinueIfExists(variableCall, var.Var.InitialValue));
            else
                Complete(variableCall);
        }

        protected virtual void OnMethod(CallMethodAction methodCall)
        {
            if (methodCall.Result == null)
                Complete(methodCall);
            else
                methodCall.Result.GetConstantTarget(result => ContinueIfExists(methodCall, result));
        }

        void ContinueIfExists(IExpression source, IExpression target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) Complete(source);
            On(target);
        }

        protected abstract void Complete(IExpression expression);

        public static void Resolve(IExpression expression, Action<IExpression> callback) => new GenericConstantExpressionResolver(callback).On(expression);
    }

    class GenericConstantExpressionResolver : ConstantExpressionResolver
    {
        readonly Action<IExpression> _action;
        public GenericConstantExpressionResolver(Action<IExpression> action) => _action = action;
        protected override void Complete(IExpression expression) => _action(expression);
    }

    class ParameterExpressionResolver : GenericConstantExpressionResolver
    {
        public ParameterExpressionResolver(Action<IExpression> action) : base(action) {}

        protected override void OnVariable(CallVariableAction callVariableAction)
        {
            if (callVariableAction.Calling is VariableInstance variableInstance && variableInstance.Var.BridgeInvocable != null)
                Complete(callVariableAction);
            else
                base.OnVariable(callVariableAction);
        }
    }

    class OnBlockApplied : IOnBlockApplied
    {
        private readonly Action _action;

        public OnBlockApplied(Action action)
        {
            _action = action;
        }

        public void Applied() => _action.Invoke();
    }
}