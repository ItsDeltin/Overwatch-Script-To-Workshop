using System;
using DS.Analysis.Types;
using DS.Analysis.Types.Components;
using DS.Analysis.Scopes;
using DS.Analysis.Expressions;
using DS.Analysis.Expressions.Identifiers;

namespace DS.Analysis.Variables
{
    class Variable : ICodeTypeElement
    {
        public ITypeDirector Type { get; }
        readonly string name;

        public Variable(string name, ITypeDirector type)
        {
            Type = type;
            ScopedElement = ScopedElement.CreateVariable(name, new IdentifierInfo(Type));
            this.name = name;
        }

        // IScopedElement
        public ScopedElement ScopedElement { get; }
    }
}