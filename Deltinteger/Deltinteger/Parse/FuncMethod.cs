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
        private Action<ParseInfo, DocRange> OnCall { get; }

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
            OnCall = builder.OnCall;
        }

        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
        public string GetLabel(bool markdown) => HoverHandler.GetLabel(!DoesReturnValue ? null : ReturnType?.Name ?? "define", Name, Parameters, markdown, Documentation);
        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall) => Action.Invoke(actionSet, methodCall);

        public void Call(ParseInfo parseInfo, DocRange callRange) => OnCall?.Invoke(parseInfo, callRange);
    }

    public class FuncMethodBuilder
    {
        /// <summary>Required: the name of the function.</summary>
        public string Name;
        /// <summary>Not required: the parameters of the function. Will default to CodeParameter[0]</summary>
        public CodeParameter[] Parameters;
        /// <summary>Not required: the return type of the function.</summary>
        public CodeType ReturnType;
        /// <summary>Required: the documentation of the function.</summary>
        public string Documentation;
        /// <summary>Not required: Determines if the function returns a value.</summary>
        public bool DoesReturnValue;
        /// <summary>Required: The action of the function.</summary>
        public Func<ActionSet, MethodCall, IWorkshopTree> Action;
        /// <summary>Not required: The code to run when the function is called.</summary>
        public Action<ParseInfo, DocRange> OnCall;

        public static implicit operator FuncMethod(FuncMethodBuilder builder) => new FuncMethod(builder);
    }
}