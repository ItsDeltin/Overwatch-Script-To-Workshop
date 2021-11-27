using System;

namespace DS.Analysis.Variables.Builder
{
    using Types.Semantics;

    class VariableBuilder : IDisposable
    {
        public string Name { get; }
        protected IVariableContextHandler ContextHandler { get; }
        IDisposable typeAssignmentValidation;

        public VariableBuilder(IVariableContextHandler contextHandler)
        {
            ContextHandler = contextHandler;
            Name = contextHandler.GetName();
        }

        public VariableProvider GetVariable(ContextInfo contextInfo)
        {
            VariableContent content = ContextHandler.GetContent(contextInfo);

            // Ensure that the expression type is assignable to the variable's type.
            if (content.Expression != null)
                typeAssignmentValidation = TypeValidation.IsAssignableTo(
                    contextInfo,
                    token: contextInfo.File.Diagnostics.CreateToken(content.ExpressionRange),
                    assignToType: content.TypeDirector,
                    valueType: content.Expression.Type);

            return new VariableProvider(Name, content.TypeDirector);
        }

        public void Dispose()
        {
            ContextHandler.Dispose();
            typeAssignmentValidation?.Dispose();
        }
    }
}