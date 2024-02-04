#nullable enable

using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

public class Lexer
{
    public VersionInstance? Content { get; private set; }
    public LexController CurrentController { get; private set; }
    public int IncrementalChangeStart { get; private set; }
    public int IncrementalChangeEnd { get; private set; }
    public bool IsPushCompleted { get; private set; }
    private readonly ParserSettings _parseSettings;
    private readonly Stack<LexerContextKind> _context = new(new[] { LexerContextKind.Normal });

    public Lexer(ParserSettings parseSettings)
    {
        _parseSettings = parseSettings;
    }

    public void Init(VersionInstance content)
    {
        Content = content;
        CurrentController = new LexController(_parseSettings, Content.Text, VanillaSymbols.Instance);
    }

    public void ProgressTo(int token)
    {
        CurrentController.ProgressTo(token);
    }

    public void Reset()
    {
        Content = null;
        IsPushCompleted = false;
    }

    public Token? ScanTokenAt(int tokenIndex) => CurrentController.GetTokenAt(tokenIndex, _context.Peek());

    public Token? ScanTokenAtOrLast(int tokenIndex) => CurrentController.GetTokenAtOrLast(tokenIndex, _context.Peek());

    public void PushCompleted()
    {
        // IsPushCompleted = true;
        // IncrementalChangeEnd = Math.Max(IncrementalChangeEnd, _currentTokenPush.Current);
    }

    public int GetTokenDelta()
    {
        if (!IsPushCompleted)
            throw new Exception("Cannot get token delta until the current token push is completed.");
        return 0;
#warning GetTokenDelta
        // return Tokens.Count - _lastTokenCount;
    }

    public T InVanillaWorkshopContext<T>(Func<T> task) => InContext(LexerContextKind.Workshop, task);

    public T InSettingsContext<T>(Func<T> task) => InContext(LexerContextKind.LobbySettings, task);

    private T InContext<T>(LexerContextKind contextKind, Func<T> task)
    {
        _context.Push(contextKind);
        var result = task();
        _context.Pop();
        return result;
    }
}