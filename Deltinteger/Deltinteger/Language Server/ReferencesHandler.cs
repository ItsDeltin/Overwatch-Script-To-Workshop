using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using LSLocation = OmniSharp.Extensions.LanguageServer.Protocol.Models.Location;
using IReferencesHandler = OmniSharp.Extensions.LanguageServer.Protocol.Document.IReferencesHandler;
using ReferenceCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.ReferenceCapability;

namespace Deltin.Deltinteger.LanguageServer
{
    class ReferenceHandler : IReferencesHandler
    {
        private DeltintegerLanguageServer _languageServer { get; }

        public ReferenceHandler(DeltintegerLanguageServer languageServer) : base()
        {
            _languageServer = languageServer;
        }

        public async Task<LocationContainer> Handle(ReferenceParams request, CancellationToken cancellationToken)
        {
            return await Task.Run<LocationContainer>(() =>
            {
                bool includeDeclaration = request.Context.IncludeDeclaration;

                // Get the declaration key from the provided range and uri.
                var key = _languageServer.LastParse?.ScriptFromUri(request.TextDocument.Uri.ToUri())?.Elements.KeyFromPosition(request.Position).key;

                // Missing script or no definition found.
                if (key == null) return new LocationContainer();

                // Get the locations.
                return new LocationContainer(_languageServer.LastParse.GetComponent<SymbolLinkComponent>().CallsFromDeclaration(key).Select(link => link.Location.ToLsLocation()));
            });
        }

        public ReferenceRegistrationOptions GetRegistrationOptions(ReferenceCapability capability, OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.ClientCapabilities clientCapabilities) => new ReferenceRegistrationOptions() {
            DocumentSelector = DeltintegerLanguageServer.DocumentSelector
        };
    }
}
