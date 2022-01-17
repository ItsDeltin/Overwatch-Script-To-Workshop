using System;

namespace DS.Analysis.Methods
{
    class MethodGroup
    {
        public string Name { get; }
        public MethodInstance[] Methods { get; }

        public MethodGroup(MethodInstance[] methods)
        {
            if (methods == null)
                throw new ArgumentNullException(nameof(methods));

            if (methods.Length == 0)
                throw new ArgumentException(nameof(methods), "The methods array should have at least 1 value");

            Name = methods[0].Name;
            Methods = methods;
        }

        public MethodGroup(string name, MethodInstance[] methods)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Methods = methods ?? throw new ArgumentNullException(nameof(methods));
        }

        public MethodGroup(MethodInstance method)
        {
            Name = method?.Name ?? throw new ArgumentNullException(nameof(method));
            Methods = new MethodInstance[] { method };
        }
    }
}