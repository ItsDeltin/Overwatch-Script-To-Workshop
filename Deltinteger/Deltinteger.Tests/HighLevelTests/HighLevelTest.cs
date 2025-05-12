namespace Deltinteger.Tests;
using Deltin.Deltinteger.Parse.Settings;
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
        TestWithBothReferenceValidationStrategies(s =>
        Compile("""
        class A {
            public void Exec() {}
        }

        globalvar Number STEP;

        rule: "Variable Generation Test" {
            (<A>0).Exec(); // Error and abort here
            STEP = 1;      // This should not be reached 
        }
        """, classGenerations: true, referenceValidationStrategy: s)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("STEP", 0)
        .AssertSearchLog("[Error] Accessed invalid reference"));
    }

    [TestMethod("HL test: Reference validation")]
    public void ReferenceValidation()
    {
        TestWithBothReferenceValidationStrategies(s =>
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
        """, classGenerations: true, referenceValidationStrategy: s)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("STEP", 1)
        .AssertSearchLog("[Error] Accessed invalid reference"));
    }

    [TestMethod("HL test: Generation validation")]
    public void GenerationValidation()
    {
        TestWithBothReferenceValidationStrategies(s =>
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
        """, classGenerations: true, referenceValidationStrategy: s)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("POINTERS_ARE_EQUAL", true)
        .AssertVariable("STEP", 1)
        .AssertSearchLog("[Error] Accessed invalid reference"));
    }

    [TestMethod("HL test: Class array validation")]
    public void ClassArrayValidation()
    {
        TestWithBothReferenceValidationStrategies(s =>
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
        """, classGenerations: true, referenceValidationStrategy: s)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("STEP", 8)
        .AssertSearchLog("[Error] Accessed invalid reference"));
    }

    [TestMethod("HL test: Initial class values (#355)")]
    public void InitialClassValues()
    {
        TestWithBothReferenceValidationStrategies(s =>
        Compile("""
        class Class {
            public Number num;
            public Str str = { x: 1, y: 2 };
        }

        struct Str {
            public Number x;
            public Number y;
        }

        rule: "" {
            Class instance = new Class();
            define a = instance.num;
            define b = instance.str.x;
            define c = instance.str.y;
        }
        """, referenceValidationStrategy: s)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("a", 0)
        .AssertVariable("b", 1)
        .AssertVariable("c", 2));
    }

    [TestMethod("HL test: Initial class values with inheritance (#355)")]
    public void InitialClassValuesWithInheritance()
    {
        TestWithBothReferenceValidationStrategies(s =>
        Compile("""
        class A {
            public Number num;
            public Str str = { x: 1, y: 2 };
        }
        class B : A {
            public Boolean bool = true;
        }
        class C : B {
            public String text = "Hello";
        }

        struct Str {
            public Number x;
            public Number y;
        }

        rule: "" {
            A instance1 = new B();
            C instance2 = new C();

            define a = instance1.num;
            define b = instance1.str.x;
            define c = instance1.str.y;
            define d = (<B>instance1).bool;

            define e = instance2.num;
            define f = instance2.str.x;
            define g = instance2.str.y;
            define h = instance2.bool;
            define i = instance2.text;
        }
        """, referenceValidationStrategy: s)
        .AssertOk()
        .EmulateTick()
        .AssertVariable("a", 0)
        .AssertVariable("b", 1)
        .AssertVariable("c", 2)
        .AssertVariable("d", true)
        .AssertVariable("e", 0)
        .AssertVariable("f", 1)
        .AssertVariable("g", 2)
        .AssertVariable("h", true)
        .AssertVariable("i", "Hello"));
    }

    void TestWithBothReferenceValidationStrategies(Action<ReferenceValidationType> action)
    {
        action(ReferenceValidationType.Inline);
        action(ReferenceValidationType.Subroutine);
    }

    [TestMethod("HL test: Switches")]
    public void Switch()
    {
        Compile(
            """
            globalvar Number TEST_0;
            globalvar Number TEST_1;
            globalvar Number TEST_2;
            globalvar Number TEST_3;
            globalvar Number TEST_4;

            void ExecuteSwitch(in Number value, ref Number out)
            {
                switch (value)
                {
                    case 0:
                        out = 1;
                        break;

                    case 1:
                        out = 2;
                        // Fallthrough to case 2
                    case 2:
                        out += 3;
                        break;
                    
                    default:
                        out = -1;
                        break;
                    
                    case 3:
                        out = -2;
                }
            }

            rule: "Test switches"
            {
                ExecuteSwitch(0, TEST_0);
                ExecuteSwitch(1, TEST_1);
                ExecuteSwitch(2, TEST_2);
                ExecuteSwitch(3, TEST_3);
                ExecuteSwitch(4, TEST_4);
            }
            """
        )
        .AssertOk()
        .EmulateTick()
        .AssertVariable("TEST_0", 1)
        .AssertVariable("TEST_1", 5)
        .AssertVariable("TEST_2", 3)
        .AssertVariable("TEST_3", -2)
        .AssertVariable("TEST_4", -1);
    }
}