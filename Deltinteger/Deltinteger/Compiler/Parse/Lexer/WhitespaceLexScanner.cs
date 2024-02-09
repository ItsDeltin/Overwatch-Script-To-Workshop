#nullable enable

using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler.Parse.Vanilla;

namespace Deltin.Deltinteger.Compiler.Parse.Lexing;

struct WhitespaceLexScanner
{
    LexPosition _start;
    LexPosition _currentPosition;
    LexPosition _lastNonWhitespacePosition;
    readonly string _content;

    public WhitespaceLexScanner(LexPosition position, string content)
    {
        _currentPosition = position;
        _lastNonWhitespacePosition = position;
        _start = position;
        _content = content;
    }

    public readonly char? Next() => ReachedEnd() ? null : _content[_currentPosition.Index];

    public readonly bool Next(out char value)
    {
        value = _currentPosition.Index < _content.Length ? _content[_currentPosition.Index] : default;
        return _currentPosition.Index < _content.Length;
    }

    public readonly bool ReachedEnd() => _currentPosition.Index >= _content.Length;

    public readonly LexPosition StartPosition() => _start;

    public readonly LexPosition CurrentPosition() => _lastNonWhitespacePosition;

    public void Advance()
    {
        if (ReachedEnd()) return;

        char current = _content[_currentPosition.Index];
        if (current == '\n')
        {
            _currentPosition.Line++;
            _currentPosition.Column = 0;
        }
        else _currentPosition.Column++;
        _currentPosition.Index++;

        if (!CharData.WhitespaceCharacters.Contains(current))
        {
            _lastNonWhitespacePosition = _currentPosition;
        }
    }

    public readonly bool DidAdvance() => _currentPosition != _start;

    private readonly string Text() => _content.Substring(_start.Index, _lastNonWhitespacePosition.Index - _start.Index);

    private readonly DocRange Range() => new(new(_start.Line, _start.Column), new(_lastNonWhitespacePosition.Line, _lastNonWhitespacePosition.Column));

    public readonly WorkshopToken AsToken(TokenType tokenType, IReadOnlySet<LanguageLinkedWorkshopItem> workshopItems)
    {
        return new(Text(), Range(), tokenType, workshopItems);
    }

    public readonly Token AsToken(TokenType tokenType)
    {
        return new(Text(), Range(), tokenType);
    }
}