using DS.Analysis.Structure.Utility;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Structure.Modules
{
    interface IModuleContentProvider
    {
        string GetName();
        AbstractDeclaredElement[] GetDeclarations(ContextInfo contextInfo);
    }

    class ModuleContentProvider : IModuleContentProvider
    {
        readonly ModuleContext syntax;

        public ModuleContentProvider(ModuleContext syntax)
        {
            this.syntax = syntax;
        }

        public string GetName() => syntax.Identifier.Text;
        public AbstractDeclaredElement[] GetDeclarations(ContextInfo contextInfo) => StructureUtility.DeclarationsFromSyntax(contextInfo, syntax.Declarations);
    }
}