using System;
using Deltin.Deltinteger.LanguageServer;
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
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public bool DoesReturnValue { get; }

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

        public FuncMethod(FuncMethodBuilder builder)
        {
            Name = builder.Name ?? throw new ArgumentNullException(nameof(Name));
            Parameters = builder.Parameters ?? new CodeParameter[0];
            ReturnType = builder.ReturnType;
            Documentation = builder.Documentation ?? throw new ArgumentNullException(nameof(Documentation));
            Action = builder.Action ?? throw new ArgumentNullException(nameof(Action));
            DoesReturnValue = builder.DoesReturnValue;
        }

        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
        public string GetLabel(bool markdown) => HoverHandler.GetLabel(!DoesReturnValue ? null : ReturnType?.Name ?? "define", Name, Parameters, markdown, null);
        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall) => Action.Invoke(actionSet, methodCall);
    }

    public class FuncMethodBuilder
    {
        public string Name;
        public CodeParameter[] Parameters;
        public CodeType ReturnType;
        public string Documentation;
        public bool DoesReturnValue;
        public Func<ActionSet, MethodCall, IWorkshopTree> Action;

        public static implicit operator FuncMethod(FuncMethodBuilder builder) => new FuncMethod(builder);
    }
}