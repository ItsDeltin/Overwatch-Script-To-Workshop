using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class StringType : CodeType, IResolveElements
    {
        private readonly ITypeSupplier _typeSupplier;

        public StringType(ITypeSupplier typeSupplier) : base("String")
        {
            _typeSupplier = typeSupplier;
            CanBeExtended = false;
        }

        public void ResolveElements()
        {
            Operations = new ITypeOperation[] {
                new StringAddOperation(_typeSupplier)
            };
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;

    class StringAddOperation : ITypeOperation
    {
        public TypeOperator Operator => TypeOperator.Add;
        public CodeType Right { get; }
        public CodeType ReturnType { get; }

        public StringAddOperation(ITypeSupplier typeSupplier)
        {
            Right = typeSupplier.Any();
            ReturnType = typeSupplier.String();
        }

        public IWorkshopTree Resolve(ActionSet actionSet, IExpression left, IExpression right)
        {
            // If we are adding strings like ["1" + "2" + "3"], we want to use one custom string, like:
            //    "{0}{1}{2}".format("1", "2", "3")
            // rather than:
            //    "{0}{1}".format("{0}{1}".format("1", "2"), "3")
            //
            // To do this, we add every element in a row of string operators to a single list by recursively calling Flatten.
            // example: (("1", "2"), "3") -> ("1", "2", "3")
            var expressions = new List<IExpression>();
            Flatten(expressions, left);
            Flatten(expressions, right);

            // Now convert every element to the workshop.
            var elements = new IWorkshopTree[expressions.Count];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = expressions[i].Parse(actionSet);
            
            // Finally, join all the elements into a string.
            return Elements.StringElement.Join(elements);
        }

        private void Flatten(List<IExpression> list, IExpression expression)
        {
            // If the expression is an operator whose operation is a StringAddOperation, recursively flatten the operator's Left and Right.
            if (expression is OperatorAction operatorAction && operatorAction.Operation is StringAddOperation)
            {
                Flatten(list, operatorAction.Left);
                Flatten(list, operatorAction.Right);
            }
            else // Otherwise, add it to the list.
                list.Add(expression);
        }
    }
}