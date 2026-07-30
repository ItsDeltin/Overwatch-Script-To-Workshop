namespace Deltinteger.Tests;

using static TestUtils;

[TestClass]
public class EnumTest
{
    [TestMethod("Enum test: Basis usage of enum with no inner values")]
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

    [TestMethod("Enum test: Single enum basic usage")]
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

    [TestMethod("Enum test: Parallel enum basic usage")]
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

    [TestMethod("Enum test: Parallel enum used as any")]
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

    [TestMethod("Enum test: Parallel enum array")]
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
            Number index_of_a_with_key = values.Map(v => v.Key).IndexOf(EnumTest.A.Key);
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
        .AssertVariable("index_of_a", 3)
        .AssertVariable("index_of_a_with_key", 3);
    }

    [TestMethod("Enum test: Invalid enum key type")]
    public void InvalidEnumKeyType()
    {
        Compile("""
        enum EnumTest {
            A = EffectRev.Color
        }
        """)
        .AssertSearchError("The key of an enum member cannot be a constant or parallel data type");

        Compile("""
        enum EnumTest {
            A = { x: "This shouldn't work :)" }
        }
        """)
        .AssertSearchError("The key of an enum member cannot be a constant or parallel data type");
    }

    [TestMethod("Enum test: Basic enum pattern matching - no inner values")]
    public void BasicEnumPatternMatchingNoInnerValues()
    {
        Compile("""
        rule: "Enum pattern matching" {
            if (EnumTest.B is EnumTest.A) {
                define a = true;
            }

            if (EnumTest.B is EnumTest.B) {
                define b = true;
            }

            if (EnumTest.B is EnumTest.C) {
                define c = true;
            }
        }

        enum EnumTest {
            A,
            B,
            C
        }
        """)
        .EmulateTick()
        .AssertVariable("a", 0)
        .AssertVariable("b", true)
        .AssertVariable("c", 0);
    }

    [TestMethod("Enum test: Enum Pattern Matching Shorthand")]
    public void EnumPatternMatchingShorthand()
    {
        Compile("""
        rule: "Enum pattern matching shorthand" {
            EnumTest shorthand = EnumTest.B(3, 4);

            if (shorthand is B(x, y)) {
                x = 5;
                y = 6;
            }
        }

        enum EnumTest {
            A,
            B(Number, Number)
        }
        """)
        .EmulateTick()
        .AssertVariable("shorthand", 1)
        .AssertVariable("shorthand_slot0", 5)
        .AssertVariable("shorthand_slot1", 6);
    }
}