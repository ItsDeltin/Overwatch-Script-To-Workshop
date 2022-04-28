using DS.Analysis.Scopes;
using DS.Analysis.Variables;
using DS.Analysis.Variables.Builder;

namespace DS.Analysis.Structure.Variables
{
    class DeclaredVariable : AbstractDeclaredElement
    {
        readonly VariableBuilder variableBuilder;
        readonly VariableProvider variable;

        public DeclaredVariable(ContextInfo contextInfo, IVariableContextHandler contextHandler)
        {
            variableBuilder = new VariableBuilder(contextHandler);
            Name = variableBuilder.Name;
            variable = variableBuilder.GetVariable(contextInfo);
        }

        public override void AddToScope(IScopeAppender scopeAppender)
        {
            // todo: Maybe use variable.CreateInstance() directly instead of encasing it in IdentifierInfo
            scopeAppender.AddScopedElement(ScopedElement.CreateVariable(Name, new Expressions.Identifiers.IdentifierInfo(variable.CreateInstance().Type)));
        }

        public override void AddToContent(TypeContentBuilder contentBuilder)
        {
            contentBuilder.AddElement(variable.CreateInstance());
        }

        public override void Dispose()
        {
            variableBuilder.Dispose();
        }
    }
}