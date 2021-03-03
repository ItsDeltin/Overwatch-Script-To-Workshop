using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    class DefineAction : IStatement
    {
        public IVariable DefiningVariable { get; }
        private string _comment;

        public DefineAction(IVariable var)
        {
            DefiningVariable = var;
        }

        public void Translate(ActionSet actionSet)
        {
            var variableInstance = DefiningVariable.GetInstance(actionSet.ThisTypeLinker);
            var value = variableInstance.GetAssigner(actionSet).GetValue(new GettableAssignerValueInfo(actionSet.SetNextComment(_comment)));
            actionSet.IndexAssigner.Add(DefiningVariable, value);
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment) => _comment = comment;
    }
}