using DS.Analysis.Scopes;
using DS.Analysis.Variables;
using DS.Analysis.Variables.Builder;

namespace DS.Analysis.Structure.Variables
{
    class VariableDeclaration : AbstractDeclaredElement
    {
        readonly VariableBuilder variableBuilder;
        VariableProvider variable;

        public VariableDeclaration(IVariableContextHandler contextHandler)
        {
            variableBuilder = new VariableBuilder(contextHandler);
        }

        public override void GetMeta(ContextInfo metaContext)
        {
            variable = variableBuilder.GetVariable(metaContext);
        }

        public override void GetContent(ContextInfo context)
        {
            // todo
        }
    }
}