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
        readonly OstwLangServer _languageServer;

        public ReferenceHandler(OstwLangServer languageServer) : base()
        {
            _languageServer = languageServer;
        }

        public async Task<LocationContainer> Handle(ReferenceParams request, CancellationToken cancellationToken)
        {
            bool includeDeclaration = request.Context.IncludeDeclaration;

            // Get the declaration key from the provided range and uri.
            var compilation = await _languageServer.ProjectUpdater.GetProjectCompilationAsync();
            var key = compilation?.ScriptFromUri(request.TextDocument.Uri.ToUri())?.Elements.KeyFromPosition(request.Position).key;

            // Missing script or no definition found.
            if (key == null) return new LocationContainer();

            // Get the locations.
            return new LocationContainer(compilation.GetComponent<SymbolLinkComponent>().CallsFromDeclaration(key).Select(link => link.Location.ToLsLocation()));
        }

        public ReferenceRegistrationOptions GetRegistrationOptions(ReferenceCapability capability, OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.ClientCapabilities clientCapabilities) => new ReferenceRegistrationOptions()
        {
            DocumentSelector = OstwLangServer.DocumentSelector
        };
    }
}
