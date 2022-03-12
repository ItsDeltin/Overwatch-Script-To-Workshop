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

        public Variable(ITypeDirector type)
        {
            Type = type;
        }

        // ICodeTypeElement todo
        ScopedElement ICodeTypeElement.ScopedElement => throw new NotImplementedException();
    }
}