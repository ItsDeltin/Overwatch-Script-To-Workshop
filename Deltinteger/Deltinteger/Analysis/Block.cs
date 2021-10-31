using DS.Analysis.Statements;

namespace DS.Analysis
{
    class BlockAction
    {
        public Statement[] Statements { get; }

        public BlockAction(Statement[] statements)
        {
            Statements = statements;
        }
    }
}