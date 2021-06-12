using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public interface IVariableInstance : IScopeable
    {
        IVariable Provider { get; }
        MarkupBuilder Documentation { get; }
        IVariableInstanceAttributes Attributes { get; }

        IGettableAssigner GetAssigner(GetVariablesAssigner getAssigner = default(GetVariablesAssigner));
        IWorkshopTree ToWorkshop(ActionSet actionSet) => actionSet.IndexAssigner.Get(Provider).GetVariable();
        ICallVariable GetExpression(ParseInfo parseInfo, DocRange callRange, IExpression[] index, CodeType[] typeArgs) => new CallVariableAction(parseInfo, this, index);
        void Call(ParseInfo parseInfo, DocRange callRange) => Call(this, parseInfo, callRange);

        MarkupBuilder GetLabel(DeltinScript deltinScript, LabelInfo labelInfo) => labelInfo.MakeVariableLabel(CodeType.GetCodeType(deltinScript), Name);

        string GetLabel(DeltinScript deltinScript) => CodeType.GetCodeType(deltinScript) + " " + Name;

        CompletionItem IScopeable.GetCompletion(DeltinScript deltinScript) => new CompletionItem() {
            Label = Name,
            Documentation = Documentation,
            Kind = CompletionItemKind.Variable,
            Detail = CodeType.GetCodeType(deltinScript).GetName() + " " + Name
        };
        
        static void Call(IVariableInstance variable, ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.Elements.AddDeclarationCall(variable.Provider, new DeclarationCall(callRange, false));
            parseInfo.Script.AddHover(callRange, variable.GetLabel(parseInfo.TranslateInfo, LabelInfo.Hover));
        }
    }

    public struct GetVariablesAssigner
    {
        public readonly InstanceAnonymousTypeLinker TypeLinker;
        public readonly bool IsGlobal;
        public readonly string Tag;

        public GetVariablesAssigner(ActionSet actionSet)
        {
            TypeLinker = actionSet?.ThisTypeLinker;
            IsGlobal = actionSet?.IsGlobal ?? true;
            Tag = null;
        }

        public GetVariablesAssigner(ActionSet actionSet, string tag)
        {
            TypeLinker = actionSet?.ThisTypeLinker;
            IsGlobal = actionSet?.IsGlobal ?? true;
            Tag = tag;
        }

        public GetVariablesAssigner(InstanceAnonymousTypeLinker typeLinker)
        {
            TypeLinker = typeLinker;
            IsGlobal = true;
            Tag = null;
        }
    }

    public interface IVariableInstanceAttributes
    {
        bool UseDefaultVariableAssigner { get; }
        bool CanBeSet { get; }
        StoreType StoreType { get; }
        bool CanBeIndexed { get; }
    }

    class VariableInstanceAttributes : IVariableInstanceAttributes
    {
        public bool UseDefaultVariableAssigner { get; set; } = true;
        public bool CanBeSet { get; set; } = true;
        public StoreType StoreType { get; set; } = StoreType.None;
        public bool CanBeIndexed { get; set; } = true;
    }
}