using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Scopes;
using DS.Analysis.Scopes.Selector;
using DS.Analysis.Utility;
using DS.Analysis.Diagnostics;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using DS.Analysis.Methods;

namespace DS.Analysis.Statements
{
    /// <summary>User-declared import statement.</summary>
    class ImportStatement : Statement, IFileImportErrorHandler
    {
        /// <summary>The import error token.</summary>
        readonly DiagnosticToken token;

        /// <summary>The name of the item being imported from.</summary>
        readonly string sourceName;

        /// <summary>The scope to import from.</summary>
        readonly IScopeSource scopeSource;

        /// <summary>If true, the entire scopeSource will be imported. Otherwise, only select items will be imported from within the scope.</summary>
        readonly bool importEntireScope;

        readonly bool valid = true;

        readonly ScopeSource selectionSource = new ScopeSource();

        /// <summary>Importing specific elements from another file.</summary>
        ImportedElement[] importedElements;

        public ImportStatement(ContextInfo context, Import syntax) : base(context)
        {
            // Importing a file
            // ex: 'import "math.del";'
            if (syntax.File != null)
            {
                token = AddDisposable(context.Diagnostics.CreateToken(syntax.File.Range));

                var fileName = syntax.File.Text.RemoveQuotes();
                sourceName = "file " + fileName;

                // Create file dependency.
                var fileRootScopeSource = new FileRootScopeSource(context.File.Analysis, context.File.GetRelativePath(fileName), this);
                AddDisposable(fileRootScopeSource);
                scopeSource = fileRootScopeSource;
            }
            // Importing a module
            // ex: 'import Pathmap;'
            else if (syntax.Module != null)
            {
                var modulePath = PathFromSyntax(syntax.Module);
                sourceName = "module " + string.Join(".", modulePath);

                // Get the module from the path.
                var module = context.File.Analysis.ModuleManager.ModuleFromPath(modulePath);

                scopeSource = module;
                DependOn(module); // Adds a reference to the module.
            }
            // Syntax eror
            else
                valid = false;

            if (valid)
            {
                // If syntax.ImportSelection != null, the user declared a list of elements to import.
                // ex: 'import { Bakemap } from Pathmap;'
                if (syntax.ImportSelection != null)
                    ImportSelected(syntax.ImportSelection.ToArray());
                // Otherwise, the entire module or file is being imported.
                // ex: 'import Pathmap;'
                else
                    importEntireScope = true;
            }
        }

        public override IScopeSource AddSourceToContext() => importEntireScope ? scopeSource : selectionSource;

        void ImportSelected(ImportSelection[] importElements)
        {
            DependOn(scopeSource);

            // Create the ScopedElement
            this.importedElements = new ImportedElement[importElements.Length];
            for (int i = 0; i < this.importedElements.Length; i++)
            {
                // The name of the element being imported.
                string reference = importElements[i].Identifier.Text;
                // The imported element's name in the current scope.
                string alias = importElements[i].Alias ? importElements[i].Alias.Text : reference;

                AddDisposable(this.importedElements[i] = new ImportedElement(
                    import: this,
                    token: Context.Diagnostics.CreateToken(importElements[i].Identifier.Range),
                    alias,
                    reference));
            }
        }

        public override void Update()
        {
            base.Update();

            selectionSource.Clear();
            foreach (var element in importedElements)
                element.Update(scopeSource.Elements);
        }

        class ImportedElement : IDisposable
        {
            readonly ImportStatement import;
            readonly DiagnosticToken token;
            readonly string alias;
            readonly string reference;

            public ImportedElement(ImportStatement import, DiagnosticToken token, string alias, string reference)
            {
                this.import = import;
                this.token = token;
                this.alias = alias;
                this.reference = reference;
            }

            public void Dispose() => token.Dispose();

            public void Update(ScopedElement[] elements)
            {
                token.Dispose();

                // Find the elements with a matching name.
                var matchesName = elements.Where(element => element.Name == reference);
                var match = elements.FirstOrDefault();

                // Not found
                if (match == null)
                {
                    if (import.sourceName != null)
                        token.Error(Messages.ElementNonexistentInSource(reference, import.sourceName));
                }
                else
                {
                    // Found; add alias
                    match.ElementSelector.Alias(new RelatedElements(matchesName), alias, import.selectionSource);
                }
            }
        }


        /// <summary>Converts a list of tokens into an array of strings.</summary>
        static string[] PathFromSyntax(List<Token> modulePath) => modulePath.Select(token => token.Text).ToArray();


        // IFileImportErrorHandler
        void IFileImportErrorHandler.Success() => token.Dispose();
        void IFileImportErrorHandler.Error(string message) => token.Error(message);
    }
}