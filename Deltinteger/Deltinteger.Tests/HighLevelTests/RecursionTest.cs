namespace Deltinteger.Tests;

using System.Diagnostics;
using Deltin.Deltinteger.Emulator;
using Deltin.Deltinteger.Parse.Settings;
using static TestUtils;

[TestClass]
public class RecursiveTests
{
    [TestMethod("HL test: Inline recursion")]
    public void InlineRecursion()
    {
        Compile("""
        rule: "Test" {
            Number f0 = factorial(0);
            Number f1 = factorial(1);
            Number f2 = factorial(2);
            Number f3 = factorial(3);
            Number f4 = factorial(4);
            Number f5 = factorial(5);
            Number f6 = factorial(6);
            LogToInspector($"{f0}, {f1}, {f2}, {f3}, {f4}, {f5}, {f6}");
        }

        recursive Number factorial(Number n) {
            if (n > 0)
                return n * factorial(n - 1);
            else
                return 1;
        }
        """)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("f0", 1)
        .AssertVariable("f1", 1)
        .AssertVariable("f2", 2)
        .AssertVariable("f3", 6)
        .AssertVariable("f4", 24)
        .AssertVariable("f5", 120)
        .AssertVariable("f6", 720);
    }

    [TestMethod("HL test: Subroutine recursion")]
    public void SubroutineRecursion()
    {
        Compile("""
        rule: "Test" {
            Number f0 = factorial(0);
            Number f1 = factorial(1);
            Number f2 = factorial(2);
            Number f3 = factorial(3);
            Number f4 = factorial(4);
            Number f5 = factorial(5);
            Number f6 = factorial(6);
            LogToInspector($"{f0}, {f1}, {f2}, {f3}, {f4}, {f5}, {f6}");
        }

        recursive Number factorial(Number n) 'factorial subroutine' {
            if (n > 0)
                return n * factorial(n - 1);
            else
                return 1;
        }
        """)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("f0", 1)
        .AssertVariable("f1", 1)
        .AssertVariable("f2", 2)
        .AssertVariable("f3", 6)
        .AssertVariable("f4", 24)
        .AssertVariable("f5", 120)
        .AssertVariable("f6", 720);
    }

    [TestMethod("HL test: Arrays in recursive functions")]
    public void ArraysInRecursiveFunctions()
    {
        Compile("""
        recursive Number[] Sub(Number[] values) "" {
            Any[] arr = [];
            arr[0] = values[0] + 1;
            arr[1] = values[1] + 1;
            arr[2] = values[2] + 1;

            if (arr[0] == 10) return [];
            return arr.Append(Sub(arr));
        }

        rule: ''
        {
            define result = Sub([1, 2, 3]);
            LogToInspector(result);
        }
        """)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("result", [2, 3, 4, 3, 4, 5, 4, 5, 6, 5, 6, 7, 6, 7, 8, 7, 8, 9, 8, 9, 10, 9, 10, 11]);
    }

    [TestMethod("HL test: Arrays in recursive closures")]
    public void ArraysInRecursiveClosure()
    {
        Compile("""
        globalvar Number[] => Number[] sub = values => {
            LogToInspector('input: ' + values);

            Any[] arr = [];
            arr[0] = values[0] + 1;
            arr[1] = values[1] + 1;
            arr[2] = values[2] + 1;

            if (arr[0] == 10) return [];

            define got = sub(arr);
            LogToInspector('got: ' + got);
            return arr.Append(got);
        };


        rule: ''
        {
            define result = sub([1, 2, 3]);
            LogToInspector(result);
        }
        """)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("result", [2, 3, 4, 3, 4, 5, 4, 5, 6, 5, 6, 7, 6, 7, 8, 7, 8, 9, 8, 9, 10, 9, 10, 11]);
    }

    [TestMethod("HL test: Recursion with object stack")]
    public void RecursionWithObjectStack()
    {
        // 2 listed variables in the struct is important.
        // The variables in the object stack need to be popped in the right order.
        Compile("""
        globalvar Number[] out = [];

        struct MyStruct
        {
            public Number A;
            public Number B;

            public recursive void func(Number input) 'my subroutine' {
                input = input + 1;
                out.ModAppend(A * input + B);

                if (input < 10)
                    func(input + 1);
            }
        }

        rule: ''
        {
            MyStruct v = { A: 100, B: 10 };
            v.func(2);
        }
        """)
        .EmulateTick()
        .AssertVariable("out", [310, 510, 710, 910, 1110]);

        // Similiar test but with a class.
        Compile("""
        globalvar Number[] out = [];
        globalvar MyClass[] classes = [new MyClass(10), new MyClass(20), new MyClass(30), new MyClass(40), new MyClass(50)];

        class MyClass {
            public Number value;

            constructor(in Number v) { value = v; }

            public recursive void func(Number i) 'my subroutine' {
                out.ModAppend(value);
                if (i + 1 < classes.Length)
                    classes[i].func(i + 1);
            }
        }

        rule: '' {
            classes[0].func(0);
            LogToInspector("[BREAK]");
        }
        """)
        .EmulateTick()
        .AssertVariable("classes", [1, 2, 3, 4, 5])
        .AssertVariable("out", [10, 20, 30, 40, 50]);
    }
}