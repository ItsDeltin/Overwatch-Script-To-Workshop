using Deltin.Deltinteger.Compiler.SyntaxTree;
using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;

namespace DS.Analysis.Structure.Methods
{
    class DeclaredMethodContentProvider : AbstractMethodContentProvider
    {
        readonly FunctionContext _syntax;

        public DeclaredMethodContentProvider(FunctionContext syntax)
        {
            _syntax = syntax;
        }

        public override string GetName() => _syntax.Identifier.Text;

        public override TypeReference GetType(ContextInfo metaContext) => TypeFromContext.TypeReferenceFromContext(metaContext.Scope, _syntax.Type);

        public override Parameter[] GetParameters(ContextInfo context)
        {
            return new Parameter[0];
        }

        public override IMethodContent GetContent(ContextInfo context)
        {
            return new DeclaredMethodContent(context.GetBlock(_syntax.Block));
        }

        class DeclaredMethodContent : IMethodContent
        {
            readonly BlockAction _block;

            public DeclaredMethodContent(BlockAction block)
            {
                _block = block;
            }
        }
    }
}