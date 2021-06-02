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
        IMethodProvider[] Methods { get; }
        IValueSolve OnReady { get; }
        AnonymousType[] GenericTypes { get; }
        StructInstance GetInstance(InstanceAnonymousTypeLinker typeLinker);
    }

    public abstract class StructInitializer : ICodeTypeInitializer, IStructProvider
    {
        public string Name { get; }
        public IValueSolve OnReady { get; protected set; } = new ValueSolveSource(true);
        public int GenericsCount => GenericTypes.Length;
        public AnonymousType[] GenericTypes { get; protected set; }
        protected IList<IVariable> Variables { get; } = new List<IVariable>();
        protected IList<IMethodProvider> Methods { get; } = new List<IMethodProvider>();

        IVariable[] IStructProvider.Variables => Variables.ToArray();
        IMethodProvider[] IStructProvider.Methods => Methods.ToArray();

        public StructInitializer(string name)
        {
            Name = name;
        }

        public abstract bool BuiltInTypeMatches(Type type);
        public CompletionItem GetCompletion() => new CompletionItem() { Label = Name };

        public abstract StructInstance GetInstance();
        public abstract StructInstance GetInstance(InstanceAnonymousTypeLinker typeLinker);

        CodeType ICodeTypeInitializer.GetInstance() => GetInstance();
        CodeType ICodeTypeInitializer.GetInstance(GetInstanceInfo instanceInfo) => GetInstance(new InstanceAnonymousTypeLinker(GenericTypes, instanceInfo.Generics));
    }
}