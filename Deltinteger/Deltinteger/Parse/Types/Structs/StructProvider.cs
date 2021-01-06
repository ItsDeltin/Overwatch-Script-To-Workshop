using System;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public interface IStructProvider
    {
        string Name { get; }
        IVariable[] Variables { get; }
    }

    public abstract class StructInitializer : ICodeTypeInitializer, IStructProvider
    {
        public string Name { get; }
        public int GenericsCount { get; }
        public List<IVariable> Variables { get; } = new List<IVariable>();
        IVariable[] IStructProvider.Variables => Variables.ToArray();

        public StructInitializer(string name)
        {
            Name = name;
        }

        public abstract bool BuiltInTypeMatches(Type type);
        public CompletionItem GetCompletion() => new CompletionItem() { Label = Name };

        public virtual CodeType GetInstance() => new StructInstance(this, InstanceAnonymousTypeLinker.Empty);
        // TODO: generics support for structs.
        public virtual CodeType GetInstance(GetInstanceInfo instanceInfo) => new StructInstance(this, InstanceAnonymousTypeLinker.Empty);
    }
}