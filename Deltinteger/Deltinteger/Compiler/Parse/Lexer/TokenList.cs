#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

public class TokenList
{
    readonly List<TokenNode> tokens = [];
    Dictionary<Token, int>? tokenIndexMap = [];

    public int Count => tokens.Count;

    public Token this[int i] { get => tokens[i].Token; }

    public void Add(int index, TokenNode node)
    {
        tokenIndexMap = null;
        tokens.Insert(index, node);
    }

    public void RemoveAt(int index)
    {
        tokenIndexMap = null;
        tokens.RemoveAt(index);
    }

    public TokenNode GetNode(int index) => tokens[index];

    public int IndexOf(Token token)
    {
        tokenIndexMap ??= tokens.Select((t, i) => new KeyValuePair<Token, int>(t.Token, i)).ToDictionary();
        return tokenIndexMap[token];
    }

    public Token? Last() => tokens.Count == 0 ? null : tokens[^1].Token;

    public Token? ElementAtOrDefault(int index) => index < tokens.Count ? tokens[index].Token : null;

    public Token? NextToken(Token previous) => ElementAtOrDefault(IndexOf(previous) + 1);

    public bool IsTokenLast(Token token) => Last() == token;

    public override string ToString()
    {
        return $"[{string.Join(", ", tokens.Select(t => t.Token.ToString()))}]";
    }
}

public class TokenNode(Token token, LexPosition startPosition, LexPosition endPosition, LexerContextKind contextKind, MatchError? error)
{
    public Token Token = token;
    public LexPosition StartPosition = startPosition;
    public LexPosition EndPosition = endPosition;
    public LexerContextKind ContextKind = contextKind;
    public MatchError? Error = error;

    public override string ToString() => Token.ToString();
}