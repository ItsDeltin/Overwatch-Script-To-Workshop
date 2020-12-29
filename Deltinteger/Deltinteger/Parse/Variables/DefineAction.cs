using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    class DefineAction : IStatement
    {
        public IVariable DefiningVariable { get; }

        public DefineAction(IVariable var)
        {
            DefiningVariable = var;
        }

        public void Translate(ActionSet actionSet)
        {
            var variableInstance = DefiningVariable.GetInstance(actionSet.ThisTypeLinker);
            var value = variableInstance.GetAssigner().GetValue(new GettableAssignerValueInfo(actionSet));
            actionSet.IndexAssigner.Add(DefiningVariable, value);
        }
    }
}