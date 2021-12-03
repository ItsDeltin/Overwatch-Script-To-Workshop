using DS.Analysis.Structure;

namespace DS.Analysis.Statements
{
    class DeclarationStatement : Statement
    {
        readonly AbstractDeclaredElement declaredElement;

        public DeclarationStatement(ContextInfo context, AbstractDeclaredElement declaredElement)
        {
            AddDisposable(this.declaredElement = declaredElement);
            declaredElement.AddToScope(context.ScopeAppender);
        }
    }
}