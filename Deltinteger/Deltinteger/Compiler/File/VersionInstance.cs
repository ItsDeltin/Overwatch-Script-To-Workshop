#nullable enable
using System;
using System.Collections.Generic;

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
        int r;
        for (r = 0; r < _newlines.Count && _newlines[r] < index; r++) ;
        return r;
    }
    public int GetColumn(int index) => index - GetLineIndex(GetLine(index));
    public DocPos GetPos(int index)
    {
        var line = GetLine(index);
        return new(line, index - GetLineIndex(index));
    }
    public int IndexOf(DocPos pos) => GetLineIndex(pos.Line) + pos.Character;
    public int GetLineIndex(int line) => line == 0 ? 0 : (_newlines[line - 1] + 1);

    public int NumberOfLines() => _newlines.Count;
    public int IndexOfLastLine() => _newlines.Count == 0 ? 0 : _newlines[^1];

    public void UpdatePosition(DocPos pos, int index)
    {
        var n = GetPos(index);
        pos.Line = n.Line;
        pos.Character = n.Character;
    }
}