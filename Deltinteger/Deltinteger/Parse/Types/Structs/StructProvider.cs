using System;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public abstract class StructInitializer : ICodeTypeInitializer
    {
        public string Name { get; }
        public int GenericsCount { get; }
        public List<IVariable> Variables { get; } = new List<IVariable>();

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