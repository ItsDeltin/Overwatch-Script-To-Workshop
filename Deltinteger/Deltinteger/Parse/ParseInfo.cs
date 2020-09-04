using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class ParseInfo
    {
        public ScriptFile Script { get; }
        public DeltinScript TranslateInfo { get; }

        public CallInfo CurrentCallInfo { get; private set; }
        public IBreakContainer BreakHandler { get; private set; }
        public IContinueContainer ContinueHandler { get; private set; }
        public ITreeContextPart SourceExpression { get; private set; }
        public IRestrictedCallHandler RestrictedCallHandler { get; private set; }

        public ParseInfo(ScriptFile script, DeltinScript translateInfo)
        {
            Script = script;
            TranslateInfo = translateInfo;
        }
        private ParseInfo(ParseInfo other)
        {
            Script = other.Script;
            TranslateInfo = other.TranslateInfo;
            CurrentCallInfo = other.CurrentCallInfo;
            BreakHandler = other.BreakHandler;
            ContinueHandler = other.ContinueHandler;
            // SourceExpression = other.SourceExpression; TODO: Should this be here?
            RestrictedCallHandler = other.RestrictedCallHandler;
        }
        public ParseInfo SetCallInfo(CallInfo currentCallInfo) => new ParseInfo(this) { CurrentCallInfo = currentCallInfo, RestrictedCallHandler = currentCallInfo };
        public ParseInfo SetLoop(LoopAction loop) => new ParseInfo(this) { BreakHandler = loop, ContinueHandler = loop };
        public ParseInfo SetBreakHandler(IBreakContainer handler) => new ParseInfo(this) { BreakHandler = handler };
        public ParseInfo SetContinueHandler(IContinueContainer handler) => new ParseInfo(this) { ContinueHandler = handler };
        public ParseInfo SetSourceExpression(ITreeContextPart treePart) => new ParseInfo(this) { SourceExpression = treePart };
        public ParseInfo SetRestrictedCallHandler(IRestrictedCallHandler callHandler) => new ParseInfo(this) { RestrictedCallHandler = callHandler };

        /// <summary>Gets an IStatement from a StatementContext.</summary>
        /// <param name="scope">The scope the statement was created in.</param>
        /// <param name="statementContext">The context of the statement.</param>
        public IStatement GetStatement(Scope scope, IParseStatement statementContext)
        {
            IStatement statement = StatementFromContext(scope, statementContext);

            // Apply related output comment.
            if (statementContext is ICommentableStatement comment)
            {
                string text = comment.ActionComment.Text.Substring(1).Trim();
                DocRange range = comment.ActionComment.Range;
                statement.OutputComment(Script.Diagnostics, range, text);
            }

            return statement;
        }

        private IStatement StatementFromContext(Scope scope, IParseStatement statementContext)
        {
            switch (statementContext)
            {
                case VariableDeclaration declare: {
                    var newVar = new ScopedVariable(scope, new DefineContextHandler(this, declare));
                    return new DefineAction(newVar);
                }
                case Assignment assignment: return new SetVariableAction(this, scope, assignment);
                case Increment increment  : return new IncrementAction(this, scope, increment);
                case If @if            : return new IfAction(this, scope, @if);
                case While @while      : return new WhileAction(this, scope, @while);
                case For @for          : return new ForAction(this, scope, @for);
                case Foreach @foreach  : return new ForeachAction(this, scope, @foreach);
                case Return @return    : return new ReturnAction(this, scope, @return);
                case Delete delete     : return new DeleteAction(this, scope, delete);
                case Continue @continue: return new ContinueAction(this, @continue.Range);
                case Break @break      : return new BreakAction(this, @break.Range);
                case Switch @switch    : return new SwitchAction(this, scope, @switch);
                case Block @block      : return new BlockAction(this, scope, @block);
                // Expression statements (functions, new)
                case ExpressionStatement exprStatement:

                    // Parse the expression
                    var expr = GetExpression(scope, exprStatement.Expression, true, false);

                    if (!expr.IsStatement())
                    {
                        if (expr != null)
                            Script.Diagnostics.Error("Expressions can't be used as statements.", statementContext.Range);
                        return null;
                    }
                    // When IsStatement is true, expr should be castable to a statement.
                    return (IStatement)expr;

                default: return null;
            }
        }

        /// <summary>Gets an IExpression from an ExprContext.</summary>
        /// <param name="scope">The scope the expression was called in.</param>
        /// <param name="exprContext">The context of the expression/</param>
        /// <param name="selfContained">Determines if the expression is not an expression tree.</param>
        /// <param name="usedAsValue">Determines if the expression is being used as a value.</param>
        /// <param name="getter">The getter scope. Used for preserving scope through parameters.</param>
        /// <returns>An IExpression created from the ExprContext.</returns>
        public IExpression GetExpression(Scope scope, IParseExpression exprContext, bool selfContained = true, bool usedAsValue = true, Scope getter = null)
        {
            if (getter == null) getter = scope;

            switch (exprContext)
            {
                case NumberExpression number: return new NumberAction(Script, number);
                case BooleanExpression boolean: return new BoolAction(Script, boolean.Value);
                case NullExpression @null: return new NullAction();
                case StringExpression @string: return new StringAction(this, scope, @string);
                case Identifier identifier: return GetVariable(scope, getter, identifier, selfContained);
                case FunctionExpression method: return new CallMethodAction(this, scope, method, usedAsValue, getter);
                case NewExpression newObject: return new CreateObjectAction(this, scope, newObject);
                case BinaryOperatorExpression op:
                    if (op.IsDotExpression())
                        return new ExpressionTree(this, scope, op, usedAsValue);
                    else
                        return new OperatorAction(this, scope, op);
                case UnaryOperatorExpression op: return new UnaryOperatorAction(this, scope, op);
                case TernaryExpression op: return new TernaryConditionalAction(this, scope, op);
                case ValueInArray arrayIndex: return new ValueInArrayAction(this, scope, arrayIndex);
                case CreateArray createArray: return new CreateArrayAction(this, scope, createArray);
                case ExpressionGroup group: return GetExpression(scope, group.Expression);
                case TypeCast typeCast: return new TypeConvertAction(this, scope, typeCast);
                case ThisExpression @this: return new ThisAction(this, scope, @this);
                case DeltinScriptParser.E_rootContext root: return new RootAction(this.TranslateInfo);
                case DeltinScriptParser.E_baseContext @base: return new BaseAction(this, scope, @base);
                case DeltinScriptParser.E_isContext @is: return new IsAction(this, scope, @is);
                case LambdaExpression lambda: return new Lambda.LambdaAction(this, scope, lambda);
                // todo
                case MissingElement missing: throw new NotImplementedException("Todo: missing IExpression");
                default: throw new Exception($"Could not determine the expression type '{exprContext.GetType().Name}'.");
            }
        }

        /// <summary>Gets a variable or type from a VariableContext.</summary>
        /// <param name="scope">The scope the variable was called in.</param>
        /// <param name="getter">The getter scope.</param>
        /// <param name="variableContext">The context of the variable.</param>
        /// <param name="selfContained">Wether the variable was not called in an expression tree.</param>
        /// <returns>An IExpression created from the context.</returns>
        public IExpression GetVariable(Scope scope, Scope getter, Identifier variableContext, bool selfContained)
        {
            // Get the variable name and range.
            string variableName = variableContext.Token.Text;
            DocRange variableRange = variableContext.Token.Range;

            // Get the variable.
            IVariable element = scope.GetVariable(variableName, getter, Script.Diagnostics, variableRange);
            if (element == null) return null;
            
            // Additional syntax checking.
            return new VariableApply(this).Apply(element, ExpressionIndexArray(getter, variableContext.Index), variableRange);
        }

        /// <summary>Gets an IExpression[] from a DeltinScriptParser.ArrayContext.</summary>
        /// <param name="scope">The scope used to parse the index values.</param>
        /// <param name="arrayContext">The context of the array.</param>
        /// <returns>An IExpression[] of each indexer in the chain. Will return null if arrayContext is null.</returns>
        public IExpression[] ExpressionIndexArray(Scope scope, List<ArrayIndex> arrayContext)
        {
            if (arrayContext == null) return null;

            IExpression[] index = null;
            if (arrayContext != null)
            {
                index = new IExpression[arrayContext.Count];
                for (int i = 0; i < index.Length; i++)
                    index[i] = GetExpression(scope, arrayContext[i].Expression);
            }
            return index;
        }

        /// <summary>Creates a macro from a Define_macroContext.</summary>
        /// <param name="objectScope">The scope of the macro if there is no static attribute.</param>
        /// <param name="staticScope">The scope of the macro if there is a static attribute.</param>
        /// <param name="macroContext">The context of the macro.</param>
        /// <returns>A DefinedMacro if the macro has parameters, a MacroVar if there are no parameters.</returns>
        public IScopeable GetMacro(Scope objectScope, Scope staticScope, MacroFunctionContext macroContext)
        {
            // Get the return type.
            CodeType returnType = CodeType.GetCodeTypeFromContext(this, macroContext.Type);

            IScopeable newMacro = new DefinedMacro(this, objectScope, staticScope, macroContext, returnType);

            TranslateInfo.ApplyBlock((IApplyBlock)newMacro);
            return newMacro;
        }

        public IScopeable GetMacro(Scope objectScope, Scope staticScope, MacroVarDeclaration macroContext)
        {
            // Get the return type.
            CodeType returnType = CodeType.GetCodeTypeFromContext(this, macroContext.Type);

            IScopeable newMacro = new MacroVar(this, objectScope, staticScope, macroContext, returnType);

            TranslateInfo.ApplyBlock((IApplyBlock)newMacro);
            return newMacro;
        }
    
        public Location GetLocation(DocRange range) => new Location(Script.Uri, range);
    }

    public class VariableApply
    {
        private readonly ParseInfo _parseInfo;

        public VariableApply(ParseInfo parseInfo)
        {
            _parseInfo = parseInfo;
        }

        public IExpression Apply(IVariable variable, IExpression[] index, DocRange variableRange)
        {
            // Callable
            if (variable is ICallable callable) Call(callable, variableRange);
            
            // IIndexReferencers are wrapped by CallVariableActions.
            if (variable is IIndexReferencer referencer)
            {
                // If the type of the variable being called is Player, check if the variable is calling Event Player.
                // If the source expression is null, Event Player is used by default.
                // Otherwise, confirm that the source expression is returning the player variable scope.
                if (referencer.VariableType == VariableType.Player)
                    EventPlayerRestrictedCall(new RestrictedCall(RestrictedCallType.EventPlayer, _parseInfo.GetLocation(variableRange), RestrictedCall.Message_EventPlayerDefault(referencer.Name)));

                return new CallVariableAction(referencer, index);
            }

            // Check value in array.
            if (index != null)
            {
                if (!variable.CanBeIndexed)
                    Error("This variable type cannot be indexed.", variableRange);
                else
                    return new ValueInArrayAction(_parseInfo, (IExpression)variable, index);
            }

            return (IExpression)variable;
        }

        protected virtual void Call(ICallable callable, DocRange range) => callable.Call(_parseInfo, range);
        protected virtual void EventPlayerRestrictedCall(RestrictedCall restrictedCall) => _parseInfo.RestrictedCallHandler.RestrictedCall(restrictedCall);
        public virtual void Error(string message, DocRange range) => _parseInfo.Script.Diagnostics.Error(message, range);
    }
}