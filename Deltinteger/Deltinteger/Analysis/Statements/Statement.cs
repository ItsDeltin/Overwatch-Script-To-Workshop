namespace DS.Analysis.Statements
{
    using Core;
    using Scopes;

    abstract class Statement : PhysicalObject, IDisposableStatement
    {
        protected Statement(ContextInfo context) : base(context)
        {
        }

        /// <summary>Adds a scope source to the current scope.</summary>
        public virtual StatementSource? AddSourceToContext() => null;
    }

    readonly record struct StatementSource(IScopeSource ScopeSource, ISource Debug);
}