using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Parse;
using ICompletionHandler = OmniSharp.Extensions.LanguageServer.Protocol.Document.ICompletionHandler;
using CompletionList = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionList;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionParams = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionParams;
using CompletionRegistrationOptions = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionRegistrationOptions;
using Container = OmniSharp.Extensions.LanguageServer.Protocol.Models.Container<string>;
using CompletionCapability = OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.CompletionCapability;

namespace Deltin.Deltinteger.LanguageServer
{
    public class CompletionHandler : ICompletionHandler
    {
        readonly OstwLangServer _languageServer;

        public CompletionHandler(OstwLangServer languageServer)
        {
            _languageServer = languageServer;
        }

        public async Task<CompletionList> Handle(CompletionParams completionParams, CancellationToken token)
        {
            var compilation = await _languageServer.ProjectUpdater.GetProjectCompilationAsync();

            // If the script has not been parsed yet, return the default completion.
            if (compilation == null) return new();

            // Add snippets.
            var items = Snippet.Snippets;

            // Get the script from the uri. If it isn't parsed, return the default completion.
            var script = compilation.ScriptFromUri(completionParams.TextDocument.Uri.ToUri());
            if (script == null) return new(items);

            // Get valid completion ranges.
            var completions = script.GetCompletionRanges();
            List<ICompletionRange> inRange = new List<ICompletionRange>();
            foreach (var completion in completions)
                if (completion.Range.IsInside(completionParams.Position))
                    inRange.Add(completion);

            if (inRange.Count > 0)
            {
                inRange = inRange
                    // Order by the size of the ranges.
                    .OrderBy(range => range.Range)
                    .ToList();

                for (int i = 0; i < inRange.Count; i++)
                {
                    // Additive
                    if (inRange[i].Kind == CompletionRangeKind.Additive)
                        items = items.Concat(inRange[i].GetCompletion(completionParams.Position, false));
                    // Catch
                    else if (inRange[i].Kind == CompletionRangeKind.Catch)
                    {
                        items = items.Concat(inRange[i].GetCompletion(completionParams.Position, false));
                        break;
                    }
                    // ClearRest
                    else if (inRange[i].Kind == CompletionRangeKind.ClearRest)
                    {
                        items = inRange[0].GetCompletion(completionParams.Position, true);
                        break;
                    }
                }
            }
            return new(items);
        }

        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities.ClientCapabilities clientCapabilities)
        {
            return new CompletionRegistrationOptions()
            {
                DocumentSelector = OstwLangServer.DocumentSelector,
                // Most tools trigger completion request automatically without explicitly requesting
                // it using a keyboard shortcut (e.g. Ctrl+Space). Typically they do so when the user
                // starts to type an identifier. For example if the user types `c` in a JavaScript file
                // code complete will automatically pop up present `console` besides others as a
                // completion item. Characters that make up identifiers don't need to be listed here.
                //
                // If code complete should automatically be trigger on characters not being valid inside
                // an identifier (for example `.` in JavaScript) list them in `triggerCharacters`.
                TriggerCharacters = new Container("."),
                // The server provides support to resolve additional
                // information for a completion item.
                ResolveProvider = false
            };
        }
    }
}