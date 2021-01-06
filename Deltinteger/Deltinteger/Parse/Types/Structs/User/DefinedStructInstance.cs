using System;

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

        public override Scope GetObjectScope() => _provider.ObjectScope;
    }
}