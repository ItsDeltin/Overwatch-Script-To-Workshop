using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Structure.RuleContentProvider
{
    class DeclaredRuleContentProvider : AbstractRuleContentProvider
    {
        readonly RuleContext _ruleContext;

        public DeclaredRuleContentProvider(RuleContext ruleContext)
        {
            _ruleContext = ruleContext;
        }
    }
}