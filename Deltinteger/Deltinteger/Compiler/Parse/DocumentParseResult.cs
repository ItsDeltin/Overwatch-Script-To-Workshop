#nullable enable

namespace Deltin.Deltinteger.Compiler.Parse;

using SyntaxTree;
using Lexing;
using System.Collections.Generic;
using System;
using Deltin.Deltinteger.Compiler.File;

public record DocumentParseResult(
    RootContext Syntax,
    TokenList Tokens,
    IReadOnlyList<IParserError> ParserErrors,
    IReadOnlyList<TokenCapture> NodeCaptures,
    VersionInstance Content)
{
    public static readonly DocumentParseResult Default = new(new(), new(), Array.Empty<IParserError>(), Array.Empty<TokenCapture>(), new(string.Empty));

    public IncrementalParse Update(VersionInstance newContent, DocumentUpdateRange updateRange)
    {
        var incrementLexer = LexerIncrementalChange.Update(Tokens, Content, newContent, updateRange);
        return new IncrementalParse(incrementLexer, NodeCaptures);
    }
}