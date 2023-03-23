namespace DS.Analysis.Rule;
using DS.Analysis.Statements;
using Deltin.Deltinteger.Compiler.SyntaxTree;

class RuleStatement : Statement
{
    public RuleStatement(ContextInfo context, RuleContext syntax) : base(context)
    {
        AddDisposable(context.StatementFromSyntax(syntax.Statement));
    }
}