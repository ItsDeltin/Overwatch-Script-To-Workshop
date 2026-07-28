namespace Deltinteger.Tests;

using static TestUtils;

[TestClass]
public class EnumTest
{
    [TestMethod("Basis usage of enum with no inner values")]
    public void BasicUsageOfEnumWithNoInnerValue()
    {
        Compile("""
        rule: "enum test" {
            define a = EnumTest.A;
            define b = EnumTest.B;
            define c = EnumTest.C;
            define d = EnumTest.D;

            define added = a + b + c + d;

            define used_as_indexer = [5, 6, 7, 8][c];
        }

        enum EnumTest {
            A,
            B,
            C,
            D
        }
        """)
        .EmulateTick()
        .AssertVariable("a", 0)
        .AssertVariable("b", 1)
        .AssertVariable("c", 2)
        .AssertVariable("d", 3)
        .AssertVariable("added", 6)
        .AssertVariable("used_as_indexer", 7);
    }

    [TestMethod("Single enum basic usage")]
    public void SingleEnumBasicUsage()
    {
        Compile("""
        rule: "enum test" {
            define b = EnumTest.B(5);
            define c = EnumTest.C("one", "two");

            define take = c[2];
            define take_key = ["a", "b", "c", "d"][c[0]];
        }

        single enum EnumTest {
            A,
            B(Number),
            C(String, String),
            D
        }
        """)
        .EmulateTick()
        .AssertVariable("b", [1, 5])
        .AssertVariable("c", [2, "one", "two"])
        .AssertVariable("take", "two")
        .AssertVariable("take_key", "c");
    }

    [TestMethod("Parallel enum basic usage")]
    public void ParallelEnumBasicUsage()
    {
        Compile("""
        rule: "Parallel enum basic usage" {
            define b = EnumTest.B(5);
            define c = EnumTest.C("one", "two");
        }

        enum EnumTest {
            A,
            B(Number),
            C(String, String),
            D
        }
        """)
        .EmulateTick()
        .AssertVariable("b", 1)
        .AssertVariable("b_slot0", 5)
        .AssertVariable("b_slot1", 0)
        .AssertVariable("c", 2)
        .AssertVariable("c_slot0", "one")
        .AssertVariable("c_slot1", "two");
    }

    [TestMethod("Parallel enum used as any")]
    public void ParallelEnumUsedAsAny()
    {
        Compile("""
        rule: 'Use enum members as function' {
            // Expected a value of type 'Any'.
            Any x = EnumTest.A;
            
            // Cannot be used as indexer.
            define y = [][EnumTest.A];

            // Cannot be indexed.
            define z = EnumTest.A[0];
        }

        enum EnumTest {
            A,
            B(Number)
        }
        """)
        .AssertSearchError("Expected a value of type 'Any'")
        .AssertSearchError("cannot be used as an indexer")
        .AssertSearchError("This struct cannot be indexed");
    }

    [TestMethod("Parallel enum array")]
    public void ParallelEnumArray()
    {
        Compile("""
        rule: "Parallel enum basic usage" {
            EnumTest[] values = [
                EnumTest.D,
                EnumTest.C("one", "two"),
                EnumTest.B(67),
                EnumTest.A
            ];

            Number index_of_a = values.IndexOf(EnumTest.A);
        }

        enum EnumTest {
            A,
            B(Number),
            C(String, String),
            D
        }
        """)
        .EmulateTick()
        .AssertVariable("values", [3, 2, 1, 0])
        .AssertVariable("values_slot0", [0, "one", 67, 0])
        .AssertVariable("values_slot1", [0, "two", 0, 0])
        .AssertVariable("index_of_a", 3);
    }
}