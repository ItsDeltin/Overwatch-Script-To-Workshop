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
        private List<Var> Variables { get; } = new List<Var>();
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

        public void AddVariable(Var variable, FileDiagnostics diagnostics, DocRange range)
        {
            if (variable == null) throw new ArgumentNullException(nameof(variable));
            if (Variables.Contains(variable)) throw new Exception("variable reference is already in scope.");

            if (IsVariable(variable.Name))
                diagnostics.Error(string.Format("A variable of the name {0} was already defined in this scope.", variable.Name), range);
            else
                Variables.Add(variable);
        }

        public void AddMethod(IMethod method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (Methods.Contains(method)) throw new Exception("method reference is already in scope.");

            // TODO: check if method signature already exists.
            Methods.Add(method);
        }

        public Var GetVariable(string name, FileDiagnostics diagnostics, DocRange range)
        {
            Var element = null;
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

        // TODO: GetMethod

        public void In(IScopeable element)
        {
            if (element == null) throw new Exception("element should not be null.");
            if (inScope.Contains(element)) throw new Exception("element is already in scope.");
            inScope.Add(element);
        }

        public IScopeable GetInScope(string name, string type, FileDiagnostics diagnostics, DocRange range)
        {
            IScopeable element = null;
            Scope current = this;
            while (current != null && element == null)
            {
                element = current.inScope.FirstOrDefault(element => element.Name == name);
                current = current.Parent;
            }

            if (range != null && element == null)
                diagnostics.Error(string.Format("The {0} {1} does not exist in the {2}.", type, name, ErrorName), range);

            return element;
        }

        public bool WasDefined(string name)
        {
            return GetInScope(name, null, null, null) != null;
        }

        public static Scope GetGlobalScope()
        {
            Scope globalScope = new Scope();

            // Add workshop methods
            foreach (var workshopMethod in ElementList.Elements)
                globalScope.In(workshopMethod);
            
            // Add custom methods
            foreach (var builtInMethod in CustomMethodData.GetCustomMethods())
                globalScope.In(builtInMethod);

            return globalScope;
        }
    }
}