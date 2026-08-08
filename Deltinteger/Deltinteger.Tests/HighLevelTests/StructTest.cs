namespace Deltinteger.Tests;

[TestClass]
public class StructTest
{
    [TestMethod("Nested Single Struct")]
    public void NestedSingleStruct()
    {
        Compile("""
        single struct NestedStruct
        {
            public String A;
            public String B;
        }

        struct Struct
        {
            public NestedStruct Nested;
            public NestedStruct GetNested() {return Nested;}
        }

        globalvar Struct myStruct = {
            Nested: {
                A: "a",
                B: "b"
            }
        };

        rule: 'Read nested struct value'
        {
            String b = myStruct.GetNested().B;
        }
        """)
        .EmulateTick()
        .AssertVariable("b", "b");
    }
}