using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class HookVar : IndexReferencer
    {
        public bool WasSet { get; private set; }
        public IExpression HookValue { get; private set; }
        public Action<IExpression> SetHook { get; } = null;

        public HookVar(string name, CodeType type, Action<IExpression> setHook) : base(name)
        {
            CodeType = type;
            SetHook = setHook;
        }

        public void TrySet(ParseInfo parseInfo, IExpression value, DocRange expressionRange)
        {
            // Check if the hook was already set.
            if (WasSet)
                parseInfo.Script.Diagnostics.Error("Hooks cannot be set twice.", expressionRange);
            // Check if the given value implements the expected value.
            else if (!value.Type().Implements(CodeType))
                parseInfo.Script.Diagnostics.Error($"Expected a value of type {CodeType.GetName()}.", expressionRange);
            // Set the hook.
            else
            {
                HookValue = value;
                SetHook?.Invoke(value);
            }

            WasSet = true;
        }

        public override bool Settable() => false;

        public static void GetHook(ParseInfo parseInfo, Scope scope, Hook context)
        {
            parseInfo = parseInfo.SetCallInfo(new CallInfo(parseInfo.Script));

            // Get the hook variable's expression.
            IExpression variableExpression = parseInfo.GetExpression(scope, context.Variable);

            // Get the hook value.
            IExpression valueExpression = parseInfo.GetExpression(scope, context.Value);

            // Resolve the variable.
            VariableResolve resolvedVariable = new VariableResolve(new VariableResolveOptions() {
                // Not indexable
                CanBeIndexed = false,
                // Hook variables are not settable.
                ShouldBeSettable = false
            }, variableExpression, context.Variable.Range, parseInfo.Script.Diagnostics);

            if (valueExpression == null) return;

            // Check if the resolved variable is a HookVar.
            if (resolvedVariable.SetVariable?.Calling is HookVar hookVar)
                // If it is, set the hook.
                hookVar.TrySet(parseInfo, valueExpression, context.Value.Range);
            else
                // Not a hook variable.
                parseInfo.Script.Diagnostics.Error("Expected a hook variable.", context.Variable.Range);
        }
    }
}