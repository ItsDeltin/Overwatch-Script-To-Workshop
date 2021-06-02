using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class SymbolLinkComponent : IComponent
    {
        readonly Dictionary<object, List<SymbolLink>> _calls = new Dictionary<object, List<SymbolLink>>();
        DeltinScript _deltinScript;

        void IComponent.Init(DeltinScript deltinScript)
        {
            _deltinScript = deltinScript;
        }

        public void Collect()
        {
            // Merge declaration calls.
            foreach (var script in _deltinScript.Importer.ScriptFiles)
                foreach (var call in script.Elements.DeclarationCalls)
                    _calls.GetValueOrAddKey(call.Key).AddRange(call.Value.Select(call => new SymbolLink(script.GetLocation(call.CallRange), call.IsDeclaration)));
        }

        public IReadOnlyList<SymbolLink> CallsFromDeclaration(object key) => _calls[key].AsReadOnly();
    }

    public class SymbolLink
    {
        public Location Location { get; }
        public bool IsDeclaration { get; }

        public SymbolLink(Location location, bool declarer)
        {
            Location = location;
            IsDeclaration = declarer;
        }
    }
}