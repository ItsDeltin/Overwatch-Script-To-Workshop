using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class ScopeGroup
    {
        private readonly List<IScopeable> InScope = new List<IScopeable>();

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

        public void In(IScopeable var)
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
            foreach (IScopeable var in InScope)
                if (var is IndexedVar)
                {
                    Element[] outOfScopeActions = ((IndexedVar)var).OutOfScope();
                    if (outOfScopeActions != null)
                        actions.AddRange(outOfScopeActions);

                    VarCollection.Free((IndexedVar)var);
                }
            
            while (Children.Count > 0)
                actions.AddRange(Children[0].Out());
            
            if (Parent != null)
                Parent.Children.Remove(this);

            return actions.ToArray();
        }

        public Var GetVar(string name, Range range)
        {
            return GetScopeable<Var>(name, range);
        }

        public IMethod GetMethod(string name, Range range)
        {
            try
            {
                // Get the method by it's name.
                return GetScopeable<UserMethod>(name, range);
            }
            catch (SyntaxErrorException ex)
            {
                // If it is not found, check if its a workshop method.
                return (IMethod)Element.GetElement(name) 
                // Then check if its a custom method.
                    ?? (IMethod)CustomMethodData.GetCustomMethod(name)
                // Throw if not found.
                    ?? throw ex;
            }
        }

        private T GetScopeable<T>(string name, Range range) where T : IScopeable
        {
            IScopeable var = null;
            ScopeGroup checkGroup = this;
            while (var == null && checkGroup != null)
            {
                var = checkGroup.InScope.FirstOrDefault(v => v is T && v.Name == name);
                checkGroup = checkGroup.Parent;
            }

            if (var == null && range != null)
                throw SyntaxErrorException.VariableDoesNotExist(name, range);

            return (T)var;
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

        public ScopeGroup Root()
        {
            ScopeGroup root = this;
            while (root.Parent != null) root = root.Parent;
            return root;
        }

        public List<IScopeable> FullVarCollection()
        {
            var varCollection = new List<IScopeable>();
            varCollection.AddRange(InScope);
            if (Parent != null)
                varCollection.AddRange(Parent.FullVarCollection());
            return varCollection;
        }

        public CompletionItem[] GetCompletionItems(Pos caret)
        {
            return FullVarCollection().Where(var => var is Var && ((Var)var).IsDefinedVar && ((Var)var).Node.Range.end < caret).Select(var => new CompletionItem(var.Name)
            {
                kind = CompletionItem.Field
            }).ToArray();
        }
    }
}