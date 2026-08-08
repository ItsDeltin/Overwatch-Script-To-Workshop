namespace Deltinteger.Tests;

[TestClass]
public class ArrayTest
{
    [TestMethod("Array Modification Function Validation")]
    public void ArrayModificationFunctionValidation()
    {
        // Basic usage
        Compile("""
        rule: "" {
            Number[] a = [3];
            Number[] b = [5];

            a += 4;
            b.ModAppend(6);
        }
        """)
        .EmulateTick()
        .AssertVariable("a", [3, 4])
        .AssertVariable("b", [5, 6]);

        // Ensure immutable variables cannot be modified.
        Compile("""
        rule: "" {
            Number[] a: [3];
            Number[] b: [5];

            a += 4;
            b.ModAppend(6);
        }
        """)
        .AssertSearchError("variable 'a' cannot be set")
        .AssertSearchError("Functions that directly modify arrays requires a mutable variable as the source");

        // Ensure variables contained in structs can be modified.
        Compile("""
        struct Struct {
            public Number[] arr;
        }

        rule: "" {
            Struct str = { arr: [] };

            str.arr += 3;
            str.arr.ModAppend(4);
        }
        """)
        .EmulateTick()
        .AssertVariable("str_arr", [3, 4]);

        // Ensure variables from immutable structs cannot be modified.
        // ModAppend
        Compile("""
        struct Struct {
            public Number[] arr;
        }

        Struct get_struct(): { arr: [] };

        rule: "" {
            get_struct().arr.ModAppend(4);
        }
        """)
        .AssertSearchError("Functions that directly modify arrays requires a mutable variable as the source");

        // +=
        Compile("""
        struct Struct {
            public Number[] arr;
        }

        Struct get_struct(): { arr: [] };

        rule: "" {
            get_struct().arr += [4];
        }
        """)
        .AssertSearchError("The variable 'arr' cannot be set in the current context");
    }

    [TestMethod("Modify Player Array in Chain")]
    public void ModifyPlayerArrayInChain()
    {
        Compile("""
        struct Str
        {
            public Number[][] value;
        }

        playervar Str str;

        rule: "" {
            HostPlayer().str = { value: [[1, 2, 3], [4, 5], [7, 8]] };
            HostPlayer().str.value[1] += 6;
            HostPlayer().str.value[2].ModAppend(9);
        }
        """)
        .EmulateTick(new(WithHostPlayer: true))
        .AssertPlayerVariable(HostName, "str_value", EmulateValue.From([
            EmulateValue.From([1,2,3]),
            EmulateValue.From([4,5,6]),
            EmulateValue.From([7,8,9])
        ]));
    }
}