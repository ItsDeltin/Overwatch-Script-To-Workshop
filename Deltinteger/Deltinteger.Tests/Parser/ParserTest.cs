using Deltin.Deltinteger;
using Deltin.Deltinteger.Compiler;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
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

    [TestMethod("Token collision")]
    public void TokenCollision()
    {
        Setup();
        var tester = TestParser();
        tester.Sequence("""
            rule("wow") {
                event {
                    Ongoing - Global;
                }
                actions {
                    Small Message(All Players(All Teams), Custom String(""));
                }
            }
            """);
        tester.Insert("", tester.IndexOf("String(\"\")") + 8, 1);
    }

    [TestMethod("Interpolated string")]
    public void InterpolatedString()
    {
        Setup();
        var tester = TestParser();
        tester.Sequence("""
            rule: "Interpolated string test"
            {
                define x = $'Total time elapsed: {TotalTimeElapsed()}, time remaining: {60 - TotalTimeElapsed()}';
            }
            """);
        tester.AssertOk();
        // Replace single quotes with double quotes
        tester.Insert("\"", tester.IndexOf("()}';") + 3, 1);
        tester.AssertNotOk();
        tester.Insert("\"", tester.IndexOf("$'Total") + 1, 1);
        tester.AssertOk();
    }

    [TestMethod("Slide symbols together")]
    public void SlideSymbolsTogether()
    {
        Setup();
        var tester = TestParser();
        tester.Sequence("""
            rule("Slide workshop tokens together")
            {
                actions {
                    Small Message(Event 
                        Player, 0);
                }
            }
            """);

        int trimStart = tester.IndexOf("Event ") + 6;
        tester.Insert("", trimStart, tester.IndexOf("Player, 0);") - trimStart);
        tester.AssertOk();
    }
}