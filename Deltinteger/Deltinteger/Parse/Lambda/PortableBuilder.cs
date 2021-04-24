using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Workshop;
using Deltin.Deltinteger.Parse.Functions.Builder;
using Deltin.Deltinteger.Parse.Functions.Builder.User;

namespace Deltin.Deltinteger.Parse.Lambda
{
    // TODO: Either switch this to IWorkshopComponent or split it into 2.
    class LambdaGroup : IComponent, IWorkshopFunctionController
    {
        DeltinScript _deltinScript;
        RecycleWorkshopVariableAssigner _parameterRecycler;
        RecycleWorkshopVariableAssigner _returnRecycler;

        public void Init(DeltinScript deltinScript)
        {
            _deltinScript = deltinScript;
            _parameterRecycler = new RecycleWorkshopVariableAssigner(_deltinScript.VarCollection);
            _returnRecycler = new RecycleWorkshopVariableAssigner(_deltinScript.VarCollection);
        }

        public IWorkshopTree Call(ActionSet actionSet, ICallInfo call, CodeType expectedType)
        {
            WorkshopFunctionBuilder.Call(actionSet, call, this);

            if (expectedType == null)
                return null;

            _returnRecycler.Reset();
            return expectedType.GetGettableAssigner(new AssigningAttributes("todo:name", true, false))
                .GetValue(new GettableAssignerValueInfo(actionSet) {
                    IndexReferenceCreator = _returnRecycler,
                    SetInitialValue = false
                })
                .GetVariable();
        }

        WorkshopFunctionControllerAttributes IWorkshopFunctionController.Attributes { get; } = new WorkshopFunctionControllerAttributes() {
            IsInstance = true,
            IsRecursive = true,
            RecursiveRequiresObjectStack = true
        };

        ReturnHandler IWorkshopFunctionController.GetReturnHandler(ActionSet actionSet) => null;
        SubroutineCatalogItem IWorkshopFunctionController.GetSubroutine() => _deltinScript.GetComponent<SubroutineCatalog>().GetSubroutine(this, () =>
            new SubroutineBuilder(_deltinScript, new SubroutineContext() {
                Controller = this,
                ElementName = "func group", ObjectStackName = "func group", RuleName = "lambda",
                VariableGlobalDefault = true
            }).SetupSubroutine()
        );
        object IWorkshopFunctionController.StackIdentifier() => this;

        void IWorkshopFunctionController.Build(ActionSet actionSet)
        {
            var returnHandlers = new List<ReturnHandler>();

            // Create the switch that chooses the lambda.
            SwitchBuilder lambdaSwitch = new SwitchBuilder(actionSet);

            foreach (var option in actionSet.ToWorkshop.PortableAssigner.AssignedPortableFunctions)
            {
                // Create the return handler for the option.
                ReturnHandler returnHandler = new ReturnHandler(
                    actionSet,
                    option.ReturnType.GetGettableAssigner(new AssigningAttributes("lambdaReturnValue", true, false))
                        // Get the IGettable
                        .GetValue(new GettableAssignerValueInfo(actionSet) {
                            SetInitialValue = false,
                            IndexReferenceCreator = _returnRecycler
                        }),
                    true);
                returnHandlers.Add(returnHandler);

                // The action set for the overload.
                ActionSet optionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

                // Go to next case
                lambdaSwitch.NextCase(Element.Num(option.Identifier));

                // Add the object variables of the selected method.
                var callerObject = ((Element)optionSet.CurrentObject)[1];

                // Add the class objects.
                option.This?.AddObjectVariablesToAssigner(optionSet.ToWorkshop, callerObject, optionSet.IndexAssigner);

                // then parse the block.
                option.Translate(actionSet, returnHandler);
                // option.Statement.Translate(optionSet.SetThis(callerObject).New(actionSet.CurrentObject));
                // Create a new contained action set.
                //     actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

                //     var infoSaver = actionSet.VarCollection.Assign("funcSaver", true, false);
                //     actionSet.AddAction(infoSaver.SetVariable((Element)actionSet.CurrentObject));

                //     actionSet = actionSet.New(infoSaver.Get());

                //     // Add the contained variables.
                //     for (int i = 0; i < _lambda.CapturedVariables.Count; i++)
                //         actionSet.IndexAssigner.Add(_lambda.CapturedVariables[i], infoSaver.CreateChild(i + 2));

                //     if (_lambda.Expression != null)
                //         actionSet.ReturnHandler.ReturnValue(_lambda.Expression.Parse(actionSet));
                //     else
                //         _lambda.Statement.Translate(actionSet);
            }

            // Finish the switch.
            lambdaSwitch.Finish(((Element)actionSet.CurrentObject)[0]);

            foreach (var returnHandler in returnHandlers)
                returnHandler.ApplyReturnSkips();
        }

        public IParameterHandler CreateParameterHandler(ActionSet actionSet, WorkshopParameter[] providedParameters)
            => new LambdaParameterHandler(_deltinScript.VarCollection, _parameterRecycler, actionSet.ToWorkshop.PortableAssigner.AssignedPortableFunctions);

        class LambdaParameterHandler : IParameterHandler
        {
            readonly RecycleWorkshopVariableAssigner _recycler;
            readonly IAssignedPortableFunction[] _lambdas;

            public LambdaParameterHandler(VarCollection varCollection, RecycleWorkshopVariableAssigner recycler, IAssignedPortableFunction[] lambdas)
            {
                _recycler = recycler;
                _lambdas = lambdas;

                // Loop through each lambda.
                foreach (var lambda in lambdas)
                {
                    lambda.AssignParameters(_recycler);
                    _recycler.Reset();
                }
            }

            public void AddParametersToAssigner(VarIndexAssigner assigner)
            {
                foreach (var lambda in _lambdas)
                    lambda.AddToAssigner(assigner);
            }

            public void Set(ActionSet actionSet, IWorkshopTree[] parameterValues)
            {
                parameterValues = ExtractStructs(parameterValues);

                for (int i = 0; i < parameterValues.Length; i++)
                    _recycler.Created[i].Set(actionSet, (Element)parameterValues[i]);
            }

            public void Push(ActionSet actionSet, IWorkshopTree[] parameterValues)
            {
                parameterValues = ExtractStructs(parameterValues);
                throw new NotImplementedException();
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
                throw new NotImplementedException();
            }
        }
    }

    public struct AssignedPortableParameter
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