namespace DS.Analysis.Types.Semantics
{
    class PhysicalType
    {
        public CodeType Type { get; }

        public PhysicalType(CodeType type)
        {
            Type = type;
        }

        public static PhysicalType Unlinked(CodeType type) => new PhysicalType(type);

        public static implicit operator PhysicalType(CodeType type) =>
            type == null ? null : new PhysicalType(type);

        public static implicit operator CodeType(PhysicalType physicalType) =>
            physicalType == null ? null : physicalType.Type;
    }
}