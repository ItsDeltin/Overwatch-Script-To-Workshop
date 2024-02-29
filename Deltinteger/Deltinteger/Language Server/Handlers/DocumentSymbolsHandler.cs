#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.LanguageServer;

public class DocumentSymbolsHandler(OstwLangServer languageServer) : DocumentSymbolHandlerBase
{
    readonly OstwLangServer languageServer = languageServer;

    public override Task<SymbolInformationOrDocumentSymbolContainer> Handle(DocumentSymbolParams request, CancellationToken cancellationToken)
        => ErrorReport.TryOrDefaultAsync(async () =>
        {
            var compilation = await languageServer.ProjectUpdater.GetProjectCompilationAsync();
            return new SymbolInformationOrDocumentSymbolContainer(
                compilation?.ScriptFromUri(request.TextDocument.Uri.ToUri())
                    ?.GetDocumentSymbols()
                    .Select(s => new SymbolInformationOrDocumentSymbol(s))
                    ?? []);
        });

    protected override DocumentSymbolRegistrationOptions CreateRegistrationOptions(DocumentSymbolCapability capability, ClientCapabilities clientCapabilities)
        => new()
        {
            DocumentSelector = OstwLangServer.DocumentSelector,
            Label = "Ostw Symbols"
        };
}