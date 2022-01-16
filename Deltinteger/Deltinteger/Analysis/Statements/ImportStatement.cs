using System;
using System.Linq;
using System.Collections.Generic;
using DS.Analysis.Structure;
using DS.Analysis.Scopes;
using DS.Analysis.Scopes.Import;
using DS.Analysis.Utility;
using DS.Analysis.Diagnostics;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using DS.Analysis.Types;
using DS.Analysis.Expressions.Identifiers;

namespace DS.Analysis.Statements
{
    /// <summary>User-declared import statement.</summary>
    class ImportStatement : Statement, IFileImportErrorHandler
    {
        /// <summary>The import error token.</summary>
        readonly DiagnosticToken token;

        /// <summary>The current import diagnostic.</summary>
        IDisposable currentDiagnostic;

        /// <summary>The scope to import from.</summary>
        readonly IScopeSource scopeSource;

        /// <summary>If true, the entire scopeSource will be imported. Otherwise, only select items will be imported from within the scope.</summary>
        readonly bool importEntireScope;

        readonly bool valid = true;

        readonly ScopeSource selectionSource = new ScopeSource();

        public ImportStatement(ContextInfo context, Import syntax)
        {
            string sourceName = null;

            // Importing a file
            // ex: 'import "math.del";'
            if (syntax.File != null)
            {
                token = context.Diagnostics.CreateToken(syntax.File.Range);

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
                var module = context.File.Analysis.ModuleManager.ModuleFromPath(PathFromSyntax(syntax.Module));

                scopeSource = module;
                AddDisposable(scopeSource.Subscribe()); // Adds a reference to the module.
            }
            // Syntax eror
            else
                valid = false;

            if (valid)
            {
                // If syntax.ImportSelection != null, the user declared a list of elements to import.
                // ex: 'import { Bakemap } from Pathmap;'
                if (syntax.ImportSelection != null)
                    ImportSelected(syntax.ImportSelection.ToArray(), scopeSource, context.ScopeAppender, context.File.Diagnostics, sourceName);
                // Otherwise, the entire module or file is being imported.
                // ex: 'import Pathmap;'
                else
                    importEntireScope = true;
            }
        }

        public override IScopeSource AddSourceToContext() => importEntireScope ? scopeSource : selectionSource;

        void ImportSelected(ImportSelection[] importElements, IScopeSource importFrom, IScopeAppender importTo, FileDiagnostics diagnostics, string sourceName)
        {
            // Create the ScopedElement
            var elements = new ImportedElement[importElements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                string reference = importElements[i].Identifier.Text;
                string alias = importElements[i].Alias ? importElements[i].Alias.Text : reference;
                AddDisposable(elements[i] = new ImportedElement(selectionSource, diagnostics.CreateToken(importElements[i].Identifier.Range), alias, reference, sourceName));
                selectionSource.AddScopedElement(elements[i]);
            }

            // Subscribe to the importFrom scope
            AddDisposable(importFrom.Subscribe(value =>
            {
                // Update the elements when the importFrom scope changes.
                foreach (var element in elements)
                    element.Update(value.Elements);
            }));

            // Add the imported elements to the scope.
            foreach (var element in elements)
                importTo.AddScopedElement(element);
        }

        class ImportedElement : ScopedElement, IDisposable
        {
            readonly ScopeSource selectionSource;
            readonly DiagnosticToken token;
            readonly string reference;
            readonly string sourceName;

            ScopedElement match;
            IDisposable diagnostic;


            public ImportedElement(ScopeSource selectionSource, DiagnosticToken token, string alias, string reference, string sourceName) : base(alias)
            {
                this.selectionSource = selectionSource;
                this.token = token;
                this.reference = reference;
                this.sourceName = sourceName;
            }

            public void Update(ScopedElement[] elements)
            {
                Reset();

                // Find matching element
                match = elements.FirstOrDefault(element => element.Name == reference);

                if (match == null && sourceName != null)
                    diagnostic = token.Error(Messages.ElementNonexistentInSource(reference, sourceName));

                selectionSource.Refresh();
            }

            void Reset()
            {
                diagnostic?.Dispose();
                diagnostic = null;
            }


            public void Dispose() => Reset();


            public override CodeTypeProvider Provider => match?.Provider;
            public override IIdentifierHandler IdentifierHandler => match?.IdentifierHandler;
            public override ITypePartHandler TypePartHandler => match?.TypePartHandler;
        }


        /// <summary>Converts a list of tokens into an array of strings.</summary>
        static string[] PathFromSyntax(List<Token> modulePath) => modulePath.Select(token => token.Text).ToArray();


        public override void Dispose()
        {
            base.Dispose();
            DisposeDiagnostic();
        }


        // IFileImportErrorHandler
        void IFileImportErrorHandler.Success() => DisposeDiagnostic();

        void IFileImportErrorHandler.Error(string message)
        {
            DisposeDiagnostic();
            currentDiagnostic = token.Error(message);
        }

        void DisposeDiagnostic()
        {
            currentDiagnostic?.Dispose();
            currentDiagnostic = null;
        }
    }
}