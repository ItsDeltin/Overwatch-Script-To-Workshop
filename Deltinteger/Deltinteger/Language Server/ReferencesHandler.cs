using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using LSLocation = OmniSharp.Extensions.LanguageServer.Protocol.Models.Location;
using IReferencesHandler = OmniSharp.Extensions.LanguageServer.Protocol.Server.IReferencesHandler;
using ReferencesCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.ReferencesCapability;

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

            var allSymbolLinks = _languageServer.LastParse?.GetSymbolLinks();

            ICallable use = null;

            foreach (var link in allSymbolLinks)
                foreach (var location in link.Value)
                    if (location.uri.Compare(request.TextDocument.Uri) && location.range.IsInside(request.Position))
                        use = link.Key;
            
            if (use == null) return new LocationContainer();

            return allSymbolLinks[use].Select(loc => loc.ToLsLocation()).ToArray();
        }

        public TextDocumentRegistrationOptions GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions() {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector
            };
        }

        private ReferencesCapability _capability;
        public void SetCapability(ReferencesCapability capability)
        {
            _capability = capability;
        }
    }
}