using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class Scope
    {
        private List<IScopeable> inScope { get; } = new List<IScopeable>();
        private Scope Parent { get; }
        private List<Scope> children { get; } = new List<Scope>();

        private Scope() {}
        private Scope(Scope parent)
        {
            Parent = parent;
            Parent.children.Add(this);
        }

        public Scope Child()
        {
            return new Scope(this);
        }

        public void In(IScopeable element)
        {
            if (inScope.Contains(element)) throw new Exception("element is already in scope.");
            inScope.Add(element);
        }

        public IScopeable GetInScope(string name)
        {
            IScopeable element = null;
            Scope current = this;
            while (current != null && element == null)
            {
                element = current.inScope.FirstOrDefault(element => element.Name == name);
                current = current.Parent;
            }
            return element;
        }

        public bool WasDefined(string name)
        {
            return GetInScope(name) != null;
        }

        public static Scope GetGlobalScope()
        {
            Scope globalScope = new Scope();

            // Add workshop methods
            foreach (var workshopMethod in ElementList.Elements)
                globalScope.In(workshopMethod);
            
            // Add custom methods
            foreach (var builtInMethod in CustomMethodData.GetCustomMethods())
                globalScope.In(builtInMethod);

            return globalScope;
        }
    }
}