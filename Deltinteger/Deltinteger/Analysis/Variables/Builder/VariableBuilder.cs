using System;

namespace DS.Analysis.Variables.Builder
{
    using Types.Semantics;
    using Core;

    class VariableBuilder : IDisposable
    {
        public string Name { get; }
        protected IVariableContextHandler ContextHandler { get; }
        DependencyNode node;

        public VariableBuilder(IVariableContextHandler contextHandler)
        {
            ContextHandler = contextHandler;
            Name = contextHandler.GetName();
        }

        public VariableProvider GetVariable(ContextInfo contextInfo)
        {
            VariableContent content = ContextHandler.GetContent(contextInfo);
            node = contextInfo.Analysis.OnlyNode(() =>
            {
                // Ensure that the expression type is assignable to the variable's type.
                node.DisposeOnUpdate(TypeValidation.IsAssignableTo(
                    contextInfo,
                    token: contextInfo.File.Diagnostics.CreateToken(content.ExpressionRange),
                    scopedElements: contextInfo.Scope.Elements,
                    assignToType: content.TypeDirector.Type,
                    valueType: content.Expression.Type));
            });
            node.DependOn(content.Expression);
            node.DependOn(content.TypeDirector);

            return new VariableProvider(Name, content.TypeDirector);
        }

        public void Dispose()
        {
            ContextHandler.Dispose();
            node?.Dispose();
        }
    }
}