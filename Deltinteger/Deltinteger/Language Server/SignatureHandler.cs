using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var def = new SignatureHelp();
            if (_languageServer.LastParse == null) return def;

            var script = _languageServer.LastParse.ScriptFromUri(signatureHelpParams.TextDocument.Uri);
            if (script == null) return def;

            // Get all signatures in the file.
            var signatures = script.GetSignatureRanges()
                // Only get the ranges that have the caret inside them.
                .Where(sig => sig.Range.IsInside(signatureHelpParams.Position))
                // Order by the size of the ranges.
                .OrderBy(sig => sig.Range)
                .ToArray();
            
            // TODO: Finish signatures
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