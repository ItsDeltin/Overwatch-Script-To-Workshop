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

        public override ScopedElement MakeScopedElement(ScopedElementParameters parameters)
        {
            string name = parameters.Alias ?? Name;
            return new ScopedElement(name, ScopedElementData.Create(name, null, variable.CreateInstance()));
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