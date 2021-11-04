using System;
using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using DS.Analysis.Structure;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Structure.Methods
{
    interface IMethodContentProvider : IDisposable
    {
        string GetName();
        void GetStructure(StructureContext structure);
        void GetMeta(ContextInfo context);
        void GetContent();
        Parameter[] GetParameters(ContextInfo metaContext);
        TypeReference GetType(ContextInfo metaContext);
    }

    class MethodContentProvider : IMethodContentProvider
    {
        readonly FunctionContext syntax;
        BlockAction blockAction;

        public MethodContentProvider(FunctionContext syntax) => this.syntax = syntax;

        public string GetName() => syntax.Identifier.Text;

        public void GetStructure(StructureContext structure) => blockAction = structure.Block(syntax.Block);
        public void GetMeta(ContextInfo context) => blockAction.GetMeta(context);
        public void GetContent() => blockAction.GetContent();

        public Parameter[] GetParameters(ContextInfo context)
        {
            return new Parameter[0];
        }

        public TypeReference GetType(ContextInfo metaContext) => TypeFromContext.TypeReferenceFromContext(metaContext.Scope, syntax.Type);


        public void Dispose() => blockAction.Dispose();
    }
}