using Deltin.Deltinteger.Elements;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class DynamicType : CodeType
    {
        private readonly DeltinScript _deltinScript;

        public DynamicType(DeltinScript deltinScript) : base("dynamic")
        {
            CanBeDeleted = true;
            CanBeExtended = false;
            _deltinScript = deltinScript;

            Operations = new TypeOperation[] {
                new TypeOperation(TypeOperator.Equal, this),
                new TypeOperation(TypeOperator.NotEqual, this),
                new TypeOperation(TypeOperator.GreaterThan, this),
                new TypeOperation(TypeOperator.GreaterThanOrEqual, this),
                new TypeOperation(TypeOperator.LessThan, this),
                new TypeOperation(TypeOperator.LessThanOrEqual, this),
                new TypeOperation(TypeOperator.And, this),
                new TypeOperation(TypeOperator.Or, this),
                
                new TypeOperation(TypeOperator.Add, this, this),
                new TypeOperation(TypeOperator.Divide, this, this),
                new TypeOperation(TypeOperator.Modulo, this, this),
                new TypeOperation(TypeOperator.Multiply, this, this),
                new TypeOperation(TypeOperator.Pow, this, this),
                new TypeOperation(TypeOperator.Subtract, this, this)
            };
        }

        public override bool Implements(CodeType type) => !type.IsConstant();
        public override bool Is(CodeType type) => !type.IsConstant();
        public override CompletionItem GetCompletion() => null;
        public override Scope ReturningScope() => _deltinScript.PlayerVariableScope;
    }
}