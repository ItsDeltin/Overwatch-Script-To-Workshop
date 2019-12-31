using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using BaseRenameHandler = OmniSharp.Extensions.LanguageServer.Protocol.Server.RenameHandler;
using RenameHandlerExtensions = OmniSharp.Extensions.LanguageServer.Protocol.Server.RenameHandlerExtensions;
using RenameCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.RenameCapability;

using IPrepareRenameHandler = OmniSharp.Extensions.LanguageServer.Protocol.Server.IPrepareRenameHandler;
using PrepareRenameHandler = OmniSharp.Extensions.LanguageServer.Protocol.Server.PrepareRenameHandler;
using PrepareRenameHandlerExtensions = OmniSharp.Extensions.LanguageServer.Protocol.Server.PrepareRenameHandlerExtensions;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Deltin.Deltinteger.LanguageServer
{
    public class RenameHandler : BaseRenameHandler, IPrepareRenameHandler
    {
        private DeltintegerLanguageServer _languageServer;

        public RenameHandler(DeltintegerLanguageServer languageServer) : base (new RenameRegistrationOptions() {
            DocumentSelector = DeltintegerLanguageServer.DocumentSelector,
            PrepareProvider = true
        })
        {
            _languageServer = languageServer;
        }

        public override async Task<WorkspaceEdit> Handle(RenameParams request, CancellationToken cancellationToken)
        {
            var link = GetLink(request.TextDocument.Uri, request.Position);
            if (link == null) return new WorkspaceEdit();

            var grouped = link.Group();
            var documentChanges = new List<WorkspaceEditDocumentChange>();
            foreach (var group in grouped)
            {
                List<TextEdit> edits = new List<TextEdit>();

                foreach (var renameRange in group.Links)
                {
                    edits.Add(new TextEdit() {
                        NewText = request.NewName,
                        Range = renameRange.ToLsRange()
                    });
                }

                var document = _languageServer.DocumentHandler.TextDocumentFromUri(group.Uri);
                WorkspaceEditDocumentChange edit = new WorkspaceEditDocumentChange(new TextDocumentEdit() {
                    Edits = edits.ToArray(),
                    TextDocument = new VersionedTextDocumentIdentifier() {
                        Version = document.Version,
                        Uri = document.Uri
                    }
                });
                documentChanges.Add(edit);
            }

            return new WorkspaceEdit() {
                DocumentChanges = documentChanges
            };
        }

        public async Task<RangeOrPlaceholderRange> Handle(PrepareRenameParams request, CancellationToken cancellationToken)
        {
            var link = GetLink(request.TextDocument.Uri, request.Position);
            if (link == null) return new RangeOrPlaceholderRange(new PlaceholderRange());

            return new RangeOrPlaceholderRange(new PlaceholderRange() {
                Range = link.Range.ToLsRange(),
                Placeholder = link.Name
            });
        }

        SymbolLink GetLink(Uri uri, Position position)
        {
            var links = _languageServer.LastParse?.GetSymbolLinks();
            if (links == null) return null;

            foreach (var link in links)
                foreach (var loc in link.Value)
                    // TODO-URI: Should use Uri.Compare?
                    if (loc.uri == uri && loc.range.IsInside(position))
                        return new SymbolLink(link, loc.range);
            
            return null;
        }

        object IRegistration<object>.GetRegistrationOptions() => null;
    }

    class SymbolLink
    {
        public string Name { get; }
        public Location[] Links { get; }
        public DocRange Range { get; }

        public SymbolLink(KeyValuePair<ICallable, List<Location>> link, DocRange range)
        {
            Name = link.Key.Name;
            Links = link.Value.ToArray();
            Range = range;
        }

        public SymbolLinkUriGroup[] Group()
        {
            var groups = new Dictionary<Uri, List<DocRange>>();   
            foreach (var link in Links)
            {
                if (!groups.ContainsKey(link.uri)) groups.Add(link.uri, new List<DocRange>());
                groups[link.uri].Add(link.range);
            }

            List<SymbolLinkUriGroup> catagorizedGroups = new List<SymbolLinkUriGroup>();
            foreach (var group in groups)
                catagorizedGroups.Add(new SymbolLinkUriGroup(group.Key, group.Value.ToArray()));
            return catagorizedGroups.ToArray();
        }
    }

    class SymbolLinkUriGroup
    {
        public Uri Uri { get; }
        public DocRange[] Links { get; }

        public SymbolLinkUriGroup(Uri uri, DocRange[] links)
        {
            Uri = uri;
            Links = links;
        }
    }
}