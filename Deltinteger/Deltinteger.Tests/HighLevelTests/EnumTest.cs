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

    [TestMethod("Enum test: Incomplete syntax errors")]
    public void IncompleteEnumErrors()
    {
        // Ensure that enums can have incomplete syntax
        // without exploding ostw :)
        Compile("""
        enum 
        """)
        .AssertSearchError("identifier expected");

        Compile("""
        enum TestEnum
        """)
        .AssertSearchError("{ expected");

        Compile("""
        rule: "Incomplete syntax errors" {
            if (0 is
        """);

        Compile("""
        rule: "Incomplete syntax errors" {
            if (0 is A.
        """);
    }

    [TestMethod("Enum test: Incompatible pattern matching errors")]
    public void IncompatiblePatternMatchingErrors()
    {
        // No inner values: OK
        Compile("""
        enum EnumTest {
            A
        }

        rule: "Incompatible pattern matching errors" {
            if (0 is EnumTest.A) {}
        }
        """)
        .AssertOk();

        // Single: OK
        Compile("""
        single enum EnumTest {
            A(Number)
        }

        rule: "Incompatible pattern matching errors" {
            if (0 is EnumTest.A) {}
        }
        """)
        .AssertOk();

        // Parallel: Not ok
        Compile("""
        enum EnumTest {
            A(Number)
        }

        rule: "Incompatible pattern matching errors" {
            if (0 is EnumTest.A) {}
        }
        """)
        .AssertSearchError("Operand type 'Number' cannot be used to pattern match with parallel enum type 'EnumTest'");

        // Constant/parallel operand: Not ok
        Compile("""
        enum EnumTest {
            A
        }

        rule: "Incompatible pattern matching errors" {
            if ({struct_value: 0} is EnumTest.A) {}
        }
        """)
        .AssertSearchError("Constant or parallel operand type '{Number struct_value}' cannot be used to pattern match with enum type 'EnumTest'");
    }

    [TestMethod("Enum test: Extraneous variable binding error")]
    public void ExtraneousVariableBindingError()
    {
        Compile("""
        enum EnumTest {
            A
        }

        rule: "Extraneous variable binding error" {
            if (0 is EnumTest.A(value)) {}
        }
        """)
        .AssertSearchError("Extraneous variable binding for enum member 'A'");
    }

    [TestMethod("Enum test: Variable binding mutability")]
    public void VariableBindingMutability()
    {
        // Immutable pattern matching operand must have an error
        // when attempting to set the bound variable.
        Compile("""
        enum EnumTest {
            A(Number),
            B(String, String)
        }

        rule: "Variable binding mutability" {
            if (EnumTest.A(0) is EnumTest.A(value)) {
                value = 5;
            }
        }
        """)
        .AssertSearchError("The variable 'value' cannot be set");

        // Operand is mutable, this is okay!
        Compile("""
        enum EnumTest {
            A(Number),
            B(String, String)
        }

        rule: "Variable binding mutability" {
            EnumTest b = EnumTest.B("first", "second");
            if (b is EnumTest.B(first, second)) {
                first = "1st";
                second = "2nd";
            }
        }
        """)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("b", 1)
        .AssertVariable("b_slot0", "1st")
        .AssertVariable("b_slot1", "2nd");

        // Same as before, but with single enum.
        Compile("""
        single enum EnumTest {
            A(Number),
            B(String, String)
        }

        rule: "Variable binding mutability" {
            EnumTest b = EnumTest.B("first", "second");
            if (b is EnumTest.B(first, second)) {
                first = "1st";
                second = "2nd";
            }
        }
        """)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("b", [1, "1st", "2nd"]);
    }

    [TestMethod("Enum test: Player variable binding")]
    public void PlayerVariableBinding()
    {
        Compile("""
        enum EnumTest {
            A(Number),
            B(String, String)
        }

        globalvar Player p = HostPlayer();
        playervar EnumTest value;

        rule: ""
        {
            p.value = EnumTest.B("one", "two");

            if (p.value is EnumTest.B(first, second)) {
                second = "three";
            }
        }
        """)
        .EmulateTick(new(WithHostPlayer: true))
        .AssertPlayerVariable(HostName, "value", 1)
        .AssertPlayerVariable(HostName, "value_slot0", "one")
        .AssertPlayerVariable(HostName, "value_slot1", "three");
    }

    [TestMethod("Enum test: Single enum array protection warning")]
    public void SingleEnumArrayProtectionWarning()
    {
        Compile("""
        single enum EnumTest { A(Number) }

        rule: ""
        {
            EnumTest[] values;
            values += <Any>0;
        }
        """)
        .AssertSearchError("Please narrow down the type of the value you are appending")
        .AssertOk();
    }

    [TestMethod("Enum test: Single enum array value guarding")]
    public void SingleEnumArrayValueGuarding()
    {
        Compile("""
        single enum EnumTest { A(String, String) }

        rule: "Behaviour test"
        {
            EnumTest[] test1 = [EnumTest.A("first", "1st")];
            test1 += EnumTest.A("second", "2nd");
        }

        rule: "Intentionally breaking the rules!"
        {
            EnumTest[] test2 = [EnumTest.A("first", "1st")];
            // This adds a compiler warning.
            test2 += <Any>EnumTest.A("second", "2nd");
        }
        """)
        .EmulateTick()
        .AssertVariable("test1", [
            EmulateValue.From([0, "first", "1st"]),
            EmulateValue.From([0, "second", "2nd"])])
        .AssertVariable("test2", [
            EmulateValue.From([0, "first", "1st"]), 0, "second", "2nd"]);
    }

    [TestMethod("Enum test: Recursive enum error")]
    public void RecursiveEnumError()
    {
        // Direct recursion
        Compile("""
        enum A { value(A) }
        """)
        .AssertSearchError("Type 'A' calls itself recursively");

        // Recursion through another type
        Compile("""
        enum A { value(B) }
        enum B { value(A) }
        """)
        .AssertSearchError("Type 'A' calls itself recursively")
        .AssertSearchError("Type 'B' calls itself recursively");

        Compile("""
        enum A { value(B) }
        struct B { A value; }
        """)
        .AssertSearchError("Type 'A' calls itself recursively");

        // Via type arguments
        Compile("""
        enum A { value(B<A>) }
        enum B<T> { value(T) }
        """)
        .AssertSearchError("Type 'A' calls itself recursively");

        Compile("""
        enum A { value(B<A>) }
        struct B<T> { T value; }
        """)
        .AssertSearchError("Type 'A' calls itself recursively");

        // Okay if 'T' is not used a value.
        Compile("""
        enum A { value(B<A>) }
        enum B<T> { value(Number) }
        """)
        .AssertOk();

        Compile("""
        enum A { value(B<A>) }
        struct B<T> { Number value; }
        """)
        .AssertOk();

        // Through an array
        Compile("""
        enum A { value(A[]) }
        """)
        .AssertSearchError("Type 'A' calls itself recursively");
    }
}