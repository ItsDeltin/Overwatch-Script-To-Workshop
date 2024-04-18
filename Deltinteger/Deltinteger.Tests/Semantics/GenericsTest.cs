using static Deltinteger.Tests.TestUtils;

namespace Deltinteger.Tests;

[TestClass]
public class GenericsTest
{
    [TestMethod("Parent type linking - inline (#476)")]
    public void MethodParentTypeLinking()
    {
        Setup();
        Compile(
            """
            rule: 'My Rule' {
                Test<Number> t = new Test<Number>();
                t.Log();
            }

            class Test<T> {
                public void LogInner() {}

                public void Log() {
                    LogInner();
                }
            }
            """
        ).AssertOk();
    }

    [TestMethod("Parent type linking - subroutine (#476)")]
    public void MethodParentTypeLinkingSubroutine()
    {
        Setup();
        Compile(
            """
            rule: 'My Rule' {
                # init
                Test<Struct> t = new Test<Struct>();
                # log
                t.Log();
            }

            struct Struct {
                public Number x;
                public Number y;
            }

            class Test<T> {
                public void LogInner() 'Inner Subroutine' {}

                public void Log() 'Outer Subroutine' {
                    LogInner();
                }
            }
            """
        ).AssertOk();
    }
}