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
        MethodSetup Setup(ContextInfo context);
    }

    struct MethodSetup
    {
        public readonly Parameter[] Parameters;
        public readonly IDisposableTypeDirector ReturnType;

        public MethodSetup(Parameter[] parameters, IDisposableTypeDirector returnType)
        {
            Parameters = parameters;
            ReturnType = returnType;
        }
    }

    class MethodContentProvider : IMethodContentProvider
    {
        readonly FunctionContext syntax;
        IDisposableTypeDirector returnType;
        BlockAction blockAction;

        public MethodContentProvider(FunctionContext syntax) => this.syntax = syntax;

        public string GetName() => syntax.Identifier.Text;

        public MethodSetup Setup(ContextInfo contextInfo)
        {
            returnType = TypeFromContext.TypeReferenceFromSyntax(contextInfo, syntax.Type);

            // Setup the block
            blockAction = contextInfo.Block(syntax.Block);

            return new MethodSetup(null, returnType);
        }


        public void Dispose()
        {
            returnType.Dispose();
            blockAction.Dispose();
        }
    }
}