using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class HookVar : IVariable, IVariableInstance
    {
        public string Name { get; }
        public ICodeTypeSolver CodeType { get; }
        public bool WasSet { get; private set; }
        public IExpression HookValue { get; private set; }
        public Action<IExpression> SetHook { get; } = null;
        public VariableType VariableType => VariableType.ElementReference;
        public IVariable Provider => this;
        public bool WholeContext => true;
        public LanguageServer.Location DefinedAt => null;
        public AccessLevel AccessLevel => AccessLevel.Public;
        public MarkupBuilder Documentation { get; set; }
        public IVariableInstanceAttributes Attributes { get; } = new VariableInstanceAttributes() {
            CanBeSet = false, StoreType = StoreType.None
        };

        public HookVar(string name, CodeType type, Action<IExpression> setHook)
        {
            Name = name;
            CodeType = type;
            SetHook = setHook;
        }

        public void TrySet(ParseInfo parseInfo, IExpression value, DocRange expressionRange)
        {
            var type = CodeType.GetCodeType(parseInfo.TranslateInfo);

            // Check if the hook was already set.
            if (WasSet)
                parseInfo.Script.Diagnostics.Error("Hooks cannot be set twice.", expressionRange);
            // Check if the given value implements the expected value.
            else if (SemanticsHelper.ExpectValueType(parseInfo, value, type, expressionRange))
            {
                // Set the hook.
                HookValue = value;
                SetHook?.Invoke(value);
            }

            WasSet = true;
        }

        public static void GetHook(ParseInfo parseInfo, Scope scope, Hook context)
        {
            parseInfo = parseInfo.SetCallInfo(new CallInfo(parseInfo.Script));

            // Get the hook variable's expression.
            IExpression variableExpression = parseInfo.GetExpression(scope, context.Variable);

            // Resolve the variable.
            VariableResolve resolvedVariable = new VariableResolve(new VariableResolveOptions()
            {
                // Not indexable
                CanBeIndexed = false,
                // Hook variables are not settable.
                ShouldBeSettable = false
            }, variableExpression, context.Variable.Range, parseInfo.Script.Diagnostics);

            // Check if the resolved variable is a HookVar.
            if (resolvedVariable.SetVariable?.Calling is HookVar hookVar)
            {
                 // Get the hook value.
                IExpression valueExpression = parseInfo.SetExpectType(hookVar.CodeType.GetCodeType(parseInfo.TranslateInfo)).GetExpression(scope, context.Value);

                if (valueExpression == null) return;

                // If it is, set the hook.
                hookVar.TrySet(parseInfo, valueExpression, context.Value.Range);
            }
            else
                // Not a hook variable.
                parseInfo.Script.Diagnostics.Error("Expected a hook variable.", context.Variable.Range);
        }

        public IVariableInstance GetInstance(CodeType definedIn, InstanceAnonymousTypeLinker genericsLinker) => this;
        public IGettableAssigner GetAssigner(GetVariablesAssigner getAssigner) => throw new NotImplementedException();
        public IVariableInstance GetDefaultInstance(CodeType definedIn) => this;
        public IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker) => throw new NotImplementedException();
        public void AddDefaultInstance(IScopeAppender scopeAppender) => throw new NotImplementedException();
    }
}