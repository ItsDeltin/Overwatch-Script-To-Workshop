using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Workshop;
using Deltin.Deltinteger.Parse.Functions.Builder;

namespace Deltin.Deltinteger.Parse.Lambda.Workshop
{
    public class LambdaBuilder : IWorkshopFunctionController
    {
        readonly ToWorkshop _toWorkshop;
        readonly RecycleWorkshopVariableAssigner _parameterRecycler;
        readonly RecycleWorkshopVariableAssigner _returnRecycler;
        readonly LambdaParameterHandler _parameterHandler;
        SubroutineBuilder _subroutineBuilder;

        // The list of portable functions.
        readonly List<CompatibleLambda> _compatibleLambdas = new List<CompatibleLambda>();
        int _assignCurrentIdentifier = 1;

        public LambdaBuilder(ToWorkshop toWorkshop)
        {
            _toWorkshop = toWorkshop;
            _parameterRecycler = new RecycleWorkshopVariableAssigner(toWorkshop.VarCollection, "lambdaParameter", true);
            _returnRecycler = new RecycleWorkshopVariableAssigner(toWorkshop.VarCollection, "lambdaValue", false);
            _parameterHandler = new LambdaParameterHandler(_parameterRecycler);

            AssignStacks();
        }

        /// <summary>
        /// Assigns stacks to _parameterRecycler and _returnRecycler
        /// </summary>
        void AssignStacks()
        {
            int parameterStacks = 0;
            int returnStacks = 0;

            foreach (var tracker in _toWorkshop.TypeArgGlob.Trackers)
                if (tracker.Key is LambdaTracker lambdaTracker)
                {
                    var combos = tracker.Value.TypeArgCombos;

                    if (combos.Count == 0 || lambdaTracker.IsConstant)
                        continue;

                    // Get the number of stacks this tracked lambda will need.
                    parameterStacks = Math.Max(parameterStacks, combos.Max(combo =>
                    {
                        // Return the number of stacks this lambda type combo will need.

                        // The lambda's parameters.
                        var parameters = combo.TypeArgs;

                        // If the lambda returns a value, the first element in combo.TypeArgs is
                        // the return type, so skip it.
                        if (lambdaTracker.LambdaReturnsValue)
                            parameters = parameters.Skip(1).ToArray();

                        // Return the sum of the parameter type's stack length.
                        return parameters.Sum(typeArg => typeArg.Attributes.StackLength);
                    }));

                    if (lambdaTracker.LambdaReturnsValue)
                        // Get the number of stacks the return type needs.
                        returnStacks = Math.Max(returnStacks, combos.Max(combo => combo.TypeArgs[0].Attributes.StackLength));
                }

            // Assign stacks for parameters.
            _parameterRecycler.CreateWithTag(parameterStacks);
            _parameterRecycler.Reset();
            _parameterRecycler.Complete();
            _parameterRecycler.InitializeNonpersistent(_toWorkshop.PersistentVariables);

            // Assign stacks for return value.
            _returnRecycler.CreateWithTag(returnStacks);
            _returnRecycler.Reset();
            _returnRecycler.Complete();
        }


        // Gets the identifier for a portable function.
        public int GetIdentifier(ActionSet actionSet, IWorkshopPortableFunctionIdentifier portableFunction)
        {
            var typeLinker = actionSet.ThisTypeLinker ?? InstanceAnonymousTypeLinker.Empty;

            var compatible = GetCompatibleLambda(typeLinker, portableFunction);
            if (compatible != null) return compatible.Identifier;
            return CreateCompatibleLambda(typeLinker, portableFunction).Identifier;
        }

        // Gets a CompatibleLambda that supports the provided type linker and portable function.
        CompatibleLambda GetCompatibleLambda(InstanceAnonymousTypeLinker typeLinker, IWorkshopPortableFunctionIdentifier portableFunction)
        {
            // Iterate through each compatible lambda.
            foreach (var compatibleLambda in _compatibleLambdas)
                // Make sure the keys match and the type linker is compatible..
                if (compatibleLambda.Runner.Key == portableFunction.Key &&
                    compatibleLambda.TypeLinker.Compatible(typeLinker))
                    // This lambda is compatible with the provided lambda.
                    return compatibleLambda;

            // No compatible lambdas were found.
            return null;
        }

        CompatibleLambda CreateCompatibleLambda(InstanceAnonymousTypeLinker typeLinker, IWorkshopPortableFunctionIdentifier portableFunction)
        {
            var compatibleLambda = new CompatibleLambda(_assignCurrentIdentifier++, portableFunction.CreateRunner(_parameterRecycler), typeLinker);
            _parameterRecycler.Reset();
            _compatibleLambdas.Add(compatibleLambda);
            return compatibleLambda;
        }

        public void Complete() => _subroutineBuilder?.Complete();

        // Calls the lambda subroutine
        public IWorkshopTree Call(ActionSet actionSet, ICallInfo call, CodeType expectedType)
        {
            WorkshopFunctionBuilder.Call(actionSet, call, this);

            // No return type expected.
            if (expectedType == null)
                return null;

            // Make sure the returnRecycler is reset.
            _returnRecycler.Reset();

            return expectedType.GetRealType(actionSet.ThisTypeLinker).GetGettableAssigner(new AssigningAttributes("todo:name", true, false))
                .GetValue(new GettableAssignerValueInfo(actionSet)
                {
                    IndexReferenceCreator = _returnRecycler,
                    SetInitialValue = SetInitialValue.DoNotSet
                })
                .GetVariable();
        }

        // The attributes of the subroutine.
        WorkshopFunctionControllerAttributes IWorkshopFunctionController.Attributes { get; } = new WorkshopFunctionControllerAttributes()
        {
            IsInstance = true,
            IsRecursive = true,
            RecursiveRequiresObjectStack = true
        };

        // We will handle the returns here.
        ReturnHandler IWorkshopFunctionController.GetReturnHandler(ActionSet actionSet) => null;

        // Creates the subroutine.
        SubroutineCatalogItem IWorkshopFunctionController.GetSubroutine() => _toWorkshop.SubroutineCatalog.GetSubroutine(this, () =>
        {
            // Create the builder.
            _subroutineBuilder = new SubroutineBuilder(_toWorkshop.DeltinScript, new SubroutineContext()
            {
                Controller = this,
                ElementName = "func group",
                ObjectStackName = "func group",
                RuleName = "lambda",
                VariableGlobalDefault = true
            });

            return new SetupSubroutine(_subroutineBuilder.Initiate(), () => { });
        });

        object IWorkshopFunctionController.StackIdentifier() => this;

        void IWorkshopFunctionController.Build(ActionSet actionSet)
        {
            var returnHandlers = new List<ReturnHandler>();

            // Create the switch that chooses the lambda.
            SwitchBuilder lambdaSwitch = new SwitchBuilder(actionSet);

            for (int i = 0; i < _compatibleLambdas.Count; i++)
            {
                var compatibleLambda = _compatibleLambdas[i];
                _returnRecycler.Reset();

                // Create the return handler for the option.
                ReturnHandler returnHandler = new ReturnHandler(
                    actionSet,
                    compatibleLambda.Runner.ReturnType?.GetGettableAssigner(new AssigningAttributes("lambdaReturnValue", true, false))
                        // Get the IGettable
                        .GetValue(new GettableAssignerValueInfo(actionSet)
                        {
                            SetInitialValue = SetInitialValue.DoNotSet,
                            IndexReferenceCreator = _returnRecycler,
                            IsRecursive = false
                        }),
                    compatibleLambda.Runner.ReturnType != null);
                returnHandlers.Add(returnHandler);

                // The action set for the overload.
                ActionSet optionSet = actionSet.ContainVariableAssigner().New(returnHandler).SetThisTypeLinker(compatibleLambda.TypeLinker);

                // Go to next case
                lambdaSwitch.NextCase(Element.Num(compatibleLambda.Identifier));

                // Add the object variables of the selected method.
                var callerObject = ((Element)optionSet.CurrentObject)[1];

                // Add the class objects.
                compatibleLambda.Runner.This?.AddObjectVariablesToAssigner(optionSet.ToWorkshop, callerObject, optionSet.IndexAssigner);

                // Add parameters.
                compatibleLambda.Runner.AddToAssigner(optionSet.IndexAssigner);

                // then parse the block.
                compatibleLambda.Runner.Build(optionSet, returnHandler);
            }

            // Finish the switch.
            lambdaSwitch.Finish(((Element)actionSet.CurrentObject)[0]);

            foreach (var returnHandler in returnHandlers)
                returnHandler.ApplyReturnSkips();
        }

        // Since this is a subroutine, we do not need to worry about the actionSet or the parameterProviders. 
        IParameterHandler IWorkshopFunctionController.CreateParameterHandler(ActionSet actionSet, WorkshopParameter[] providedParameters) => _parameterHandler;

        class CompatibleLambda
        {
            public int Identifier { get; }
            public IWorkshopPortableFunctionRunner Runner { get; }
            public InstanceAnonymousTypeLinker TypeLinker { get; }

            public CompatibleLambda(int identifier, IWorkshopPortableFunctionRunner runner, InstanceAnonymousTypeLinker typeLinker)
            {
                Identifier = identifier;
                Runner = runner;
                TypeLinker = typeLinker;
            }
        }

        // Portable lambda parameter handler.
        class LambdaParameterHandler : IParameterHandler
        {
            readonly RecycleWorkshopVariableAssigner _recycler;
            public LambdaParameterHandler(RecycleWorkshopVariableAssigner recycler) => _recycler = recycler;

            public void AddParametersToAssigner(VarIndexAssigner assigner) { }

            public void Set(ActionSet actionSet, IWorkshopTree[] parameterValues)
            {
                parameterValues = ExtractStructs(parameterValues);

                for (int i = 0; i < parameterValues.Length; i++)
                    _recycler.Created[i].Set(actionSet, (Element)parameterValues[i]);
            }

            public void Push(ActionSet actionSet, IWorkshopTree[] parameterValues)
            {
                parameterValues = ExtractStructs(parameterValues);

                int i = 0;
                for (; i < parameterValues.Length; i++)
                    _recycler.Created[i].Push(actionSet, (Element)parameterValues[i]);

                // Push to remaining values.
                for (; i < _recycler.Created.Length; i++)
                    _recycler.Created[i].Push(actionSet, Element.Num(0));
            }

            static IWorkshopTree[] ExtractStructs(IWorkshopTree[] parameterValues)
            {
                var extracted = new List<IWorkshopTree>();

                // Extract all values from the parameters.
                foreach (var parameter in parameterValues)
                {
                    // Unfold struct.
                    if (parameter is IStructValue structValue)
                        extracted.AddRange(structValue.GetAllValues());
                    else // Normal
                        extracted.Add(parameter);
                }

                return extracted.ToArray();
            }

            public void Pop(ActionSet actionSet)
            {
                foreach (var parameterStack in _recycler.Created)
                    parameterStack.Pop(actionSet);
            }
        }
    }
}