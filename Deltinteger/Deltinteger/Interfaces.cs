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
        void Call(ParseInfo parseInfo, DocRange callRange) {}
        bool DoesReturnValue => CodeType != null;

        public static string GetLabel(IMethod function, bool includeReturnType)
        {
            // Get the return type.
            string result = "";
            if (includeReturnType)
                result += (function.DoesReturnValue ? function.CodeType?.GetName() ?? "define" : "void") + " ";
            
            result += function.Name + "(";

            // Get the parameters.
            for (int i = 0; i < function.Parameters.Length; i++)
            {
                result += function.Parameters[i].GetLabel();
                if (i < function.Parameters.Length - 1) result += ", ";
            }
            
            result += ")";
            return result;
        }
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
        CodeType CodeType { get; }
        bool Static { get; }
        bool WholeContext { get; }
        CompletionItem GetCompletion();
    }

    public interface IVariable : IScopeable
    {
        bool CanBeIndexed => true;
    }

    public interface ICallable : INamed
    {
        void Call(ParseInfo parseInfo, DocRange callRange);
    }

    public interface IParameterCallable : ILabeled, IAccessable
    {
        CodeParameter[] Parameters { get; }
        string Documentation { get; }
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
        string GetLabel(bool markdown);
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
