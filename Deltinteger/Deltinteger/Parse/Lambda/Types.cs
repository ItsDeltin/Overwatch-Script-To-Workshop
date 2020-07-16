using System.Linq;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse.Lambda
{
    /// <summary>The base class for lambda CodeTypes.</summary>
    public abstract class BaseLambda : CodeType
    {
        public bool ReturnsValue { get; protected set; }
        public CodeType ReturnType { get; protected set; }
        public CodeType[] ArgumentTypes { get; }
        protected readonly Scope _objectScope;

        protected BaseLambda(string name) : base(name)
        {
            CanBeDeleted = false;
            CanBeExtended = false;
            Kind = "constant";
            ArgumentTypes = new CodeType[0];
            _objectScope = new Scope("lambda");
            _objectScope.AddNativeMethod(new LambdaInvoke(this));
        }
        protected BaseLambda(string name, CodeType[] argumentTypes) : base(name)
        {
            CanBeDeleted = false;
            CanBeExtended = false;
            Kind = "constant";
            ArgumentTypes = argumentTypes ?? new CodeType[0];
            _objectScope = new Scope("lambda");
            _objectScope.AddNativeMethod(new LambdaInvoke(this));
        }

        public override bool Implements(CodeType type)
        {
            if (type == null || type.GetType() != this.GetType()) return false;

            BaseLambda otherLambda = (BaseLambda)type;

            // If the argument length is not the same, return false.
            if (ArgumentTypes.Length != otherLambda.ArgumentTypes.Length) return false;

            // If the other's return type does not implement this return type, return false.
            if (ReturnType != null && (otherLambda.ReturnType == null || !otherLambda.ReturnType.Implements(ReturnType))) return false;

            // If any of the other's parameters to not implement this respective parameters, return false.
            for (int i = 0; i < ArgumentTypes.Length; i++)
            {
                if ((ArgumentTypes[i] == null) != (otherLambda.ArgumentTypes[i] == null)) return false;
                if (ArgumentTypes[i] != null && !otherLambda.ArgumentTypes[i].Implements(ArgumentTypes[i])) return false;
            }
            
            return true;
        }

        public override Scope GetObjectScope() => _objectScope;
        public override Scope ReturningScope() => null;
        public override bool IsConstant() => true;
        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Constant
        };
        public override string GetName()
        {
            if (ArgumentTypes.Length == 0) return Name;
            else return Name + "<" + string.Join(", ", ArgumentTypes.Select(at => at?.GetName() ?? "define")) + ">";
        }
    }

    public class BlockLambda : BaseLambda
    {
        public BlockLambda() : base("BlockLambda") {
            _objectScope.AddNativeMethod(new LambdaToRule(ArgumentTypes));
        }
        public BlockLambda(params CodeType[] argumentTypes) : base("BlockLambda", argumentTypes) {
            _objectScope.AddNativeMethod(new LambdaToRule(ArgumentTypes));
        }
        protected BlockLambda(string name) : base(name) {}
        protected BlockLambda(string name, CodeType[] argumentTypes) : base(name, argumentTypes) {}
    }

    public class ValueBlockLambda : BlockLambda
    {
        public ValueBlockLambda() : base("ValueLambda")
        {
            ReturnsValue = true;
            _objectScope.AddNativeMethod(new LambdaToRule(ArgumentTypes));
        }
        public ValueBlockLambda(CodeType returnType, params CodeType[] argumentTypes) : base("ValueLambda", argumentTypes)
        {
            ReturnType = returnType;
            _objectScope.AddNativeMethod(new LambdaToRule(ArgumentTypes));
        }
    }

    public class MacroLambda : BaseLambda
    {
        public MacroLambda() : base("MacroLambda")
        {
            ReturnsValue = true;
        }

        public MacroLambda(CodeType returnType, params CodeType[] argumentTypes) : base("MacroLambda", argumentTypes)
        {
            ReturnType = returnType;
        }
    }
}