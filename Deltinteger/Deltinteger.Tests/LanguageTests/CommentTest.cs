namespace Deltinteger.Tests;

[TestClass]
public class CommentTest
{
    [DataTestMethod]
    [DataRow("plain comment", "plain comment")]
    [DataRow("""test " test""", """test \" test""")]
    [DataRow("""say "hello" here""", """say \"hello\" here""")]
    [DataRow("""test \" test""", """test \" test""")]
    [DataRow("""test \\ test""", """test \\ test""")]
    [DataRow("""test \\" test""", """test \\\" test""")]
    [DataRow("""test \" and " test""", """test \" and \" test""")]
    public void EscapeQuotesInActionAndConditionComments(string comment, string expected)
    {
        var result = Compile(
            $$"""
            rule: "Comment"
            # {{comment}}
            if (IsGameInProgress())
            {
                # {{comment}}
                Wait(1);
            }
            """).AssertOk();

        Assert.AreEqual(2, result.Code.Split($"\"{expected}\"").Length - 1, result.Code);
        Compile(result.Code).AssertOk();
    }
}
