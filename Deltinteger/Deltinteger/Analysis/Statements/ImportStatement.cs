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
    class ImportStatement : Statement, IFileImportErrorHandler
    {
        readonly DocRange range;
        readonly IScopeSource scopeSource;
        readonly bool importEntireScope;
        readonly FileDiagnostics diagnostics;
        Diagnostic currentDiagnostic;

        public ImportStatement(StructureContext structure, Import syntax)
        {
            diagnostics = structure.File.Diagnostics;

            string sourceName = null;

            // Importing a file
            // ex: 'import "math.del";'
            if (syntax.File != null)
            {
                range = syntax.File.Range;

                var fileName = syntax.File.Text.RemoveQuotes();
                sourceName = "file " + fileName;

                // Create file dependency.
                var fileRootScopeSource = new FileRootScopeSource(structure.File.Analysis, structure.File.GetRelativePath(fileName), this);
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
                var module = structure.File.Analysis.ModuleManager.ModuleFromPath(PathFromSyntax(syntax.Module));
                
                scopeSource = module;
                AddDisposable(scopeSource.Subscribe()); // Adds a reference to the module.
            }

            // If syntax.ImportSelection != null, the user declared a list of elements to import.
            // ex: 'import { Bakemap } from Pathmap;'
            if (syntax.ImportSelection != null)
                ImportSelected(syntax.ImportSelection.ToArray(), scopeSource, structure.ScopeSource, structure.File.Diagnostics, sourceName);
            // Otherwise, the entire module or file is being imported.
            // ex: 'import Pathmap;'
            else
                importEntireScope = true;
        }

        public override Scope ProceedWithScope() => importEntireScope ? ContextInfo.Scope.CreateChild(scopeSource) : null;

        void ImportSelected(ImportSelection[] importElements, IScopeSource importFrom, IScopeAppender importTo, FileDiagnostics diagnostics, string sourceName)
        {
            // Create the ScopedElement
            var elements = new ImportedElement[importElements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                string name = importElements[i].Identifier.Text;
                string alias = importElements[i].Alias ? importElements[i].Alias.Text : name;
                AddDisposable(elements[i] = new ImportedElement(alias, name, importElements[i].Identifier.Range, diagnostics, sourceName));
            }

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

        class ImportedElement : ScopedElement, IDisposable
        {
            readonly string reference;
            readonly DocRange referenceRange;
            readonly FileDiagnostics diagnostics;
            readonly string sourceName;
            Diagnostic referenceDiagnostic;
            IDisposable referenceSubscription;

            public ImportedElement(string alias, string reference, DocRange referenceRange, FileDiagnostics diagnostics, string sourceName) : base(alias)
            {
                this.reference = reference;
                this.referenceRange = referenceRange;
                this.diagnostics = diagnostics;
                this.sourceName = sourceName;
            }

            public void Update(ScopedElement[] elements)
            {
                referenceSubscription?.Dispose();
                referenceSubscription = null;
                DisposeDiagnostic();

                var match = elements.FirstOrDefault(element => element.Alias == reference);

                if (match == null)
                {
                    if (sourceName != null)
                        referenceDiagnostic = diagnostics.Error(Messages.ElementNonexistentInSource(reference, sourceName), referenceRange);
                    Observers.Set(ScopedElementData.Unknown);
                    return;
                }
                
                // Create link
                referenceSubscription = match.Subscribe(scopedElementData => Observers.Set(new ImportedElementData(scopedElementData, Alias)));
            }

            void DisposeDiagnostic()
            {
                referenceDiagnostic?.Dispose();
                referenceDiagnostic = null;
            }

            void IDisposable.Dispose() => DisposeDiagnostic();

            class ImportedElementData : ScopedElementData
            {
                readonly ScopedElementData baseData;
                readonly string alias;

                public ImportedElementData(ScopedElementData baseData, string alias)
                {
                    this.baseData = baseData;
                    this.alias = alias;
                }


                public override CodeTypeProvider GetCodeTypeProvider() => baseData.GetCodeTypeProvider();
                public override IIdentifierHandler GetIdentifierHandler() => baseData.GetIdentifierHandler();
                public override bool IsMatch(string name) => alias == name;
            }
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
            currentDiagnostic = diagnostics.Error(message, range);
        }

        void DisposeDiagnostic()
        {
            currentDiagnostic?.Dispose();
            currentDiagnostic = null;
        }
    }
}