using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class Scope
    {
        private List<IScopeable> Variables { get; } = new List<IScopeable>();
        private List<IMethod> Methods { get; } = new List<IMethod>();
        private Scope Parent { get; }
        private List<Scope> children { get; } = new List<Scope>();
        public string ErrorName { get; set; } = "current scope";
        public CodeType This { get; set; }
        public bool PrivateCatch { get; set; }
        public bool ProtectedCatch { get; set; }
        public bool IsObjectScope { get; set; }

        public Scope() {}
        private Scope(Scope parent)
        {
            Parent = parent;
            Parent.children.Add(this);
        }
        public Scope(string name)
        {
            ErrorName = name;
        }
        private Scope(Scope parent, string name)
        {
            Parent = parent;
            ErrorName = name;
        }

        public Scope Child()
        {
            return new Scope(this);
        }

        public Scope Child(string name)
        {
            return new Scope(this, name);
        }

        public Scope Child(bool isObjectScope)
        {
            Scope newScope = new Scope(this);
            newScope.IsObjectScope = isObjectScope;
            return newScope;
        }

        private void AllInScope(Scope getter, Func<ScopeIterate, ScopeIterateAction> element)
        {
            Scope current = this;

            while (current != null)
            {
                List<IScopeable> checkScopeables = new List<IScopeable>();
                checkScopeables.AddRange(current.Variables);
                checkScopeables.AddRange(current.Methods);

                bool stopAfterScope = false;

                foreach (IScopeable check in checkScopeables)
                {
                    // Check if the accessor is valid.
                    // bool accessorMatches = check.AccessLevel == AccessLevel.Public ||
                    //     getter == null ||
                    //     (check.AccessLevel == AccessLevel.Private && current.DoSharePrivateGroup(getter)) ||
                    //     (check.AccessLevel == AccessLevel.Protected && current.DoShareProtectedGroup(getter));
                    bool accessorMatches = current.AccessorMatches(getter, check);
                    
                    // Check if the static/object accessor is valid.
                    //bool objectMatches = check.Static || getter.IsObjectScope;
                    bool objectMatches = StaticMatches(getter, check);

                    ScopeIterateAction action = element(new ScopeIterate(current, check, accessorMatches, objectMatches));
                    if (action == ScopeIterateAction.Stop) return;
                    if (action == ScopeIterateAction.StopAfterScope) stopAfterScope = true;
                }

                if (stopAfterScope) return;

                current = current.Parent;
            }
        }

        public bool AccessorMatches(Scope getter, IAccessable element)
        {
            return element.AccessLevel == AccessLevel.Public ||
                getter == null ||
                (element.AccessLevel == AccessLevel.Private && DoSharePrivateGroup(getter)) ||
                (element.AccessLevel == AccessLevel.Protected && DoShareProtectedGroup(getter));
        }

        public bool StaticMatches(Scope getter, IScopeable element)
        {
            return (element.Static != IsObjectScope) && (getter == null || DoShareProtectedGroup(getter) || element.AccessLevel == AccessLevel.Public);
        }

        /// <summary>
        /// Adds a variable to the current scope.
        /// When handling variables added by the user, supply the diagnostics and range to show the syntax error at.
        /// When handling variables added internally, have the diagnostics and range parameters be null. An exception will be thrown instead if there is a syntax error.
        /// </summary>
        /// <param name="variable">The variable that will be added to the current scope. If the object reference is already in the direct scope, an exception will be thrown.</param>
        /// <param name="diagnostics">The file diagnostics to throw errors with. Should be null when adding variables internally.</param>
        /// <param name="range">The document range to throw errors at. Should be null when adding variables internally.</param>
        public void AddVariable(IScopeable variable, FileDiagnostics diagnostics, DocRange range)
        {
            if (variable == null) throw new ArgumentNullException(nameof(variable));
            if (Variables.Contains(variable)) throw new Exception("variable reference is already in scope.");

            if (IsVariable(variable.Name))
            {
                string message = string.Format("A variable of the name {0} was already defined in this scope.", variable.Name);

                if (diagnostics != null && range != null)
                    diagnostics.Error(message, range);
                else
                    throw new Exception(message);
            }
            else
                Variables.Add(variable);
        }

        public void AddNativeVariable(IScopeable variable)
        {
            AddVariable(variable, null, null);
        }

        public bool IsVariable(string name)
        {
            return GetVariable(name, null, null, null) != null;
        }

        public IScopeable GetVariable(string name, Scope getter, FileDiagnostics diagnostics, DocRange range)
        {
            ScopeIterate elementAccessInfo = null;

            AllInScope(getter, (it) => {
                if (it.Element.Name == name && it.Element is IMethod == false)
                {
                    elementAccessInfo = it;
                    return ScopeIterateAction.Stop;
                }
                return ScopeIterateAction.Continue;
            });

            if (diagnostics != null && range != null)
            {
                // Syntax error if the variable was not found.
                if (elementAccessInfo == null)
                    diagnostics.Error(string.Format("The variable {0} does not exist in the {1}.", name, ErrorName), range);
                
                // Syntax error if the variable can't be obtained because of its access level.
                else if (!elementAccessInfo.AccessorMatches)
                    diagnostics.Error(string.Format("'{0}' is inaccessable due to its access level.", name), range);
                
                // Object variable is being accessed in a static way.
                else if (!elementAccessInfo.ObjectMatches)
                    diagnostics.Error(string.Format("'{0}' requires an object reference to access.", name), range);
            }

            return elementAccessInfo?.Element;
        }

        /// <summary>
        /// Adds a method to the current scope.
        /// When handling methods added by the user, supply the diagnostics and range to show the syntax error at.
        /// When handling methods added internally, have the diagnostics and range parameters be null. An exception will be thrown instead if there is a syntax error.
        /// </summary>
        /// <param name="method">The method that will be added to the current scope. If the object reference is already in the direct scope, an exception will be thrown.</param>
        /// <param name="diagnostics">The file diagnostics to throw errors with. Should be null when adding methods internally.</param>
        /// <param name="range">The document range to throw errors at. Should be null when adding methods internally.</param>
        public void AddMethod(IMethod method, FileDiagnostics diagnostics, DocRange range)
        {
            var allMethods = AllMethodsInScope();

            if (method == null) throw new ArgumentNullException(nameof(method));
            if (allMethods.Contains(method)) throw new Exception("method reference is already in scope.");

            foreach (var m in allMethods)
                if (method.Name == m.Name && method.Parameters.Length == m.Parameters.Length)
                {
                    bool matches = true;
                    for (int p = 0; p < method.Parameters.Length; p++)
                        if (method.Parameters[p].Type != m.Parameters[p].Type)
                            matches = false;

                    if (matches)
                    {
                        string message = "A method with the same name and parameter types was already defined in this scope.";

                        if (diagnostics != null && range != null)
                        {
                            diagnostics.Error(message, range);
                            return;
                        }
                        else
                            throw new Exception(message);
                    }
                }

            Methods.Add(method);
        }

        public void AddNativeMethod(IMethod method)
        {
            AddMethod(method, null, null);
        }

        private IMethod[] AllMethodsInScope()
        {
            List<IMethod> methods = new List<IMethod>();

            AllInScope(null, (it) => {
                if (it.Element is IMethod method)
                    methods.Add(method);
                return ScopeIterateAction.Continue;
            });

            return methods.ToArray();
        }

        public IMethod[] GetMethodsByName(string name)
        {
            List<IMethod> methods = new List<IMethod>();

            Scope current = this;
            while (current != null)
            {
                foreach (var method in current.Methods)
                    if (method.Name == name)
                        methods.Add(method);
                current = current.Parent;
            }

            return methods.ToArray();
        }

        public CodeType GetThis()
        {
            CodeType @this = null;
            Scope current = this;

            while (@this == null && current != null)
            {
                @this = current.This;
                current = current.Parent;
            }

            return @this;
        }

        private bool DoSharePrivateGroup(Scope other)
        {
            Scope thisGroup = RootPrivateScope();
            Scope otherGroup = other.RootPrivateScope();
            return thisGroup == otherGroup;
        }

        private bool DoShareProtectedGroup(Scope other)
        {
            Scope thisGroup = RootProtectedScope();
            Scope otherGroup = other.RootProtectedScope();
            return thisGroup == otherGroup;
        }
        
        private Scope RootPrivateScope()
        {
            Scope current = this;
            while (current.Parent != null && !current.PrivateCatch) current = current.Parent;
            return current;
        }
        private Scope RootProtectedScope()
        {
            Scope current = this;
            while (current.Parent != null && !current.ProtectedCatch) current = current.Parent;
            return current;
        }

        public CompletionItem[] GetCompletion(Pos pos, bool immediate, Scope getter = null)
        {
            List<CompletionItem> completions = new List<CompletionItem>();

            // Iterate through all scopeables.
            AllInScope(getter, (it) => {
                if (it.AccessorMatches && it.ObjectMatches && WasScopedAtPosition(it.Element, pos, getter))
                    completions.Add(it.Element.GetCompletion());
                
                if (it.Container.ProtectedCatch) return ScopeIterateAction.StopAfterScope;
                return ScopeIterateAction.Continue;
            });
                
            return completions.ToArray();
        }

        private bool WasScopedAtPosition(IScopeable element, Pos pos, Scope getter)
        {
            return pos == null || element.DefinedAt == null || element.WholeContext || element.DefinedAt.range.start <= pos;
        }

        public static Scope GetGlobalScope()
        {
            Scope globalScope = new Scope();

            // Add workshop methods
            foreach (var workshopMethod in ElementList.Elements)
                globalScope.AddMethod(workshopMethod, null, null);
            
            // Add custom methods
            foreach (var builtInMethod in CustomMethods.CustomMethodData.GetCustomMethods())
                if (builtInMethod.Global)
                    globalScope.AddMethod(builtInMethod, null, null);

            return globalScope;
        }
    }

    class ScopeIterate
    {
        public Scope Container { get; }
        public IScopeable Element { get; }
        public bool AccessorMatches { get; }
        public bool ObjectMatches { get; }

        public ScopeIterate(Scope container, IScopeable element, bool accessorMatches, bool objectMatches)
        {
            Container = container;
            Element = element;
            AccessorMatches = accessorMatches;
            ObjectMatches = objectMatches;
        }
    }

    enum ScopeIterateAction
    {
        Continue,
        Stop,
        StopAfterScope
    }
}