namespace DS.Analysis.Statements
{
    class BlockStatement : Statement
    {
        readonly BlockAction block;

        public BlockStatement(ContextInfo context, BlockAction block) : base(context)
        {
            AddDisposable(this.block = block);
        }
    }
}