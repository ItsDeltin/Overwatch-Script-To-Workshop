using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Lambda;

namespace Deltin.Deltinteger.Parse
{
    public class CodeParameter : IRestrictedCallHandler, IParameterLike
    {
        public string Name { get; set; }
        public MarkupBuilder Documentation { get; set; }
        public ExpressionOrWorkshopValue DefaultValue { get; set; }
        public List<RestrictedCallType> RestrictedCalls { get; set; } = new List<RestrictedCallType>();
        public ParameterInvokedInfo Invoked { get; set; } = new ParameterInvokedInfo();
        public ParameterAttributes Attributes { get; set; }
        private ICodeTypeSolver _type;

        private CodeParameter(string name)
        {
            Name = name;
        }

        public CodeParameter(string name, ICodeTypeSolver type)
        {
            Name = name;
            _type = type;
        }

        public CodeParameter(string name, ICodeTypeSolver type, ExpressionOrWorkshopValue defaultValue)
        {
            Name = name;
            _type = type;
            DefaultValue = defaultValue;
        }

        public CodeParameter(string name, MarkupBuilder documentation, ICodeTypeSolver type)
        {
            Name = name;
            _type = type;
            Documentation = documentation;
        }

        public CodeParameter(string name, MarkupBuilder documentation, ICodeTypeSolver type, ExpressionOrWorkshopValue defaultValue)
        {
            Name = name;
            _type = type;
            DefaultValue = defaultValue;
            Documentation = documentation;
        }

        public virtual object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            // If the type of the parameter is a lambda, then resolve the expression.
            if (_type is PortableLambdaType lambdaType && lambdaType.LambdaKind == LambdaKind.Constant)
                ConstantExpressionResolver.Resolve(value, expr =>
                {
                    // If the expression is a lambda...
                    if (expr is Lambda.LambdaAction lambda)
                        // ...then if this parameter is invoked, apply the restricted calls and recursion info.
                        Invoked.OnInvoke(() =>
                        {
                            LambdaInvoke.LambdaInvokeApply(parseInfo, lambda, valueRange);
                        });
                    // Otherwise, if the expression resolves to an IBridgeInvocable...
                    else if (LambdaInvoke.ParameterInvocableBridge(value, out IBridgeInvocable invocable))
                        // ...then this lambda parameter is invoked, invoke the resolved invocable. 
                        Invoked.OnInvoke(() => invocable.WasInvoked());
                });
            return null;
        }
        public virtual IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData) => expression.Parse(actionSet);

        public void AddRestrictedCall(RestrictedCall restrictedCall)
        {
            if (!RestrictedCalls.Contains(restrictedCall.CallType))
                RestrictedCalls.Add(restrictedCall.CallType);
        }

        public CodeType GetCodeType(DeltinScript deltinScript) => _type.GetCodeType(deltinScript);

        public string GetLabel(DeltinScript deltinScript, AnonymousLabelInfo labelInfo)
        {
            string result = string.Empty;

            if (Attributes.Ref) result = "ref ";
            else if (Attributes.In) result = "in ";

            result += labelInfo.NameFromSolver(deltinScript, _type) + " " + Name;
            if (DefaultValue != null) result = "[" + result + "]";
            return result;
        }

        override public string ToString() => Name;

        public MarkupBuilder GetSignatureDocumentation()
        {
            if (Documentation == null)
                return null;
            return new MarkupBuilder().Code(Name).Add(":").NewLine().Add(Documentation).NewSection();
        }

        public static string GetLabels(DeltinScript deltinScript, AnonymousLabelInfo labelInfo, CodeParameter[] parameters)
        {
            return "(" + string.Join(", ", parameters.Select(p => p.GetLabel(deltinScript, labelInfo))) + ")";
        }
    }

    public struct ParameterAttributes
    {
        public bool Ref { get; }
        public bool In { get; }

        public ParameterAttributes(bool isRef, bool in_)
        {
            Ref = isRef;
            In = in_;
        }
    }

    public class ParameterInvokedInfo : IBridgeInvocable
    {
        public bool Invoked { get; private set; }
        private List<Action> _onInvoke = new List<Action>();

        public void WasInvoked()
        {
            if (Invoked) return;
            Invoked = true;

            foreach (Action onInvoke in _onInvoke)
                onInvoke.Invoke();
        }

        public void OnInvoke(Action onInvoke)
        {
            if (Invoked) onInvoke.Invoke();
            else _onInvoke.Add(onInvoke);
        }
    }

    public class ExpressionOrWorkshopValue : IExpression
    {
        public IExpression Expression { get; }
        public IWorkshopTree WorkshopValue { get; }

        public ExpressionOrWorkshopValue(IExpression expression)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }
        public ExpressionOrWorkshopValue(IWorkshopTree workshopValue)
        {
            WorkshopValue = workshopValue ?? throw new ArgumentNullException(nameof(workshopValue));
        }
        public ExpressionOrWorkshopValue() { }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            if (Expression != null) return Expression.Parse(actionSet);
            return WorkshopValue;
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;

        public static implicit operator ExpressionOrWorkshopValue(Element value) => new ExpressionOrWorkshopValue(value);
    }
}