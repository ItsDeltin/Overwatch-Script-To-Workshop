using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Parse
{
    public class FuncMethod : IMethod
    {
        public string Name { get; }
        public CodeParameter[] Parameters { get; set; }
        public ICodeTypeSolver CodeType { get; set; }
        public MethodAttributes Attributes { get; set; } = new MethodAttributes();
        public bool Static { get; set; }
        public bool WholeContext { get; set; } = true;
        public MarkupBuilder Documentation { get; set; }
        public LanguageServer.Location DefinedAt => null;
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        IMethodExtensions IMethod.MethodInfo { get; } = new MethodInfo();

        private readonly Func<ActionSet, MethodCall, IWorkshopTree> _action;
        private readonly Func<ParseInfo, DocRange, object> _onCall;

        public FuncMethod(string name, Func<ActionSet, MethodCall, IWorkshopTree> action)
        {
            Name = name;
            _action = action;
        }

        public FuncMethod(string name, CodeParameter[] parameters, Func<ActionSet, MethodCall, IWorkshopTree> action)
        {
            Name = name;
            Parameters = parameters;
            _action = action;
        }

        public FuncMethod(FuncMethodBuilder builder)
        {
            Name = builder.Name ?? throw new ArgumentNullException(nameof(Name));
            Parameters = builder.Parameters ?? new CodeParameter[0];
            CodeType = builder.ReturnType;
            Documentation = builder.Documentation ?? throw new ArgumentNullException(nameof(Documentation));
            _action = builder.Action ?? throw new ArgumentNullException(nameof(_action));
            _onCall = builder.OnCall;
        }

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall) => _action.Invoke(actionSet, methodCall);
        public object Call(ParseInfo parseInfo, DocRange callRange) => _onCall?.Invoke(parseInfo, callRange);
    }

    public class FuncMethodBuilder
    {
        /// <summary>Required: the name of the function.</summary>
        public string Name;
        /// <summary>Not required: the parameters of the function. Will default to CodeParameter[0]</summary>
        public CodeParameter[] Parameters;
        /// <summary>Not required: the return type of the function. Void by default.</summary>
        public CodeType ReturnType;
        /// <summary>Required: the documentation of the function.</summary>
        public string Documentation;
        /// <summary>Required: The action of the function.</summary>
        public Func<ActionSet, MethodCall, IWorkshopTree> Action;
        /// <summary>Not required: The code to run when the function is called.</summary>
        public Func<ParseInfo, DocRange, object> OnCall;

        public static implicit operator FuncMethod(FuncMethodBuilder builder) => new FuncMethod(builder);

        public FuncMethod GetMethod() => new FuncMethod(this);
    }
}