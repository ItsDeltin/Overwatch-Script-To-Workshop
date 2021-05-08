using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Lambda;

namespace Deltin.Deltinteger.Parse.Workshop
{
    public class PortableAssigner
    {
        public IAssignedPortableFunction[] AssignedPortableFunctions { get; }

        public PortableAssigner(DeltinScript deltinScript)
        {
            int id = 1;
            var assigned = new List<IAssignedPortableFunction>();

            foreach (var script in deltinScript.Importer.ScriptFiles)
            {
                // Assign lambdas
                foreach (var lambda in script.Elements.Lambdas)
                {
                    assigned.Add(new AssignedLambdaFunction(id, lambda));
                    id++;
                }

                // Assign method group calls.
                foreach (var methodGroup in script.Elements.MethodGroupCalls)
                {
                    assigned.Add(new AssignedMethodFunction(deltinScript, id, methodGroup));
                    id++;
                }
            }

            AssignedPortableFunctions = assigned.ToArray();
        }

        public IAssignedPortableFunction FunctionFromKey(ILambdaApplier key) => AssignedPortableFunctions.First(assigned => assigned.Key == key);
    }

    public interface IAssignedPortableFunction
    {
        ILambdaApplier Key { get; }
        int Identifier { get; }
        CodeType This { get; }
        CodeType ReturnType { get; }
        void AssignParameters(RecycleWorkshopVariableAssigner recycler);
        void AddToAssigner(VarIndexAssigner assigner);
        void Translate(ActionSet actionSet, ReturnHandler returnHandler);
    }

    class AssignedLambdaFunction : IAssignedPortableFunction
    {
        ILambdaApplier IAssignedPortableFunction.Key => Lambda;
        public CodeType This => Lambda.This;
        public CodeType ReturnType => Lambda.ReturnType;
        public int Identifier { get; }
        public LambdaAction Lambda { get; }
        IEnumerable<AssignedPortableParameter> _parameters;

        public AssignedLambdaFunction(int id, LambdaAction lambda)
        {
            Identifier = id;
            Lambda = lambda;
        }

        public void AssignParameters(RecycleWorkshopVariableAssigner recycler)
            =>  _parameters = from parameter in Lambda.Parameters
                select new AssignedPortableParameter(
                    parameter,
                    // Create the gettable.
                    parameter.CodeType
                        .GetGettableAssigner(new AssigningAttributes(parameter.Name, true, false))
                        .GetValue(new GettableAssignerValueInfo(recycler.VarCollection) {
                            IndexReferenceCreator = recycler,
                            SetInitialValue = false
                        }));

        public void AddToAssigner(VarIndexAssigner assigner)
        {
            foreach (var assignedParameter in _parameters)
                assigner.Add(assignedParameter.Variable, assignedParameter.Gettable);
        }

        public void Translate(ActionSet actionSet, ReturnHandler returnHandler)
        {
            if (Lambda.Expression != null)
                returnHandler.ReturnValue(Lambda.Expression.Parse(actionSet));
            else
                Lambda.Statement.Translate(actionSet);
        }
    }

    class AssignedMethodFunction : IAssignedPortableFunction
    {
        ILambdaApplier IAssignedPortableFunction.Key => _methodCall;
        public CodeType This => null; // TODO
        public CodeType ReturnType => Method.CodeType.GetCodeType(_deltinScript);
        public IMethod Method => _methodCall.ChosenFunction;
        public int Identifier { get; }

        readonly DeltinScript _deltinScript;
        readonly CallMethodGroup _methodCall;
        IGettable[] _parameterGettables;

        public AssignedMethodFunction(DeltinScript deltinScript, int id, CallMethodGroup methodCall)
        {
            _deltinScript = deltinScript;
            Identifier = id;
            _methodCall = methodCall;
        }

        public void AssignParameters(RecycleWorkshopVariableAssigner recycler)
            => _parameterGettables = (from parameter in Method.Parameters
               select parameter.GetCodeType(_deltinScript)
                               .GetGettableAssigner(new AssigningAttributes(parameter.Name, true, false))
                               .GetValue(new GettableAssignerValueInfo(recycler.VarCollection) {
                                   IndexReferenceCreator = recycler,
                                   SetInitialValue = false
                               })).ToArray();

        // Will be handled by the function controller.
        public void AddToAssigner(VarIndexAssigner assigner) {}

        public void Translate(ActionSet actionSet, ReturnHandler returnHandler) => Method.Parse(
            actionSet,
            new MethodCall(_parameterGettables.Select(gettable => gettable.GetVariable()).ToArray()) {
                ProvidedReturnHandler = returnHandler
            }
        );
    }
}