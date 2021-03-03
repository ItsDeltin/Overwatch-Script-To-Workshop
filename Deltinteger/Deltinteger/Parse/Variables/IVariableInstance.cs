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

        IGettableAssigner GetAssigner(ActionSet actionSet);
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
            parseInfo.Script.AddHover(callRange, variable.GetLabel(parseInfo.TranslateInfo, LabelInfo.Hover).ToString());
        }
    }

    public interface IVariableInstanceAttributes
    {
        bool UseDefaultVariableAssigner { get; }
        bool CanBeSet { get; }
        StoreType StoreType { get; }
    }

    class VariableInstanceAttributes : IVariableInstanceAttributes
    {
        public bool UseDefaultVariableAssigner { get; set; } = true;
        public bool CanBeSet { get; set; } = true;
        public StoreType StoreType { get; set; } = StoreType.None;
    }
}