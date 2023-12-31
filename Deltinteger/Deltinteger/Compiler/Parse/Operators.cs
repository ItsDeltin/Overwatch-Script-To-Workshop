namespace Deltin.Deltinteger.Compiler.Parse
{
    public class DefaultRhsHandler : IOperatorRhsHandler
    {
        public void Get(OperatorInfo op, Parser parser) => parser.GetExpressionWithArray();
    }

    public class DotRhsHandler : IOperatorRhsHandler
    {
        public void Get(OperatorInfo op, Parser parser)
        {
            parser.Operands.Push(parser.Identifier());
            parser.GetArrayAndInvokes();

            if (op.Operator == CompilerOperator.Squiggle)
                parser.PopAllOperators();
        }
    }
}