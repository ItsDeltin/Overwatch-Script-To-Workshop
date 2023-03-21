namespace DS.Analysis.Utility;
using DS.Analysis.Expressions;
using DS.Analysis.Types;
using DS.Analysis.Types.Semantics;

class ExpressionHelperManager
{
    public ExpressionHelper Helper { get; }
    public AutoExpressionHost? Host { get; private set; }

    public ExpressionHelperManager() => Helper = new ExpressionHelper(this);

    public void Init(AutoExpressionHost host) => this.Host = host;
}

struct ExpressionHelper
{
    readonly ExpressionHelperManager manager;
    public ExpressionHelper(ExpressionHelperManager manager) => this.manager = manager;
    public void SetUnlinkedType(CodeType type) => SetType(PhysicalType.Unlinked(type));
    public void SetType(PhysicalType physicalType) => manager.Host!.Type = physicalType;
}