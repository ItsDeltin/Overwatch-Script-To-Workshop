namespace Deltinteger.Tests;

[TestClass]
public class ImportJsonTest
{
    [TestMethod("Import json object")]
    [DeploymentItem(@"Assets/TestJsonImport/Struct.json")]
    public void ImportJsonObject()
    {
        Compile("""
        rule: "" {
            define x = import("Struct.json");
        }
        """).AssertOk().EmulateTick()
        .AssertVariable("x_number", 1)
        .AssertVariable("x_string", "b")
        .AssertVariable("x_true", true)
        .AssertVariable("x_false", false)
        .AssertVariable("x_array", [1, 2, 3])
        .AssertVariable("x_struct_one", 1)
        .AssertVariable("x_struct_two", 2)
        .AssertVariable("x_struct_three", 3)
        .AssertVariable("x_vector", EmulateValue.From(50, 50, 50))
        .AssertVariable("x_color", EmulateValue.From(1, 2, 3, 4))
        ;
    }

    [TestMethod("Import json array")]
    [DeploymentItem(@"Assets/TestJsonImport/Array.json")]
    public void ImportJsonArray()
    {
        Compile("""
        rule: "" {
            define x = import("Array.json");
        }
        """).AssertOk().EmulateTick()
        .AssertVariable("x", [1, 2, 3, 4]);
    }

    [TestMethod("Import json value")]
    [DeploymentItem(@"Assets/TestJsonImport/Value.json")]
    public void ImportJsonValue()
    {
        Compile("""
        rule: "" {
            define x = import("Value.json");
        }
        """).AssertOk().EmulateTick()
        .AssertVariable("x", "Hello!");
    }
}