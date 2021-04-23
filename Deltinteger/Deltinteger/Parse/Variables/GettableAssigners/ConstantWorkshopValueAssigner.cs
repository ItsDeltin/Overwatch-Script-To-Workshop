namespace Deltin.Deltinteger.Parse
{
    /// <summary>Assigner for constant workshop values.</summary>
    class ConstantWorkshopValueAssigner : IGettableAssigner
    {
        private readonly ExpressionOrWorkshopValue _value;

        public ConstantWorkshopValueAssigner(ExpressionOrWorkshopValue value) => _value = value;
        public ConstantWorkshopValueAssigner(IWorkshopTree value) => _value = new ExpressionOrWorkshopValue(value);
        public ConstantWorkshopValueAssigner(IExpression value) => _value = new ExpressionOrWorkshopValue(value);

        public GettableAssignerResult GetResult(GettableAssignerValueInfo info)
        {
            var value = _value.Parse(info.ActionSet);
            return new GettableAssignerResult(new WorkshopElementReference(value), value);
        }
        public IGettable AssignClassStacks(GetClassStacks info) => throw new System.NotImplementedException();
        public int StackDelta() => 0;
    }
}