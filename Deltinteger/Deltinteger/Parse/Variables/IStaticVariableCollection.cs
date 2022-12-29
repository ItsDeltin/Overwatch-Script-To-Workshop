using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    class StaticVariableCollection : IComponent
    {
        public IEnumerable<StaticVariable> StaticVariables => _staticVariables;
        readonly List<StaticVariable> _staticVariables = new List<StaticVariable>();
        private readonly GetVariablesAssigner defaultStaticAssigner = new GetVariablesAssigner(null as InstanceAnonymousTypeLinker);

        // Static variable with known value
        public void AddVariable(IVariable variable, IWorkshopTree value) =>
            _staticVariables.Add(new StaticVariable(variable, actionSet => actionSet.DeltinScript.DefaultIndexAssigner.Add(variable, value)));

        // User-defined static variable
        public void AddVariable(IVariable variable) =>
            _staticVariables.Add(new StaticVariable(variable, actionSet => actionSet.DeltinScript.DefaultIndexAssigner.Add(variable,
                variable.GetDefaultInstance(null).GetAssigner(defaultStaticAssigner).GetValue(
                    new GettableAssignerValueInfo(actionSet)))));

        public void Init(DeltinScript deltinScript) { }
    }

    public struct StaticVariable
    {
        public IVariable Provider;
        public Action<ActionSet> Assign;

        public StaticVariable(IVariable provider, Action<ActionSet> assign)
        {
            Provider = provider;
            Assign = assign;
        }
    }
}
