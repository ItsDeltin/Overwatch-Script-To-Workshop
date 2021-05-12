using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class Scope : IScopeHandler
    {
        public IVariableInstance[] Variables => _variables.ToArray();
        public IMethod[] Methods => _methods.ToArray();
        public ICodeTypeInitializer[] Types => _types.ToArray();

        readonly List<IVariableInstance> _variables = new List<IVariableInstance>();
        readonly List<IMethod> _methods = new List<IMethod>();
        readonly List<ICodeTypeInitializer> _types = new List<ICodeTypeInitializer>();
        
        public Scope Parent { get; }
        public string ErrorName { get; set; } = "current scope";
        public bool PrivateCatch { get; set; }
        public bool ProtectedCatch { get; set; }
        public bool CompletionCatch { get; set; }
        public bool MethodContainer { get; set; }
        public bool CatchConflict { get; set; }
        public bool TagPlayerVariables { get; set; }

        public Scope() { }
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

        private void IterateElements(bool iterateVariables, bool iterateMethods, Func<ScopeIterate, ScopeIterateAction> element, Func<Scope, ScopeIterateAction> onEmpty = null)
        {
            Scope current = this;

            bool getPrivate = true;
            bool getProtected = true;

            while (current != null)
            {
                List<IScopeable> checkScopeables = new List<IScopeable>();

                // If variables are being iterated, add them to the list.
                if (iterateVariables) checkScopeables.AddRange(current._variables);

                // If functions are being iterated, add them to the list.
                if (iterateMethods) checkScopeables.AddRange(current._methods);

                bool stopAfterScope = false;

                foreach (IScopeable check in checkScopeables)
                {
                    // Check if the accessor is valid.
                    bool accessorMatches = check.AccessLevel == AccessLevel.Public ||
                        (getPrivate && check.AccessLevel == AccessLevel.Private) ||
                        (getProtected && check.AccessLevel == AccessLevel.Protected);

                    ScopeIterateAction action = element(new ScopeIterate(current, check, accessorMatches));
                    if (action == ScopeIterateAction.Stop) return;
                    if (action == ScopeIterateAction.StopAfterScope) stopAfterScope = true;
                }
                // If there are no scopeables and onEmpty is not null, invoke onEmpty. 
                if (checkScopeables.Count == 0 && onEmpty != null)
                {
                    ScopeIterateAction action = onEmpty.Invoke(current);
                    if (action != ScopeIterateAction.Continue) return;
                }

                if (current.PrivateCatch) getPrivate = false;
                if (current.ProtectedCatch) getProtected = false;
                if (stopAfterScope) return;

                current = current.Parent;
            }
        }

        public void IterateParents(Func<Scope, bool> iterate)
        {
            Scope current = this;
            while (current != null)
            {
                if (iterate(current)) return;
                current = current.Parent;
            }
        }

        public void CopyAll(Scope other)
        {
            other.IterateParents(scope =>
            {
                _methods.AddRange(scope._methods);
                return true;
            });

            other.IterateElements(true, true, iterate =>
            {
                // Add the element.
                if (iterate.Element is IVariableInstance variable && !_variables.Contains(variable))
                    _variables.Add(variable);

                if (iterate.Container.PrivateCatch || iterate.Container.CompletionCatch) return ScopeIterateAction.StopAfterScope;
                return ScopeIterateAction.Continue;
            }, scope =>
            {
                // On empty scope.
                if (scope.PrivateCatch || scope.CompletionCatch) return ScopeIterateAction.StopAfterScope;
                return ScopeIterateAction.Continue;
            });
        }

        /// <summary>
        /// Adds a variable to the current scope.
        /// When handling variables added by the user, supply the diagnostics and range to show the syntax error at.
        /// When handling variables added internally, have the diagnostics and range parameters be null. An exception will be thrown instead if there is a syntax error.
        /// </summary>
        /// <param name="variable">The variable that will be added to the current scope. If the object reference is already in the direct scope, an exception will be thrown.</param>
        /// <param name="diagnostics">The file diagnostics to throw errors with. Should be null when adding variables internally.</param>
        /// <param name="range">The document range to throw errors at. Should be null when adding variables internally.</param>
        public void AddVariable(IVariableInstance variable, FileDiagnostics diagnostics, DocRange range)
        {
            if (variable == null) throw new ArgumentNullException(nameof(variable));
            if (_variables.Contains(variable)) throw new Exception("variable reference is already in scope.");

            if (Conflicts(variable))
                diagnostics.Error(string.Format("A variable of the name {0} was already defined in this scope.", variable.Name), range);
            else
                _variables.Add(variable);
        }

        public void AddNativeVariable(IVariableInstance variable)
        {
            if (variable == null) throw new ArgumentNullException(nameof(variable));
            if (_variables.Contains(variable)) throw new Exception("variable reference is already in scope.");
            _variables.Add(variable);
        }

        /// <summary>Adds a variable to the scope that already belongs to another scope.</summary>
        public void CopyVariable(IVariableInstance variable)
        {
            if (variable == null) throw new ArgumentNullException(nameof(variable));
            if (!_variables.Contains(variable))
                _variables.Add(variable);
        }

        public bool IsVariable(string name) => GetVariable(name, false) != null;

        public IVariableInstance GetVariable(string name, bool methodGroupsOnly)
        {
            IVariableInstance variable = null;
            List<IMethod> functions = new List<IMethod>();

            Scope current = this;
            while (current != null)
            {
                // Add functions with the same name.
                functions.AddRange(current._methods.Where(f => f.Name == name));

                // Set the variable if it was not set yet.
                if (variable == null)
                    variable = current._variables.Where(
                        v => !methodGroupsOnly ||
                        v is MethodGroup ||
                        v.CodeType is Lambda.PortableLambdaType
                        ).FirstOrDefault(element => element.Name == name);

                // Go to the parent scope.
                current = current.Parent;
            }

            // Variables take priority over method groups.
            if (variable != null) return variable;

            // If there were any functions that share the variable name, return the method group.
            if (functions.Count > 0) return new MethodGroup(name, functions.ToArray());

            // Otherwise, return null.
            return null;
        }

        public IVariableInstance GetVariable(string name, Scope getter, FileDiagnostics diagnostics, DocRange range, bool methodGroupsOnly)
        {
            IVariableInstance element = GetVariable(name, methodGroupsOnly);

            if (range != null && element == null)
                diagnostics.Error(string.Format("The variable {0} does not exist in the {1}.", name, ErrorName), range);

            if (element != null && getter != null && !getter.AccessorMatches(element))
            {
                if (range == null) throw new Exception();
                diagnostics.Error(string.Format("'{0}' is inaccessable due to its access level.", name), range);
            }

            return element;
        }

        public IVariableInstance[] GetAllVariables(string name)
        {
            List<IVariableInstance> variables = new List<IVariableInstance>();
            IterateElements(true, false, it => {
                if (it.Element.Name == name) variables.Add((IVariableInstance)it.Element);
                return ScopeIterateAction.Continue;
            });
            return variables.ToArray();
        }
        
        public bool Conflicts(IScopeable scopeable, bool variables = true, bool functions = true)
        {
            bool conflicts = false;
            IterateElements(variables, functions, action =>
            {
                // If the element name matches, set conflicts to true then stop iterating.
                if (scopeable.Name == action.Element.Name)
                {
                    if (functions || action.Element is MethodGroup == false)
                        conflicts = true;
                    return ScopeIterateAction.Stop;
                }

                return action.Container.CatchConflict ? ScopeIterateAction.StopAfterScope : ScopeIterateAction.Continue;
            }, scope =>
            {
                return scope.CatchConflict ? ScopeIterateAction.StopAfterScope : ScopeIterateAction.Continue;
            });
            return conflicts;
        }

        /// <summary>
        /// Adds a method to the current scope.
        /// When handling methods added by the user, supply the diagnostics and range to show the syntax error at.
        /// When handling methods added internally, have the diagnostics and range parameters be null. An exception will be thrown instead if there is a syntax error.
        /// </summary>
        /// <param name="method">The method that will be added to the current scope. If the object reference is already in the direct scope, an exception will be thrown.</param>
        /// <param name="diagnostics">The file diagnostics to throw errors with. Should be null when adding methods internally.</param>
        /// <param name="range">The document range to throw errors at. Should be null when adding methods internally.</param>
        public void AddMethod(IMethod method, ParseInfo parseInfo, DocRange range, bool checkConflicts = true)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            if (checkConflicts && HasConflict(parseInfo.TranslateInfo, method))
                parseInfo.Script.Diagnostics.Error("A method with the same name and parameter types was already defined in this scope.", range);

            AddNativeMethod(method);
        }

        public void AddNativeMethod(IMethod method) => _methods.Add(method);

        /// <summary>
        /// Blindly copies a method to the current scope without doing any syntax checking.
        /// Use this to link to a method that already belongs to another scope. The other scope should have already handled the syntax checking.
        /// </summary>
        /// <param name="method">The method to copy.</param>
        public void CopyMethod(IMethod method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            AddNativeMethod(method);
        }

        /// <summary>Checks if a method conflicts with another method in the scope.</summary>
        /// <param name="method">The method to check.</param>
        /// <returns>Returns true if the current scope already has the same name and parameters as the input method.</returns>
        public bool HasConflict(DeltinScript deltinScript, IMethod method) => Conflicts(method, functions: false) || GetMethodOverload(deltinScript, method) != null;

        /// <summary>Gets a method in the scope that has the same name and parameter types. Can potentially resolve to itself if the method being tested is in the scope.</summary>
        /// <param name="method">The method to get a matching overload.</param>
        /// <returns>A method with the matching overload, or null if none is found.</returns>
        public IMethod GetMethodOverload(DeltinScript deltinScript, IMethod method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            return GetMethodOverload(deltinScript, method.Name, method.Parameters.Select(p => p.GetCodeType(deltinScript)).ToArray());
        }

        /// <summary>Gets a method overload in the scope that has the same name and parameter types.</summary>
        /// <param name="name">The name of the method.</param>
        /// <param name="parameterTypes">The types of the parameters.</param>
        /// <returns>A method with the name and parameter types, or null if none is found.</returns>
        public IMethod GetMethodOverload(DeltinScript deltinScript, string name, CodeType[] parameterTypes)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (parameterTypes == null) throw new ArgumentNullException(nameof(parameterTypes));

            IMethod method = null;

            IterateElements(false, true, itElement =>
            {
                // Convert the current element to an IMethod for checking.
                IMethod checking = (IMethod)itElement.Element;

                // If the name does not match or the number of parameters are not equal, continue.
                if (checking.Name != name || checking.Parameters.Length != parameterTypes.Length) return ScopeIterateAction.Continue;

                // Loop through all parameters.
                for (int p = 0; p < checking.Parameters.Length; p++)
                    // If the parameter types do not match, continue.
                    if (checking.Parameters[p].GetCodeType(deltinScript) != parameterTypes[p])
                        return ScopeIterateAction.Continue;

                // Parameter overload matches.
                method = checking;
                return ScopeIterateAction.Stop;
            });

            return method;
        }

        public void AddType(ICodeTypeInitializer initializer) => _types.Add(initializer);

        public void AddNative(IScopeable scopeable)
        {
            if (scopeable is IMethod method) AddNativeMethod(method);
            if (scopeable is IVariableInstance variable) AddNativeVariable(variable);
            if (scopeable is ICodeTypeInitializer type) AddType(type);
        }

        public bool AccessorMatches(IScopeable element)
        {
            if (element.AccessLevel == AccessLevel.Public) return true;

            bool matches = false;

            IterateElements(true, true, itElement =>
            {
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

            Scope current = this;
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

        public CompletionItem[] GetCompletion(DeltinScript deltinScript, DocPos pos, bool immediate, Scope getter = null)
        {
            var completions = new List<CompletionItem>(); // The list of completion items in this scope.

            // Get the functions.
            var batches = new List<FunctionBatch>();
            IterateParents(scope =>
            {
                // Iterate through each function.
                foreach (var func in scope._methods)
                    // If the function is scoped at pos,
                    // add it to a batch.
                    if (scope.WasScopedAtPosition(func, pos, getter))
                    {
                        bool batchFound = false; // Determines if a batch was found for the function.

                        // Iterate through each existing batch.
                        foreach (var batch in batches)
                            // If the current batch's name is equal to the function's name, add it to the batch.
                            if (batch.Name == func.Name)
                            {
                                batch.Add();
                                batchFound = true;
                                break;
                            }

                        // If no batch was found for the function name, create a new batch.
                        if (!batchFound)
                            batches.Add(new FunctionBatch(func.Name, func));
                    }

                // Add the variables.
                foreach (var variable in scope._variables)
                    // Make sure the variable is not a method group and it is scoped at the current position.
                    if (variable is MethodGroup == false && scope.WasScopedAtPosition(variable, pos, getter))
                        // If TagPlayerVariables is true and the variable is a player variable, use a new completion item that highlights the variable.
                        if (TagPlayerVariables && variable.Provider.VariableType == VariableType.Player)
                            completions.Add(new CompletionItem() {
                                Label = "★ " + variable.Name,
                                SortText = "!" + variable.Name, // Prepend '!' to the variable name so it shows up at the top of the completion list.
                                InsertText = variable.Name, // Override InsertText so that the star is not inserted with the variable name.
                                Kind = CompletionItemKind.Variable,
                                Detail = variable.CodeType.GetCodeType(deltinScript).GetName() + " " + variable.Name
                            });
                        else
                            completions.Add(variable.GetCompletion(deltinScript));

                // Add the types.
                foreach (var type in scope._types)
                {
                    var completion = type.GetCompletion();
                    if (completion != null)
                        completions.Add(completion);
                }

                return scope.CompletionCatch;
            });

            // Get the batch completion.
            foreach (var batch in batches)
                completions.Add(batch.GetCompletion(deltinScript));

            return completions.ToArray();
        }

        private bool WasScopedAtPosition(IScopeable element, DocPos pos, Scope getter)
        {
            return (pos == null || element.DefinedAt == null || element.WholeContext || element.DefinedAt.range.Start <= pos) && (getter == null || getter.AccessorMatches(element));
        }

        public bool ScopeContains(IScopeable element)
        {
            // Variable
            if (element is IVariableInstance variable) return ScopeContains(variable);
            // Function
            else if (element is IMethod function) return ScopeContains(function);
            else throw new NotImplementedException();
        }

        public bool ScopeContains(IVariable provider)
        {
            bool found = false;
            IterateElements(true, true, iterate => {
                if ((iterate.Element is IVariableInstance instance) && instance.Provider == provider)
                {
                    found = true;
                    return ScopeIterateAction.Stop;
                }
                return ScopeIterateAction.Continue;
            });
            return found;
        }

        public bool ScopeContains(IVariableInstance variable)
        {
            bool found = false;
            IterateElements(true, true, iterate =>
            {
                if (iterate.Element == variable)
                {
                    found = true;
                    return ScopeIterateAction.Stop;
                }
                return ScopeIterateAction.Continue;
            });
            return found;
        }

        public bool ScopeContains(IMethod function)
        {
            bool found = false;
            IterateParents(scope =>
            {
                found = scope._methods.Contains(function);
                return found;
            });
            return found;
        }

        public void EndScope(ActionSet actionSet, bool includeParents)
        {
            if (MethodContainer) return;

            foreach (IVariableInstance variable in _variables)
                if (actionSet.IndexAssigner.TryGet(variable.Provider, out IGettable gettable) && // and the current scopeable is assigned to an index,
                    gettable is RecursiveIndexReference recursiveIndexReference) // and the assigned index is a RecursiveIndexReference,
                    // Pop the variable stack.
                    actionSet.AddAction(recursiveIndexReference.Pop());

            if (includeParents && Parent != null)
                Parent.EndScope(actionSet, true);
        }

        Scope IScopeProvider.GetObjectBasedScope() => this;
        Scope IScopeProvider.GetStaticBasedScope() => this;
        IMethod IScopeProvider.GetOverridenFunction(DeltinScript deltinScript, FunctionOverrideInfo functionOverrideInfo) => throw new NotImplementedException();
        IVariableInstance IScopeProvider.GetOverridenVariable(string variableName) => throw new NotImplementedException();
        void IScopeAppender.AddObjectBasedScope(IMethod function) => CopyMethod(function);
        void IScopeAppender.AddStaticBasedScope(IMethod function) => CopyMethod(function);
        void IScopeAppender.AddObjectBasedScope(IVariableInstance variable) => CopyVariable(variable);
        void IScopeAppender.AddStaticBasedScope(IVariableInstance variable) => CopyVariable(variable);
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

    class FunctionBatch
    {
        public string Name { get; }
        public IMethod Primary { get; }
        public int Overloads { get; private set; }

        public FunctionBatch(string name, IMethod primary)
        {
            Name = name;
            Primary = primary;
        }

        public void Add() => Overloads++;

        public CompletionItem GetCompletion(DeltinScript deltinScript) => new CompletionItem()
        {
            Label = Name,
            Kind = CompletionItemKind.Function,
            Documentation = Primary.Documentation,
            Detail = IMethod.DefaultLabel(deltinScript, LabelInfo.SignatureOverload, Primary).ToString(false)
            // Fancy label (similiar to what c# does)
            // Documentation = new MarkupBuilder()
            //     .StartCodeLine()
            //     .Add(
            //         (Primary.DoesReturnValue ? (Primary.ReturnType == null ? "define" : Primary.ReturnType.GetName()) : "void") + " " +
            //         Primary.GetLabel(false) + (Overloads == 0 ? "" : " (+" + Overloads + " overloads)")
            //     ).EndCodeLine().ToMarkup()
        };
    }
}