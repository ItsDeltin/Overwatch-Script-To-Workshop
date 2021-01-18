using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.WorkshopWiki;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using SignatureInformation = OmniSharp.Extensions.LanguageServer.Protocol.Models.SignatureInformation;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger
{
    public interface IMethod : IScopeable, IParameterCallable
    {
        MethodAttributes Attributes { get; }
        IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall);
        bool DoesReturnValue => CodeType != null;
        CompletionItem IScopeable.GetCompletion(DeltinScript deltinScript) => GetFunctionCompletion(deltinScript, this);
        MarkupBuilder ILabeled.GetLabel(DeltinScript deltinScript, LabelInfo labelInfo) => DefaultLabel(deltinScript, labelInfo, this);

        public static MarkupBuilder DefaultLabel(DeltinScript deltinScript, LabelInfo labelInfo, IMethod function)
        {
            MarkupBuilder markup = new MarkupBuilder().StartCodeLine();
            
            // Add return type.
            if (labelInfo.IncludeReturnType)
                markup.Add(ICodeTypeSolver.GetNameOrVoid(deltinScript, function.CodeType)).Add(" ");
            
            // Add function name and parameters.
            markup.Add(function.Name + CodeParameter.GetLabels(deltinScript, function.Parameters))
                .EndCodeLine();
            
            // Add documentation.
            if (labelInfo.IncludeDocumentation && function.Documentation != null)
                markup.NewSection().Add(function.Documentation);
            
            return markup;
        }

        public static CompletionItem GetFunctionCompletion(DeltinScript deltinScript, IMethod function) => new CompletionItem()
        {
            Label = function.Name,
            Kind = CompletionItemKind.Method,
            Detail = ICodeTypeSolver.GetNameOrVoid(deltinScript, function.CodeType) + " " + function.Name + CodeParameter.GetLabels(deltinScript, function.Parameters),
            Documentation = Extras.GetMarkupContent(function.Documentation)
        };
    }

    public interface ISkip
    {
        int SkipParameterIndex();
    }

    public interface INamed
    {
        string Name { get; }
    }

    public interface IScopeable : INamed, IAccessable
    {
        ICodeTypeSolver CodeType { get; }
        bool Static { get; }
        bool WholeContext { get; }
        CompletionItem GetCompletion(DeltinScript deltinScript);
    }

    public interface IVariable : IScopeable, ILabeled
    {
        bool CanBeIndexed => true;
        MarkupBuilder Documentation { get; }

        MarkupBuilder ILabeled.GetLabel(DeltinScript deltinScript, LabelInfo labelInfo)
        {
            var builder = new MarkupBuilder().StartCodeLine();

            if (labelInfo.IncludeReturnType)
                builder.Add(ICodeTypeSolver.GetNameOrVoid(deltinScript, CodeType)).Add(" ");
            
            builder.Add(Name).EndCodeLine();
            return builder;
        }

        CompletionItem IScopeable.GetCompletion(DeltinScript deltinScript) => new CompletionItem() {
            Label = Name,
            Documentation = Documentation
        };
    }

    public interface ICallable : INamed
    {
        void Call(ParseInfo parseInfo, DocRange callRange);
    }

    public interface IParameterCallable : ILabeled, IAccessable
    {
        CodeParameter[] Parameters { get; }
        MarkupBuilder Documentation { get; }
        object Call(ParseInfo parseInfo, DocRange callRange) => null;
        bool RestrictedValuesAreFatal => true;
    }

    public interface IAccessable
    {
        Location DefinedAt { get; }
        AccessLevel AccessLevel { get; }
    }

    public interface IGettable
    {
        IWorkshopTree GetVariable(Element eventPlayer = null);
    }

    public interface ILabeled
    {
        MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo);
    }

    public interface IApplyBlock : IBlockListener, ILabeled
    {
        void SetupParameters();
        void SetupBlock();
        CallInfo CallInfo { get; }
    }

    public interface IBlockListener
    {
        void OnBlockApply(IOnBlockApplied onBlockApplied);
    }

    public interface IOnBlockApplied
    {
        void Applied();
    }
}
