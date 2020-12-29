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
    public class ReferenceHandler : IReferencesHandler
    {
        private DeltintegerLanguageServer _languageServer { get; }

        public ReferenceHandler(DeltintegerLanguageServer languageServer) : base()
        {
            _languageServer = languageServer;
        }

        public async Task<LocationContainer> Handle(ReferenceParams request, CancellationToken cancellationToken)
        {
            bool includeDeclaration = request.Context.IncludeDeclaration;

            var allSymbolLinks = _languageServer.LastParse?.GetComponent<SymbolLinkComponent>().GetSymbolLinks();
            if (allSymbolLinks == null) return new LocationContainer();

            ISymbolLink use = null;
            Location declaredAt = null;

            foreach (var pair in allSymbolLinks)
                foreach (var link in pair.Value)
                    if (link.Location.uri.Compare(request.TextDocument.Uri.ToUri()) && link.Location.range.IsInside(request.Position))
                    {
                        use = pair.Key;
                        declaredAt = link.Location;
                    }
            
            if (use == null) return new LocationContainer();

            return allSymbolLinks[use]
                .GetSymbolLinks(includeDeclaration)
                // Convert to Language Server API location.
                .Select(loc => loc.Location.ToLsLocation())
                .ToArray();
        }

        public ReferenceRegistrationOptions GetRegistrationOptions()
        {
            return new ReferenceRegistrationOptions() {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector
            };
        }

        private ReferenceCapability _capability;
        public void SetCapability(ReferenceCapability capability)
        {
            _capability = capability;
        }
    }
}