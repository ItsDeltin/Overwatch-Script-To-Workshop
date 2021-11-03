using DS.Analysis.Structure;
using DS.Analysis.Scopes;
using DS.Analysis.Scopes.Import;
using DS.Analysis.Utility;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Statements
{
    class ImportStatement : Statement
    {
        readonly FileRootScopeSource _scopeSource;
        ContextInfo context;

        public ImportStatement(StructureContext structure, Import syntax)
        {
            // Create file dependency.
            AddDisposable(_scopeSource = new FileRootScopeSource(structure.File.Analysis, structure.File.GetRelativePath(syntax.File.Text.RemoveQuotes())));
        }

        public override void GetMeta(ContextInfo context) => this.context = context;

        public override Scope ProceedWithScope() => context.Scope.CreateChild(_scopeSource);
    }
}