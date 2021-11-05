using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Structure;
using DS.Analysis.Scopes;
using DS.Analysis.Scopes.Import;
using DS.Analysis.Utility;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Statements
{
    class ImportStatement : Statement
    {
        readonly IScopeSource scopeSource;
        readonly bool importEntireScope;

        public ImportStatement(StructureContext structure, Import syntax)
        {
            // Importing a file
            // ex: 'import "math.del";'
            if (syntax.File != null)
            {
                // Create file dependency.
                var fileRootScopeSource = new FileRootScopeSource(structure.File.Analysis, structure.File.GetRelativePath(syntax.File.Text.RemoveQuotes()));
                AddDisposable(fileRootScopeSource);
                scopeSource = fileRootScopeSource;
            }
            // Importing a module
            // ex: 'import Pathmap;'
            else if (syntax.Module != null)
            {
                // Get the module from the path.
                var module = structure.File.Analysis.ModuleManager.ModuleFromPath(PathFromSyntax(syntax.Module));
                
                scopeSource = module;
                AddDisposable(scopeSource.Subscribe()); // Adds a reference to the module.
            }

            // If syntax.ImportSelection != null, the user declared a list of elements to import.
            // ex: 'import { Bakemap } from Pathmap;'
            if (syntax.ImportSelection != null)
                ImportSelected(ImportElementListFromSyntax(syntax.ImportSelection), scopeSource, structure.ScopeSource);
            // Otherwise, the entire module or file is being imported.
            // ex: 'import Pathmap;'
            else
                importEntireScope = true;
        }

        public override Scope ProceedWithScope() => importEntireScope ? ContextInfo.Scope.CreateChild(scopeSource) : null;

        void ImportSelected(ImportElement[] importElements, IScopeSource importFrom, IScopeAppender importTo)
        {
            // Create the ScopedElement
            var elements = new ImportedElement[importElements.Length];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = new ImportedElement(importElements[i].alias ?? importElements[i].name, importElements[i].name);

            // Subscribe to the importFrom scope
            AddDisposable(importFrom.Subscribe(value => {
                // Update the elements when the importFrom scope changes.
                foreach (var element in elements)
                    element.Update(value.Elements);
            }));

            // Add the imported elements to the scope.
            foreach (var element in elements)
                importTo.AddScopedElement(element);
        }

        readonly record struct ImportElement(string name, string alias);

        class ImportedElement : ScopedElement
        {
            readonly string reference;
            IDisposable referenceSubscription;

            public ImportedElement(string alias, string reference) : base(alias)
            {
                this.reference = reference;
            }

            public void Update(ScopedElement[] elements)
            {
                referenceSubscription?.Dispose();
                referenceSubscription = null;

                var match = elements.FirstOrDefault(element => element.Alias == reference);

                if (match == null)
                {
                    Observers.Set(ScopedElementData.Unknown);
                    return;
                }
                
                // Create link
                referenceSubscription = match.Subscribe(Observers.Set);
            }
        }


        /// <summary>Converts a list of ImportSelections into an array of ImportElement records.</summary>
        static ImportElement[] ImportElementListFromSyntax(List<ImportSelection> syntax)
            => syntax.Select(selection => new ImportElement(selection.Identifier.Text, selection.Alias?.Text)).ToArray();
        
        /// <summary>Converts a list of tokens into an array of strings.</summary>
        static string[] PathFromSyntax(List<Token> modulePath)
            => modulePath.Select(token => token.Text).ToArray();
    }
}