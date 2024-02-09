using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.File;
using Deltin.Deltinteger.Compiler.Parse;
using Deltin.Deltinteger.Compiler.Parse.Lexing;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;

namespace Deltinteger.Tests;

static class ParserTestUtil
{
    public static void TestTokens(string content, params TokenType[] tokenTypes) => AssertTokens(OstwParse(content, null).Tokens, tokenTypes);

    public static void AssertTokens(TokenList tokens, params TokenType[] tokenTypes)
    {
        Assert.AreEqual(tokens.Count, tokenTypes.Length, "Token counts are mismatched");
        for (int i = 0; i < tokenTypes.Length; i++)
        {
            Assert.AreEqual(tokenTypes[i], tokens[i].TokenType);
        }
    }

    public static void AssertLexerIncrement(LexerIncrementalChange change, int changeStartToken, int stopLexingAtCharacter)
    {
        Assert.AreEqual(changeStartToken, change.ChangeStartToken);
        Assert.AreEqual(stopLexingAtCharacter, change.StopLexingAtIndex);
    }

    public static DocumentParseResult OstwParse(string content, IncrementalParse? increment)
    {
        var p = new Parser(new(content), ParserSettings.Default, null);
        return p.Parse();
    }

    public static TestLexIncrementer TestIncrementer(string? initialContent = null) => new(initialContent ?? string.Empty);

    public static TestIncrementalParser TestParser() => new(string.Empty);

    public static DocumentUpdateRange GetAppendRange(VersionInstance content, string text)
    {
        var position = content.GetPos(content.Text.Length);
        return new(new(position, position), text, 0);
    }

    public static DocumentUpdateRange GetInsertRange(VersionInstance content, string insertText, int start, int length)
    {
        var startPos = content.GetPos(start);
        var endPos = content.GetPos(start + length);
        return new(startPos + endPos, insertText, length);
    }
}

class TestLexIncrementer
{
    VersionInstance content;
    TokenList? currentList;

    public TestLexIncrementer(string initialContent)
    {
        content = new(initialContent);
        LexUntilEnd(null);
    }

    public void LexUntilEnd(LexerIncrementalChange? incrementalChange)
    {
        var controller = new LexController(ParserSettings.Default, content.Text, VanillaSymbols.Instance, incrementalChange);

        int current = 0;
        while (controller.GetTokenAt(current, LexerContextKind.Normal))
            current++;

        currentList = controller.GetTokenList();
    }

    public LexerIncrementalChange Increment(DocumentUpdateRange updateRange)
    {
        var newContent = new VersionInstance(updateRange.ApplyChangeToString(content.Text));
        var incrementInfo = LexerIncrementalChange.Update(currentList!, content, newContent, updateRange);
        content = newContent;
        return incrementInfo;
    }

    void IncrementAndLex(DocumentUpdateRange updateRange)
    {
        var incrementInfo = Increment(updateRange);
        LexUntilEnd(incrementInfo);
    }

    public TestLexIncrementer Append(string text)
    {
        IncrementAndLex(ParserTestUtil.GetAppendRange(content, text));
        return this;
    }

    public TestLexIncrementer Insert(string text, int start, int length)
    {
        IncrementAndLex(GetUpdateRange(text, start, length));
        return this;
    }

    public DocumentUpdateRange GetUpdateRange(string text, int start, int length) => ParserTestUtil.GetInsertRange(content, text, start, length);

    public TestLexIncrementer Assert(params TokenType[] tokens)
    {
        ParserTestUtil.AssertTokens(currentList!, tokens);
        return this;
    }

    public int IndexOfSnippet(string text) => content.Text.IndexOf(text);
}

class TestIncrementalParser
{
    VersionInstance content;
    DocumentParseResult? lastParseResult;

    public TestIncrementalParser(string initialContent)
    {
        content = new(initialContent);
        lastParseResult = DoParse(null);
    }

    DocumentParseResult DoParse(IncrementalParse? increment)
    {
        var parser = new Parser(content, ParserSettings.Default, increment);
        return parser.Parse();
    }

    public void ApplyChange(DocumentUpdateRange updateRange)
    {
        content = new VersionInstance(updateRange.ApplyChangeToString(content.Text));
        lastParseResult = DoParse(lastParseResult?.Update(content, updateRange));

        // Compare incremental parse with parse from scratch
        var fromScratch = DoParse(null);
        for (int i = 0; i < lastParseResult.Tokens.Count && i < fromScratch.Tokens.Count; i++)
        {
            Token a = fromScratch.Tokens[i], b = lastParseResult.Tokens[i];
            Assert.AreEqual(a.TokenType, b.TokenType);
            Assert.AreEqual(a.Range, b.Range);
        }
        Assert.AreEqual(fromScratch.Tokens.Count, lastParseResult.Tokens.Count);
    }

    public TestIncrementalParser Append(string text)
    {
        ApplyChange(ParserTestUtil.GetAppendRange(content, text));
        return this;
    }

    public TestIncrementalParser Insert(string text, int start, int length)
    {
        ApplyChange(ParserTestUtil.GetInsertRange(content, text, start, length));
        return this;
    }

    public TestIncrementalParser Sequence(string text)
    {
        foreach (var c in text)
            Append(c.ToString());
        return this;
    }

    public int Length => content.Text.Length;

    public int IndexOf(string snippet) => content.Text.IndexOf(snippet);

    public TestIncrementalParser DeleteSnippet(string snippet, string replaceWith)
    {
        var pos = IndexOf(snippet);
        return Insert(replaceWith, pos, replaceWith.Length);
    }
}