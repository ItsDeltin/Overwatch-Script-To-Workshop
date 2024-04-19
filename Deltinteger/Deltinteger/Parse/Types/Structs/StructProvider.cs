using System;
using System.Linq;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public interface IStructProvider
    {
        string Name { get; }
        bool Parallel { get; }
        IVariable[] Variables { get; }
        IVariable[] StaticVariables { get; }
        IEnumerable<IVariable> InstanceInlineVariables { get; }
        IMethodProvider[] Methods { get; }
        AnonymousType[] GenericTypes { get; }
        StructInstance GetInstance(InstanceAnonymousTypeLinker typeLinker);
        void DependMeta();
        void DependContent();
    }

    public abstract class StructInitializer : ICodeTypeInitializer, IStructProvider
    {
        public string Name { get; }
        public bool Parallel { get; }
        public int GenericsCount => GenericTypes.Length;
        public AnonymousType[] GenericTypes { get; protected set; }
        protected List<IVariable> Variables { get; } = new List<IVariable>();
        protected List<IVariable> StaticVariables { get; } = new List<IVariable>();
        protected List<IVariable> InstanceInlineVariables { get; } = new List<IVariable>();
        protected List<IMethodProvider> Methods { get; } = new List<IMethodProvider>();

        IVariable[] IStructProvider.Variables => Variables.ToArray();
        IVariable[] IStructProvider.StaticVariables => StaticVariables.ToArray();
        IEnumerable<IVariable> IStructProvider.InstanceInlineVariables => InstanceInlineVariables.ToArray();
        IMethodProvider[] IStructProvider.Methods => Methods.ToArray();

        public StructInitializer(string name, bool parallel)
        {
            Name = name;
            Parallel = parallel;
        }

        public abstract bool BuiltInTypeMatches(Type type);
        public CompletionItem GetCompletion() => new CompletionItem() { Label = Name };

        public abstract StructInstance GetInstance();
        public abstract StructInstance GetInstance(InstanceAnonymousTypeLinker typeLinker);
        public abstract void DependMeta();
        public abstract void DependContent();

        CodeType ICodeTypeInitializer.GetInstance() => GetInstance();
        CodeType ICodeTypeInitializer.GetInstance(GetInstanceInfo instanceInfo) => GetInstance(new InstanceAnonymousTypeLinker(GenericTypes, instanceInfo.Generics));
    }
}