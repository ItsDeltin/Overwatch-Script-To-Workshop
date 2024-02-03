#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

public class TokenList
{
    readonly List<Token> tokens = new();
    readonly Dictionary<Token, TokenNode> tokenData = new();

    public int Count => tokens.Count;

    public Token this[int i] { get => tokens[i]; }

    public bool Add(Token token, LexPosition endPosition, LexerContextKind contextKind)
    {
        if (tokenData.TryAdd(token, new(Count, endPosition, contextKind)))
        {
            tokens.Add(token);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        tokenData.Remove(tokens[index]);
        tokens.RemoveAt(index);
    }

    public TokenNode GetNode(int index) => tokenData[tokens[index]];

    public int IndexOf(Token token) => tokenData[token].Index;

    public Token Last() => tokens.Last();

    public Token? ElementAtOrDefault(int index) => tokens.ElementAtOrDefault(index);
}

public record struct TokenNode(int Index, LexPosition EndPosition, LexerContextKind ContextKind);

public readonly struct ReadonlyTokenList
{
    readonly TokenList tokens;

    public ReadonlyTokenList(TokenList tokens) => this.tokens = tokens;

    public readonly Token? NextToken(Token token) => tokens.ElementAtOrDefault(tokens.IndexOf(token) + 1);
    public readonly bool IsTokenLast(Token token) => tokens.Count == 0 || tokens.Last() == token;
}