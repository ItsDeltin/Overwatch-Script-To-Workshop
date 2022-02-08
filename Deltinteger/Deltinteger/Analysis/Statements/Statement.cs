namespace DS.Analysis.Statements
{
    using Core;
    using Scopes;

    abstract class Statement : PhysicalObject
    {
        protected Statement(ContextInfo context) : base(context)
        {
        }

        /// <summary>Adds a scope source to the current scope.</summary>
        public virtual IScopeSource AddSourceToContext() => null;
    }
}