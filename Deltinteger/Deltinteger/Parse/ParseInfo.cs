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
        }
        public ParseInfo SetCallInfo(CallInfo currentCallInfo) => new ParseInfo(this) { CurrentCallInfo = currentCallInfo };
        public ParseInfo SetLoop(LoopAction loop) => new ParseInfo(this) { BreakHandler = loop, ContinueHandler = loop };
        public ParseInfo SetBreakHandler(IBreakContainer handler) => new ParseInfo(this) { BreakHandler = handler };
        public ParseInfo SetContinueHandler(IContinueContainer handler) => new ParseInfo(this) { ContinueHandler = handler };

        /// <summary>Gets an IStatement from a StatementContext.</summary>
        /// <param name="scope">The scope the statement was created in.</param>
        /// <param name="statementContext">The context of the statement.</param>
        public IStatement GetStatement(Scope scope, DeltinScriptParser.StatementContext statementContext)
        {
            switch (statementContext)
            {
                case DeltinScriptParser.S_defineContext define    : {
                    var newVar = new ScopedVariable(scope, new DefineContextHandler(this, define.define()));
                    return new DefineAction(newVar);
                }
                case DeltinScriptParser.S_methodContext method    : return new CallMethodAction(this, scope, method.method(), false, scope);
                case DeltinScriptParser.S_varsetContext varset    : return new SetVariableAction(this, scope, varset.varset());
                case DeltinScriptParser.S_exprContext s_expr      : {

                    var expr = GetExpression(scope, s_expr.expr(), true, false);
                    if (expr is ExpressionTree == false || ((ExpressionTree)expr)?.Result is IStatement == false)
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
            string variableName = variableContext.PART().GetText();
            DocRange variableRange = DocRange.GetRange(variableContext.PART());

            var type = TranslateInfo.Types.GetCodeType(variableName, null, null);
            
            if (type != null)
            {
                if (selfContained)
                    Script.Diagnostics.Error("Types can't be used as expressions.", variableRange);
                
                if (variableContext.array() != null)
                    Script.Diagnostics.Error("Indexers cannot be used with types.", DocRange.GetRange(variableContext.array()));

                type.Call(this, variableRange);
                return type;
            }

            IVariable element = scope.GetVariable(variableName, getter, Script.Diagnostics, variableRange);
            if (element == null)
                return null;
            
            if (element is ICallable)
                ((ICallable)element).Call(this, variableRange);
            
            if (element is IApplyBlock)
                CurrentCallInfo?.Call((IApplyBlock)element, variableRange);
            
            IExpression[] index = null;
            if (variableContext.array() != null)
            {
                index = new IExpression[variableContext.array().expr().Length];
                for (int i = 0; i < index.Length; i++)
                    index[i] = GetExpression(getter, variableContext.array().expr(i));
            }

            if (element is IIndexReferencer referencer) return new CallVariableAction(referencer, index);

            if (index != null)
            {
                if (!element.CanBeIndexed)
                    Script.Diagnostics.Error("This variable type cannot be indexed.", variableRange);
                else
                    return new ValueInArrayAction(this, (IExpression)element, index);
            }

            return (IExpression)element;
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
                newMacro = new DefinedMacro(this, objectScope, staticScope, macroContext, returnType, true);
            else
                newMacro = new MacroVar(this, objectScope, staticScope, macroContext, returnType);

            TranslateInfo.ApplyBlock((IApplyBlock)newMacro);
            return newMacro;
        }
    }
}