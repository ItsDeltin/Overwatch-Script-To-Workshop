using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class VariableElements
    {
        public IGettable IndexReference { get; }
        public Element Target { get; }
        public Element[] Index { get; }

        public VariableElements(IGettable indexReference, Element target, Element[] index)
        {
            IndexReference = indexReference;
            Target = target;
            Index = index;
        }

        public IGettable Childify()
        {
            var current = IndexReference;
            for (int i = 0; i < Index.Length; i++)
                current = current.ChildFromClassReference(Index[i]);
            return current;
        }
    }
}