#nullable enable
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using LspDocumentSymbol = OmniSharp.Extensions.LanguageServer.Protocol.Models.DocumentSymbol;
using LspSymbolKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind;

namespace Deltin.Deltinteger.Parse.Vanilla.Ide;

sealed class DocumentSymbolNode(string name, LspSymbolKind kind, DocRange range, DocRange? selectionRange = null, string? detail = null)
{
    public string Name { get; } = name;
    public LspSymbolKind Kind { get; } = kind;
    public DocRange Range { get; } = range;
    public DocRange SelectionRange { get; } = selectionRange ?? range;
    public string? Detail { get; } = detail;
    public List<DocumentSymbolNode> Children { get; } = [];

    public void Add(DocumentSymbolNode child) => Children.Add(child);

    public LspDocumentSymbol ToLsp() => new()
    {
        Name = Name,
        Kind = Kind,
        Children = new(Children.Select(c => c.ToLsp())),
        Range = Range,
        SelectionRange = SelectionRange,
        Detail = Detail
    };
}