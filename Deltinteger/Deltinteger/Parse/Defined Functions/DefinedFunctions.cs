using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.WorkshopWiki;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Parse
{
    public abstract class DefinedFunction : IMethod, ICallable, IApplyBlock
    {
        public string Name { get; }
        public CodeType ReturnType { get; protected set; }
        public CodeParameter[] Parameters { get; private set; }
        public AccessLevel AccessLevel { get; protected set; }
        public Location DefinedAt { get; }
        public bool WholeContext { get; } = true;
        public StringOrMarkupContent Documentation { get; } = null;

        protected ParseInfo parseInfo { get; }
        protected Scope methodScope { get; }
        protected Var[] ParameterVars { get; private set; }

        public CallInfo CallInfo { get; }

        public DefinedFunction(ParseInfo parseInfo, Scope scope, string name, Location definedAt)
        {
            Name = name;
            DefinedAt = definedAt;
            this.parseInfo = parseInfo;
            methodScope = scope.Child();
            CallInfo = new CallInfo(this, parseInfo.Script);

            parseInfo.TranslateInfo.AddSymbolLink(this, definedAt);
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.Function, DefinedAt.range));
        }

        public abstract void SetupBlock();

        protected void SetupParameters(DeltinScriptParser.SetParametersContext context, VariableDefineType defineType = VariableDefineType.Parameter)
        {
            var parameterInfo = CodeParameter.GetParameters(parseInfo, methodScope, context, defineType);
            Parameters = parameterInfo.Parameters;
            ParameterVars = parameterInfo.Variables;
        }

        public void Call(ScriptFile script, DocRange callRange)
        {
            script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.TranslateInfo.AddSymbolLink(this, new Location(script.Uri, callRange));
        }

        public virtual bool DoesReturnValue() => true;

        public string GetLabel(bool markdown) => HoverHandler.GetLabel(ReturnType, Name, Parameters, markdown, null);

        public abstract IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData);

        public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = Name,
                Kind = CompletionItemKind.Method
            };
        }
    }
}