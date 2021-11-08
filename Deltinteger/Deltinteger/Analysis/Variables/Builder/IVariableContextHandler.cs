using System;
using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Variables.Builder
{
    interface IVariableContextHandler : IDisposable
    {
        string GetName();

        ITypeDirector GetTypeDirector(ContextInfo contextInfo);
    }

    class VariableContextHandler : IVariableContextHandler
    {
        readonly VariableDeclaration declaration;
        TypeReference typeReference;

        public VariableContextHandler(VariableDeclaration declaration)
        {
            this.declaration = declaration;
        }

        public string GetName() => declaration.Identifier.Text;
        public ITypeDirector GetTypeDirector(ContextInfo contextInfo) => typeReference = TypeFromContext.TypeReferenceFromContext(contextInfo, declaration.Type);

        public void Dispose()
        {
            typeReference.Dispose();
        }
    }
}