namespace DS.Analysis.Structure.DataTypes
{
    abstract class AbstractDataTypeContentProvider
    {
        public abstract AbstractDeclaredElement[] GetDeclarations();
        public abstract string GetName();
    }
}