using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using MediatR;
using FileSystemWatcher = OmniSharp.Extensions.LanguageServer.Protocol.Models.FileSystemWatcher;
using IDidChangeWatchedFilesHandler = OmniSharp.Extensions.LanguageServer.Protocol.Workspace.IDidChangeWatchedFilesHandler;
using DidChangeWatchedFilesParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.DidChangeWatchedFilesParams;
using DidChangeWatchedFilesCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.DidChangeWatchedFilesCapability;
using DidChangeWatchedFilesRegistrationOptions = OmniSharp.Extensions.LanguageServer.Protocol.Models.DidChangeWatchedFilesRegistrationOptions;
using ClientCapabilities = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.ClientCapabilities;
using FileSystemWatcherContainer = OmniSharp.Extensions.LanguageServer.Protocol.Models.Container<OmniSharp.Extensions.LanguageServer.Protocol.Models.FileSystemWatcher>;
using WatchKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.WatchKind;
using FileChangeType = OmniSharp.Extensions.LanguageServer.Protocol.Models.FileChangeType;

namespace Deltin.Deltinteger.LanguageServer
{
    public class DidChangeWatchedFilesHandlerBuilder
    {
        private readonly List<FileSystemWatcher> _systemWatchers = new List<FileSystemWatcher>();
        private readonly List<IDidChangeWatchedFilesPatternHandler> _patternHandlers = new List<IDidChangeWatchedFilesPatternHandler>();

        public void AddWatcher(string globPattern, Func<Uri, bool> isMatch, Action<Uri, FileChangeType> handle)
        {
            _systemWatchers.Add(new FileSystemWatcher() {
                GlobPattern = globPattern,
                Kind = WatchKind.Create | WatchKind.Change | WatchKind.Delete
            });
            _patternHandlers.Add(new DidChangeWatchedFilesPatternHandler(isMatch, handle));
        }

        public void AddWatcher(string globPattern, IDidChangeWatchedFilesPatternHandler patternHandler)
        {
            _systemWatchers.Add(new FileSystemWatcher() {
                GlobPattern = globPattern,
                Kind = WatchKind.Create | WatchKind.Change | WatchKind.Delete
            });
            _patternHandlers.Add(patternHandler);
        }

        public DidChangeWatchedFilesHandler GetHandler() => new DidChangeWatchedFilesHandler(_systemWatchers.ToArray(), _patternHandlers.ToArray());
    }

    public interface IDidChangeWatchedFilesPatternHandler
    {
        bool IsMatch(Uri uri);
        void Handle(Uri uri, FileChangeType kind);
    }

    public class DidChangeWatchedFilesPatternHandler : IDidChangeWatchedFilesPatternHandler
    {
        private readonly Func<Uri, bool> _isMatch;
        private readonly Action<Uri, FileChangeType> _handle;

        public DidChangeWatchedFilesPatternHandler(Func<Uri, bool> isMatch, Action<Uri, FileChangeType> handle)
        {
            _isMatch = isMatch;
            _handle = handle;
        }

        public void Handle(Uri uri, FileChangeType kind) => _handle(uri, kind);
        public bool IsMatch(Uri uri) => _isMatch(uri);
    }

    public class DidChangeWatchedFilesHandler : IDidChangeWatchedFilesHandler
    {
        private readonly FileSystemWatcher[] _systemWatchers;
        private readonly IDidChangeWatchedFilesPatternHandler[] _patternHandlers;
        private DidChangeWatchedFilesCapability _capability;

        public DidChangeWatchedFilesHandler(FileSystemWatcher[] systemWatchers, IDidChangeWatchedFilesPatternHandler[] patternHandlers)
        {
            _systemWatchers = systemWatchers;
            _patternHandlers = patternHandlers;
        }

        public DidChangeWatchedFilesRegistrationOptions GetRegistrationOptions(
            DidChangeWatchedFilesCapability watchedFilesChangedCopability, ClientCapabilities clientCapability
        ) => new DidChangeWatchedFilesRegistrationOptions() {
            Watchers = new FileSystemWatcherContainer(_systemWatchers)
        };

        public Task<Unit> Handle(DidChangeWatchedFilesParams request, CancellationToken cancellationToken) => Task.Run(() => {
            foreach (var change in request.Changes)
                foreach (var handler in _patternHandlers)
                {
                    var uri = change.Uri.ToUri();
                    if (handler.IsMatch(uri))
                        handler.Handle(uri, change.Type);
                }

            return Unit.Value;
        });

        public void SetCapability(DidChangeWatchedFilesCapability capability)
        {
            _capability = capability;
        }
    }
}