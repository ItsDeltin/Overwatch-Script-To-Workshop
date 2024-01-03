#nullable enable

using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse.Vanilla;

interface IVanillaExpressionAnalysis
{
    public DocRange DocRange();
    public VanillaType Type();

    public static IVanillaExpressionAnalysis New(VanillaContext context, IVanillaExpression node)
    {
        return new VanillaExpression(node.Range);
    }

    record class VanillaExpression(DocRange Range) : IVanillaExpressionAnalysis
    {
        public DocRange DocRange() => Range;
    }
}

static class VanillaExpressions
{
    public IVanillaExpression Symbol(VanillaContext context, VanillaSymbolExpression syntax)
    {
        // todo: get workshop type from symbol name
    }

    public IVanillaExpression Invoke(VanillaContext context, VanillaInvokeExpression syntax)
    {

    }
}