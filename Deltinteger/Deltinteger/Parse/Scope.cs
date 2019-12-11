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

        public Scope Child()
        {
            return new Scope(this);
        }

        /// <summary>
        /// Adds a variable to the current scope.
        /// When handling variables added by the user, supply the diagnostics and range to show the syntax error at.
        /// When handling variables added internally, have the diagnostics and range parameters null. An exception will be thrown instead if there is a syntax error.
        /// </summary>
        /// <param name="variable">The variable that will be added to the current scope. If the object reference is already in the direct scope, an exception will be thrown.</param>
        /// <param name="diagnostics"></param>
        /// <param name="range"></param>
        public void AddVariable(IScopeable variable, FileDiagnostics diagnostics, DocRange range)
        {
            if (variable == null) throw new ArgumentNullException(nameof(variable));
            if (Variables.Contains(variable)) throw new Exception("variable reference is already in scope.");

            if (IsVariable(variable.Name))
                diagnostics.Error(string.Format("A variable of the name {0} was already defined in this scope.", variable.Name), range);
            else
                Variables.Add(variable);
        }

        public bool IsVariable(string name)
        {
            return GetVariable(name, null, null) != null;
        }

        public IScopeable GetVariable(string name, FileDiagnostics diagnostics, DocRange range)
        {
            IScopeable element = null;
            Scope current = this;
            while (current != null && element == null)
            {
                element = current.Variables.FirstOrDefault(element => element.Name == name);
                current = current.Parent;
            }

            if (range != null && element == null)
                diagnostics.Error(string.Format("The variable {0} does not exist in the {1}.", name, ErrorName), range);

            return element;
        }

        public IScopeable[] AllVariablesInScope()
        {
            List<IScopeable> variables = new List<IScopeable>();

            Scope current = this;
            while (current != null)
            {
                variables.AddRange(current.Variables);
                current = current.Parent;
            }

            return variables.ToArray();
        }

        public IScopeable[] AllChildVariables()
        {
            List<IScopeable> allVariables = new List<IScopeable>();
            allVariables.AddRange(Variables);
            foreach (var child in children)
                allVariables.AddRange(child.Variables);
            return allVariables.ToArray();
        }

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
                        if (range == null) throw new Exception();
                        diagnostics.Error("A method with the same name and parameter types was already defined in this scope.", range);
                        return;
                    }
                }

            Methods.Add(method);
        }

        public IMethod[] AllMethodsInScope()
        {
            List<IMethod> methods = new List<IMethod>();

            Scope current = this;
            while (current != null)
            {
                methods.AddRange(current.Methods);
                current = current.Parent;
            }

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

        public CompletionItem[] GetCompletion(Pos pos)
        {
            List<CompletionItem> completions = new List<CompletionItem>();

            var variables = AllVariablesInScope();
            for (int i = 0; i < variables.Length; i++)
                if (variables[i].DefinedAt == null || variables[i].DefinedAt.range.start <= pos)
                    completions.Add(variables[i].GetCompletion());

            var methods = AllMethodsInScope();
            for (int i = 0; i < methods.Length; i++)
                completions.Add(methods[i].GetCompletion());
                
            return completions.ToArray();
        }

        public static Scope GetGlobalScope()
        {
            Scope globalScope = new Scope();

            // Add workshop methods
            foreach (var workshopMethod in ElementList.Elements)
                globalScope.AddMethod(workshopMethod, null, null);
            
            // Add custom methods
            foreach (var builtInMethod in CustomMethodData.GetCustomMethods())
                globalScope.AddMethod(builtInMethod, null, null);

            return globalScope;
        }
    }
}