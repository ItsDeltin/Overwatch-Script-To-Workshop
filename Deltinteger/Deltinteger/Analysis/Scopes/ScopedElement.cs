using System;
using DS.Analysis.Expressions.Identifiers;
using DS.Analysis.Methods;
using DS.Analysis.Scopes.Selector;

namespace DS.Analysis.Scopes
{
    class ScopedElement
    {
        public string Name { get; }

        public virtual ITypeNodeManager TypePartHandler { get; protected set; }

        public virtual MethodInstance Method { get; protected set; }

        public virtual IElementSelector ElementSelector { get; protected set; }


        public ScopedElement(string alias)
        {
            Name = alias;
        }


        public override string ToString() => Name;


        public static ScopedElement CreateVariable(string name, IdentifierInfo identifierInfo) => new ScopedElement(name)
        {
            ElementSelector = new UnambiguousSelector(new IdentifiedElement(identifierInfo))
        };

        public static ScopedElement CreateAlias(string name, IdentifiedElement identifiedElement) => new ScopedElement(name)
        {
            TypePartHandler = identifiedElement.TypePartHandler,
            ElementSelector = new UnambiguousSelector(identifiedElement)
        };

        public static ScopedElement CreateType(string name, ITypeNodeManager partHandler) => new ScopedElement(name)
        {
            ElementSelector = new UnambiguousSelector(new IdentifiedElement(partHandler)),
            TypePartHandler = partHandler
        };

        public static ScopedElement CreateMethod(MethodInstance instance) => CreateMethod(instance.Name, instance);

        public static ScopedElement CreateMethod(string name, MethodInstance instance) => new ScopedElement(name)
        {
            Method = instance,
            ElementSelector = new MethodGroupSelector()
        };
    }
}