using Deltin.Deltinteger.Elements;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class AnyType : CodeType, IResolveElements
    {
        private readonly DeltinScript _deltinScript;

        public AnyType(DeltinScript deltinScript) : this("Any", deltinScript) {}

        public AnyType(string name, DeltinScript deltinScript) : base(name)
        {
            CanBeDeleted = true;
            CanBeExtended = false;
            _deltinScript = deltinScript;
        }

        public void ResolveElements()
        {
            Operations.AddTypeOperation(new TypeOperation[] {
                new TypeOperation(_deltinScript.Types, TypeOperator.Equal, this),
                new TypeOperation(_deltinScript.Types, TypeOperator.NotEqual, this),
                new TypeOperation(_deltinScript.Types, TypeOperator.GreaterThan, this),
                new TypeOperation(_deltinScript.Types, TypeOperator.GreaterThanOrEqual, this),
                new TypeOperation(_deltinScript.Types, TypeOperator.LessThan, this),
                new TypeOperation(_deltinScript.Types, TypeOperator.LessThanOrEqual, this),
                new TypeOperation(_deltinScript.Types, TypeOperator.And, this),
                new TypeOperation(_deltinScript.Types, TypeOperator.Or, this),
                
                new TypeOperation(TypeOperator.Add, this, this),
                new TypeOperation(TypeOperator.Divide, this, this),
                new TypeOperation(TypeOperator.Modulo, this, this),
                new TypeOperation(TypeOperator.Multiply, this, this),
                new TypeOperation(TypeOperator.Pow, this, this),
                new TypeOperation(TypeOperator.Subtract, this, this)
            });
            Operations.AddTypeOperation(AssignmentOperation.GetNumericOperations(this));
        }

        public override bool Implements(CodeType type) => !type.IsConstant();
        public override bool Is(CodeType type) => !type.IsConstant();
        public override CompletionItem GetCompletion() => GetTypeCompletion(this);
        public override Scope GetObjectScope() => _deltinScript.PlayerVariableScope;
        public override Scope ReturningScope() => null;
    }
}