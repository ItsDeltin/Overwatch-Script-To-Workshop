#nullable enable

using System.Linq;

namespace Deltin.Deltinteger.Parse.Vanilla;

class VanillaType
{
    public string Name { get; }
    public string[] NotableValues { get; set; }

    public VanillaType(string name, params string[] notableValues)
    {
        Name = name;
        NotableValues = notableValues;
    }

    public override string ToString() => Name;
}

class VanillaArrayType : VanillaType
{
    public VanillaType InnerType { get; }

    public VanillaArrayType(VanillaType innerType) : base(innerType.Name + "[]")
    {
        InnerType = innerType;
    }
}

class VanillaPipeType : VanillaType
{
    public VanillaType A { get; }
    public VanillaType B { get; }

    public VanillaPipeType(VanillaType a, VanillaType b) : base($"{a.Name} | {b.Name}")
    {
        A = a;
        B = b;
        NotableValues = A.NotableValues.Concat(B.NotableValues).ToArray();
    }

    public override string ToString() => A.ToString() + " | " + B.ToString();
}