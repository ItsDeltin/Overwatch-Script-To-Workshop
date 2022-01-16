using System.Collections.Generic;
using DS.Analysis.Types;
using DS.Analysis.Types.Components;

namespace DS.Analysis.Structure
{
    class TypeContentBuilder
    {
        public TypeLinker TypeLinker { get; }
        readonly List<ICodeTypeElement> elements = new List<ICodeTypeElement>();

        public TypeContentBuilder(TypeLinker typeLinker)
        {
            TypeLinker = typeLinker;
        }

        public void AddElement(ICodeTypeElement element)
        {
            elements.Add(element);
        }

        public void AddAll(IEnumerable<AbstractDeclaredElement> elements)
        {
            foreach (var element in elements)
                element.AddToContent(this);
        }

        public CodeTypeContent ToCodeTypeContent() => new CodeTypeContent(elements.ToArray());
    }
}