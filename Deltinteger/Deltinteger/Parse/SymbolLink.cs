using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
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