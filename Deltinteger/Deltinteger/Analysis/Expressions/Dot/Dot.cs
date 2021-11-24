using System;
using System.Linq;
using DS.Analysis.Types;
using DS.Analysis.Types.Standard;
using DS.Analysis.Scopes;
using DS.Analysis.Utility;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace DS.Analysis.Expressions.Dot
{
    class DotExpression : Expression, ITypeDirector
    {
        readonly ContextInfo contextInfo;

        // Operands in a dot tree (x.(y.z)) flattened (x.y.z)
        readonly FlattenSyntax flattenSyntax;

        // Observers watching the right-hand operand's type.
        readonly ObserverCollection<CodeType> typeObservers = new ValueObserverCollection<CodeType>(StandardTypes.Unknown.Instance);

        Expression[] expressions;
        IDisposable[] partTypeSubscriptions;

        public DotExpression(ContextInfo contextInfo, BinaryOperatorExpression binaryOperatorSyntax)
        {
            this.contextInfo = contextInfo;
            flattenSyntax = new FlattenSyntax(binaryOperatorSyntax);

            expressions = new Expression[flattenSyntax.Parts.Count];
            partTypeSubscriptions = new IDisposable[flattenSyntax.Parts.Count];

            Type = this;

            // Initialize expressions
            GetExpression(0, null);
        }

        void GetExpression(int index, Scope scope)
        {
            // 'scope' is null if 'index' is 0

            // Dispose
            partTypeSubscriptions[index]?.Dispose();
            partTypeSubscriptions[index] = null;
            expressions[index]?.Dispose();
            expressions[index] = null;

            ContextInfo partContext = contextInfo;

            // If this is not the first expression, clear tail data and set the source expression.
            if (index != 0) partContext = partContext.ClearTail().SetSourceExpression(expressions[index - 1]).SetScope(scope);
            // If this is not the last expression, clear head data.
            if (index != expressions.Length) partContext = partContext.ClearHead();

            // Get the expression.
            expressions[index] = flattenSyntax.Parts[index].GetExpression(partContext);

            // If this is not the last expression...
            if (index < expressions.Length - 1)
            {
                // ... then subscribe to the expression's type to update the next expression in the list.
                partTypeSubscriptions[index] = expressions[index].Scope.Subscribe(scope => GetExpression(index + 1, scope));
            }
            // This is the last expression.
            else
                UpdateRighthandTypeSubscription();
        }

        void UpdateRighthandTypeSubscription()
        {
            int last = partTypeSubscriptions.Length - 1;

            partTypeSubscriptions[last]?.Dispose(); // Dispose existing if it exists.
            partTypeSubscriptions[last] = expressions[last].Type.Subscribe(typeObservers.Set);
        }

        public override void Dispose()
        {
            base.Dispose();

            for (int i = 0; i < expressions.Length; i++)
            {
                expressions[i].Dispose();
                partTypeSubscriptions[i].Dispose();
            }
            typeObservers.Complete();
        }

        // ITypeDirector
        public IDisposable Subscribe(IObserver<CodeType> observer) => typeObservers.Add(observer);
    }
}