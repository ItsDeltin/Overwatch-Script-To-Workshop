using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;

using IHoverHandler = OmniSharp.Extensions.LanguageServer.Protocol.Document.IHoverHandler;
using HoverCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.HoverCapability;

namespace Deltin.Deltinteger.LanguageServer
{
    public class HoverHandler : IHoverHandler
    {
        private readonly OstwLangServer _languageServer;

        public HoverHandler(OstwLangServer languageServer)
        {
            _languageServer = languageServer;
        }

        public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capabilties, ClientCapabilities clientCapabilities)
        {
            return new HoverRegistrationOptions()
            {
                DocumentSelector = OstwLangServer.DocumentSelector
            };
        }

        public async Task<Hover> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            var compilation = await _languageServer.ProjectUpdater.GetProjectCompilationAsync();
            var hoverRanges = compilation?.ScriptFromUri(request.TextDocument.Uri.ToUri())?.GetHoverRanges();
            if (hoverRanges == null || hoverRanges.Length == 0) return new Hover();

            HoverRange chosen = hoverRanges
                .Where(hoverRange => hoverRange.Range.IsInside(request.Position))
                .OrderBy(hoverRange => hoverRange.Range)
                .FirstOrDefault();

            if (chosen == null) return new Hover();

            return new Hover()
            {
                Range = chosen.Range,
                Contents = chosen.Content
            };
        }

        // Definition capability
        private HoverCapability _capability;
        public void SetCapability(HoverCapability capability)
        {
            _capability = capability;
        }
    }
}
