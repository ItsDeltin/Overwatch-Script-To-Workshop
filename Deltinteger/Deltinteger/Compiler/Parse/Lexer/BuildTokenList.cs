#nullable enable
#if false

using System;
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

public class BuildTokenList
{
    readonly Func<Token?> tokenStream;
    readonly ITokenPush pushTokens;
    readonly List<Token> queue = new();
    LexerContextKind currentQueueContext;

    public BuildTokenList(Func<Token?> tokenStream, ITokenPush pushTokens)
    {
        this.tokenStream = tokenStream;
        this.pushTokens = pushTokens;
    }

    public Token? GetTokenAt(int index, LexerContextKind queueContext)
    {
        if (currentQueueContext != queueContext)
        {
            queue.Clear();
            currentQueueContext = queueContext;
        }
        else if (index < QueueTokensStartAt())
        {
        }
        else if (index >= QueueTokensStartAt() && index - QueueTokensStartAt() < queue.Count)
        {
            return queue[index - QueueTokensStartAt()];
        }

        int queueOffset = 0;
        while (!pushTokens.WasInsertCompleted() && pushTokens.Current + queueOffset < index)
        {
            var next = tokenStream();

            if (next is null)
                return null;

            queue.Add(next);
            queueOffset++;
        }

        return queue.LastOrDefault();
    }

    public void Commit(int until)
    {
        foreach (var push in queue)
            pushTokens.PushToken(push);
    }

    private int QueueTokensStartAt() => pushTokens.Current;
}

/// <summary>
/// When the LexController scans a token, implementers will handle the obtained token.
/// There are just 2 implementers: the InitTokenPush and the IncrementalTokenInsert.
/// </summary>
public interface ITokenPush
{
    IReadOnlyList<Token> Tokens { get; }
    int Current { get; }
    void PushToken(Token token);
    bool WasInsertCompleted();
    void OnEndOfDocumentReached();
}

/// <summary>The first lex of a file will use this for the ITokenPush.
/// When a token is encountered, simply add the token to the token list.</summary>
class InitTokenPush : ITokenPush
{
    public IReadOnlyList<Token> Tokens => _tokens;
    public int Current => _tokens.Count;
    private readonly Lexer _lexer;
    private readonly List<Token> _tokens;
    private bool _finished = false;

    public InitTokenPush(Lexer lexer)
    {
        _lexer = lexer;
        _tokens = lexer.Tokens;
    }

    public void PushToken(Token token) => _tokens.Add(token);
    public bool WasInsertCompleted() => _finished;
    public void OnEndOfDocumentReached()
    {
        _finished = true;
        _lexer.PushCompleted();
    }
}

/// <summary>Every subsequent lex after the first will use this for the ITokenPush.
/// This is used for updating the lexer incrementally. When a token is encountered, it will be
/// inserted into the token list depending on where the update occured.</summary>
class IncrementalTokenInsert : ITokenPush
{
    public IReadOnlyList<Token> Tokens => _tokens;
    public int Current { get; private set; } // Where in the token list the tokens will be inserted to when a token is found.
    private readonly Lexer _lexer;
    private readonly List<Token> _tokens; // The list of tokens.
    private bool _lexUntilEnd = true; // Determines if lexing should occur until the end of the file is reached.
    private int _stopAt; // The index to stop lexing at.
    private bool _lastInsertWasEqual; // Will be true when we have resynced with the tokens.

    public IncrementalTokenInsert(Lexer lexer, int startingTokenIndex, int stopMinimum)
    {
        Current = startingTokenIndex;
        _lexer = lexer;
        _tokens = lexer.Tokens;
        if (stopMinimum < _tokens.Count)
        {
            _stopAt = stopMinimum;
            _lexUntilEnd = false;
        }
    }

    public void PushToken(Token token)
    {
        if (_lastInsertWasEqual) return;
        // This is used to determine whether _stopAt is correctly being incremented as tokens are inserted.
        // System.Diagnostics.Debug.Assert(_lexUntilEnd || _tokens.IndexOf(_debugEndToken) == _stopAt);

        // Check if we have reached the end of the changed token range.
        // If *all* of these conditions are met, '_lastInsertWasEqual' will be set to true, the block will be skipped and lexing will stop.
        if (!(_lastInsertWasEqual =
            // First, check if _lexUntilEnd is false. _lexUntilEnd may be true when adding characters to the end of the file.
            // If it is true, we don't need to worry about stopping early since we are lexing until the end of the file.
            !_lexUntilEnd &&
            // Check if the 'Current' index is equal to or greater than _stopAt.
            Current >= _stopAt &&
            // Make sure Current is less than the count of the _tokens list.
            // This can occur if a token near the end of the file was changed that is not the last token.
            Current < _tokens.Count &&
            // Now we make sure the token we got is equal to the Current token. If it is, we don't need to lex anymore.
            // Make sure the type, range, and text are equal. In most scenarios if TokenType is the same then text will be the same, the only exceptions being numbers, strings, and identifiers.
            _tokens[Current].TokenType == token.TokenType &&
            _tokens[Current].Range.Equals(token.Range) &&
            _tokens[Current].Text == token.Text
        ))
        {
            // When the starting token was not found, insert the current token to the first index.
            if (Current == -1)
            {
                _tokens.Insert(0, token);
                // Set Current to 1.
                // This increment will set Current from -1 to 0,
                // the next increment will set Current from 0 to 1.
                Current++;
                _stopAt++; // The ending token was pushed up.
            }
            // If Current surpassed the token count, add the token to the top of the list.
            else if (Current == _tokens.Count)
            {
                _tokens.Add(token);
                _stopAt++; // The ending token was pushed up.
            }
            // If Current is the at or pass the index of the ending token, insert the token to the current position.
            else if (!_lexUntilEnd && Current >= _stopAt)
            {
                _tokens.Insert(Current, token);
                _stopAt++; // The ending token was pushed up.
            }
            // If none of the previous conditions were satisfied, we are inside
            // the modified range. Replace the current token.
            else
            {
                _tokens.RemoveAt(Current);
                _tokens.Insert(Current, token);
                // The number of tokens is unchanged, so we don't need to change _stopAt.
            }
        }
        Current++;
    }

    public bool WasInsertCompleted() => _lastInsertWasEqual;

    public void OnEndOfDocumentReached()
    {
        // Remove extraneous tokens.
        if (Current != -1)
            while (_tokens.Count > Current)
                _tokens.RemoveAt(Current);
        _lexer.PushCompleted();
    }
}
#endif