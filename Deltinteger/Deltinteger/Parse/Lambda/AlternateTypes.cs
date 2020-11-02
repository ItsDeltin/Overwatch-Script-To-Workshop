using System.Linq;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse.Lambda
{
    /// <summary>The base class for lambda CodeTypes.</summary>
    public abstract class BaseLambda : PortableLambdaType
    {
        protected BaseLambda(string name, LambdaKind lambdaKind, CodeType[] argumentTypes) : base(name, lambdaKind, argumentTypes)
        {
            CanBeDeleted = false;
            CanBeExtended = false;
            Kind = "constant";
        }

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = Name,
            Kind = CompletionItemKind.Constant
        };
        public override string GetName()
        {
            // No type args
            if (Parameters.Length == 0 && !ReturnsValue)
                return Name;
            // Return type and parameters
            else if (ReturnsValue)
                return Name + "<" + string.Join(", ", Parameters.Select(p => p.GetName()).Prepend(ReturnType.GetName())) + ">";
            // Parameters
            else
                return Name + "<" + string.Join(", ", Parameters.Select(p => p.GetName())) + ">";
        }
    }

    public class BlockLambda : BaseLambda
    {
        public BlockLambda(params CodeType[] argumentTypes) : base("BlockLambda", LambdaKind.ConstantBlock, argumentTypes) {}
    }

    public class ValueBlockLambda : BaseLambda
    {
        public ValueBlockLambda(CodeType returnType, params CodeType[] argumentTypes) : base("ValueLambda", LambdaKind.ConstantValue, argumentTypes)
        {
            ReturnsValue = true;
            ReturnType = returnType;
        }
    }

    public class MacroLambda : BaseLambda
    {
        public MacroLambda(CodeType returnType, params CodeType[] argumentTypes) : base("MacroLambda", LambdaKind.ConstantMacro, argumentTypes)
        {
            ReturnsValue = true;
            ReturnType = returnType;
        }
    }
}