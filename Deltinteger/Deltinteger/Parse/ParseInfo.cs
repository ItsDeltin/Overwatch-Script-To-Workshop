using System;
using Deltin.Deltinteger.LanguageServer;

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
        public IStatement GetStatement(Scope scope, DeltinScriptParser.Documented_statementContext statementContext)
        {
            IStatement statement = StatementFromContext(scope, statementContext);

            // Apply related output comment.
            if (statementContext.DOCUMENTATION() != null)
            {
                string text = statementContext.DOCUMENTATION().GetText().Substring(1).Trim();
                DocRange range = DocRange.GetRange(statementContext.DOCUMENTATION());
                statement.OutputComment(Script.Diagnostics, range, text);
            }

            return statement;
        }

        private IStatement StatementFromContext(Scope scope, DeltinScriptParser.Documented_statementContext statementContext)
        {
            switch (statementContext.statement())
            {
                case DeltinScriptParser.S_defineContext define    : {
                    var newVar = new ScopedVariable(scope, new DefineContextHandler(this, define.define()));
                    return new DefineAction(newVar);
                }
                case DeltinScriptParser.S_methodContext method    : return new CallMethodAction(this, scope, method.method(), false, scope);
                case DeltinScriptParser.S_varsetContext varset    : return new SetVariableAction(this, scope, varset.varset());
                case DeltinScriptParser.S_exprContext s_expr      : {

                    var expr = GetExpression(scope, s_expr.expr(), true, false);
                    if (expr is ExpressionTree == false || (((ExpressionTree)expr)?.Result is IStatement == false && (((ExpressionTree)expr)?.Completed ?? false)))
                    {
                        if (expr != null)
                            Script.Diagnostics.Error("Expressions can't be used as statements.", DocRange.GetRange(statementContext));
                        return null;
                    }
                    else return (ExpressionTree)expr;
                }
                case DeltinScriptParser.S_ifContext s_if            : return new IfAction(this, scope, s_if.@if());
                case DeltinScriptParser.S_whileContext s_while      : return new WhileAction(this, scope, s_while.@while());
                case DeltinScriptParser.S_forContext s_for          : return new ForAction(this, scope, s_for.@for());
                case DeltinScriptParser.S_for_autoContext s_forAuto : return new AutoForAction(this, scope, s_forAuto.for_auto());
                case DeltinScriptParser.S_foreachContext s_foreach  : return new ForeachAction(this, scope, s_foreach.@foreach());
                case DeltinScriptParser.S_returnContext s_return    : return new ReturnAction(this, scope, s_return.@return());
                case DeltinScriptParser.S_deleteContext s_delete    : return new DeleteAction(this, scope, s_delete.delete());
                case DeltinScriptParser.S_continueContext s_continue: return new ContinueAction(this, DocRange.GetRange(s_continue));
                case DeltinScriptParser.S_breakContext s_break      : return new BreakAction(this, DocRange.GetRange(s_break));
                case DeltinScriptParser.S_switchContext s_switch    : return new SwitchAction(this, scope, s_switch.@switch());
                case DeltinScriptParser.S_blockContext s_block: return new BlockAction(this, scope, s_block);
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
        public IExpression GetExpression(Scope scope, DeltinScriptParser.ExprContext exprContext, bool selfContained = true, bool usedAsValue = true, Scope getter = null)
        {
            if (getter == null) getter = scope;

            switch (exprContext)
            {
                case DeltinScriptParser.E_numberContext number: return new NumberAction(Script, number.number());
                case DeltinScriptParser.E_trueContext @true: return new BoolAction(Script, true);
                case DeltinScriptParser.E_falseContext @false: return new BoolAction(Script, false);
                case DeltinScriptParser.E_nullContext @null: return new NullAction();
                case DeltinScriptParser.E_stringContext @string: return new StringAction(this, @string.@string());
                case DeltinScriptParser.E_formatted_stringContext formattedString: return new StringAction(this, scope, formattedString.formatted_string());
                case DeltinScriptParser.E_variableContext variable: return GetVariable(scope, getter, variable.variable(), selfContained);
                case DeltinScriptParser.E_methodContext method: return new CallMethodAction(this, scope, method.method(), usedAsValue, getter);
                case DeltinScriptParser.E_new_objectContext newObject: return new CreateObjectAction(this, scope, newObject.create_object());
                case DeltinScriptParser.E_expr_treeContext exprTree: return new ExpressionTree(this, scope, exprTree, usedAsValue);
                case DeltinScriptParser.E_array_indexContext arrayIndex: return new ValueInArrayAction(this, scope, arrayIndex);
                case DeltinScriptParser.E_create_arrayContext createArray: return new CreateArrayAction(this, scope, createArray.createarray());
                case DeltinScriptParser.E_expr_groupContext group: return GetExpression(scope, group.exprgroup().expr());
                case DeltinScriptParser.E_type_convertContext typeConvert: return new TypeConvertAction(this, scope, typeConvert.typeconvert());
                case DeltinScriptParser.E_notContext not: return new NotAction(this, scope, not.expr());
                case DeltinScriptParser.E_inverseContext inverse: return new InverseAction(this, scope, inverse.expr());
                case DeltinScriptParser.E_op_1Context             op1: return new OperatorAction(this, scope, op1);
                case DeltinScriptParser.E_op_2Context             op2: return new OperatorAction(this, scope, op2);
                case DeltinScriptParser.E_op_boolContext       opBool: return new OperatorAction(this, scope, opBool);
                case DeltinScriptParser.E_op_compareContext opCompare: return new OperatorAction(this, scope, opCompare);
                case DeltinScriptParser.E_ternary_conditionalContext ternary: return new TernaryConditionalAction(this, scope, ternary);
                case DeltinScriptParser.E_rootContext root: return new RootAction(this.TranslateInfo);
                case DeltinScriptParser.E_thisContext @this: return new ThisAction(this, scope, @this);
                case DeltinScriptParser.E_baseContext @base: return new BaseAction(this, scope, @base);
                case DeltinScriptParser.E_isContext @is: return new IsAction(this, scope, @is);
                case DeltinScriptParser.E_lambdaContext lambda: return new Lambda.LambdaAction(this, scope, lambda.lambda());
                default: throw new Exception($"Could not determine the expression type '{exprContext.GetType().Name}'.");
            }
        }

        /// <summary>Gets a variable or type from a VariableContext.</summary>
        /// <param name="scope">The scope the variable was called in.</param>
        /// <param name="getter">The getter scope.</param>
        /// <param name="variableContext">The context of the variable.</param>
        /// <param name="selfContained">Wether the variable was not called in an expression tree.</param>
        /// <returns>An IExpression created from the context.</returns>
        public IExpression GetVariable(Scope scope, Scope getter, DeltinScriptParser.VariableContext variableContext, bool selfContained)
        {
            // Get the variable name and range.
            string variableName = variableContext.PART().GetText();
            DocRange variableRange = DocRange.GetRange(variableContext.PART());

            // Get the variable.
            IVariable element = scope.GetVariable(variableName, getter, Script.Diagnostics, variableRange);
            if (element == null) return null;
            
            // Additional syntax checking.
            return new VariableApply(this).Apply(element, ExpressionIndexArray(getter, variableContext.array()), variableRange);
        }

        /// <summary>Gets an IExpression[] from a DeltinScriptParser.ArrayContext.</summary>
        /// <param name="scope">The scope used to parse the index values.</param>
        /// <param name="arrayContext">The context of the array.</param>
        /// <returns>An IExpression[] of each indexer in the chain. Will return null if arrayContext is null.</returns>
        public IExpression[] ExpressionIndexArray(Scope scope, DeltinScriptParser.ArrayContext arrayContext)
        {
            if (arrayContext == null) return null;

            IExpression[] index = null;
            if (arrayContext != null)
            {
                index = new IExpression[arrayContext.expr().Length];
                for (int i = 0; i < index.Length; i++)
                    index[i] = GetExpression(scope, arrayContext.expr(i));
            }
            return index;
        }

        /// <summary>Creates a macro from a Define_macroContext.</summary>
        /// <param name="objectScope">The scope of the macro if there is no static attribute.</param>
        /// <param name="staticScope">The scope of the macro if there is a static attribute.</param>
        /// <param name="macroContext">The context of the macro.</param>
        /// <returns>A DefinedMacro if the macro has parameters, a MacroVar if there are no parameters.</returns>
        public IScopeable GetMacro(Scope objectScope, Scope staticScope, DeltinScriptParser.Define_macroContext macroContext)
        {
            // If the ; is missing, syntax error.
            if (macroContext.STATEMENT_END() == null)
                Script.Diagnostics.Error("Expected ;", DocRange.GetRange((object)macroContext.TERNARY_ELSE() ?? (object)macroContext.name ?? (object)macroContext).end.ToRange());

            // If the : is missing, syntax error.
            if (macroContext.TERNARY_ELSE() == null)
                Script.Diagnostics.Error("Expected :", DocRange.GetRange(macroContext).end.ToRange());
            else
            {
                // Get the expression that will be parsed.
                if (macroContext.expr() == null)
                    Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(macroContext.TERNARY_ELSE()));
            }

            // Get the return type.
            CodeType returnType = CodeType.GetCodeTypeFromContext(this, macroContext.code_type());

            IScopeable newMacro;

            if (macroContext.LEFT_PAREN() != null || macroContext.RIGHT_PAREN() != null)
                newMacro = new DefinedMacro(this, objectScope, staticScope, macroContext, returnType);
            else
                newMacro = new MacroVar(this, objectScope, staticScope, macroContext, returnType);

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