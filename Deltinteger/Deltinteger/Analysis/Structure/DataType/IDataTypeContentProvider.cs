using System;
using System.Linq;
using DS.Analysis.Types;
using DS.Analysis.Core;

namespace DS.Analysis.Structure.DataTypes
{
    interface IDataTypeContentProvider : IDisposable
    {
        string GetName();
        SetupDataType Setup(ContextInfo contextInfo);
    }

    struct SetupDataType
    {
        public readonly AbstractDeclaredElement[] Declarations;
        public readonly ICodeTypeProvider DataTypeProvider;

        public SetupDataType(AbstractDeclaredElement[] declarations, ICodeTypeProvider dataTypeProvider)
        {
            Declarations = declarations;
            DataTypeProvider = dataTypeProvider;
        }
    }
}