namespace Deltinteger.Tests;
using static TestUtils;

[TestClass]
public class HighLevelTest
{
    [TestMethod("Test emulation")]
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

    [TestMethod("Test emulation: If chain")]
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

    [TestMethod("HL test: class allocation")]
    public void ClassAllocation()
    {
        void test(bool classGenerations)
        {
            Compile("""
            class A {
                public Number Value;
            }

            globalvar Number A1_VALUE;
            globalvar Number A2_VALUE;
            globalvar Number A3_VALUE;

            rule: "Variable Generation Test" {
                A a1 = new A();
                A a2 = new A();
                A a3 = new A();

                a1.Value = 1;
                a2.Value = 2;
                a3.Value = 3;

                A1_VALUE = a1.Value;
                A2_VALUE = a2.Value;
                A3_VALUE = a3.Value;
            }
            """, classGenerations: classGenerations)
            .AssertOk()
            .EmulateTick()
            .AssertVariable("A1_VALUE", 1)
            .AssertVariable("A2_VALUE", 2)
            .AssertVariable("A3_VALUE", 3);
        }
        // Test with classGenerations enabled and disabled
        test(false);
        test(true);
    }

    [TestMethod("HL test: Inheritance & overrides")]
    public void InheritanceAndOverrides()
    {
        void test(bool classGenerations)
        {
            Compile("""
            class A {
                public virtual Number Value: 1;
                public virtual Number Get() { return 1; }
            }
            class B : A {
                // skip
            }
            class C : B {
                public override Number Value: 2;
                public override Number Get() { return 2; }
            }
            class D : A {
                public override Number Value: 3;
                public override Number Get() { return 3; }
            }
            globalvar Number A1; globalvar Number A2;
            globalvar Number B1; globalvar Number B2;
            globalvar Number C1; globalvar Number C2;
            globalvar Number D1; globalvar Number D2;

            rule: "Test" {
                A[] items = [new A(), new B(), new C(), new D()];
                A1 = items[0].Value; A2 = items[0].Get();
                B1 = items[1].Value; B2 = items[1].Get();
                C1 = items[2].Value; C2 = items[2].Get();
                D1 = items[3].Value; D2 = items[3].Get();
            }
            """, classGenerations: classGenerations)
            .AssertOk()
            .EmulateTick()
            .AssertVariable("A1", 1).AssertVariable("A2", 1)
            .AssertVariable("B1", 1).AssertVariable("B2", 1)
            .AssertVariable("C1", 2).AssertVariable("C2", 2)
            .AssertVariable("D1", 3).AssertVariable("D2", 3);
        }
        // Test with classGenerations enabled and disabled
        test(false);
        test(true);
    }

    [TestMethod("HL test: Simple reference validation")]
    public void SimpleReferenceValidation()
    {
        Compile("""
        class A {
            public void Exec() {}
        }

        globalvar Number STEP;

        rule: "Variable Generation Test" {
            (<A>0).Exec(); // Error and abort here
            STEP = 1;      // This should not be reached 
        }
        """, classGenerations: true)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("STEP", 0)
        .AssertSearchLog("[Error] Accessed invalid reference");
    }

    [TestMethod("HL test: Reference validation")]
    public void ReferenceValidation()
    {
        Compile("""
        class A {
            public void Exec() {}
        }

        globalvar Number STEP;

        rule: "Variable Generation Test" {
            A a = new A();
            a.Exec(); // Should continue OK 

            delete a;
            STEP = 1;
            
            a.Exec(); // Error and abort here
            STEP = 2; // This should not be reached
        }
        """, classGenerations: true)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("STEP", 1)
        .AssertSearchLog("[Error] Accessed invalid reference");
    }

    [TestMethod("HL test: Generation validation")]
    public void GenerationValidation()
    {
        Compile("""
        class A {
            public void Exec() {}
        }

        globalvar Number STEP;
        globalvar Boolean POINTERS_ARE_EQUAL;

        rule: "Variable Generation Test" {
            A a1 = new A();
            delete a1;

            // Should replace a1's index.
            A a2 = new A();

            POINTERS_ARE_EQUAL = XOf(<Any>a1) == XOf(<Any>a2);

            // Should work with new referecne
            a2.Exec();
            STEP = 1;

            // Should fail on old reference
            a1.Exec(); // Error and abort here
            STEP = 2;  // This should not be reached
        }
        """, classGenerations: true)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("POINTERS_ARE_EQUAL", true)
        .AssertVariable("STEP", 1)
        .AssertSearchLog("[Error] Accessed invalid reference");
    }

    [TestMethod("HL test: Class array validation")]
    public void ClassArrayValidation()
    {
        Compile("""
        class A {
            public void Exec() {}
        }

        globalvar Number STEP;

        rule: "Variable Generation Test" {
            A[] a = [];
            for (Number i = 0; i < 10; i++)
                a[i] = new A();
            
            delete a[8];

            for (STEP = 0; a.Length; 1)
                a[STEP].Exec();
        }
        """, classGenerations: true)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("STEP", 8)
        .AssertSearchLog("[Error] Accessed invalid reference");
    }
}