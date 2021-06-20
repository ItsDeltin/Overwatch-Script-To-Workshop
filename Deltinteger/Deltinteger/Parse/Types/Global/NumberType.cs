using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class NumberType : CodeType, IGetMeta
    {
        readonly ITypeSupplier _supplier;
        readonly Scope _scope = new Scope();

        public NumberType(DeltinScript deltinScript, ITypeSupplier supplier) : base("Number")
        {
            _supplier = supplier;

            deltinScript.StagedInitiation.On(this);
        }

        public void GetMeta()
        {
            // Floor
            _scope.AddNativeMethod(new FuncMethodBuilder() {
                Name = "Floor",
                Documentation = "Rounds the provided number down to the nearest integer.",
                ReturnType = this,
                Action = (actionSet, methodCall) => Element.RoundToInt(actionSet.CurrentObject, Rounding.Down)
            }.GetMethod());
            // Ceil
            _scope.AddNativeMethod(new FuncMethodBuilder() {
                Name = "Ceil",
                Documentation = "Return the ceiling of the provided number.",
                ReturnType = this,
                Action = (actionSet, methodCall) => Element.RoundToInt(actionSet.CurrentObject, Rounding.Up)
            }.GetMethod());
            // Round
            _scope.AddNativeMethod(new FuncMethodBuilder() {
                Name = "Round",
                Documentation = "Returns the provided number rounded to the nearest integer.",
                ReturnType = this,
                Action = (actionSet, methodCall) => Element.RoundToInt(actionSet.CurrentObject, Rounding.Nearest)
            }.GetMethod());
            // Absolute value
            _scope.AddNativeMethod(new FuncMethodBuilder() {
                Name = "Abs",
                Documentation = "Returns the absolute value of the provided number. Also known as the value's distance to 0.",
                ReturnType = this,
                Action = (actionSet, methodCall) => Element.Abs(actionSet.CurrentObject)
            }.GetMethod());

            Operations.AddTypeOperation(new TypeOperation[] {
                new TypeOperation(TypeOperator.Add, this, this), // Number + number
                new TypeOperation(TypeOperator.Subtract, this, this), // Number - number
                new TypeOperation(TypeOperator.Multiply, this, this), // Number * number
                new TypeOperation(TypeOperator.Divide, this, this), // Number / number
                new TypeOperation(TypeOperator.Modulo, this, this), // Number % number
				new TypeOperation(TypeOperator.Pow, this, this),
                new TypeOperation(TypeOperator.Multiply, _supplier.Vector(), _supplier.Vector()), // Number * vector
				new TypeOperation(TypeOperator.LessThan, this, _supplier.Boolean()), // Number < number
                new TypeOperation(TypeOperator.LessThanOrEqual, this, _supplier.Boolean()), // Number <= number
                new TypeOperation(TypeOperator.GreaterThanOrEqual, this, _supplier.Boolean()), // Number >= number
                new TypeOperation(TypeOperator.GreaterThan, this, _supplier.Boolean()), // Number > number
            });
            Operations.AddTypeOperation(AssignmentOperation.GetNumericOperations(this));
        }

        public override Scope GetObjectScope() => _scope;

        protected override bool DoesImplement(CodeType type) => base.DoesImplement(type) || type.Implements(_supplier.Boolean());

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Struct
        };
        public override Scope ReturningScope() => null;
    }
}
