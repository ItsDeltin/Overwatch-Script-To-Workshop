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

        public Scope Child() => new Scope(this);
        public Scope Child(string name) => new Scope(this) { ErrorName = name };
        public Scope Child(bool containConflicts) => new Scope(this) { CatchConflict = true };

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

        public IEnumerable<IVariableInstance> GetAllVariables(string name, bool methodGroupsOnly)
        {
            var functions = new List<IMethod>();
            var variables = new List<IVariableInstance>();

            Scope current = this;
            while (current != null)
            {
                // Add functions with the same name.
                functions.AddRange(current._methods.Where(f => f.Name == name));

                // Set the variable if it was not set yet.
                variables.AddRange(current._variables.Where(
                    v => v.Name == name && (!methodGroupsOnly ||
                    v is MethodGroup ||
                    v.CodeType is Lambda.PortableLambdaType)));

                // Go to the parent scope.
                current = current.Parent;
            }

            // Variables take priority over method groups.
            if (variables.Count > 0 || functions.Count == 0) return variables;

            // If there were any functions that share the variable name, return the method group.
            return new[] { new MethodGroup(name, functions.ToArray()) };
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

        public void AddNativeMethod(IMethod method) => _methods.Add(method);

        public void AddType(ICodeTypeInitializer initializer) => _types.Add(initializer);

        public void AddNative(IScopeable scopeable)
        {
            if (scopeable is IMethod method) AddNativeMethod(method);
            if (scopeable is IVariableInstance variable) AddNativeVariable(variable);
            if (scopeable is ICodeTypeInitializer type) AddType(type);
        }

        public CompletionItem[] GetCompletion(DeltinScript deltinScript, DocPos pos, bool immediate, CodeType getter = null)
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

        private bool WasScopedAtPosition(IVariableInstance variable, DocPos pos, CodeType getter) => WasScopedAtPosition(variable, pos, variable.Attributes.ContainingType, getter);
        private bool WasScopedAtPosition(IMethod method, DocPos pos, CodeType getter) => WasScopedAtPosition(method, pos, method.Attributes.ContainingType, getter);
        private bool WasScopedAtPosition(IScopeable element, DocPos pos, CodeType containingType, CodeType getter) =>
            (pos == null || element.DefinedAt == null || element.WholeContext || element.DefinedAt.range.Start <= pos) && SemanticsHelper.AccessLevelMatches(element.AccessLevel, containingType, getter);

        public bool ScopeContains(IScopeable element)
        {
            // Variable
            if (element is IVariableInstance variable) return ScopeContains(variable);
            // Function
            else if (element is IMethod function) return ScopeContains(function);
            else throw new NotImplementedException();
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
        void IScopeAppender.AddObjectBasedScope(IMethod function) => AddNativeMethod(function);
        void IScopeAppender.AddStaticBasedScope(IMethod function) => AddNativeMethod(function);
        void IScopeAppender.AddObjectBasedScope(IVariableInstance variable) => AddNativeVariable(variable);
        void IScopeAppender.AddStaticBasedScope(IVariableInstance variable) => AddNativeVariable(variable);

        public void CheckConflict(ParseInfo parseInfo, CheckConflict identifier, DocRange range) => SemanticsHelper.ErrorIfConflicts(
            parseInfo: parseInfo,
            identifier: identifier,
            nameConflictMessage: "The definition '" + identifier.Name + "' already exists in the current scope",
            overloadConflictMessage: "The current scope already contains a definition '" + identifier.Name + "' with the same name and parameter types",
            range: range,
            this);
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