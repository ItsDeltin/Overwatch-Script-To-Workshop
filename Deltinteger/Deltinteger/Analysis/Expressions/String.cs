namespace DS.Analysis.Expressions;
using DS.Analysis.Types;
using StringSyntax = Deltin.Deltinteger.Compiler.SyntaxTree.StringExpression;

static class StringAnalysis
{
    public static IExpressionHost NewExpression(ContextInfo context, StringSyntax stringSyntax)
    {
        return context.CreateExpressionHost("String Expression", self =>
        {
            // Notice: this will be executed every time the string expression updates.
            self.SetUnlinkedType(StandardType.String.Instance);
        });
    }
}