using System;
using Deltin.Deltinteger.LanguageServer;

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
            // TODO: IMPORTANT: Null check HookValue type
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

        public static void GetHook(ParseInfo parseInfo, Scope scope, DeltinScriptParser.HookContext context)
        {
            // Get the hook variable's expression.
            IExpression variableExpression = parseInfo.GetExpression(scope, context.var);

            // Get the hook value.
            IExpression valueExpression = parseInfo.GetExpression(scope, context.value);

            // Resolve the variable.
            VariableResolve resolvedVariable = new VariableResolve(new VariableResolveOptions() {
                // Not indexable
                CanBeIndexed = false,
                // Hook variables are not settable.
                ShouldBeSettable = false
            }, variableExpression, DocRange.GetRange(context.var), parseInfo.Script.Diagnostics);

            if (valueExpression == null) return;

            // Check if the resolved variable is a HookVar.
            if (resolvedVariable.SetVariable?.Calling is HookVar hookVar)
                // If it is, set the hook.
                hookVar.TrySet(parseInfo, valueExpression, DocRange.GetRange(context.value));
            else
                // Not a hook variable.
                parseInfo.Script.Diagnostics.Error("Expected a hook variable.", DocRange.GetRange(context.var));
        }
    }
}