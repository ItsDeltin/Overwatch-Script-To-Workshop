using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using ISignatureHelpHandler = OmniSharp.Extensions.LanguageServer.Protocol.Server.ISignatureHelpHandler;
using SignatureHelpCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.SignatureHelpCapability;

namespace Deltin.Deltinteger.LanguageServer
{
    public class SignatureHandler : ISignatureHelpHandler
    {
        private DeltintegerLanguageServer _languageServer { get; }

        public SignatureHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public async Task<SignatureHelp> Handle(SignatureHelpParams signatureHelpParams, CancellationToken token)
        {
            await _languageServer.DocumentHandler.WaitForCompletedTyping();

            var def = new SignatureHelp();
            if (_languageServer.LastParse == null) return def;

            var script = _languageServer.LastParse.ScriptFromUri(signatureHelpParams.TextDocument.Uri);
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
        }

        public SignatureHelpRegistrationOptions GetRegistrationOptions()
        {
            return new SignatureHelpRegistrationOptions() {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector,
                TriggerCharacters = new Container<string>("(", ",")
            };
        }

        // Client capability
        private SignatureHelpCapability _capability;
        public void SetCapability(SignatureHelpCapability capability)
        {
            _capability = capability;
        }
    }
}