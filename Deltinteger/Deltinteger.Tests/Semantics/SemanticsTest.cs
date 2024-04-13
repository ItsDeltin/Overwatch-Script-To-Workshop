using static Deltinteger.Tests.TestUtils;

namespace Deltinteger.Tests;

[TestClass]
public class SemanticsTest
{
    [TestMethod("Struct variable guard")]
    public void StructVariableGuard()
    {
        Setup();

        const string structWithRef = """
        struct TestStruct
        {
            public Number X;

            ref public void ModifyX()
            {
                X = 0;
            }
        }
        """;

        // X cannot be set without the ref attribute.
        Compile("""
        struct TestStruct
        {
            public Number X;

            public void ModifyX()
            {
                X = 0;
            }
        }
        """).AssertSearchError("cannot be set in the current context");

        // Normal functions are valid without a variable reference.
        Compile("""
        struct TestStruct
        {
            public Number X;
            public void Func() {}
        }
        rule: "Test" {
            TestStruct test1;
            TestStruct test2: { X: 0 };
            test1.Func();
            test2.Func();
        }
        """).AssertOk();

        // X can be set with the ref attribute.
        Compile(structWithRef).AssertOk();

        // Call ModifyX with a valid reference.
        Compile(structWithRef + """
        rule: "Test" {
            TestStruct test;
            test.ModifyX();
        }
        """).AssertOk();

        // Call ModifyX with an invalid reference (parameterless inline expr function).
        Compile(structWithRef + """
        rule: "Test" {
            TestStruct test: { X: 0 };
            test.ModifyX();
        }
        """).AssertSearchError("Functions that directly modify arrays requires a variable as the source");
    }
}