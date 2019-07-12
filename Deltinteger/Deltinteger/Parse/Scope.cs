using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class ScopeGroup
    {
        private readonly List<Var> InScope = new List<Var>();

        private readonly List<ScopeGroup> Children = new List<ScopeGroup>();
        
        private readonly ScopeGroup Parent = null;

        public bool Recursive { get; }

        public VarCollection VarCollection { get; }

        private bool IsInScope = true;

        public ScopeGroup(VarCollection varCollection)
        {
            VarCollection = varCollection;
        }

        private ScopeGroup(VarCollection varCollection, ScopeGroup parent) : this(varCollection)
        {
            Parent = parent;
            Recursive = parent.Recursive;
        }

        private ScopeGroup(VarCollection varCollection, ScopeGroup parent, bool recursive) : this(varCollection)
        {
            Parent = parent;
            Recursive = recursive;
        }

        public void In(Var var)
        {
            if (!InScope.Contains(var))
                InScope.Add(var);
        }

        public Element[] Out()
        {
            if (!IsInScope)
                throw new Exception("ScopeGroup is already out of scope.");

            IsInScope = false;

            List<Element> actions = new List<Element>();
            foreach (Var var in InScope)
                if (var is IndexedVar)
                {
                    Element[] outOfScopeActions = ((IndexedVar)var).OutOfScope();
                    if (outOfScopeActions != null)
                        actions.AddRange(outOfScopeActions);

                    VarCollection.Free((IndexedVar)var);
                }
            
            while (Children.Count > 0)
                actions.AddRange(Children[0].Out());
            
            Parent.Children.Remove(this);

            return actions.ToArray();
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

        public ScopeGroup Child(bool recursive)
        {
            var newChild = new ScopeGroup(VarCollection, this, recursive);
            Children.Add(newChild);
            return newChild;
        }

        public ScopeGroup Child()
        {
            var newChild = new ScopeGroup(VarCollection, this);
            Children.Add(newChild);
            return newChild;
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
            return FullVarCollection().Where(var => var.Node.Range.end < caret).Select(var => new CompletionItem(var.Name)
            {
                kind = CompletionItem.Field
            }).ToArray();
        }

        public List<Var> AllChildVariables()
        {
            List<Var> childVars = new List<Var>();
            childVars.AddRange(InScope);
            foreach(ScopeGroup child in Children)
                childVars.AddRange(child.AllChildVariables());
            return childVars;
        }
    }
}