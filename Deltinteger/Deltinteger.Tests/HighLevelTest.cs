namespace Deltinteger.Tests;
using static TestUtils;

[TestClass]
public class HighLevelTest
{
    [TestMethod]
    public void TestEmulation()
    {
        Compile("""
        globalvar Number RESULT;

        rule: "Test" {
            RESULT = 3;
        }
        """)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("RESULT", 3);
    }

    [TestMethod]
    public void TestEmulationIfChain()
    {
        UnitTest("""
        globalvar Number RESULT;

        rule: "Test" {
            Number value = {0};
            if (value == 1) { RESULT = 1; }
            else if (value == 3) { RESULT = 2; }
            else { RESULT = 3; }
        }
        """,
        (["0"], [("RESULT", 3)]),
        (["1"], [("RESULT", 1)]),
        (["2"], [("RESULT", 3)]),
        (["3"], [("RESULT", 2)]),
        (["4"], [("RESULT", 3)])
        );
    }

    [TestMethod]
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

    [TestMethod]
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
}