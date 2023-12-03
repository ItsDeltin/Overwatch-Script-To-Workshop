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
    public class SemanticTokenHandler : SemanticTokensHandlerBase
    {
        public static string[] SemanticTokenTypes { get; } = SemanticTokenType.Defaults.Select(d => d.ToString()).ToArray();
        public static string[] SemanticTokenModifiers { get; } = SemanticTokenModifier.Defaults.Select(d => d.ToString()).ToArray();

        public static readonly SemanticTokensLegend Legend = new SemanticTokensLegend()
        {
            TokenTypes = SemanticTokenType.Defaults.ToArray(),
            TokenModifiers = SemanticTokenModifier.Defaults.ToArray()
        };

        readonly OstwLangServer _server;
        public SemanticTokenHandler(OstwLangServer languageServer) => this._server = languageServer;

        protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
        {
            return new SemanticTokensRegistrationOptions()
            {
                DocumentSelector = OstwLangServer.DocumentSelector,
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
            var compilation = await _server.ProjectUpdater.GetProjectCompilationAsync();
            var tokens = compilation?.ScriptFromUri(identifier.TextDocument.Uri.ToUri())?.GetSemanticTokens();

            if (tokens != null)
                foreach (var token in tokens)
                    builder.Push(token.Range, token.TokenType, token.Modifiers);

            builder.Commit();
        }
    }
}