namespace DS.Analysis.Statements
{
    class BlockStatement : Statement
    {
        readonly BlockAction block;

        public BlockStatement(BlockAction block)
        {
            AddDisposable(this.block = block);
        }
    }
}