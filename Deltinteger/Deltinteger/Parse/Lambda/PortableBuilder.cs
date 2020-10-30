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
        private readonly Dictionary<object, int> _identifiers = new Dictionary<object, int>();
        private int _parameterCount;
        private int _functionIdentifier = 0;
        private SubroutineInfo _subroutineInfo = null;

        public LambdaGroup() {}
        public void Init() {}

        public int Add(IFunctionHandler lambda)
        {
            if (_identifiers.TryGetValue(lambda.UniqueIdentifier(), out int existingIdentifier))
                return existingIdentifier;

            _functionIdentifier++;
            _identifiers.Add(lambda.UniqueIdentifier(), _functionIdentifier);

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
        public bool MultiplePaths() => true;
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
        public RecursiveStack GetExistingRecursiveStack(List<RecursiveStack> stack) => throw new NotImplementedException();
        public object GetStackIdentifier() => throw new NotImplementedException();

        // IFunctionLookupTable
        public void Build(FunctionBuildController builder)
        {
            // Create the switch that chooses the lambda.
            SwitchBuilder lambdaSwitch = new SwitchBuilder(builder.ActionSet);

            foreach (IFunctionHandler option in _functionHandlers)
            {
                // The action set for the overload.
                ActionSet optionSet = builder.ActionSet.New(builder.ActionSet.IndexAssigner.CreateContained());

                // Go to next case
                lambdaSwitch.NextCase(new V_Number(_identifiers[option.UniqueIdentifier()]));

                // Add the object variables of the selected method.
                var callerObject = ((Element)builder.ActionSet.CurrentObject)[1];

                // Add the class objects.
                option.ContainingType?.AddObjectVariablesToAssigner(callerObject, optionSet.IndexAssigner);

                // then parse the block.
                builder.Subcall(optionSet.SetThis(callerObject).New(builder.ActionSet.CurrentObject), option);
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

    public class LambdaHandler : IFunctionHandler
    {
        public CodeType ContainingType => _lambda.This;
        private readonly LambdaAction _lambda;

        public LambdaHandler(LambdaAction lambda)
        {
            _lambda = lambda;
        }

        public bool DoesReturnValue() => _lambda.LambdaType.ReturnsValue;
        public IIndexReferencer GetParameterVar(int index) => index < _lambda.Parameters.Length ? _lambda.Parameters[index] : null;
        public int ParameterCount() => _lambda.Parameters.Length;

        public SubroutineInfo GetSubroutineInfo() => throw new NotImplementedException();
        public bool IsSubroutine() => false;

        public string GetName() => throw new NotImplementedException();
        public bool IsObject() => throw new NotImplementedException();
        public bool IsRecursive() => throw new NotImplementedException();
        public bool MultiplePaths() => throw new NotImplementedException();
        public object UniqueIdentifier() => _lambda;

        public void ParseInner(ActionSet actionSet)
        {
            // Create a new contained action set.
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            var infoSaver = actionSet.VarCollection.Assign("funcSaver", true, false);
            actionSet.AddAction(infoSaver.SetVariable((Element)actionSet.CurrentObject));

            actionSet = actionSet.New(infoSaver.Get());

            // Add the contained variables.
            for (int i = 0; i < _lambda.CapturedVariables.Count; i++)
                actionSet.IndexAssigner.Add(_lambda.CapturedVariables[i], infoSaver.CreateChild(i + 2));
            
            if (_lambda.Expression != null)
                actionSet.ReturnHandler.ReturnValue(_lambda.Expression.Parse(actionSet));
            else
                _lambda.Statement.Translate(actionSet);
        }
    }
}