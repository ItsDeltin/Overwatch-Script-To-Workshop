using System;
using System.Reflection.Metadata;
using Deltin.Deltinteger.Elements;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class TypeOperation
    {
        public static readonly Func<IGettable, IGettable, ActionSet, IWorkshopTree> Add = (l,r,a) => Element.Part<V_Add>(l.GetVariable(), r.GetVariable());
        public static readonly Func<IGettable, IGettable, ActionSet, IWorkshopTree> Subtract = (l,r,a) => Element.Part<V_Subtract>(l.GetVariable(), r.GetVariable());
        public static readonly Func<IGettable, IGettable, ActionSet, IWorkshopTree> Multiply = (l,r,a) => Element.Part<V_Multiply>(l.GetVariable(), r.GetVariable());
        public static readonly Func<IGettable, IGettable, ActionSet, IWorkshopTree> Divide = (l,r,a) => Element.Part<V_Divide>(l.GetVariable(), r.GetVariable());
        public static readonly Func<IGettable, IGettable, ActionSet, IWorkshopTree> Modulo = (l,r,a) => Element.Part<V_Modulo>(l.GetVariable(), r.GetVariable());

        public TypeOperator Operator { get; }
        /// <summary>The righthand of the operator. May be null if there is no right operator.</summary>
        public CodeType Right { get; }
        /// <summary>The return type of the operation.</summary>
        public CodeType ReturnType { get; }
        private readonly Func<IGettable, IGettable, ActionSet, IWorkshopTree> Resolver;

        private Scope ObjectScope;

        public TypeOperation(TypeOperator op, CodeType right, CodeType returnType, Scope objectScope, Func<IGettable, IGettable, ActionSet, IWorkshopTree> resolver)
        {
            Operator = op;
            Right = right ?? throw new ArgumentNullException(nameof(right));
            ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
            Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            ObjectScope = objectScope;
        }

        public IWorkshopTree Resolve(IWorkshopTree left, IWorkshopTree right, ActionSet actionSet)
        {
            var leftVar = new OperatorVar("left");
            var rightVar = new OperatorVar("right");


            var contained = actionSet.New(actionSet.IndexAssigner.CreateContained());
            var leftTree = contained.IndexAssigner.Add(leftVar, left);
            var rightTree = contained.IndexAssigner.Add(rightVar, right);

            if(ObjectScope != null)
            {
                
                ObjectScope.AddNativeVariable(leftVar);
                ObjectScope.AddNativeVariable(rightVar);
            }

            return Resolver.Invoke(leftTree, rightTree, contained);
        }

        public static TypeOperator TypeOperatorFromString(string str)
        {
            switch (str)
            {
                case "+": return TypeOperator.Add;
                case "-": return TypeOperator.Subtract;
                case "*": return TypeOperator.Multiply;
                case "/": return TypeOperator.Divide;
                case "^": return TypeOperator.Pow;
                case "%": return TypeOperator.Modulo;
                case "<": return TypeOperator.LessThan;
                case "<=": return TypeOperator.LessThanOrEqual;
                case "==": return TypeOperator.Equal;
                case ">=": return TypeOperator.GreaterThanOrEqual;
                case ">": return TypeOperator.GreaterThan;
                case "!=": return TypeOperator.NotEqual;
                case "&&": return TypeOperator.And;
                case "||": return TypeOperator.Or;
                case "[]": return TypeOperator.ArrOf;
                default: throw new NotImplementedException();
            }
        }
    }

    public enum TypeOperator
    {
        ///<summary>a ^ b</summary>
        Pow,
        ///<summary>a * b</summary>
        Multiply,
        ///<summary>a / b</summary>
        Divide,
        ///<summary>a % b</summary>
        Modulo,
        ///<summary>a + b</summary>
        Add,
        ///<summary>a - b</summary>
        Subtract,
        ///<summary>a < b</summary>
        LessThan,
        ///<summary>a <= b</summary>
        LessThanOrEqual,
        ///<summary>a == b</summary>
        Equal,
        ///<summary>a >= b</summary>
        GreaterThanOrEqual,
        ///<summary>a > b</summary>
        GreaterThan,
        ///<summary>a != b</summary>
        NotEqual,
        ///<summary>a (and) b</summary>
        And,
        ///<summary>a || b</summary>
        Or,
        ArrOf
    }


    class OperatorVar : InternalVar
    {
        public OperatorVar(string name, CompletionItemKind completionItemKind = CompletionItemKind.Variable) : base(name, completionItemKind)
        {}

        public override IWorkshopTree Parse(ActionSet a) {
            IGettable got;
            a.IndexAssigner.TryGet(this, out got);
            return got.GetVariable();
        }

        public CompletionItem GetCompletion()
        {
            throw new NotImplementedException();
        }
    }
}