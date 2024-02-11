#nullable enable

using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

record struct LexerState(int RelexSpan, int StopRelexingAt);

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