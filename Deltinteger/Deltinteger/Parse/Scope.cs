using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    class ScopeGroup : IDisposable
    {
        private ScopeGroup() {}

        private ScopeGroup(ScopeGroup parent) 
        {
            Parent = parent;
        }

        public void In(DefinedVar var)
        {
            InScope.Add(var);
        }

        public void Out()
        {
            Parent.Children.Remove(this);
        }

        public bool IsVar(string name)
        {
            return GetVar(name, null) != null ? true : false;
        }

        public DefinedVar GetVar(string name, IToken token)
        {
            DefinedVar var = null;
            ScopeGroup checkGroup = this;
            while (var == null && checkGroup != null)
            {
                var = checkGroup.InScope.FirstOrDefault(v => v.Name == name);
                checkGroup = checkGroup.Parent;
            }

            if (var == null && token != null)
                throw new SyntaxErrorException($"The variable {name} does not exist.", token);

            return var;
        }

        public ScopeGroup Child()
        {
            var newChild = new ScopeGroup(this);
            Children.Add(newChild);
            return newChild;
        }

        public List<DefinedVar> VarCollection()
        {
            return InScope;
        }

        public void Dispose()
        {
            Out();
        }

        private readonly List<DefinedVar> InScope = new List<DefinedVar>();

        private readonly List<ScopeGroup> Children = new List<ScopeGroup>();
        private readonly ScopeGroup Parent = null;

        public static ScopeGroup Root = new ScopeGroup();
    }
}