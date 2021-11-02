using DS.Analysis.Structure.Utility;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Structure.DataTypes
{
    interface IDataTypeContentProvider
    {
        string GetName();
        AbstractDeclaredElement[] GetDeclarations(StructureContext structure);
    }

    class DataTypeContentProvider : IDataTypeContentProvider
    {
        readonly ClassContext _context;
        public DataTypeContentProvider(ClassContext context) => _context = context;
        public string GetName() => _context.Identifier.Text;
        public AbstractDeclaredElement[] GetDeclarations(StructureContext structure) => StructureUtility.DeclarationsFromSyntax(structure, _context.Declarations);
    }
}