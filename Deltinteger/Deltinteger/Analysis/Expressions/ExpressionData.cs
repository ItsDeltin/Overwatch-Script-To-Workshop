using System;
using DS.Analysis.Types;
using DS.Analysis.Scopes;
using DS.Analysis.Methods;

namespace DS.Analysis.Expressions
{
    /// <summary>
    /// The components of an expression in it's current state. This can be achieved by subscribing to an Expression.
    /// </summary>
    class ExpressionData
    {
        /// <summary>The type of the expression. Will never be null.</summary>
        public CodeType Type { get; }

        /// <summary>The scope of the expression. This is usually the same as <c>Type.Content.Scope</c>.
        /// This is used for expressions that do not have a type but do have a scope, such as 'base'.
        /// Will never be null.</summary>
        public Scope Scope { get; }

        /// <summary>If the expression is a variable, this will contain that variable data. May be null.</summary>
        public VariableExpressionData Variable { get; }

        /// <summary>The method group that the expression is pointing to. May be null.</summary>
        public MethodGroup MethodGroup { get; }


        /// <summary>Creates an ExpressionData from a CodeType. This is adequate for most expression types.</summary>
        /// <param name="type">The type of the expression.</param>
        public ExpressionData(CodeType type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Scope = new Scope(type.Content.ScopeSource);
        }

        /// <summary>Creates an ExpressionData.</summary>
        /// <param name="type">The type of the expression</param>
        /// <param name="scope">The scope of the expression.</param>
        /// <param name="variable">The variable data the expression is pointing to.</param>
        /// <param name="methodGroup">The method group the expression is pointing to.</param>
        public ExpressionData(CodeType type, Scope scope, VariableExpressionData variable, MethodGroup methodGroup)
        {
            Type = type;
            Scope = scope;
            Variable = variable;
            MethodGroup = methodGroup;
        }
    }

    /// <summary>
    /// Contains the same fields as <c>ExpressionData</c>. When any of the fields is changed, a new <c>ExpressionData</c> is
    /// automatically pushed to the provided action.
    /// </summary>
    class AutoPushExpressionData
    {
        readonly Action<ExpressionData> pushAction;

        public AutoPushExpressionData(Action<ExpressionData> pushAction)
        {
            this.pushAction = pushAction;
        }

        CodeType type;
        Scope scope;
        VariableExpressionData variable;
        MethodGroup methodGroup;

        /// <summary>The type of the expression. Will never be null.</summary>
        public CodeType Type
        {
            get => type;
            set
            {
                type = value;
                TryPush();
            }
        }

        /// <summary>The scope of the expression. This is usually the same as <c>Type.Content.Scope</c>.
        /// This is used for expressions that do not have a type but do have a scope, such as 'base'.
        /// Will never be null.</summary>
        public Scope Scope
        {
            get => scope;
            set
            {
                scope = value;
                TryPush();
            }
        }

        /// <summary>If the expression is a variable, this will contain that variable data. May be null.</summary>
        public VariableExpressionData Variable
        {
            get => variable;
            set
            {
                variable = value;
                TryPush();
            }
        }

        /// <summary>The method group that the expression is pointing to. May be null.</summary>
        public MethodGroup MethodGroup
        {
            get => methodGroup;
            set
            {
                methodGroup = value;
                TryPush();
            }
        }

        public void SetType(CodeType type) => Type = type;
        public void SetScope(Scope scope) => Scope = scope;

        /// <summary>
        /// Determines if a new ExpressionData will be pushed to the action if a value is changed.
        /// </summary>
        public bool AutoPush { get; set; } = true;

        void TryPush()
        {
            if (AutoPush)
                Push();
        }

        /// <summary>
        /// Manually pushes a new ExpressionData.
        /// </summary>
        public void Push() => pushAction(new ExpressionData(type, scope, variable, methodGroup));
    }
}