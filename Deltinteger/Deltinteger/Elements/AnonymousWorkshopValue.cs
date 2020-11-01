namespace Deltin.Deltinteger.Elements
{
    class AnonymousWorkshopValue : IWorkshopTree
    {
        public string Value { get; }
        private readonly bool _keyword;

        public AnonymousWorkshopValue(string value, bool keyword)
        {
            Value = value;
            _keyword = keyword;
        }

        public void ToWorkshop(WorkshopBuilder b, ToWorkshopContext context)
        {
            if (_keyword)
                b.AppendKeyword(Value);
            else
                b.Append(Value);
        }

        public bool EqualTo(IWorkshopTree other) => other is AnonymousWorkshopValue value && Value == value.Value;
    }
}