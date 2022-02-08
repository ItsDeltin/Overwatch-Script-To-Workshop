namespace DS.Analysis.Expressions
{
    using Core;
    using Types.Semantics;
    using Scopes;
    using Methods;

    interface IExpression : IDependable
    {
        PhysicalType Type { get; }
        Scope Scope { get; }
        MethodGroup MethodGroup { get; }
        VariableExpressionData Variable { get; }
    }
}