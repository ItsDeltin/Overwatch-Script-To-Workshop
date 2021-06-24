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
        private readonly DeltintegerLanguageServer _languageServer;

        public HoverHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capabilties, ClientCapabilities clientCapabilities)
        {
            return new HoverRegistrationOptions()
            {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector
            };
        }

        public async Task<Hover> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var hoverRanges = _languageServer.LastParse?.ScriptFromUri(request.TextDocument.Uri.ToUri())?.GetHoverRanges();
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
            });
        }

        // Definition capability
        private HoverCapability _capability;
        public void SetCapability(HoverCapability capability)
        {
            _capability = capability;
        }
    }
}
