using System;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Lambda
{
    public class LambdaAction : IExpression, IWorkshopTree, IApplyBlock
    {
        /// <summary>The type of the lambda. This can either be BlockLambda, ValueBlockLambda, or MacroLambda.</summary>
        private readonly BaseLambda LambdaType;
        /// <summary>The parameters of the lambda.</summary>
        public Var[] Parameters { get; }
        /// <summary>The invocation status of the lambda parameters/</summary>
        public SubLambdaInvoke[] InvokedState { get; }
        /// <summary>Determines if the lambda has multiple return statements. LambdaType will be ValueBlockLambda if this is true.</summary>
        public bool MultiplePaths { get; }

        /// <summary>The block of the lambda. This will be null if LambdaType is MacroLambda.</summary>
        public BlockAction Block { get; }
        /// <summary>The expression of the lambda. This will be null if LambdaType is BlockLambda or ValueBlockLambda.</summary>
        public IExpression Expression { get; }

        public CallInfo CallInfo { get; }
        public IRecursiveCallHandler RecursiveCallHandler { get; }

        public LambdaAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.LambdaContext context)
        {
            Scope lambdaScope = scope.Child();
            RecursiveCallHandler = new LambdaRecursionHandler(this);
            CallInfo = new CallInfo(RecursiveCallHandler, parseInfo.Script);

            // Get the lambda parameters.
            Parameters = new Var[context.define().Length];
            InvokedState = new SubLambdaInvoke[Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                InvokedState[i] = new SubLambdaInvoke();
                // TODO: Make custom builder.
                Parameters[i] = new ParameterVariable(lambdaScope, new DefineContextHandler(parseInfo, context.define(i)), InvokedState[i]);
            }
            
            CodeType[] argumentTypes = Parameters.Select(arg => arg.CodeType).ToArray();

            // context.block() will not be null if the lambda is a block.
            // () => {}
            if (context.block() != null)
            {
                // Parse the block.
                Block = new BlockAction(parseInfo.SetCallInfo(CallInfo), lambdaScope, context.block());

                // Validate the block.
                BlockTreeScan validation = new BlockTreeScan(parseInfo, Block, "lambda", DocRange.GetRange(context.INS()));
                validation.ValidateReturns();

                if (validation.ReturnsValue)
                {
                    LambdaType = new ValueBlockLambda(validation.ReturnType, argumentTypes);
                    MultiplePaths = validation.MultiplePaths;
                }
                else
                    LambdaType = new BlockLambda(argumentTypes);
            }
            // context.expr() will not be null if the lambda is an expression.
            // () => 2 * x
            else if (context.expr() != null)
            {
                // Get the lambda expression.
                Expression = parseInfo.SetCallInfo(CallInfo).GetExpression(lambdaScope, context.expr());
                LambdaType = new MacroLambda(Expression.Type(), argumentTypes);
            }

            // Add so the lambda can be recursive-checked.
            parseInfo.TranslateInfo.RecursionCheck(CallInfo);

            // Add hover info
            parseInfo.Script.AddHover(DocRange.GetRange(context.INS()), new MarkupBuilder().StartCodeLine().Add(LambdaType.GetName()).EndCodeLine().ToString());
        }

        public IWorkshopTree Parse(ActionSet actionSet) => this;
        public Scope ReturningScope() => LambdaType.GetObjectScope();
        public CodeType Type() => LambdaType;

        public string ToWorkshop(OutputLanguage outputLanguage, ToWorkshopContext context) => throw new NotImplementedException();
        public bool EqualTo(IWorkshopTree other) => throw new NotImplementedException();

        public IWorkshopTree Invoke(ActionSet actionSet, params IWorkshopTree[] parameterValues)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            for (int i = 0; i < parameterValues.Length; i++)
                actionSet.IndexAssigner.Add(Parameters[i], parameterValues[i]);
            
            if (Block != null)
            {
                ReturnHandler returnHandler = new ReturnHandler(actionSet, "lambda", MultiplePaths);
                Block.Translate(actionSet.New(returnHandler));
                returnHandler.ApplyReturnSkips();
                
                return returnHandler.GetReturnedValue();
            }
            else if (Expression != null)
            {
                return Expression.Parse(actionSet);
            }
            else throw new NotImplementedException();
        }

        public string GetLabel(bool markdown)
        {
            string label = "";

            if (Parameters.Length == 1) label += Parameters[0].GetLabel(false);
            else
            {
                label += "(";
                for (int i = 0; i < Parameters.Length; i++)
                {
                    if (i != 0) label += ", ";
                    label += Parameters[i].GetLabel(false);
                }
                label += ")";
            }
            label += " => ";

            if (LambdaType is MacroLambda macroLambda) label += macroLambda.ReturnType?.GetName() ?? "define";
            else if (LambdaType is ValueBlockLambda vbl) label += "{" + (vbl.ReturnType?.GetName() ?? "define") + "}";
            else if (LambdaType is BlockLambda) label += "{}";

            return label;
        }

        public void SetupParameters() {}
        public void SetupBlock() {}
        public void OnBlockApply(IOnBlockApplied onBlockApplied) => onBlockApplied.Applied();

        public bool EmptyBlock => Block == null || Block.Statements.Length == 0;

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
            public string GetLabel() => Lambda.GetLabel(false);
        }
    }
}