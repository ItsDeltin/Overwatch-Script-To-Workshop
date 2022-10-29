using System;
using System.Linq;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public interface IStructProvider
    {
        string Name { get; }
        IVariable[] Variables { get; }
        IVariable[] StaticVariables { get; }
        IElementProvider[] Methods { get; }
        IElementProvider[] StaticMethods { get; }
        AnonymousType[] GenericTypes { get; }
        StructInstance GetInstance(InstanceAnonymousTypeLinker typeLinker);
        void DependMeta();
        void DependContent();
    }

    public abstract class StructInitializer : ICodeTypeInitializer, IStructProvider
    {
        public string Name { get; }
        public int GenericsCount => GenericTypes.Length;
        public AnonymousType[] GenericTypes { get; protected set; }
        protected IList<IVariable> Variables { get; } = new List<IVariable>();
        protected IList<IVariable> StaticVariables { get; } = new List<IVariable>();
        protected IList<IElementProvider> Methods { get; } = new List<IElementProvider>();
        protected IList<IElementProvider> StaticMethods { get; } = new List<IElementProvider>();

        IVariable[] IStructProvider.Variables => Variables.ToArray();
        IVariable[] IStructProvider.StaticVariables => StaticVariables.ToArray();
        IElementProvider[] IStructProvider.Methods => Methods.ToArray();
        IElementProvider[] IStructProvider.StaticMethods => StaticMethods.ToArray();

        public StructInitializer(string name)
        {
            Name = name;
        }

        public CompletionItem GetCompletion() => new CompletionItem() { Label = Name };

        public StructInstance GetInstance() => GetInstance(InstanceAnonymousTypeLinker.Empty);
        public abstract StructInstance GetInstance(InstanceAnonymousTypeLinker typeLinker);
        public abstract void DependMeta();
        public abstract void DependContent();

        CodeType ICodeTypeInitializer.GetInstance() => GetInstance();
        CodeType ICodeTypeInitializer.GetInstance(GetInstanceInfo instanceInfo) => GetInstance(new InstanceAnonymousTypeLinker(GenericTypes, instanceInfo.Generics));
    }
}