using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Overload;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using ISignatureHelpHandler = OmniSharp.Extensions.LanguageServer.Protocol.Document.ISignatureHelpHandler;
using SignatureHelpCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.SignatureHelpCapability;

namespace Deltin.Deltinteger.LanguageServer
{
    public class SignatureHandler : ISignatureHelpHandler
    {
        readonly OstwLangServer _languageServer;

        public SignatureHandler(OstwLangServer languageServer)
        {
            _languageServer = languageServer;
        }

        public async Task<SignatureHelp> Handle(SignatureHelpParams signatureHelpParams, CancellationToken token)
        {
            var def = new SignatureHelp();
            var compilation = await _languageServer.ProjectUpdater.GetProjectCompilationAsync();
            if (compilation == null) return def;

            var script = compilation.ScriptFromUri(signatureHelpParams.TextDocument.Uri.ToUri());
            if (script == null) return def;

            // Get all signatures in the file.
            ISignatureHelp signature = script.GetSignatures()
                // Only get the ranges that have the caret inside them.
                .Where(sig => sig.Range.IsInside(signatureHelpParams.Position))
                // Order by the size of the ranges.
                .OrderBy(sig => sig.Range)
                // Choose the first signature.
                .FirstOrDefault();

            if (signature != null)
                return signature.GetSignatureHelp(signatureHelpParams.Position);
            return def;
        }

        public SignatureHelpRegistrationOptions GetRegistrationOptions(SignatureHelpCapability capability, OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.ClientCapabilities clientCapabilities)
        {
            return new SignatureHelpRegistrationOptions()
            {
                DocumentSelector = OstwLangServer.DocumentSelector,
                TriggerCharacters = new Container<string>("(", ",")
            };
        }
    }
}
