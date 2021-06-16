using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Functions.Builder;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Lambda
{
    public class LambdaAction : IExpression, IWorkshopTree, IVariableTracker, ILambdaApplier
    {
        readonly LambdaExpression _context;
        readonly Scope _lambdaScope;
        readonly ParseInfo _parseInfo;
        readonly bool _isExplicit;
        CodeType[] _argumentTypes;
        bool _resolved;

        /// <summary>The type of the lambda.</summary>
        public PortableLambdaType LambdaType { get; private set; }

        /// <summary>The parameters of the lambda.</summary>
        public Var[] Parameters { get; private set; }

        /// <summary>The invocation status of the lambda parameters/</summary>
        public IBridgeInvocable[] InvokedState { get; private set; }

        /// <summary>Determines if the lambda has multiple return statements. LambdaType will be ValueBlockLambda if this is true.</summary>
        public bool MultiplePaths { get; private set; }

        /// <summary>The block of the lambda. This will be null if LambdaType is MacroLambda.</summary>
        public IStatement Statement { get; private set; }

        /// <summary>The expression of the lambda. This will be null if LambdaType is BlockLambda or ValueBlockLambda.</summary>
        public IExpression Expression { get; private set; }

        /// <summary>The captured local variables.</summary>
        public List<IVariableInstance> CapturedVariables { get; } = new List<IVariableInstance>();

        ///<summary> The parent type that the lambda is defined in.</summary>
        public CodeType This { get; }

        public CodeType ReturnType { get; private set; }

        public CallInfo CallInfo { get; }
        public IRecursiveCallHandler RecursiveCallHandler { get; }
        public bool ResolvedSource => true;

        public LambdaAction(ParseInfo parseInfo, Scope scope, LambdaExpression context)
        {
            _context = context;
            _lambdaScope = scope.Child();
            _parseInfo = parseInfo;
            RecursiveCallHandler = new LambdaRecursionHandler(this);
            CallInfo = new CallInfo(RecursiveCallHandler, parseInfo.Script);
            This = parseInfo.ThisType;

            _isExplicit = context.Parameters.Any(p => p.Type != null);
            var parameterState = context.Parameters.Count == 0 || _isExplicit ? ParameterState.CountAndTypesKnown : ParameterState.CountKnown;

            new CheckLambdaContext(
                parseInfo,
                this,
                "Cannot determine lambda in the current context",
                context.Range,
                parameterState
            ).Check();

            // Add hover info
            // parseInfo.Script.AddHover(context.Arrow.Range, new MarkupBuilder().StartCodeLine().Add(LambdaType.GetName()).EndCodeLine().ToString());
        }

        public void GetLambdaStatement(PortableLambdaType expecting)
        {
            _getLambdaStatement(expecting);

            // Check if the current lambda implements the expected type.
            if (!LambdaType.Implements(expecting))
                _parseInfo.Script.Diagnostics.Error("Expected lambda of type '" + expecting.GetName() + "'", _context.Arrow.Range);
        }

        public void GetLambdaStatement() => _getLambdaStatement(null);

        private void _getLambdaStatement(PortableLambdaType expectingType)
        {
            _resolved = true;

            // Get the lambda parameters.
            Parameters = new Var[_context.Parameters.Count];
            InvokedState = new SubLambdaInvoke[Parameters.Length];
            _argumentTypes = new CodeType[Parameters.Length];

            for (int i = 0; i < Parameters.Length; i++)
            {
                if (_isExplicit && _context.Parameters[i].Type == null)
                    _parseInfo.Script.Diagnostics.Error("Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit", _context.Parameters[i].Range);

                InvokedState[i] = new SubLambdaInvoke();
                Parameters[i] = (Var)new LambdaVariable(i, expectingType, _lambdaScope, new LambdaContextHandler(_parseInfo, _context.Parameters[i]), InvokedState[i]).GetVar();
                _argumentTypes[i] = Parameters[i].GetDefaultInstance(null).CodeType.GetCodeType(_parseInfo.TranslateInfo);
            }

            ParseInfo parser = _parseInfo.SetCallInfo(CallInfo).AddVariableTracker(this).SetExpectType(expectingType?.ReturnType).SetReturnType(expectingType?.ReturnType);

            bool returnsValue = false;

            // Get the statements.
            if (_context.Statement is Block block)
            {
                // Parse the block.
                Statement = new BlockAction(parser, _lambdaScope, block);

                // Validate the block.
                BlockTreeScan validation = new BlockTreeScan(_parseInfo, (BlockAction)Statement, "lambda", _context.Arrow.Range);
                validation.ValidateReturns();

                if (validation.ReturnsValue)
                {
                    ReturnType = validation.ReturnType;
                    MultiplePaths = validation.MultiplePaths;
                    returnsValue = true;
                }
            }
            else if (_context.Statement is ExpressionStatement exprStatement)
            {
                // Get the lambda expression.
                Expression = parser.GetExpression(_lambdaScope, exprStatement.Expression);
                ReturnType = Expression.Type();
                returnsValue = true;
            }
            else
            {
                // Statement
                Statement = parser.GetStatement(_lambdaScope, _context.Statement);
                if (Statement is IExpression expr)
                {
                    Expression = expr;
                    ReturnType = expr.Type();
                    if (ReturnType != null) returnsValue = true;
                }
            }

            LambdaType = new PortableLambdaType(new PortableLambdaTypeBuilder(
                kind: expectingType?.LambdaKind ?? LambdaKind.Anonymous,
                parameters: _argumentTypes,
                returnType: ReturnType,
                returnsValue: returnsValue,
                parameterTypesKnown: _isExplicit,
                callContainer: CallInfo));

            // Add so the lambda can be recursive-checked.
            _parseInfo.TranslateInfo.GetComponent<RecursionCheckComponent>().AddCheck(CallInfo);
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            // If the lambda type is constant, return the lambda itself.
            if (LambdaType.IsConstant())
                return new LambdaActionWorkshopInstance(actionSet, this);

            // Encode the lambda.
            return Workshop.CaptureEncoder.Encode(actionSet, this);
        }
        public Scope ReturningScope() => LambdaType.GetObjectScope();
        public CodeType Type() => _resolved ? (CodeType)LambdaType : new UnknownLambdaType(_context.Parameters.Count);

        public void ToWorkshop(WorkshopBuilder builder, ToWorkshopContext context) => throw new NotImplementedException();
        public bool EqualTo(IWorkshopTree other) => throw new NotImplementedException();

        public IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues) => Invoke(null, actionSet, parameterValues);

        public IWorkshopTree Invoke(VarIndexAssigner lambaAssigner, ActionSet actionSet, params IWorkshopTree[] parameterValues)
        {
            switch (LambdaType.LambdaKind)
            {
                // Constant macro
                case LambdaKind.ConstantMacro:
                    return OutputConstantMacro(lambaAssigner, actionSet, parameterValues);

                // Constant block
                case LambdaKind.ConstantBlock:
                case LambdaKind.ConstantValue:
                    return OutputContantBlock(lambaAssigner, actionSet, parameterValues);

                // Portable
                case LambdaKind.Portable:
                    return OutputPortable(actionSet, parameterValues);

                // Unknown
                case LambdaKind.Anonymous:
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>Assigns the parameter values to the action set for the constant lambdas.</summary>
        private ActionSet AssignContainedParameters(VarIndexAssigner lambaAssigner, ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            var newSet = actionSet.New(actionSet.IndexAssigner.CreateContained());
            actionSet.IndexAssigner.CopyAll(lambaAssigner);

            for (int i = 0; i < parameterValues.Length; i++)
                newSet.IndexAssigner.Add(Parameters[i], parameterValues[i]);

            return newSet;
        }
        /// <summary>Outputs a constant macro lambda.</summary>
        private IWorkshopTree OutputConstantMacro(VarIndexAssigner lambaAssigner, ActionSet actionSet, IWorkshopTree[] parameterValues) => Expression.Parse(AssignContainedParameters(lambaAssigner, actionSet, parameterValues));

        /// <summary>Outputs a constant block.</summary>
        private IWorkshopTree OutputContantBlock(VarIndexAssigner lambdaAssigner, ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            ReturnHandler returnHandler = new ReturnHandler(
                actionSet,
                ReturnType?.GetGettableAssigner(new AssigningAttributes("lambda", actionSet.IsGlobal, false))
                           .GetValue(new GettableAssignerValueInfo(actionSet) { SetInitialValue = false }),
                MultiplePaths);
            actionSet = AssignContainedParameters(lambdaAssigner, actionSet, parameterValues).New(returnHandler);

            if (Expression != null)
                returnHandler.ReturnValue(Expression.Parse(actionSet));
            else
                Statement.Translate(actionSet);

            returnHandler.ApplyReturnSkips();

            return returnHandler.GetReturnedValue();
        }

        /// <summary>Outputs a portable lambda.</summary>
        private IWorkshopTree OutputPortable(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            var controller = actionSet.ToWorkshop.LambdaBuilder;
            return WorkshopFunctionBuilder.Call(actionSet, new Functions.Builder.CallInfo(parameterValues), controller);
        }

        public MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo)
        {
            var builder = new MarkupBuilder().StartCodeLine();

            if (Parameters.Length == 1)
                builder.Add(_argumentTypes[0].GetName() + " " + Parameters[0].Name);
            else
            {
                builder.Add("(");
                for (int i = 0; i < Parameters.Length; i++)
                {
                    if (i != 0) builder.Add(", ");
                    builder.Add(_argumentTypes[i].GetName() + " " + Parameters[i].Name);
                }
                builder.Add(")");
            }
            builder.Add(" => ");
            builder.Add(ReturnType.GetNameOrVoid());

            return builder;
        }

        public void LocalVariableAccessed(IVariableInstance variable)
        {
            if (!CapturedVariables.Contains(variable) && _lambdaScope.Parent.ScopeContains(variable))
                CapturedVariables.Add(variable);
        }

        public bool EmptyBlock => Statement == null || (Statement is BlockAction block && block.Statements.Length == 0);

        class LambdaRecursionHandler : IRecursiveCallHandler
        {
            public LambdaAction Lambda { get; }

            public LambdaRecursionHandler(LambdaAction lambda)
            {
                Lambda = lambda;
            }

            public CallInfo CallInfo => Lambda.CallInfo;
            public string TypeName => "lambda";
            public bool CanBeRecursivelyCalled() => false;
            public bool DoesRecursivelyCall(IRecursiveCallHandler calling) => calling is LambdaRecursionHandler lambdaRecursion && Lambda == lambdaRecursion.Lambda;
            public string GetLabel(DeltinScript deltinScript) => Lambda.GetLabel(deltinScript, LabelInfo.RecursionError).ToString(false);
        }
    }
}