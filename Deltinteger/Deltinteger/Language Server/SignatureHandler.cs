using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse.Overload;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using ISignatureHelpHandler = OmniSharp.Extensions.LanguageServer.Protocol.Document.ISignatureHelpHandler;
using SignatureHelpCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.SignatureHelpCapability;

namespace Deltin.Deltinteger.LanguageServer
{
    class SignatureHandler : ISignatureHelpHandler
    {
        private DeltintegerLanguageServer _languageServer { get; }

        public SignatureHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public async Task<SignatureHelp> Handle(SignatureHelpParams signatureHelpParams, CancellationToken token)
        {
            return await Task.Run(() =>
            {
                var def = new SignatureHelp();
                if (_languageServer.LastParse == null) return def;

                var script = _languageServer.LastParse.ScriptFromUri(signatureHelpParams.TextDocument.Uri.ToUri());
                if (script == null) return def;

                // Get all signatures in the file.
                OverloadChooser signature = script.GetSignatures()
                    // Only get the ranges that have the caret inside them.
                    .Where(sig => sig.CallRange.IsInside(signatureHelpParams.Position))
                    // Order by the size of the ranges.
                    .OrderBy(sig => sig.CallRange)
                    // Choose the first signature.
                    .FirstOrDefault();

                if (signature != null)
                    return signature.GetSignatureHelp(signatureHelpParams.Position);
                return def;
            });
        }

        public SignatureHelpRegistrationOptions GetRegistrationOptions(SignatureHelpCapability capability, OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.ClientCapabilities clientCapabilities)
        {
            return new SignatureHelpRegistrationOptions()
            {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector,
                TriggerCharacters = new Container<string>("(", ",")
            };
        }
    }
}
