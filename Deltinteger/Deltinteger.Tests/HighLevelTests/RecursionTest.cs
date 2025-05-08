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
}