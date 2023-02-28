using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SemanticTokensHandlerBase = OmniSharp.Extensions.LanguageServer.Protocol.Document.SemanticTokensHandlerBase;
using SemanticTokensDocument = OmniSharp.Extensions.LanguageServer.Protocol.Document.SemanticTokensDocument;
using SemanticTokensBuilder = OmniSharp.Extensions.LanguageServer.Protocol.Document.SemanticTokensBuilder;

namespace Deltin.Deltinteger.LanguageServer
{
    class SemanticTokenHandler : SemanticTokensHandlerBase
    {
        static readonly SemanticTokensLegend Legend = new SemanticTokensLegend()
        {
            TokenTypes = SemanticTokenType.Defaults.ToArray(),
            TokenModifiers = SemanticTokenModifier.Defaults.ToArray()
        };

        readonly DeltintegerLanguageServer _server;
        public SemanticTokenHandler(DeltintegerLanguageServer languageServer) => this._server = languageServer;

        protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
        {
            return new SemanticTokensRegistrationOptions()
            {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector,
                Legend = Legend,
                Full = true,
                Range = false
            };
        }

        protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(ITextDocumentIdentifierParams @params, CancellationToken cancellationToken)
            => Task.FromResult(new SemanticTokensDocument(Legend));

        protected override async Task Tokenize(SemanticTokensBuilder builder, ITextDocumentIdentifierParams identifier, CancellationToken cancellationToken)
        {
            // Get the tokens in the document.
            await _server.DocumentHandler.WaitForParse();
            var tokens = _server.LastParse?.ScriptFromUri(identifier.TextDocument.Uri.ToUri())?.GetSemanticTokens();

            if (tokens != null)
                foreach (var token in tokens)
                    builder.Push(token.Range, token.TokenType, token.Modifiers);

            builder.Commit();
        }
    }
}