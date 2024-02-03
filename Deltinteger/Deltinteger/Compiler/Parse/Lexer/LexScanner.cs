#nullable enable

using System.Linq;
using System.Text;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

public class LexScanner
{
    public bool WasAdvanced { get; private set; }
    public bool ReachedEnd => _position.Index >= _content.Length;
    public LexPosition Position => _position;

    private LexPosition _position;
    private readonly string _content;
    private readonly DocPos _startPos;
    private readonly StringBuilder _captured = new StringBuilder();

    public LexScanner(LexPosition position, string content)
    {
        _position = position;
        _content = content;
        _startPos = new DocPos(position.Line, position.Column);
    }

    public void Advance()
    {
        if (ReachedEnd) return;

        _captured.Append(_content[_position.Index]);
        WasAdvanced = true;
        if (_content[_position.Index] == '\n')
        {
            _position.Line++;
            _position.Column = 0;
        }
        else _position.Column++;
        _position.Index++;
    }

    public bool Match(string content)
    {
        for (int i = 0; i < content.Length; i++)
            if (!Match(content[i]))
                return false;
        return true;
    }

    public bool Match(char character)
    {
        if (!ReachedEnd && _content[_position.Index] == character)
        {
            Advance();
            return true;
        }
        return false;
    }

    public char? Current() => _content.ElementAtOrDefault(_position.Index);
    public bool At(char chr) => !ReachedEnd && _content[_position.Index] == chr;
    public bool At(char chr, int lookahead) => _position.Index + lookahead < _content.Length && _content[_position.Index + lookahead] == chr;
    public bool AtIdentifierChar() => !ReachedEnd && CharData.IdentifierCharacters.Contains(_content[_position.Index]);
    public bool AtWhitespace() => !ReachedEnd && CharData.WhitespaceCharacters.Contains(_content[_position.Index]);
    public bool AtNumeric() => !ReachedEnd && CharData.NumericalCharacters.Contains(_content[_position.Index]);
    public Token AsToken(TokenType tokenType) => new Token(_captured.ToString(), new DocRange(_startPos, new DocPos(_position.Line, _position.Column)), tokenType);
}