#nullable enable

using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

record struct LexerState(int RelexSpan, int StopRelexingAt, bool LexCompleted, int? ResyncToken);

record LexerStateModification(int Token, LexerState State, int? StartOverwriteAt, int OverwriteTokenCount, List<TokenNode> AddedTokens)
{
    public TokenNode? GetAtIndex(int index, out int newIndex)
    {
        if (index < Token)
        {
            newIndex = index + OverwriteTokenCount;
            return null;
        }
        else if (index < Token + AddedTokens.Count)
        {
            newIndex = index;
            return AddedTokens[index - Token];
        }
        else
        {
            newIndex = index + OverwriteTokenCount - AddedTokens.Count;
            return null;
        }
    }

    public int Delta() => AddedTokens.Count - OverwriteTokenCount;

    public void Commit(TokenList tokens)
    {
        for (int i = 0; i < AddedTokens.Count; i++)
        {
            if (StartOverwriteAt is not null && StartOverwriteAt == i)
            {
                for (int remove = 0; remove < OverwriteTokenCount; remove++)
                    tokens.RemoveAt(Token + StartOverwriteAt.Value + remove);
            }
            tokens.Add(Token + i, AddedTokens[i]);
        }
    }
}

class LexerStateManager
{
    public int TokenCount { get; private set; }
    public LexerState CurrentState { get; private set; }
    readonly TokenList tokens;
    readonly List<LexerStateModification> states = [];
    int lockedInTokens;
    LexerState lockedInState;

    public LexerStateManager(TokenList tokens, int stopRelexingAt)
    {
        this.tokens = tokens;
        lockedInState = new(0, stopRelexingAt, false, null);
        TokenCount = tokens.Count;
        CurrentState = lockedInState;
    }

    public void AddState(LexerStateModification stateModification)
    {
        states.Add(stateModification);
        TokenCount += stateModification.Delta();
        CurrentState = stateModification.State;
    }

    public void DiscardCurrentState()
    {
        states.Clear();
        TokenCount = tokens.Count;
        CurrentState = lockedInState;
    }

    public TokenNode GetTokenAt(int index)
    {
        for (int i = states.Count - 1; i >= 0; i--)
        {
            var item = states[i].GetAtIndex(index, out index);
            if (item is not null)
                return item;
        }
        return tokens.GetNode(index);
    }

    public void ProgressTo(int tokenIndex)
    {
        lockedInTokens = tokenIndex;

        while (states.Count > 0 && states[0].Token < lockedInTokens)
        {
            states[0].Commit(tokens);
            lockedInState = states[0].State;
            states.RemoveAt(0);
        }
    }
}