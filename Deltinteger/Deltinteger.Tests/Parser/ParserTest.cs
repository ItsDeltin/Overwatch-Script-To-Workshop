using Deltin.Deltinteger;
using Deltin.Deltinteger.Compiler;
using static Deltinteger.Tests.ParserTestUtil;
using static Deltinteger.Tests.TestUtils;

namespace Deltinteger.Tests;

[TestClass]
public class ParserTest
{
    [TestMethod("Basic parser test")]
    public void BasicParserTest()
    {
        Setup();
        TestTokens("rule: \"hello\" {}", TokenType.Rule, TokenType.Colon, TokenType.String, TokenType.CurlyBracket_Open, TokenType.CurlyBracket_Close);
    }

    [TestMethod("Lexer increment test")]
    public void LexerIncrementTest()
    {
        Setup();
        var tester = TestIncrementer("rule: \"hello!\"")
            .Assert(TokenType.Rule, TokenType.Colon, TokenType.String);

        // Remove the 'rule' keyword
        var change = tester.Increment(tester.GetUpdateRange("", 0, 4));

        AssertLexerIncrement(change, changeStartToken: 0, stopLexingAtCharacter: 10);
        tester.Assert();

        // Lex again with incremented data.
        tester.LexUntilEnd(change);
        tester.Assert(TokenType.Colon, TokenType.String);
    }

    [TestMethod("Multiline increment test")]
    public void MultilineIncrementTest()
    {
        Setup();
        var tester = TestIncrementer("""
            rule: "Test incremental lexer on multiple lines"
            Event.OngoingPlayer
            {
            }
            """);

        // Remove the 'rule' keyword
        var change = tester.Increment(tester.GetUpdateRange("", 50, 5));

        AssertLexerIncrement(change,
            changeStartToken: 3,
            stopLexingAtCharacter: tester.IndexOfSnippet("{")
        );
        // Ensure correct items were removed.
        tester.Assert(TokenType.Rule, TokenType.Colon, TokenType.String, TokenType.CurlyBracket_Open, TokenType.CurlyBracket_Close);

        // Lex again with incremented data.
        tester.LexUntilEnd(change);
    }

    [TestMethod("Realtime typing")]
    public void SequentialTyping()
    {
        Setup();
        var tester = TestIncrementer().Assert();
        tester.Append("r").Assert(TokenType.Identifier);
        tester.Append("u").Assert(TokenType.Identifier);
        tester.Append("le").Assert(TokenType.Rule);
        tester.Append(":").Assert(TokenType.Rule, TokenType.Colon);
        tester.Append(" \"").Assert(TokenType.Rule, TokenType.Colon, TokenType.String);
        tester.Append("my rule").Assert(TokenType.Rule, TokenType.Colon, TokenType.String);
        tester.Append("\"").Assert(TokenType.Rule, TokenType.Colon, TokenType.String);
        tester.Append("\n{").Assert(TokenType.Rule, TokenType.Colon, TokenType.String, TokenType.CurlyBracket_Open);
        tester.Append("\n}").Assert(TokenType.Rule, TokenType.Colon, TokenType.String, TokenType.CurlyBracket_Open, TokenType.CurlyBracket_Close);
        tester.Insert("_", 1, 2).Assert(TokenType.Identifier, TokenType.Colon, TokenType.String, TokenType.CurlyBracket_Open, TokenType.CurlyBracket_Close);
        tester.Insert("ul", 1, 1).Assert(TokenType.Rule, TokenType.Colon, TokenType.String, TokenType.CurlyBracket_Open, TokenType.CurlyBracket_Close);
        tester.Append("\n").Assert(TokenType.Rule, TokenType.Colon, TokenType.String, TokenType.CurlyBracket_Open, TokenType.CurlyBracket_Close);
        tester.Append("\n").Assert(TokenType.Rule, TokenType.Colon, TokenType.String, TokenType.CurlyBracket_Open, TokenType.CurlyBracket_Close);
    }

    [TestMethod("Incremental parser test")]
    public void IncrementalParserTest()
    {
        Setup();
        var tester = TestParser();
        tester.Sequence("""
        rule: "my rule" {

        }
        rule("my vanilla rule") {
            event
            {
                Ongoing - Global;
            }
            actions
            {
                Small Message(All Players(All Teams), Custom String("This is a test!"));
            }
        }
        """);
        tester.Insert("", tester.Length - 1, 1); // Delete last character
        tester.DeleteSnippet("Custom String(\"This is a test!\")", "");
        tester.Append("}");
    }
}