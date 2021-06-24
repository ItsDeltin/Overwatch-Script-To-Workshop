using System.Linq;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse.Lambda.Workshop
{
    /// <summary>Used by the LambdaBuilder to get a matching compatible lambda.</summary>
    public interface IWorkshopPortableFunctionIdentifier
    {
        /// <summary>The unique key of the lambda.</summary>
        ILambdaApplier Key { get; }

        /// <summary>Creates an executor for the LambdaBuilder.</summary>
        IWorkshopPortableFunctionRunner CreateRunner(RecycleWorkshopVariableAssigner parameterAssigner);
    }

    /// <summary>IWorkshopPortableFunctionRunner handles compiling portable functions to the workshop.</summary>
    public interface IWorkshopPortableFunctionRunner
    {
        /// <summary>The unique key of the lambda.</summary>
        ILambdaApplier Key { get; }
        
        /// <summary>The return type of the lambda.</summary>
        CodeType ReturnType { get; }

        /// <summary>The class or struct that encapsulates the portable function's expression.</summary>
        CodeType This { get; }

        /// <summary>Adds the portable function's parameters to a variable assigner.</summary>
        void AddToAssigner(VarIndexAssigner assigner);

        /// <summary>Compiles the actual portable function's contents to the workshop using the provided ActionSet.</summary>
        void Build(ActionSet actionSet, ReturnHandler returnHandler);
    }

    /// <summary>Identifies and creates runners for lambdas.</summary>
    class AnonymousWorkshopPortableFunction : IWorkshopPortableFunctionIdentifier
    {
        ILambdaApplier IWorkshopPortableFunctionIdentifier.Key => _action;
        readonly LambdaAction _action;
        public AnonymousWorkshopPortableFunction(LambdaAction lambda) => _action = lambda;

        public IWorkshopPortableFunctionRunner CreateRunner(RecycleWorkshopVariableAssigner parameterAssigner) =>
            new AnonymousRunner(_action, parameterAssigner);

        class AnonymousRunner : IWorkshopPortableFunctionRunner
        {
            readonly LambdaAction _lambda;
            readonly AssignedPortableParameter[] _parameters;

            public AnonymousRunner(LambdaAction lambda, RecycleWorkshopVariableAssigner parameterAssigner)
            {
                _lambda = lambda;
                _parameters = (from parameter in _lambda.Parameters select new AssignedPortableParameter(
                    parameter,
                    // Create the gettable.
                    parameter.CodeType
                        .GetGettableAssigner(new AssigningAttributes(parameter.Name, true, false))
                        .GetValue(new GettableAssignerValueInfo(parameterAssigner) { SetInitialValue = SetInitialValue.DoNotSet }))).ToArray();
            }

            public void AddToAssigner(VarIndexAssigner assigner)
            {
                foreach (var parameter in _parameters)
                    assigner.Add(parameter.Variable, parameter.Gettable);
            }

            public void Build(ActionSet actionSet, ReturnHandler returnHandler)
            {
                CaptureEncoder.DecodeCaptured(actionSet, actionSet.CurrentObject, _lambda);

                if (_lambda.Expression != null)
                {
                    var expr = _lambda.Expression.Parse(actionSet);

                    // 'expr' will be null if the expression is a void method.
                    if (expr != null)
                        returnHandler.ReturnValue(expr);
                }
                else
                    _lambda.Statement.Translate(actionSet);
            }

            ILambdaApplier IWorkshopPortableFunctionRunner.Key => _lambda;
            CodeType IWorkshopPortableFunctionRunner.ReturnType => _lambda.ReturnType;
            CodeType IWorkshopPortableFunctionRunner.This => _lambda.This;

            // Represents a variable and gettable pair.
            struct AssignedPortableParameter
            {
                public IVariable Variable;
                public IGettable Gettable;

                public AssignedPortableParameter(IVariable variable, IGettable gettable)
                {
                    Variable = variable;
                    Gettable = gettable;
                }
            }
        }
    }

    /// <summary>Identifies and creates runners for method groups.</summary>
    class MethodGroupWorkshopPortableFunction : IWorkshopPortableFunctionIdentifier
    {
        ILambdaApplier IWorkshopPortableFunctionIdentifier.Key => _methodGroup;
        readonly DeltinScript _deltinScript;
        readonly CallMethodGroup _methodGroup;

        public MethodGroupWorkshopPortableFunction(DeltinScript deltinScript, CallMethodGroup methodGroup)
        {
            _deltinScript = deltinScript;
            _methodGroup = methodGroup;
        } 

        public IWorkshopPortableFunctionRunner CreateRunner(RecycleWorkshopVariableAssigner parameterAssigner) =>
            new MethodGroupRunner(_deltinScript, _methodGroup, parameterAssigner);

        class MethodGroupRunner : IWorkshopPortableFunctionRunner
        {
            readonly CallMethodGroup _methodGroup;
            readonly DeltinScript _deltinScript;
            readonly IGettable[] _parameterGettables;

            public MethodGroupRunner(DeltinScript deltinScript, CallMethodGroup methodGroup, RecycleWorkshopVariableAssigner parameterAssigner)
            {
                _methodGroup = methodGroup;
                _deltinScript = deltinScript;

                // Create gettables for the method's parameters.
                _parameterGettables = (from parameter in Method.Parameters
                    select parameter.GetCodeType(deltinScript)
                        .GetGettableAssigner(new AssigningAttributes(parameter.Name, true, false))
                        .GetValue(new GettableAssignerValueInfo(parameterAssigner.VarCollection) {
                            IndexReferenceCreator = parameterAssigner,
                            SetInitialValue = SetInitialValue.DoNotSet
                        })).ToArray();
            }

            public void Build(ActionSet actionSet, ReturnHandler returnHandler) => Method.Parse(
                actionSet,
                new MethodCall(_parameterGettables.Select(gettable => gettable.GetVariable()).ToArray()) {
                    ProvidedReturnHandler = returnHandler
                }
            );

            IMethod Method => _methodGroup.ChosenFunction;
            void IWorkshopPortableFunctionRunner.AddToAssigner(VarIndexAssigner assigner) { } // Will be handled by the function controller.
            ILambdaApplier IWorkshopPortableFunctionRunner.Key => _methodGroup;
            CodeType IWorkshopPortableFunctionRunner.ReturnType => Method.CodeType?.GetCodeType(_deltinScript);
            CodeType IWorkshopPortableFunctionRunner.This => Method.Attributes.ContainingType;
        }
    }
}