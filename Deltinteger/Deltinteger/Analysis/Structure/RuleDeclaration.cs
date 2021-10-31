using DS.Analysis.Structure.RuleContentProvider;

namespace DS.Analysis.Structure
{
    abstract class AbstractRuleDeclaraction : AbstractDeclaredElement
    {
    }

    class GenericRuleDeclaration : AbstractRuleDeclaraction
    {
        readonly AbstractRuleContentProvider _ruleContent;

        public GenericRuleDeclaration(AbstractRuleContentProvider ruleContent)
        {
            _ruleContent = ruleContent;
        }

        public override void GetContent(ContextInfo context)
        {
            throw new System.NotImplementedException();
        }
    }
}