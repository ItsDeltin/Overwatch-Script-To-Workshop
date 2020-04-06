using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class SymbolLinkComponent : IComponent
    {
        public DeltinScript DeltinScript { get; set; }

        private Dictionary<ICallable, SymbolLinkCollection> callRanges { get; } = new Dictionary<ICallable, SymbolLinkCollection>();

        public void Init() {}

        public void AddSymbolLink(ICallable callable, Location calledFrom, bool isDeclarer = false)
        {
            if (callable == null) throw new ArgumentNullException(nameof(callable));
            if (calledFrom == null) throw new ArgumentNullException(nameof(calledFrom));

            if (!callRanges.ContainsKey(callable)) callRanges.Add(callable, new SymbolLinkCollection());
            callRanges[callable].Add(new SymbolLink(calledFrom, isDeclarer));
        }

        public Dictionary<ICallable, SymbolLinkCollection> GetSymbolLinks() => callRanges;
        
        public SymbolLinkCollection GetSymbolLinks(ICallable callable) => callRanges[callable];
    }

    public class SymbolLinkCollection : List<SymbolLink>
    {
        public SymbolLink[] GetSymbolLinks(bool includeDeclarer)
        {
            if (includeDeclarer)
                return ToArray();
            else
            {
                SymbolLink[] links = this.Where(sl => !sl.Declarer).ToArray();
                return links;
            }
        }
    }

    public class SymbolLink
    {
        public Location Location { get; }
        public bool Declarer { get; }

        public SymbolLink(Location location, bool declarer)
        {
            Location = location;
            Declarer = declarer;
        }
    }
}