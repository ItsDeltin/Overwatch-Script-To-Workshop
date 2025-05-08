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

    [TestMethod("Call ref function from another local function")]
    public void CallRefFunctionFromAnotherLocalFunction()
    {
        Setup();

        // Calling ref from non-ref function: Error
        Compile("""
        struct TestStruct {
            public Number Local;

            public void FuncA() {
                FuncB();
            }

            public ref void FuncB() {
                Local = 3;
            }
        }
        """).AssertSearchError("Cannot call ref function in a non-ref function");

        // Calling ref from ref function: Ok
        Compile("""
        struct TestStruct {
            public Number Local;

            public ref void FuncA() {
                FuncB();
            }

            public ref void FuncB() {
                Local = 3;
            }
        }
        """).AssertOk();
    }

    [TestMethod("Parallel struct indexers (#351)")]
    public void ParallelStructIndexers()
    {
        // Not ok: index into struct
        Compile("""
        struct Str {
            Number a;
        }

        rule: ''
        {
            Str a;
            a[0] = 1;
        }
        """).AssertSearchError("This struct cannot be indexed");

        // Ok: index into struct array
        Compile("""
        struct Str {
            Number a;
        }

        rule: ''
        {
            Str[] a;
            a[0] = { a: 0 };
        }
        """).AssertOk();

        // Not ok: index into struct
        Compile("""
        struct Str {
            Number a;
        }
        Str str(): { a: 0 };

        rule: ''
        {
            Any a = str()[0];
        }
        """).AssertSearchError("This struct cannot be indexed");

        // Ok: index into struct array
        Compile("""
        struct Str {
            Number a;
        }
        Str[] str(): [{ a: 0 }];

        rule: ''
        {
            Str a = str()[0];
        }
        """).AssertOk();

        // Ok: index into single struct
        Compile("""
        single struct Str {
            Number a;
        }

        rule: ''
        {
            Str a;
            a[0] = 1;
        }
        """).AssertOk();

        // Ok: index into single struct
        Compile("""
        single struct Str {
            Number a;
        }
        Str str(): { a: 0 };

        rule: ''
        {
            Any a = str()[0];
        }
        """).AssertOk();
    }
}