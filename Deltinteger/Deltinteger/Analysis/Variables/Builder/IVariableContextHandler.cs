using System;
using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using DS.Analysis.Expressions;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Variables.Builder
{
    interface IVariableContextHandler : IDisposable
    {
        string GetName();

        VariableContent GetContent(ContextInfo contextInfo);
    }

    class VariableContextHandler : IVariableContextHandler
    {
        readonly VariableDeclaration declaration;
        TypeReference typeReference;
        Expression expression;

        public VariableContextHandler(VariableDeclaration declaration)
        {
            this.declaration = declaration;
        }

        public string GetName() => declaration.Identifier.Text;
        public VariableContent GetContent(ContextInfo contextInfo)
        {
            typeReference = TypeFromContext.TypeReferenceFromContext(contextInfo, declaration.Type);
            if (declaration.InitialValue != null)
                expression = contextInfo.GetExpression(declaration.InitialValue);

            return new VariableContent(typeReference, expression);
        }

        public void Dispose()
        {
            typeReference.Dispose();
            expression?.Dispose();
        }
    }

    struct VariableContent
    {
        public ITypeDirector TypeDirector { get; }
        public Expression Expression { get; }

        public VariableContent(ITypeDirector typeDirector, Expression expression)
        {
            TypeDirector = typeDirector;
            Expression = expression;
        }
    }
}