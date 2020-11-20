using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger
{
    public interface IElementProvider
    {
        void AddInstance(IScopeAppender scopeHandler, InstanceAnonymousTypeLinker genericsLinker);
    }

    public interface IMethodProvider
    {
        string Name { get; }
        AnonymousType[] GenericTypes { get; }
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

        public DefaultProvider(IMethod function)
        {
            _function = function;
        }

        public string Name => _function.Name;
        public AnonymousType[] GenericTypes => null;
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

        public static MarkupBuilder Hover(InstanceAnonymousTypeLinker typeLinker, string name, CodeType returnType, CodeType[] typeArgs, CodeParameter[] parameters)
        {
            MarkupBuilder builder = new MarkupBuilder()
                .StartCodeLine()
                .Add(typeLinker == null || returnType == null ? returnType.GetNameOrVoid() : returnType.GetRealerType(typeLinker).GetNameOrVoid())
                .Add(" " + name);

            if (typeArgs != null && typeArgs.Length > 0)
            {
                builder.Add("<");
                for (int i = 0; i < typeArgs.Length; i++)
                {
                    builder.Add((typeLinker == null ? typeArgs[i] : typeArgs[i].GetRealerType(typeLinker)).GetName());
                    if (i != typeArgs.Length - 1) builder.Add(", ");
                }
                builder.Add(">");
            }

            builder.Add("(");
            for (int i = 0; i < parameters.Length; i++)
            {
                builder.Add((typeLinker == null ? parameters[i].Type : parameters[i].Type.GetRealerType(typeLinker)).GetName() + " " + parameters[i].Name);
                if (i != parameters.Length - 1) builder.Add(", ");
            }
            builder.Add(")");

            return builder.EndCodeLine();
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

    public interface IResolveElements
    {
        void ResolveElements();
    }

    public interface IWorkshopInit
    {
        void WorkshopInit(DeltinScript deltinScript);
    }
}