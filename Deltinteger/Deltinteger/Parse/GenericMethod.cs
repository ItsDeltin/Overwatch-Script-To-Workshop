using System;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Parse
{
    public class FuncMethod : IMethod
    {
        public string Name { get; }
        public CodeParameter[] Parameters { get; set; }
        public CodeType ReturnType { get; set; }
        public MethodAttributes Attributes { get; set; } = new MethodAttributes();
        public bool Static { get; set; }
        public bool WholeContext { get; set; } = true;
        public string Documentation { get; set; }
        public LanguageServer.Location DefinedAt => null;
        public AccessLevel AccessLevel => AccessLevel.Public;

        private Func<ActionSet, MethodCall, IWorkshopTree> Action { get; }

        public FuncMethod(string name, Func<ActionSet, MethodCall, IWorkshopTree> action)
        {
            Name = name;
            Action = action;
        }

        public FuncMethod(string name, CodeParameter[] parameters, Func<ActionSet, MethodCall, IWorkshopTree> action)
        {
            Name = name;
            Parameters = parameters;
            Action = action;
        }

        public bool DoesReturnValue() => ReturnType != null;
        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
        public string GetLabel(bool markdown) => "todo";
        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall) => Action.Invoke(actionSet, methodCall);
    }
}