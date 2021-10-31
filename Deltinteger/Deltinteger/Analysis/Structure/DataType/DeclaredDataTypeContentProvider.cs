using Deltin.Deltinteger.Compiler.SyntaxTree;
using DS.Analysis.Structure.Utility;

namespace DS.Analysis.Structure.DataTypes
{
    class DeclaredDataTypeContentProvider : AbstractDataTypeContentProvider
    {
        readonly ClassContext _context;
        public DeclaredDataTypeContentProvider(ClassContext context) => _context = context;
        public override AbstractDeclaredElement[] GetDeclarations() => StructureUtility.DeclarationsFromSyntax(_context.Declarations);
        public override string GetName() => _context.Identifier.Text;
    }
}