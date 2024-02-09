#nullable enable
using System.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Compiler.File;

public readonly record struct DocumentUpdateRange(DocRange Range, string Text, int RangeLength)
{
    public static implicit operator DocumentUpdateRange(TextDocumentContentChangeEvent changeEvent) => new(changeEvent.Range, changeEvent.Text, changeEvent.RangeLength);

    public string ApplyChangeToString(string str)
    {
        int start = Extras.TextIndexFromPosition(str, Range.Start);
        int length = RangeLength; // int length = Extras.TextIndexFromPosition(str, Range.End) - start;

        StringBuilder rep = new StringBuilder(str);
        rep.Remove(start, length);
        rep.Insert(start, Text);

        return rep.ToString();
    }
}