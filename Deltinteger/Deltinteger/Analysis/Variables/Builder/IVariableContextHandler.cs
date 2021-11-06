using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Variables.Builder
{
    interface IVariableContextHandler
    {
        string GetName();

        ITypeDirector GetTypeDirector(ContextInfo contextInfo);
    }

    class VariableContextHandler : IVariableContextHandler
    {
        readonly VariableDeclaration declaration;

        public VariableContextHandler(VariableDeclaration declaration)
        {
            this.declaration = declaration;
        }

        public string GetName() => declaration.Identifier.Text;
        public ITypeDirector GetTypeDirector(ContextInfo contextInfo) => TypeFromContext.TypeReferenceFromContext(contextInfo, declaration.Type);
    }
}