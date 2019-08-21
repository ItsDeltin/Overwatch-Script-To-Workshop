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
        private bool IsInScope = true;

        public bool Recursive { get; }

        public VarCollection VarCollection { get; }

        public IndexedVar This { get; set; }

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
            //if (!FullVarCollection().Any(v => var.Name == v.Name))
            if (IsAlreadyDefined(var.Name) && var.Node != null)
                throw SyntaxErrorException.AlreadyDefined(var.Name, var.Node.Location);
            else
                InScope.Add(var);
        }

        private bool IsAlreadyDefined(string name)
        {
            int index = 0;
            ScopeGroup check = this;

            while (check != null)
            {
                if (check.This != null && index > 0)
                    return false;

                if (check.InScope.Any(v => v.Name == name))
                    return true;

                index++;
                check = check.Parent;
            }

            return false;
        }

        public void Out()
        {
            if (!IsInScope)
                throw new Exception("ScopeGroup is already out of scope.");

            IsInScope = false;

            foreach (IScopeable var in InScope)
                if (var is IndexedVar)
                    VarCollection.Free((IndexedVar)var);
            
            for (int i = 0; i < Children.Count; i++)
                if (Children[0].IsInScope)
                    throw new Exception();
        }

        public Element[] RecursiveMethodStackPop()
        {
            List<Element> actions = new List<Element>();
            foreach (IScopeable var in InScope)
                if (var is IndexedVar)
                {
                    Element[] outOfScopeActions = ((IndexedVar)var).OutOfScope();
                    if (outOfScopeActions != null)
                        actions.AddRange(outOfScopeActions);
                }

            for (int i = 0; i < Children.Count; i++)
                actions.AddRange(Children[0].RecursiveMethodStackPop());

            return actions.ToArray();
        }

        public Var GetVar(string name, Location location)
        {
            Var var = GetScopeable<Var>(name);
            if (var == null && location != null) throw SyntaxErrorException.VariableDoesNotExist(name, location);
            return var;
        }

        public IMethod GetMethod(string name, Location location)
        {
            // Get the method by it's name.
            IMethod method = GetScopeable<UserMethod>(name)
            // If it is not found, check if its a workshop method.
                ?? (IMethod)Element.GetElement(name) 
            // Then check if its a custom method.
                ?? (IMethod)CustomMethodData.GetCustomMethod(name);
            // Throw if not found.
            if (method == null && location != null) throw SyntaxErrorException.NonexistentMethod(name, location);
            return method;
        }

        private T GetScopeable<T>(string name) where T : IScopeable
        {
            IScopeable var = null;
            ScopeGroup checkGroup = this;
            while (var == null && checkGroup != null)
            {
                var = checkGroup.InScope.FirstOrDefault(v => v is T && v.Name == name);
                checkGroup = checkGroup.Parent;
            }

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

        // Get This was an Australian radio comedy show which aired on Triple M and was hosted
        // by Tony Martin and Ed Kavalee, with contributions from panel operator, Richard Marsland.
        // A different guest co-host was featured nearly every day on the show and included music played throughout.
        // On the 15 October 2007 episode, the Get This team announced that Triple M/Austereo would not be renewing the show for 2008.
        // The final broadcast was on 23 November 2007. During its lifetime and since its cancellation, Get This developed a strong cult following. 
        public IndexedVar GetThis(Location errorLocation)
        {
            ScopeGroup check = this;
            IndexedVar @this = null;
            while (check != null && @this == null)
            {
                @this = check.This;
                check = check.Parent;
            }

            if (errorLocation != null && @this == null)
                throw SyntaxErrorException.ThisCantBeUsed(errorLocation);
            
            return @this;
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
            return FullVarCollection().Where(var =>
                var is Var &&
                ((Var)var).IsDefinedVar &&
                ((Var)var).Node.Location.range.end < caret).Select(var => new CompletionItem(var.Name)
            {
                kind = CompletionItem.Field
            }).ToArray();
        }
    }
}