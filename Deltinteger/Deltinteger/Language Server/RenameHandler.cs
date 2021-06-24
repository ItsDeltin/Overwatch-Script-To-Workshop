using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using IRenameHandler = OmniSharp.Extensions.LanguageServer.Protocol.Document.IRenameHandler;
using RenameCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.RenameCapability;
using IPrepareRenameHandler = OmniSharp.Extensions.LanguageServer.Protocol.Document.IPrepareRenameHandler;

namespace Deltin.Deltinteger.LanguageServer
{
    class DoRenameHandler : IRenameHandler, IPrepareRenameHandler
    {
        public static RenameLink GetLink(DeltintegerLanguageServer languageServer, Uri uri, Position position)
        {
            // Get the script from the uri, and get the 
            var keyRange = languageServer.LastParse?.ScriptFromUri(uri)?.Elements.KeyFromPosition(position);

            // Script was not found, script not yet read, or no key was found.
            if (keyRange?.key == null) return null;

            var locations = languageServer.LastParse.GetComponent<SymbolLinkComponent>().CallsFromDeclaration(keyRange.Value.key).Select(link => link.Location);
            return new RenameLink(keyRange?.key.Name, keyRange?.range, locations);
        }

        private DeltintegerLanguageServer _languageServer;

        public DoRenameHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
        }

        public RenameRegistrationOptions GetRegistrationOptions(RenameCapability capability, OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.ClientCapabilities clientCapabilities)
        {
            return new RenameRegistrationOptions() {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector,
                PrepareProvider = true
            };
        }

        public Task<WorkspaceEdit> Handle(RenameParams request, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var link = GetLink(_languageServer, request.TextDocument.Uri.ToUri(), request.Position);
                if (link == null) return new WorkspaceEdit();

                var grouped = link.Group();
                var documentChanges = new List<WorkspaceEditDocumentChange>();
                foreach (var group in grouped)
                {
                    List<TextEdit> edits = new List<TextEdit>();

                    foreach (var renameRange in group.Links)
                    {
                        edits.Add(new TextEdit()
                        {
                            NewText = request.NewName,
                            Range = renameRange
                        });
                    }

                    var document = _languageServer.DocumentHandler.TextDocumentFromUri(group.Uri)?.AsItem();

                    // document will be null if the editor doesn't have the document of the group opened.
                    if (document == null)
                    {
                        ImportedScript importedScript = _languageServer.FileGetter.GetImportedFile(group.Uri);
                        document = new TextDocumentItem()
                        {
                            Uri = group.Uri,
                            Text = importedScript.Content,
                            LanguageId = "ostw"
                        };
                    }

                    WorkspaceEditDocumentChange edit = new WorkspaceEditDocumentChange(new TextDocumentEdit()
                    {
                        Edits = edits.ToArray(),
                        TextDocument = new OptionalVersionedTextDocumentIdentifier() {
                            Version = document.Version,
                            Uri = document.Uri
                        }
                    });
                    documentChanges.Add(edit);
                }

                return new WorkspaceEdit()
                {
                    DocumentChanges = documentChanges
                };
            });
        }

        public Task<RangeOrPlaceholderRange> Handle(PrepareRenameParams request, CancellationToken cancellationToken) => Task.Run(() => {
            var link = GetLink(_languageServer, request.TextDocument.Uri.ToUri(), request.Position);
            if (link == null) return new RangeOrPlaceholderRange(new PlaceholderRange());

            return new RangeOrPlaceholderRange(new PlaceholderRange()
            {
                Range = link.SourceRange,
                Placeholder = link.Name
            });
        });
    }

    class RenameLink
    {
        public string Name { get; }
        public DocRange SourceRange { get; }
        public IEnumerable<Location> Links { get; }

        public RenameLink(string placeholderName, DocRange sourceRange, IEnumerable<Location> links)
        {
            Name = placeholderName;
            SourceRange = sourceRange;
            Links = links;
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
