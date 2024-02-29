#nullable enable
using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler.Parse.Lexing;

namespace Deltin.Deltinteger.Compiler.File;

public class VersionInstance
{
    public string Text { get; }
    readonly List<int> _newlines = new();

    public int Length => Text.Length;

    public VersionInstance(string text)
    {
        Text = text;
        for (int i = 0; i < text.Length; i++)
            if (text[i] == '\n')
                _newlines.Add(i);
    }

    public int GetLine(int index)
    {
        if (_newlines.Count == 0 || index <= _newlines[0])
            return 0;

        int l = 0, r = _newlines.Count;
        while (true)
        {
            int current = l + (r - l) / 2;

            // Too low
            if (_newlines[current] < index && current + 1 < _newlines.Count && _newlines[current + 1] < index)
            {
                l = current + 1;
            }
            // Too high
            else if (_newlines[current] > index)
            {
                r = current - 1;
            }
            else if (_newlines[current] == index)
                return current;
            else
                return current + 1;
        }
    }
    public DocPos GetPos(int index)
    {
        var pos = GetLexPosition(index);
        return new(pos.Line, pos.Column);
    }
    public LexPosition GetLexPosition(int index)
    {
        var line = GetLine(index);
        var column = index - GetLineIndex(line);
        return new(index, line, column);
    }
    public int IndexOf(DocPos pos) => GetLineIndex(pos.Line) + pos.Character;
    private int GetLineIndex(int line) => line == 0 ? 0 : (_newlines[line - 1] + 1);

    public int NumberOfLines() => _newlines.Count;
    public int IndexOfLastLine() => _newlines.Count == 0 ? 0 : _newlines[^1];

    public void UpdatePosition(DocPos pos, int index)
    {
        var n = GetPos(index);
        pos.Line = n.Line;
        pos.Character = n.Character;
    }
}