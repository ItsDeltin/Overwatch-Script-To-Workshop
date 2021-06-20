using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public interface IUnaryTypeOperation
    {
        UnaryTypeOperator Operator { get; }
        CodeType ReturnType { get; }
        IWorkshopTree Resolve(ActionSet actionSet, IExpression value);
    }

    public class UnaryTypeOperation : IUnaryTypeOperation
    {
        public UnaryTypeOperator Operator { get; }
        public CodeType ReturnType { get; }
        private readonly Func<IWorkshopTree, IWorkshopTree> _resolver;

        public UnaryTypeOperation(UnaryTypeOperator op, CodeType returnType, Func<IWorkshopTree, IWorkshopTree> resolver)
        {
            Operator = op;
            ReturnType = returnType;
            _resolver = resolver;
        }

        public IWorkshopTree Resolve(ActionSet actionSet, IExpression value) => _resolver(value.Parse(actionSet));

        public static UnaryTypeOperator OperatorFromString(string str)
        {
            switch(str)
            {
                case "-": return UnaryTypeOperator.Minus;
                case "!": return UnaryTypeOperator.Not;
                default: throw new NotImplementedException();
            }
        }
    }
}