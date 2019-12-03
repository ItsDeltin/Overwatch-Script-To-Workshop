using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class Scope
    {
        private List<IScopeable> inScope { get; } = new List<IScopeable>();
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

        public void AddVariable(IScopeable variable, FileDiagnostics diagnostics, DocRange range)
        {
            if (variable == null) throw new ArgumentNullException(nameof(variable));
            if (Variables.Contains(variable)) throw new Exception("variable reference is already in scope.");

            if (IsVariable(variable.Name))
                diagnostics.Error(string.Format("A variable of the name {0} was already defined in this scope.", variable.Name), range);
            else
                Variables.Add(variable);
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

        public bool IsVariable(string name)
        {
            return GetVariable(name, null, null) != null;
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
                        if (method.Parameters[p] != m.Parameters[p])
                            matches = false;

                    if (matches)
                    {
                        if (range == null) throw new Exception();
                        diagnostics.Error("A method with the same name and parameter types was already defined in this scope.", range);
                        return;
                    }
                }

            // TODO: check if method signature already exists.
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