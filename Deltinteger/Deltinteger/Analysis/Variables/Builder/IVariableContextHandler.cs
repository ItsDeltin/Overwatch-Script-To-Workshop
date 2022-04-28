using System;
using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;
using DS.Analysis.Expressions;
using Deltin.Deltinteger.Compiler;
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
        IDisposableTypeDirector typeReference;
        IExpressionHost expression;

        public VariableContextHandler(VariableDeclaration declaration)
        {
            this.declaration = declaration;
        }

        public string GetName() => declaration.Identifier.Text;
        public VariableContent GetContent(ContextInfo contextInfo)
        {
            typeReference = TypeFromContext.TypeReferenceFromSyntax(contextInfo, declaration.Type);
            if (declaration.InitialValue != null)
                expression = contextInfo.GetExpression(declaration.InitialValue);

            return new VariableContent(typeReference, expression, declaration.InitialValue?.Range);
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
        public IExpressionHost Expression { get; }
        public DocRange ExpressionRange { get; }

        public VariableContent(ITypeDirector typeDirector, IExpressionHost expression, DocRange expressionRange)
        {
            TypeDirector = typeDirector;
            Expression = expression;
            ExpressionRange = expressionRange;
        }
    }
}