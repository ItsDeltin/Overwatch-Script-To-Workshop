using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class ScopeGroup
    {
        private readonly List<Var> InScope = new List<Var>();

        private readonly List<ScopeGroup> Children = new List<ScopeGroup>();
        
        private readonly ScopeGroup Parent = null;

        public bool Recursive { get; }

        public ScopeGroup() {}

        private ScopeGroup(ScopeGroup parent, bool recursive) 
        {
            Parent = parent;
            Recursive = recursive;
        }

        public void In(Var var)
        {
            InScope.Add(var);
        }

        public bool IsVar(string name)
        {
            return GetVar(name, null) != null ? true : false;
        }

        public Var GetVar(string name, Range range)
        {
            Var var = null;
            ScopeGroup checkGroup = this;
            while (var == null && checkGroup != null)
            {
                var = checkGroup.InScope.FirstOrDefault(v => v.Name == name);
                checkGroup = checkGroup.Parent;
            }

            if (var == null && range != null)
                throw SyntaxErrorException.VariableDoesNotExist(name, range);

            return var;
        }

        public Var GetVar(VariableNode variableNode)
        {
            return GetVar(variableNode.Name, variableNode.Range);
        }

        public ScopeGroup Child(bool recursive = false)
        {
            var newChild = new ScopeGroup(this, recursive);
            Children.Add(newChild);
            return newChild;
        }

        public List<Var> VarCollection()
        {
            return InScope;
        }

        public List<Var> FullVarCollection()
        {
            var varCollection = new List<Var>();
            varCollection.AddRange(InScope);
            if (Parent != null)
                varCollection.AddRange(Parent.FullVarCollection());
            return varCollection;
        }

        public CompletionItem[] GetCompletionItems(Pos caret)
        {
            return FullVarCollection().Where(var => var.DefinedRange.end < caret).Select(var => new CompletionItem(var.Name)
            {
                kind = CompletionItem.Field
            }).ToArray();
        }

        //public static ScopeGroup Root = new ScopeGroup();
    }
}