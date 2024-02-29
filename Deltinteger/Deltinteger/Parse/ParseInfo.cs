using System;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Lambda;
using Deltin.Deltinteger.Parse.Vanilla;

namespace Deltin.Deltinteger.Parse
{
    public class ParseInfo
    {
        public ScriptFile Script { get; }
        public DeltinScript TranslateInfo { get; }

        public CallInfo CurrentCallInfo { get; private set; }
        public bool BreaksAllowed { get; private set; }
        public bool ContinuesAllowed { get; private set; }
        public IRestrictedCallHandler RestrictedCallHandler { get; private set; }
        public ExpectingLambdaInfo ExpectingLambda { get; private set; }
        public ITreeContextPart SourceExpression { get; private set; }
        public UsageResolver CurrentUsageResolver { get; private set; }
        public UsageResolver SourceUsageResolver { get; private set; }
        public CodeType ReturnType { get; private set; }
        public CodeType ThisType => TypeInitializer?.WorkingInstance;
        public IDefinedTypeInitializer TypeInitializer { get; private set; }
        public VariableModifierGroup ContextualVariableModifiers { get; private set; }
        public ReturnTracker ReturnTracker { get; private set; }
        public VanillaScope ScopedVanillaVariables { get; init; }

        // Target
        public CodeType ExpectingType { get; private set; }
        public bool IsUsedAsValue { get; private set; } = true;

        // Tail
        public IVariableTracker[] LocalVariableTracker { get; private set; }

        // Head
        public ResolveInvokeInfo ResolveInvokeInfo { get; private set; }
        public AsyncInfo AsyncInfo { get; private set; }

        public Elements.ITypeSupplier Types => TranslateInfo.Types;

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
            BreaksAllowed = other.BreaksAllowed;
            ContinuesAllowed = other.ContinuesAllowed;
            RestrictedCallHandler = other.RestrictedCallHandler;
            ExpectingLambda = other.ExpectingLambda;
            SourceExpression = other.SourceExpression;
            CurrentUsageResolver = other.CurrentUsageResolver;
            SourceUsageResolver = other.SourceUsageResolver;
            ReturnType = other.ReturnType;
            TypeInitializer = other.TypeInitializer;
            ContextualVariableModifiers = other.ContextualVariableModifiers;
            ReturnTracker = other.ReturnTracker;
            ScopedVanillaVariables = other.ScopedVanillaVariables;
            ExpectingType = other.ExpectingType;
            IsUsedAsValue = other.IsUsedAsValue;
            LocalVariableTracker = other.LocalVariableTracker;
            ResolveInvokeInfo = other.ResolveInvokeInfo;
            AsyncInfo = other.AsyncInfo;
        }
        public ParseInfo SetCallInfo(CallInfo currentCallInfo) => new ParseInfo(this) { CurrentCallInfo = currentCallInfo, RestrictedCallHandler = currentCallInfo };
        public ParseInfo SetLoopAllowed(bool allowed) => new ParseInfo(this) { BreaksAllowed = allowed, ContinuesAllowed = allowed };
        public ParseInfo SetBreaksAllowed(bool allowed) => new ParseInfo(this) { BreaksAllowed = allowed };
        public ParseInfo SetContinuesAllowed(bool allowed) => new ParseInfo(this) { ContinuesAllowed = allowed };
        public ParseInfo SetSourceExpression(ITreeContextPart treePart) => new ParseInfo(this) { SourceExpression = treePart };
        public ParseInfo SetRestrictedCallHandler(IRestrictedCallHandler callHandler) => new ParseInfo(this) { RestrictedCallHandler = callHandler };
        public ParseInfo AddVariableTracker(IVariableTracker variableTracker)
        {
            if (LocalVariableTracker == null) return new ParseInfo(this) { LocalVariableTracker = new IVariableTracker[] { variableTracker } };
            // Create a new variable tracker array with +1 length.
            var variableTrackerArray = new IVariableTracker[LocalVariableTracker.Length + 1];
            // Copy the current variable trackers.
            LocalVariableTracker.CopyTo(variableTrackerArray, 0);
            // Set the tracker.
            variableTrackerArray[LocalVariableTracker.Length] = variableTracker;

            return new ParseInfo(this) { LocalVariableTracker = variableTrackerArray };
        }
        public ParseInfo SetExpectingLambda(CodeType sourceType) => new ParseInfo(this) { ExpectingLambda = sourceType is PortableLambdaType portable ? new ExpectingLambdaInfo(portable) : null };
        public ParseInfo SetLambdaInfo(ExpectingLambdaInfo lambdaInfo) => new ParseInfo(this) { ExpectingLambda = lambdaInfo };
        public ParseInfo SetInvokeInfo(ResolveInvokeInfo invokeInfo) => new ParseInfo(this) { ResolveInvokeInfo = invokeInfo };
        public ParseInfo SetAsyncInfo(AsyncInfo asyncInfo) => new ParseInfo(this) { AsyncInfo = asyncInfo };
        public ParseInfo SetUsageResolver(UsageResolver currentUsageResolver, UsageResolver sourceUsageResolver) => new ParseInfo(this)
        {
            CurrentUsageResolver = currentUsageResolver,
            SourceUsageResolver = sourceUsageResolver
        };
        public ParseInfo SetExpectType(CodeType type) => new ParseInfo(this) { ExpectingType = type, IsUsedAsValue = true }.SetExpectingLambda(type);
        public ParseInfo SetReturnType(CodeType type) => new ParseInfo(this) { ReturnType = type };
        public ParseInfo SetThisType(IDefinedTypeInitializer typeInitializer) => new ParseInfo(this) { TypeInitializer = typeInitializer };
        public ParseInfo SetContextualModifierGroup(VariableModifierGroup modifierGroup) => new ParseInfo(this) { ContextualVariableModifiers = modifierGroup };
        public ParseInfo SetReturnTracker(ReturnTracker returnTracker) => new ParseInfo(this) { ReturnTracker = returnTracker };
        public ParseInfo ClearExpectations() => new ParseInfo(this)
        {
            ExpectingType = null,
            ExpectingLambda = null,
            ReturnType = null,
            IsUsedAsValue = true
        };
        public ParseInfo SetIsUsedAsValue(bool isUsedAsValue) => new ParseInfo(this) { IsUsedAsValue = isUsedAsValue };

        /// <summary>Gets an IStatement from a StatementContext.</summary>
        /// <param name="scope">The scope the statement was created in.</param>
        /// <param name="statementContext">The context of the statement.</param>
        public IStatement GetStatement(Scope scope, IParseStatement statementContext)
        {
            IStatement statement = SetIsUsedAsValue(false).StatementFromContext(scope, statementContext);

            // Apply related output comment.
            if (statementContext.Comment != null)
                statement.OutputComment(Script.Diagnostics, statementContext.Comment.Range, statementContext.Comment.GetContents());

            return statement;
        }

        private IStatement StatementFromContext(Scope scope, IParseStatement statementContext)
        {
            switch (statementContext)
            {
                case VariableDeclaration declare:
                    {
                        var newVar = new ScopedVariable(true, scope, new DefineContextHandler(this, declare)).GetVar();
                        return new DefineAction(newVar);
                    }
                case Assignment assignment: return new SetVariableAction(this, scope, assignment);
                case Increment increment: return new IncrementAction(this, scope, increment);
                case If @if: return new IfAction(this, scope, @if);
                case While @while: return new WhileAction(this, scope, @while);
                case For @for: return new ForAction(this, scope, @for);
                case Foreach @foreach: return new ForeachAction(this, scope, @foreach);
                case Return @return: return new ReturnAction(this, scope, @return);
                case Delete delete: return new DeleteAction(this, scope, delete);
                case Continue @continue: return new ContinueAction(this, @continue.Range);
                case Break @break: return new BreakAction(this, @break.Range);
                case Switch @switch: return new SwitchAction(this, scope, @switch);
                case Block @block: return new BlockAction(this, scope, @block);
                case FunctionExpression func: return new CallMethodAction(this, scope, func, false, scope);
                // Expression statements (functions)
                case ExpressionStatement exprStatement:

                    // Parse the expression
                    var expr = GetExpression(scope, exprStatement.Expression, false);

                    if (!expr.IsStatement())
                    {
                        Script.Diagnostics.Error("Expressions can't be used as statements.", statementContext.Range);
                        return new MissingElementAction(TranslateInfo);
                    }
                    if (expr is IStatement == false) return new MissingElementAction(TranslateInfo);
                    return (IStatement)expr;
                // New
                case NewExpression newExpression: return new CreateObjectAction(this, scope, newExpression);

                default: return new MissingElementAction(TranslateInfo);
            }
        }

        /// <summary>Gets an IExpression from an ExprContext.</summary>
        /// <param name="scope">The scope the expression was called in.</param>
        /// <param name="exprContext">The context of the expression/</param>
        /// <param name="usedAsValue">Determines if the expression is being used as a value.</param>
        /// <param name="getter">The getter scope. Used for preserving scope through parameters.</param>
        /// <returns>An IExpression created from the ExprContext.</returns>
        public IExpression GetExpression(Scope scope, IParseExpression exprContext, bool usedAsValue = true, Scope getter = null)
        {
            if (getter == null) getter = scope;

            switch (exprContext)
            {
                case NumberExpression number: return new NumberAction(this, number);
                case BooleanExpression boolean: return new BoolAction(this, boolean.Value);
                case NullExpression @null: return new NullAction(this);
                case StringExpression @string: return new StringAction(this, scope, @string);
                case InterpolatedStringExpression interpolatedString: return new Strings.InterpolatedStringAction(interpolatedString, this, scope);
                case Identifier identifier: return GetVariable(scope, getter, identifier);
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
                case RootExpression root: return new RootAction(this.TranslateInfo);
                case LambdaExpression lambda: return new Lambda.LambdaAction(this, scope, lambda);
                case AsyncContext asyncContext: return AsyncInfo.ParseAsync(this, scope, asyncContext, usedAsValue);
                case StructDeclarationContext structDeclaration: return new StructDeclarationExpression(this, scope, structDeclaration);
                case ImportJsonSyntax importJson: return new ImportJson(this, importJson);
                // Missing
                case MissingElement missing: return new MissingElementAction(TranslateInfo);
                default: throw new Exception($"Could not determine the expression type '{exprContext.GetType().Name}'.");
            }
        }

        /// <summary>Gets a variable or type from a VariableContext.</summary>
        /// <param name="scope">The scope the variable was called in.</param>
        /// <param name="getter">The getter scope.</param>
        /// <param name="variableContext">The context of the variable.</param>
        /// <param name="selfContained">Whether the variable was not called in an expression tree.</param>
        /// <returns>An IExpression created from the context.</returns>
        public IExpression GetVariable(Scope scope, Scope getter, Identifier variableContext)
        {
            if (!variableContext.Token) return new MissingElementAction(TranslateInfo);

            var name = variableContext.Token.Text;
            var range = variableContext.Token.Range;

            // Get the variable.
            var variable = scope.GetAllVariables(name, ResolveInvokeInfo != null).FirstOrDefault();

            // Variable does not exist.
            if (variable == null)
            {
                Script.Diagnostics.Error(string.Format("The variable {0} does not exist in the {1}.", name, scope.ErrorName), range);
                variable = new MissingVariable(TranslateInfo, name);
            }

            // Check the access level.
            if (!SemanticsHelper.AccessLevelMatches(variable.AccessLevel, variable.Attributes.ContainingType, ThisType))
            {
                Script.Diagnostics.Error(string.Format("'{0}' is inaccessable due to its access level.", name), range);
            }

            var apply = new VariableApply(this, scope, getter, variable, variableContext);
            apply.Accept();
            return apply.VariableCall;
        }

        public void LocalVariableAccessed(IVariableInstance variable)
        {
            if (LocalVariableTracker != null)
                foreach (var tracker in LocalVariableTracker)
                    tracker.LocalVariableAccessed(variable);
        }

        public ParseInfo ClearTail() => new ParseInfo(this)
        {
            // LocalVariableTracker = null
        };

        public ParseInfo ClearHead() => new ParseInfo(this)
        {
            ResolveInvokeInfo = null,
            AsyncInfo = null,
            IsUsedAsValue = true,
        };

        public ParseInfo ClearTargetted() => new ParseInfo(this)
        {
            ExpectingType = null,
            IsUsedAsValue = true
        };

        public ParseInfo ClearContextual() => new ParseInfo(this)
        {
            SourceExpression = null
        }.ClearTail().ClearHead().ClearTargetted();

        public Location GetLocation(DocRange range) => new Location(Script.Uri, range);

        public string WorkshopLogRange(DocRange range)
        {
            var fileName = System.IO.Path.GetFileName(this.Script.Document.Uri.LocalPath);
            return " in '" + fileName + "' at line " + range.Start.Line;
        }

        public DiagnosticsToken CreateDiagnosticsToken(DocRange range) => new DiagnosticsToken(Script.Diagnostics, range);

        public void Error(string message, DocRange range) => Script.Diagnostics.Error(message, range);
    }

    public class VariableApply
    {
        public ICallVariable VariableCall { get; }
        public IVariableInstance Variable { get; }
        public DocRange CallRange { get; }
        private readonly ParseInfo _parseInfo;
        private readonly string _name;
        private readonly IExpression[] _index;
        private readonly CodeType[] _generics;

        public VariableApply(ParseInfo parseInfo, Scope scope, Scope getter, IVariableInstance variable, Identifier variableContext)
        {
            Variable = variable;
            _parseInfo = parseInfo;
            _name = variableContext.Token.Text;
            CallRange = variableContext.Token.Range;
            getter = getter ?? scope;

            // Get the index.
            if (variableContext.Index != null)
            {
                _index = new IExpression[variableContext.Index.Count];
                for (int i = 0; i < _index.Length; i++)
                {
                    _index[i] = parseInfo.GetExpression(scope, variableContext.Index[i].Expression, getter: getter);
                    if (_index[i].Type().Attributes.IsStruct)
                    {
                        parseInfo.Script.Diagnostics.Error("Structs cannot be used as an indexer", variableContext.Index[i].Expression.Range);
                    }
                }
            }

            // Get the generics.
            if (variableContext.TypeArgs != null)
            {
                _generics = new CodeType[variableContext.TypeArgs.Count];
                for (int i = 0; i < _generics.Length; i++)
                    _generics[i] = TypeFromContext.GetCodeTypeFromContext(parseInfo, getter, variableContext.TypeArgs[i]);
            }

            VariableCall = Variable.GetExpression(_parseInfo, CallRange, _index, _generics);
        }

        public void Accept()
        {
            // Callable
            Variable.Call(_parseInfo, CallRange);

            // If the type of the variable being called is Player, check if the variable is calling Event Player.
            // If the source expression is null, Event Player is used by default.
            // Otherwise, confirm that the source expression is returning the player variable scope.
            if (Variable.Provider.VariableType == VariableType.Player)
            {
                // No source expression, Event Player is used by default.
                if (_parseInfo.SourceExpression == null)
                    DefaultEventPlayerRestrictedCall();
                else // There is a source expression.
                    _parseInfo.SourceExpression.OnResolve(expr =>
                    {
                        // An expression that is not targettable.
                        if (expr is RootAction)
                            DefaultEventPlayerRestrictedCall();
                    });
            }

            // If there is a local variable tracker and the variable requires capture.
            if (Variable.Provider.RequiresCapture)
                _parseInfo.LocalVariableAccessed(Variable);

            VariableCall.Accept();
        }

        void DefaultEventPlayerRestrictedCall() => _parseInfo.RestrictedCallHandler.AddRestrictedCall(
            new RestrictedCall(RestrictedCallType.EventPlayer, _parseInfo.GetLocation(CallRange), RestrictedCall.Message_EventPlayerDefault(_name))
        );
    }

    public readonly struct DiagnosticsToken
    {
        readonly FileDiagnostics diagnostics;
        readonly DocRange range;

        public DiagnosticsToken(FileDiagnostics diagnostics, DocRange range)
        {
            this.diagnostics = diagnostics;
            this.range = range;
        }

        public void Error(string message) => diagnostics.Error(message, range);
    }
}
