using System;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using MediatR;
namespace Deltin.Deltinteger.LanguageServer
{
    class ColorHandler : IDocumentColorHandler, IColorPresentationHandler
    {
        private readonly DeltintegerLanguageServer _server;
        private ColorProviderCapability _capability;

        public ColorHandler(DeltintegerLanguageServer server)
        {
            _server = server;
        }

        public DocumentColorRegistrationOptions GetRegistrationOptions(ColorProviderCapability capability, ClientCapabilities clientCapabilities)
        {
            return new DocumentColorRegistrationOptions() {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector
            };
        }

        public async Task<Container<ColorInformation>> Handle(DocumentColorParams request, CancellationToken cancellationToken) {
            await _server.DocumentHandler.WaitForParse();
            var ranges = _server.LastParse?.ScriptFromUri(request.TextDocument.Uri.ToUri())?.GetColorRanges();
            return new Container<ColorInformation>(ranges ?? new ColorInformation[0]);
        }

        public Task<Container<ColorPresentation>> Handle(ColorPresentationParams request, CancellationToken cancellationToken) {

            string label = request.Color.Red * 255 + ", " + request.Color.Green * 255 + ", " + request.Color.Blue * 255 + ", " + request.Color.Alpha * 255;

            var result = new Container<ColorPresentation>(new ColorPresentation[] {
                new ColorPresentation() {
                    Label = label,
                    TextEdit = new TextEdit() {
                        NewText = "CustomColor(" + label + ")",
                        Range = request.Range
                    }
                }
            });
            return Task.FromResult(result);
        }

        public void SetCapability(ColorProviderCapability capability, ClientCapabilities clientCapabilities) => _capability = capability;
    }
}