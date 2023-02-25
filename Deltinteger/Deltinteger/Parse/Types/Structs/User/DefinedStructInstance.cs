using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    class DefinedStructInstance : StructInstance
    {
        public DefinedStructInitializer Provider { get; }

        public DefinedStructInstance(DefinedStructInitializer provider, InstanceAnonymousTypeLinker genericsLinker) : base(provider, genericsLinker)
        {
            Provider = provider;

            Constructors = new Constructor[] {
                new Constructor(this, Provider.DefinedAt, AccessLevel.Public)
            };

            Description = provider.Doc?.Description;
        }

        public override IEnumerable<CodeType> GetAssigningTypes()
        {
            yield return this; // Structs are always assigning types.

            // Get the generics that are assigning types.
            for (int i = 0; i < Generics.Length; i++)
                if (Provider.GenericAssigns[i])
                    foreach (var recursive in Generics[i].GetAssigningTypes())
                        yield return recursive;
        }

        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            base.Call(parseInfo, callRange);
            parseInfo.Script.Elements.AddDeclarationCall(Provider, new DeclarationCall(callRange, false));
            parseInfo.Script.AddDefinitionLink(callRange, Provider.DefinedAt);
        }
    }
}