using System.Collections.Generic;
using DS.Analysis.Types.Components;

namespace DS.Analysis.Structure
{
    class TypeContentBuilder
    {
        readonly List<ICodeTypeElement> elements = new List<ICodeTypeElement>();

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