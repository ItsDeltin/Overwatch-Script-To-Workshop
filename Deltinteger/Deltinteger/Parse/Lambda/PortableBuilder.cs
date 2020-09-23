using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.FunctionBuilder;

namespace Deltin.Deltinteger.Parse.Lambda
{
    class LambdaGroup : IComponent, IGroupDeterminer, ISubroutineContext, IFunctionLookupTable
    {
        public DeltinScript DeltinScript { get; set; }
        private readonly List<IFunctionHandler> _functionHandlers = new List<IFunctionHandler>();
        private int _parameterCount;
        private int _functionIdentifier = 0;
        private SubroutineInfo _subroutineInfo = null;

        public LambdaGroup() {}
        public void Init() {}

        public int Add(ILambdaHandler lambda)
        {
            _functionIdentifier++;
            lambda.Identifier = _functionIdentifier;

            // Update the parameter count.
            _parameterCount = Math.Max(_parameterCount, lambda.ParameterCount());

            // Add the function handler.
            _functionHandlers.Add(lambda);
            return _functionIdentifier;
        }

        public IWorkshopTree Call(ActionSet actionSet, ICallHandler call)
        {
            var builder = new FunctionBuildController(actionSet, call, this);
            return builder.Call();
        }

        // IGroupDeterminer
        public string GroupName() => "func group";
        public bool IsRecursive() => true;
        public bool IsObject() => true;
        public bool IsSubroutine() => true;
        public bool MultiplePaths() => ReturnsValue();
        public bool IsVirtual() => true;
        public bool ReturnsValue() => true;
        public IFunctionLookupTable GetLookupTable() => this;
        public SubroutineInfo GetSubroutineInfo()
        {
            if (_subroutineInfo == null)
            {
                var builder = new SubroutineBuilder(DeltinScript, this);
                builder.SetupSubroutine();
                _subroutineInfo = builder.SubroutineInfo;
            }
            return _subroutineInfo;
        }
        public IParameterHandler[] Parameters() => DefinedParameterHandler.GetDefinedParameters(_parameterCount, _functionHandlers.ToArray(), true);
        public NewRecursiveStack GetExistingRecursiveStack(List<NewRecursiveStack> stack) => throw new NotImplementedException();
        public object GetStackIdentifier() => throw new NotImplementedException();

        // IFunctionLookupTable
        public void Build(FunctionBuildController builder)
        {
            // Create the switch that chooses the lambda.
            SwitchBuilder lambdaSwitch = new SwitchBuilder(builder.ActionSet);

            foreach (ILambdaHandler option in _functionHandlers)
            {
                // The action set for the overload.
                ActionSet optionSet = builder.ActionSet.New(builder.ActionSet.IndexAssigner.CreateContained());

                // Add the object variables of the selected method.
                // option.ContainingType.AddObjectVariablesToAssigner(optionSet.CurrentObject, optionSet.IndexAssigner);

                // Go to next case
                lambdaSwitch.NextCase(new V_Number(option.Identifier));            

                // then parse the block.
                builder.Subcall(optionSet, option);
            }

            // Finish the switch.
            lambdaSwitch.Finish(((Element)builder.ActionSet.CurrentObject)[0]);
        }

        // ISubroutineContext
        string ISubroutineContext.RuleName() => "func group";
        string ISubroutineContext.ElementName() => "func group";
        string ISubroutineContext.ThisArrayName() => "func group";
        bool ISubroutineContext.VariableGlobalDefault() => true;
        IParameterHandler[] ISubroutineContext.Parameters() => Parameters();
        CodeType ISubroutineContext.ContainingType() => null;
        void ISubroutineContext.Finish(Rule rule) {}
        IGroupDeterminer ISubroutineContext.GetDeterminer() => this;
        void ISubroutineContext.SetSubroutineInfo(SubroutineInfo subroutineInfo) => _subroutineInfo = subroutineInfo;
    }

    interface ILambdaHandler : IFunctionHandler
    {
        int Identifier { get; set; }
    }

    public class LambdaHandler : ILambdaHandler
    {
        public int Identifier { get; set; }
        private readonly LambdaAction _lambda;

        public LambdaHandler(LambdaAction lambda)
        {
            _lambda = lambda;
        }

        public bool DoesReturnValue() => _lambda.LambdaType.DoesReturnValue();
        public IIndexReferencer GetParameterVar(int index) => index < _lambda.Parameters.Length ? _lambda.Parameters[index] : null;
        public int ParameterCount() => _lambda.Parameters.Length;

        public SubroutineInfo GetSubroutineInfo() => throw new NotImplementedException();
        public bool IsSubroutine() => false;

        public string GetName() => throw new NotImplementedException();
        public bool IsObject() => throw new NotImplementedException();
        public bool IsRecursive() => throw new NotImplementedException();
        public bool MultiplePaths() => throw new NotImplementedException();
        public object StackIdentifier() => _lambda;

        public void ParseInner(ActionSet actionSet)
        {
            // Create a new contained action set.
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            var infoSaver = actionSet.VarCollection.Assign("funcSaver", true, false);
            actionSet.AddAction(infoSaver.SetVariable((Element)actionSet.CurrentObject));

            // Add the contained variables.
            for (int i = 0; i < _lambda.CapturedVariables.Count; i++)
                actionSet.IndexAssigner.Add(_lambda.CapturedVariables[i], infoSaver.CreateChild(i + 1));

            _lambda.Statement.Translate(actionSet);
        }
    }
}