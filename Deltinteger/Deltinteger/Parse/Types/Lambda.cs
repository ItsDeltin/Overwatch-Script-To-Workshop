using System;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse.Lambda
{
    public class LambdaAction : IExpression, IWorkshopTree, IApplyBlock
    {
        private readonly BaseLambda LambdaType;
        public Var[] Parameters { get; }
        public SubLambdaInvoke[] InvokedState { get; }
        public bool MultiplePaths { get; }

        // For block lambda
        public BlockAction Block { get; }
        // For macro lambda
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

    public class SubLambdaInvoke : IBridgeInvocable
    {
        public bool Invoked { get; private set; }
        public void WasInvoked() => Invoked = true;
    }

    /// <summary>Lambda invoke function.</summary>
    public class LambdaInvoke : IMethod
    {
        public string Name => "Invoke";
        public CodeType ReturnType { get; }
        public CodeParameter[] Parameters { get; }

        public MethodAttributes Attributes => new MethodAttributes();
        public bool Static => false;
        public bool WholeContext => true;
        public string Documentation => "Invokes the lambda expression.";
        public Location DefinedAt => null;
        public AccessLevel AccessLevel => AccessLevel.Public;
        public bool DoesReturnValue => LambdaType is MacroLambda || LambdaType is ValueBlockLambda;
        
        public BaseLambda LambdaType { get; }

        public LambdaInvoke(BaseLambda lambdaType)
        {
            LambdaType = lambdaType;
            ReturnType = lambdaType.ReturnType;
            Parameters = ParametersFromTypes(lambdaType.ArgumentTypes);
        }

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            LambdaAction lambda = (LambdaAction)actionSet.CurrentObject;
            return lambda.Invoke(actionSet, methodCall.ParameterValues);
        }

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            if (parseInfo.SourceExpression != null) parseInfo.SourceExpression.OnResolve(expr => ConstantExpressionResolver.Resolve(expr, expr => {
                if (expr is LambdaAction source)
                {
                    parseInfo.CurrentCallInfo?.Call(source.RecursiveCallHandler, callRange);

                    // Add restricted calls.
                    foreach (RestrictedCall call in source.CallInfo.RestrictedCalls)
                        parseInfo.RestrictedCallHandler.RestrictedCall(new RestrictedCall(
                            call.CallType,
                            parseInfo.GetLocation(callRange),
                            new CallStrategy("The lambda '" + source.GetLabel(false) + "' calls a restricted value of type '" + RestrictedCall.StringFromCallType(call.CallType) + "'.")
                        ));
                    
                    for (int i = 0; i < source.InvokedState.Length; i++)
                        if (source.InvokedState[i].Invoked)
                            Parameters[i].Invoked.WasInvoked();
                }
                else if (expr is CallVariableAction callVariable && callVariable.Calling is Var var && var.BridgeInvocable != null)
                {
                    var.BridgeInvocable.WasInvoked();
                }
                else parseInfo.Script.Diagnostics.Warning("Source lambda not found.", callRange);
            }));
        }

        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
        public string GetLabel(bool markdown) => HoverHandler.GetLabel(DoesReturnValue ? ReturnType?.Name ?? "define" : "void", Name, Parameters, markdown, Documentation);

        /// <summary>Gets the 'Invoke' parameters from an array of CodeTypes.</summary>
        /// <param name="argumentTypes">The array of CodeTypes. The resulting CodeParameter[] will have an equal length to this.</param>
        private static CodeParameter[] ParametersFromTypes(CodeType[] argumentTypes)
        {
            if (argumentTypes == null) return new CodeParameter[0];

            CodeParameter[] parameters = new CodeParameter[argumentTypes.Length];
            for (int i = 0; i < parameters.Length; i++) parameters[i] = new CodeParameter($"arg{i}", argumentTypes[i]);
            return parameters;
        }
    }

    /// <summary>The base class for lambda CodeTypes.</summary>
    public abstract class BaseLambda : CodeType
    {
        public bool ReturnsValue { get; protected set; }
        public CodeType ReturnType { get; protected set; }
        public CodeType[] ArgumentTypes { get; }
        private readonly Scope _objectScope;

        protected BaseLambda(string name) : base(name)
        {
            CanBeDeleted = false;
            CanBeExtended = false;
            Kind = "constant";
            ArgumentTypes = new CodeType[0];
            _objectScope = new Scope("lambda");
            _objectScope.AddNativeMethod(new LambdaInvoke(this));
        }
        protected BaseLambda(string name, CodeType[] argumentTypes) : base(name)
        {
            CanBeDeleted = false;
            CanBeExtended = false;
            Kind = "constant";
            ArgumentTypes = argumentTypes ?? new CodeType[0];
            _objectScope = new Scope("lambda");
            _objectScope.AddNativeMethod(new LambdaInvoke(this));
        }

        public override bool Implements(CodeType type)
        {
            if (type == null || type.GetType() != this.GetType()) return false;

            BaseLambda otherLambda = (BaseLambda)type;

            // If the argument length is not the same, return false.
            if (ArgumentTypes.Length != otherLambda.ArgumentTypes.Length) return false;

            // If the other's return type does not implement this return type, return false.
            if (ReturnType != null && (otherLambda.ReturnType == null || !otherLambda.ReturnType.Implements(ReturnType))) return false;

            // If any of the other's parameters to not implement this respective parameters, return false.
            for (int i = 0; i < ArgumentTypes.Length; i++)
            {
                if ((ArgumentTypes[i] == null) != (otherLambda.ArgumentTypes[i] == null)) return false;
                if (ArgumentTypes[i] != null && !otherLambda.ArgumentTypes[i].Implements(ArgumentTypes[i])) return false;
            }
            
            return true;
        }

        public override Scope GetObjectScope() => _objectScope;
        public override Scope ReturningScope() => null;
        public override bool IsConstant() => true;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Constant
        };
        public override string GetName()
        {
            if (ArgumentTypes.Length == 0) return Name;
            else return Name + "<" + string.Join(", ", ArgumentTypes.Select(at => at?.GetName() ?? "define")) + ">";
        }
    }

    public class BlockLambda : BaseLambda
    {
        public BlockLambda() : base("BlockLambda") {}
        public BlockLambda(params CodeType[] argumentTypes) : base("BlockLambda", argumentTypes) {}
        protected BlockLambda(string name) : base(name) {}
        protected BlockLambda(string name, CodeType[] argumentTypes) : base(name, argumentTypes) {}
    }

    public class ValueBlockLambda : BlockLambda
    {
        public ValueBlockLambda() : base("ValueLambda")
        {
            ReturnsValue = true;
        }
        public ValueBlockLambda(CodeType returnType, params CodeType[] argumentTypes) : base("ValueLambda", argumentTypes)
        {
            ReturnType = returnType;
        }
    }

    public class MacroLambda : BaseLambda
    {
        public MacroLambda() : base("MacroLambda")
        {
            ReturnsValue = true;
        }

        public MacroLambda(CodeType returnType, params CodeType[] argumentTypes) : base("MacroLambda", argumentTypes)
        {
            ReturnType = returnType;
        }
    }

    public interface IBridgeInvocable
    {
        void WasInvoked();
    }
}