namespace Deltinteger.Tests;

[TestClass]
public class RecursiveCallTest
{
    [TestMethod("Restricted call test - Inline variables and functions")]
    public void InlineVariableAndFunctions()
    {
        // Inline variable/function loop
        Compile("""
        Any a: b;
        Any b: c;
        Any c: d();
        Any d(): e();
        Any e() { return a; }
        """)
        .AssertSearchError("Recursion is not allowed here, the variable 'Any b' calls 'Any a'")
        .AssertSearchError("Recursion is not allowed here, the variable 'Any c' calls 'Any b'")
        .AssertSearchError("Recursion is not allowed here, the function 'd()' calls 'Any c'")
        .AssertSearchError("Recursion is not allowed here, the function 'e()' calls 'd()'")
        .AssertSearchError("Recursion is not allowed here, the variable 'Any a' calls 'e()'");
    }

    [TestMethod("Restricted call test - Valid recursion")]
    public void ValidRecursion()
    {
        // Via function marked as recursive.
        Compile("""
        Any a: b;
        Any b: c;
        Any c: d();
        Any d(): e();
        recursive Any e() { return a; }
        """)
        .AssertOk();

        // Via subroutine.
        Compile("""
        Any a: b;
        Any b: c;
        Any c: d();
        Any d(): e();
        Any e() 'my subroutine' { return a; }
        """)
        .AssertOk();

        // Via constant function.
        Compile("""
        Any a: b;
        Any b: c(() => { return a; });
        Any c(const () => Any func): func();
        """)
        .AssertSearchError("Recursion is not allowed here, the lambda '() => Any' calls 'Any b'");

        // Non-constant function is okay.
        Compile("""
        Any a: b;
        Any b: c(() => { return a; });
        Any c(() => Any func): func();

        rule: 'compile test' { Any _ = a; }
        """)
        .AssertOk();
    }
}