using System;

namespace DS.Analysis.Expressions
{
    using Core;
    using Types.Semantics;
    using Scopes;
    using Methods;

    /// <summary>
    /// Represents an expression.
    /// </summary>
    interface IExpressionHost : IDependable, IDisposable
    {
        /// <summary>The type of the expression.</summary>
        PhysicalType Type { get; }

        /// <summary>The scope of the expression. Will usually be the same as Type.Content.Scope.</summary>
        IScopeSource ScopeSource { get; }

        /// <summary>The method group that the expression points to. May be null.</summary>
        MethodGroup MethodGroup { get; }

        /// <summary>The variable that the expression points to. May be null.</summary>
        VariableExpressionData Variable { get; }
    }
}