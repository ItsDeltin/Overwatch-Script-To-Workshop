namespace Deltinteger.Tests;

[TestClass]
public class FeatureTest
{
    [TestMethod("Feature test - Get all enum values")]
    public void GetAllEnumValues()
    {
        // Valid usage.
        Compile("""
        enum MyEnum { A, B = 6, C }

        globalvar MyEnum[] x = GetAllEnumValues<MyEnum>();
        globalvar Team[] y = GetAllEnumValues<Team>();
        """)
        .EmulateTick()
        .AssertVariable("x", [0, 6, 2])
        .AssertVariable("y", [EmulateValue.Team("All"), EmulateValue.Team("Team 1"), EmulateValue.Team("Team 2")]);

        // Invalid: Using inner values
        Compile("""
        enum WithInnerValues { A(Number), B }

        globalvar WithInnerValues[] y = GetAllEnumValues<WithInnerValues>();
        """)
        .AssertSearchError("Enumerator type must not have inner values");

        Compile("""
        single enum WithInnerValues { A(Number), B }

        globalvar WithInnerValues[] y = GetAllEnumValues<WithInnerValues>();
        """)
        .AssertSearchError("Enumerator type must not have inner values");

        // Invalid: Not an enumerator.
        Compile("""
        globalvar Any y = GetAllEnumValues<Number>();
        """)
        .AssertSearchError("Type argument must be an enumerator");

        // Invalid: Constant enumerator.
        Compile("""
        globalvar Any y = GetAllEnumValues<EffectRev>();
        """)
        .AssertSearchError("Type argument cannot be constant");

    }
}
