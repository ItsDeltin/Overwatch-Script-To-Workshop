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
    class CompletionHandler : ICompletionHandler
    {
        private DeltintegerLanguageServer _languageServer { get; }
        private Scope _globalScope { get; }

        public CompletionHandler(DeltintegerLanguageServer languageServer)
        {
            _languageServer = languageServer;
            _globalScope = Scope.GetGlobalScope(new EmptyTypeSupplier());
        }

        public async Task<CompletionList> Handle(CompletionParams completionParams, CancellationToken token)
        {
            // If the script has not been parsed yet, return the default completion.
            if (_languageServer.LastParse == null) return new CompletionList();
            List<CompletionItem> items = new List<CompletionItem>();

            // Add the user defined types.
            foreach (var definedType in _languageServer.LastParse.Types.AllTypes)
                items.Add(definedType.GetCompletion());

            // Get the script from the uri. If it isn't parsed, return the default completion. 
            var script = _languageServer.LastParse.ScriptFromUri(completionParams.TextDocument.Uri.ToUri());
            if (script == null) return items;

            var completions = script.GetCompletionRanges();
            List<CompletionRange> inRange = new List<CompletionRange>();
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
                        items.AddRange(inRange[i].GetCompletion(completionParams.Position, false));
                    // Catch
                    else if (inRange[i].Kind == CompletionRangeKind.Catch)
                    {
                        items.AddRange(inRange[i].GetCompletion(completionParams.Position, false));
                        break;
                    }
                    // ClearRest
                    else if (inRange[i].Kind == CompletionRangeKind.ClearRest)
                    {
                        items.Clear();
                        items.AddRange(inRange[0].GetCompletion(completionParams.Position, true));
                        break;
                    }
                }
            }
            else items.AddRange(_globalScope.GetCompletion(null, false));
            return items;
        }

        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions()
            {
                DocumentSelector = DeltintegerLanguageServer.DocumentSelector,
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

        // Client compatibility
        private CompletionCapability _capability;
        public void SetCapability(CompletionCapability capability)
        {
            _capability = capability;
        }
    }

    class EmptyTypeSupplier : Elements.ITypeSupplier
    {
        public CodeType Any() => null;
        public CodeType AnyArray() => new ArrayType(null);
        public CodeType Boolean() => null;
        public CodeType Button() => throw new NotImplementedException();
        public CodeType ConstButton() => throw new NotImplementedException();
        public CodeType Default() => null;
        public CodeType EnumType(string typeName) => throw new NotImplementedException();
        public CodeType Number() => null;
        public CodeType Player() => null;
        public CodeType PlayerArray() => null;
        public CodeType PlayerOrVector() => null;
        public CodeType Players() => null;
        public CodeType Vector() => VectorType.Instance;
    }
}