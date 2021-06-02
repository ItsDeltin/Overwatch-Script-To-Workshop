using System;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    class DefinedStructInstance : StructInstance
    {
        private readonly DefinedStructInitializer _provider;

        public DefinedStructInstance(DefinedStructInitializer provider, InstanceAnonymousTypeLinker genericsLinker) : base(provider, genericsLinker)
        {
            _provider = provider;

            Constructors = new Constructor[] {
                new Constructor(this, _provider.DefinedAt, AccessLevel.Public)
            };
        }

        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            base.Call(parseInfo, callRange);
            parseInfo.Script.Elements.AddDeclarationCall(_provider, new DeclarationCall(callRange, false));
            parseInfo.Script.AddDefinitionLink(callRange, _provider.DefinedAt);
        }
    }
}