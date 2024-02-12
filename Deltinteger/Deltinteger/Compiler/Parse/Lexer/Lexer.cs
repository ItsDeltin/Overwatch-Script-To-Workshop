#nullable enable

using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.File;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

public class Lexer
{
    public LexController CurrentController { get; private set; }

    private readonly ParserSettings _parseSettings;
    private readonly LexerIncrementalChange? _incrementalChange;

    private readonly Stack<LexerContextKind> _context = new(new[] { LexerContextKind.Normal });

    public Lexer(
        ParserSettings parseSettings,
        LexerIncrementalChange? incrementalChange,
        VersionInstance content)
    {
        _parseSettings = parseSettings;
        _incrementalChange = incrementalChange;
        CurrentController = new LexController(_parseSettings, content.Text, VanillaSymbols.Instance, _incrementalChange);
    }

    public void ProgressTo(int token)
    {
        CurrentController.ProgressTo(token);
    }

    public Token? ScanTokenAt(int tokenIndex) => CurrentController.ScanTokenAt(tokenIndex, _context.Peek());

    public Token? ScanTokenAtOrLast(int tokenIndex) => CurrentController.ScanTokenAtOrLast(tokenIndex, _context.Peek());

    public Token? GetTokenAtOrEnd(int tokenIndex) => CurrentController.GetTokenAtOrLast(tokenIndex)?.Token;

    public int GetTokenDelta() => CurrentController.GetTokenDelta();

    public bool IsLexCompleted() => CurrentController.IsLexCompleted();

    public int? LexResyncedAt() => CurrentController.LexResyncedAt();

    public T InVanillaWorkshopContext<T>(Func<T> task) => InContext(LexerContextKind.Workshop, task);

    public T InSettingsContext<T>(Func<T> task) => InContext(LexerContextKind.LobbySettings, task);

    public T InContext<T>(LexerContextKind contextKind, Func<T> task)
    {
        _context.Push(contextKind);
        var result = task();
        _context.Pop();
        return result;
    }
}