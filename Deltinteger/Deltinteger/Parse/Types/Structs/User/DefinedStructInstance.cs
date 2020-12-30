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

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            // Get the struct value from the reference.
            var structValue = (StructValue)reference;

            // Ensure that the number of elements in the struct value's children is equal to the number of variables in the struct.
            if (_provider.Variables.Count != structValue.Children.Length)
                throw new Exception("Struct's variable count is not equal to the number of elements in the struct value.");

            // Assign the struct variables to their respective value.
            for (int i = 0; i < _provider.Variables.Count; i++)
                assigner.Add(_provider.Variables[i], structValue.Children[i]);
        }
    }
}