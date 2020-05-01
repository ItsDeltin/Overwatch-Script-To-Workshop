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
        private List<IVariable> Variables { get; } = new List<IVariable>();
        private List<IMethod> Methods { get; } = new List<IMethod>();
        private Scope Parent { get; }
        public string ErrorName { get; set; } = "current scope";
        public CodeType This { get; set; }
        public bool PrivateCatch { get; set; }
        public bool ProtectedCatch { get; set; }
        public bool CompletionCatch { get; set; }
        public bool MethodContainer { get; set; }

        public Scope() {}
        private Scope(Scope parent)
        {
            Parent = parent;
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

        private void IterateElements(Scope getter, bool iterateVariables, bool iterateMethods, Func<ScopeIterate, ScopeIterateAction> element)
        {
            Scope current = this;

            bool getPrivate = true;
            bool getProtected = true;

            while (current != null)
            {
                List<IScopeable> checkScopeables = new List<IScopeable>();
                if (iterateVariables) checkScopeables.AddRange(current.Variables);
                if (iterateMethods) checkScopeables.AddRange(current.Methods);

                bool stopAfterScope = false;

                foreach (IScopeable check in checkScopeables)
                {
                    // Check if the accessor is valid.
                    bool accessorMatches = check.AccessLevel == AccessLevel.Public ||
                        (getPrivate   && check.AccessLevel == AccessLevel.Private) ||
                        (getProtected && check.AccessLevel == AccessLevel.Protected);

                    ScopeIterateAction action = element(new ScopeIterate(current, check, accessorMatches));
                    if (action == ScopeIterateAction.Stop) return;
                    if (action == ScopeIterateAction.StopAfterScope) stopAfterScope = true;
                }

                if (current.PrivateCatch) getPrivate = false;
                if (current.ProtectedCatch) getProtected = false;
                if (stopAfterScope) return;

                current = current.Parent;
            }
        }

        /// <summary>
        /// Adds a variable to the current scope.
        /// When handling variables added by the user, supply the diagnostics and range to show the syntax error at.
        /// When handling variables added internally, have the diagnostics and range parameters be null. An exception will be thrown instead if there is a syntax error.
        /// </summary>
        /// <param name="variable">The variable that will be added to the current scope. If the object reference is already in the direct scope, an exception will be thrown.</param>
        /// <param name="diagnostics">The file diagnostics to throw errors with. Should be null when adding variables internally.</param>
        /// <param name="range">The document range to throw errors at. Should be null when adding variables internally.</param>
        public void AddVariable(IVariable variable, FileDiagnostics diagnostics, DocRange range)
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

        public void AddNativeVariable(IVariable variable)
        {
            AddVariable(variable, null, null);
        }

        /// <summary>Adds a variable to the scope that already belongs to another scope.</summary>
        public void CopyVariable(IVariable variable)
        {
            if (variable == null) throw new ArgumentNullException(nameof(variable));
            if (!Variables.Contains(variable))
                Variables.Add(variable);
        }

        public bool IsVariable(string name)
        {
            return GetVariable(name, null, null, null) != null;
        }

        public IVariable GetVariable(string name, Scope getter, FileDiagnostics diagnostics, DocRange range)
        {
            IVariable element = null;
            Scope current = this;



            while (current != null && element == null)
            {
                element = current.Variables.FirstOrDefault(element => element.Name == name);
                current = current.Parent;
            }


            if (range != null && element == null)
                diagnostics.Error(string.Format("The variable {0} does not exist in the {1}.", name, ErrorName), range);
            
            if (element != null && getter != null && !getter.AccessorMatches(element))
            {
                if (range == null) throw new Exception();
                diagnostics.Error(string.Format("'{0}' is inaccessable due to its access level.", name), range);
            }

            return element;
        }

        /// <summary>
        /// Adds a method to the current scope.
        /// When handling methods added by the user, supply the diagnostics and range to show the syntax error at.
        /// When handling methods added internally, have the diagnostics and range parameters be null. An exception will be thrown instead if there is a syntax error.
        /// </summary>
        /// <param name="method">The method that will be added to the current scope. If the object reference is already in the direct scope, an exception will be thrown.</param>
        /// <param name="diagnostics">The file diagnostics to throw errors with. Should be null when adding methods internally.</param>
        /// <param name="range">The document range to throw errors at. Should be null when adding methods internally.</param>
        public void AddMethod(IMethod method, FileDiagnostics diagnostics, DocRange range, bool checkConflicts = true)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (Methods.Contains(method)) throw new Exception("method reference is already in scope.");

            if (checkConflicts && HasConflict(method))
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

            Methods.Add(method);
        }

        public void AddMacro(MacroVar macro, FileDiagnostics diagnostics, DocRange range, bool checkConflicts = true)
        {
            if (macro == null) throw new ArgumentNullException(nameof(macro));
            if (Variables.Contains(macro)) throw new Exception("macro reference is already in scope.");

            if (checkConflicts && HasConflict(macro))
            {
                string message = "A macro with the same name and parameter types was already defined in this scope.";

                if (diagnostics != null && range != null)
                {
                    diagnostics.Error(message, range);
                    return;
                }
                else
                    throw new Exception(message);
            }

            Variables.Add(macro);
        }

        public void AddNativeMethod(IMethod method)
        {
            AddMethod(method, null, null);
        }

        /// <summary>
        /// Blindly copies a method to the current scope without doing any syntax checking.
        /// Use this to link to a method that already belongs to another scope. The other scope should have already handled the syntax checking.
        /// </summary>
        /// <param name="method">The method to copy.</param>
        public void CopyMethod(IMethod method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (!Methods.Contains(method))
                Methods.Add(method);
        }

        /// <summary>Checks if a method conflicts with another method in the scope.</summary>
        /// <param name="method">The method to check.</param>
        /// <returns>Returns true if the current scope already has the same name and parameters as the input method.</returns>
        public bool HasConflict(IMethod method)
        {
            return GetMethodOverload(method) != null;
        }

        public bool HasConflict(MacroVar macro)
        {
            return GetMacroOverload(macro.Name, macro.DefinedAt) != null;
        }

        /// <summary>Gets a method in the scope that has the same name and parameter types. Can potentially resolve to itself if the method being tested is in the scope.</summary>
        /// <param name="method">The method to get a matching overload.</param>
        /// <returns>A method with the matching overload, or null if none is found.</returns>
        public IMethod GetMethodOverload(IMethod method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            return GetMethodOverload(method.Name, method.Parameters.Select(p => p.Type).ToArray());
        }

        /// <summary>Gets a method overload in the scope that has the same name and parameter types.</summary>
        /// <param name="name">The name of the method.</param>
        /// <param name="parameterTypes">The types of the parameters.</param>
        /// <returns>A method with the name and parameter types, or null if none is found.</returns>
        public IMethod GetMethodOverload(string name, CodeType[] parameterTypes)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (parameterTypes == null) throw new ArgumentNullException(nameof(parameterTypes));

            IMethod method = null;

            IterateElements(null, false, true, itElement => {
                // Convert the current element to an IMethod for checking.
                IMethod checking = (IMethod)itElement.Element;

                // If the name does not match or the number of parameters are not equal, continue.
                if (checking.Name != name || checking.Parameters.Length != parameterTypes.Length) return ScopeIterateAction.Continue;

                // Loop through all parameters.
                for (int p = 0; p < checking.Parameters.Length; p++)
                    // If the parameter types do not match, continue.
                    if (checking.Parameters[p].Type != parameterTypes[p])
                        return ScopeIterateAction.Continue;
                
                // Parameter overload matches.
                method = checking;
                return ScopeIterateAction.Stop;
            });

            return method;
        }

        public IVariable GetMacroOverload(string name, Location definedAt)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            IVariable variable = null;

            IterateElements(null, true, false, itElement => {
                // Convert the current element to an IMethod for checking.
                IVariable checking = (IVariable)itElement.Element;

                // If the name does not match or the number of parameters are not equal, continue.
                if (checking.Name != name || checking.DefinedAt == definedAt) return ScopeIterateAction.Continue;

                // Loop through all parameters.
               
                // Parameter overload matches.
                variable = checking;
                return ScopeIterateAction.Stop;
            });

            return variable;

        }

        /// <summary>Gets all methods in the scope with the provided name.</summary>
        /// <param name="name">The name of the methods.</param>
        /// <returns>An array of methods with the matching name.</returns>
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

        public bool AccessorMatches(IScopeable element)
        {
            if (element.AccessLevel == AccessLevel.Public) return true;

            bool matches = false;

            IterateElements(null, true, true, itElement => {
                if (element == itElement.Element)
                {
                    matches = true;
                    return ScopeIterateAction.Stop;
                }

                if ((itElement.Container.PrivateCatch && element.AccessLevel == AccessLevel.Private) ||
                    (itElement.Container.ProtectedCatch && element.AccessLevel == AccessLevel.Protected))
                    return ScopeIterateAction.StopAfterScope;
                return ScopeIterateAction.Continue;
            });

            return matches;
        }

        public bool AccessorMatches(Scope lookingForScope, AccessLevel accessLevel)
        {
            // Just return true if the access level is public.
            if (accessLevel == AccessLevel.Public) return true;

            Scope current = Parent;
            while (current != null)
            {
                // If the current scope is the scope being looked for, return true.
                if (current == lookingForScope)
                    return true;

                // If the current scope catches private elements and the target access level is private, return false.
                if (current.PrivateCatch && accessLevel == AccessLevel.Private) return false;

                // If the current scope catches protected elements and the target access level is protected, return false.
                if (current.ProtectedCatch && accessLevel == AccessLevel.Protected) return false;

                // Next current is parent.
                current = current.Parent;
            }

            return false;
        }

        public CompletionItem[] GetCompletion(Pos pos, bool immediate, Scope getter = null)
        {
            List<CompletionItem> completions = new List<CompletionItem>();

            IterateElements(getter, true, true, (itElement) => {
                // Add the completion of the current element.
                if (WasScopedAtPosition(itElement.Element, pos, getter))
                    completions.Add(itElement.Element.GetCompletion());

                // If the container is a completion catcher, stop iterating after the scope.
                if (itElement.Container.CompletionCatch) return ScopeIterateAction.StopAfterScope;

                // Otherwise, continue.
                return ScopeIterateAction.Continue;
            });
                
            return completions.ToArray();
        }

        private bool WasScopedAtPosition(IScopeable element, Pos pos, Scope getter)
        {
            return (pos == null || element.DefinedAt == null || element.WholeContext || element.DefinedAt.range.start <= pos) && (getter == null || getter.AccessorMatches(element));
        }

        public static Scope GetGlobalScope()
        {
            Scope globalScope = new Scope();

            // Add workshop methods
            foreach (var workshopMethod in ElementList.Elements)
                if (!workshopMethod.Hidden)
                    globalScope.AddMethod(workshopMethod, null, null);
            
            // Add custom methods
            foreach (var builtInMethod in CustomMethods.CustomMethodData.GetCustomMethods())
                if (builtInMethod.Global)
                    globalScope.AddMethod(builtInMethod, null, null);

            return globalScope;
        }

        public bool IsAlreadyInScope(IMethod method) => Methods.Contains(method);
        public bool IsAlreadyInScope(IScopeable scopeable) => Variables.Contains(scopeable);
    
        public void EndScope(ActionSet actionSet, bool includeParents)
        {
            if (MethodContainer) return;

            foreach (IScopeable variable in Variables)
                if (variable is IIndexReferencer referencer && // If the current scopeable is an IIndexReferencer,
                    actionSet.IndexAssigner.TryGet(referencer, out IGettable gettable) && // and the current scopeable is assigned to an index,
                    gettable is RecursiveIndexReference recursiveIndexReference) // and the assigned index is a RecursiveIndexReference,
                    // Pop the variable stack.
                    actionSet.AddAction(recursiveIndexReference.Pop());
            
            if (includeParents && Parent != null)
                Parent.EndScope(actionSet, true);
        }
    }

    class ScopeIterate
    {
        public Scope Container { get; }
        public IScopeable Element { get; }
        public bool AccessorMatches { get; }

        public ScopeIterate(Scope container, IScopeable element, bool accessorMatches)
        {
            Container = container;
            Element = element;
            AccessorMatches = accessorMatches;
        }
    }

    enum ScopeIterateAction
    {
        Continue,
        Stop,
        StopAfterScope
    }
}