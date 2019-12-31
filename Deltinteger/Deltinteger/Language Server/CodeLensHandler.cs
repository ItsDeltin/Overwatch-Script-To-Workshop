using System;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using ICodeLensHandler = OmniSharp.Extensions.LanguageServer.Protocol.Server.ICodeLensHandler;
using CodeLensCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.CodeLensCapability;
using System.Threading.Tasks;
using System.Threading;

namespace Deltin.Deltinteger.LanguageServer
{
    public class CodeLensHandler : ICodeLensHandler
    {
        private DeltintegerLanguageServer _languageServer { get; }

        public CodeLensHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public async Task<CodeLensContainer> Handle(CodeLensParams request, CancellationToken cancellationToken)
        {
            var script = _languageServer.LastParse?.ScriptFromUri(request.TextDocument.Uri);
            throw new NotImplementedException();
        }

        public CodeLensRegistrationOptions GetRegistrationOptions()
        {
            return new CodeLensRegistrationOptions()
            {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector,
                ResolveProvider = false 
            };
        }

        private CodeLensCapability _capability;
        public void SetCapability(CodeLensCapability capability)
        {
            _capability = capability;
        }
    }
}