using System;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger
{
    public interface IElementProvider
    {
        IScopeable AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker);
        void AddDefaultInstance(IScopeAppender scopeAppender);
    }

    public interface IMethodProvider
    {
        string Name { get; }
        AnonymousType[] GenericTypes { get; }
        CodeType[] ParameterTypes { get; }
        public int TypeArgCount => GenericTypes == null ? 0 : GenericTypes.Length;

        IMethod GetDefaultInstance()
        {
            if (this is IMethod method)
                return method;
            throw new NotImplementedException();
        }
        IMethod GetInstance(GetInstanceInfo instanceInfo) => GetDefaultInstance();

        void Override(IMethodProvider overridenBy) => throw new NotImplementedException();

        public InstanceAnonymousTypeLinker GetInstanceInfo(CodeType[] typeArgs) => new InstanceAnonymousTypeLinker(GenericTypes, typeArgs);
    }

    class DefaultProvider : IMethodProvider
    {
        private readonly IMethod _function;
        public string Name => _function.Name;
        public AnonymousType[] GenericTypes => null;
        public CodeType[] ParameterTypes { get; }

        public DefaultProvider(IMethod function)
        {
            _function = function;
            ParameterTypes = function.Parameters.Select(p => p.Type).ToArray();
        }
    }

    public interface IMethod : IScopeable, IParameterCallable
    {
        MethodAttributes Attributes { get; }
        IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall);
        bool DoesReturnValue => CodeType != null;

        IMethodProvider GetProvider()
        {
            if (this is IMethodProvider provider) return provider;
            else return new DefaultProvider(this);
        }

        public static MarkupBuilder DefaultLabel(bool includeDescription, IMethod function)
        {
            MarkupBuilder markup = new MarkupBuilder()
                .StartCodeLine()
                .Add(function.CodeType.GetNameOrVoid())
                .Add(" ")
                .Add(function.Name + CodeParameter.GetLabels(function.Parameters))
                .EndCodeLine();
            
            if (includeDescription && function.Documentation != null)
            {
                markup
                    .NewSection()
                    .Add(function.Documentation);
            }
            
            return markup;
        }

        public static MarkupBuilder Hover(InstanceAnonymousTypeLinker typeLinker, string name, CodeType returnType, CodeType[] typeArgs, CodeParameter[] parameters)
        {
            MarkupBuilder builder = new MarkupBuilder()
                .StartCodeLine()
                .Add(typeLinker == null || returnType == null ? returnType.GetNameOrVoid() : returnType.GetRealType(typeLinker).GetNameOrVoid())
                .Add(" " + name);

            if (typeArgs != null && typeArgs.Length > 0)
            {
                builder.Add("<");
                for (int i = 0; i < typeArgs.Length; i++)
                {
                    builder.Add((typeLinker == null ? typeArgs[i] : typeArgs[i].GetRealType(typeLinker)).GetName());
                    if (i != typeArgs.Length - 1) builder.Add(", ");
                }
                builder.Add(">");
            }

            builder.Add("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                builder.Add((typeLinker == null ? parameters[i].Type : parameters[i].Type.GetRealType(typeLinker)).GetName() + " " + parameters[i].Name);
                if (i != parameters.Length - 1) builder.Add(", ");
            }
            builder.Add(")");

            return builder.EndCodeLine();
        }
        public static CompletionItem GetFunctionCompletion(IMethod function) => new CompletionItem()
        {
            Label = function.Name,
            Kind = CompletionItemKind.Method,
            Detail = function.CodeType.GetNameOrVoid() + " " + function.Name + CodeParameter.GetLabels(function.Parameters),
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
        CodeType CodeType { get; }
        bool WholeContext { get; }
        CompletionItem GetCompletion();
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
    }

    public interface IAccessable
    {
        Location DefinedAt { get; }
        AccessLevel AccessLevel { get; }
    }

    public interface IGettable
    {
        IWorkshopTree GetVariable(Element eventPlayer = null);
        void Set(ActionSet actionSet, IWorkshopTree value) => Set(actionSet, value, null, null);
        void Set(ActionSet actionSet, IWorkshopTree value, Element target, params Element[] index);
        void Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, Element target, params Element[] index);
        IGettable ChildFromClassReference(IWorkshopTree reference);
    }

    public interface ILabeled
    {
        string GetLabel(bool markdown);
    }

    public interface IApplyBlock : IBlockListener, ILabeled
    {
        void SetupParameters() {}
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

    public interface IResolveElements
    {
        void ResolveElements();
    }

    public interface IWorkshopInit
    {
        void WorkshopInit(DeltinScript deltinScript);
    }
}