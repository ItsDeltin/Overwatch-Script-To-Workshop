using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using IDefinitionHandler = OmniSharp.Extensions.LanguageServer.Protocol.Document.IDefinitionHandler;
using DefinitionCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.DefinitionCapability;
using ITypeDefinitionHandler = OmniSharp.Extensions.LanguageServer.Protocol.Document.ITypeDefinitionHandler;
using TypeDefinitionCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.TypeDefinitionCapability;

namespace Deltin.Deltinteger.LanguageServer
{
    public class DefinitionHandler : IDefinitionHandler
    {
        private DeltintegerLanguageServer _languageServer { get; }

        public DefinitionHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public async Task<LocationOrLocationLinks> Handle(DefinitionParams definitionParams, CancellationToken token)
        {
            var links = _languageServer.LastParse?.ScriptFromUri(definitionParams.TextDocument.Uri.ToUri())?.GetDefinitionLinks();
            if (links == null) return new LocationOrLocationLinks();

            links = links.Where(link => ((DocRange)link.OriginSelectionRange).IsInside(definitionParams.Position)).ToArray();
            LocationOrLocationLink[] items = new LocationOrLocationLink[links.Length];
            for (int i = 0; i < items.Length; i++) items[i] = new LocationOrLocationLink(links[i]);

            return new LocationOrLocationLinks(items);
        }

        public DefinitionRegistrationOptions GetRegistrationOptions()
        {
            return new DefinitionRegistrationOptions() {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector
            };
        }

        private DefinitionCapability _capability;
        public void SetCapability(DefinitionCapability capability)
        {
            _capability = capability;
        }
    }
}