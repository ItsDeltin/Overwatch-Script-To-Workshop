using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    // Assigns workshop variables to be assigned to by types.
    public class WorkshopVariableAssigner
    {
        public VarCollection VarCollection { get; }

        public WorkshopVariableAssigner(VarCollection collection)
        {
            VarCollection = collection;
        }

        public virtual IndexReference Create(AssigningAttributes attributes) => VarCollection.Assign(
            name: attributes.Name,
            variableType: attributes.VariableType,
            isGlobal: attributes.IsGlobal,
            extended: attributes.Extended,
            id: attributes.ID);
    }

    // This allows workshop-assigned variables to be reused.
    public class RecycleWorkshopVariableAssigner : WorkshopVariableAssigner
    {
        readonly List<IndexReference> _created = new List<IndexReference>();
        readonly string _tagName;
        bool _stackStyle;
        int _current;
        bool _complete;

        public RecycleWorkshopVariableAssigner(VarCollection collection, string tagName, bool stackStyle) : base(collection) =>
            (_tagName, _stackStyle) = (tagName, stackStyle);

        public override IndexReference Create(AssigningAttributes attributes)
        {
            // New workshop variable must be assigned.
            if (_current == _created.Count)
            {
                ThrowIfCompleted();

                var newAttributes = attributes;
                if (_tagName != null) newAttributes.Name = GetTag();

                // Create the IndexReference.
                var reference = base.Create(newAttributes);

                // If _stackStyle is true, turn it into a RecursiveIndexReference.
                if (_stackStyle)
                    reference = new RecursiveIndexReference(reference);

                // Add the IndexReference to the list.
                _created.Add(reference);
            }

            return _created[_current++];
        }

        /// <summary>
        /// Creates an assigned workshop variable.
        /// </summary>
        public void CreateWithTag()
        {
            Create(new AssigningAttributes(GetTag(), true, false));
        }

        public void CreateWithTag(int count)
        {
            for (int i = 0; i < count; i++)
                CreateWithTag();
        }

        public void Reset()
        {
            _current = 0;
        }

        public void Complete()
        {
            ThrowIfCompleted();
            _complete = true;
        }

        public IndexReference[] Created => _created.ToArray();

        private string GetTag()
        {
            if (_tagName == null)
                throw new System.Exception("RecycleWorkshopVariableAssigner has no tag name");
            return _tagName + "_" + _current;
        }

        private void ThrowIfCompleted()
        {
            if (_complete)
                throw new System.Exception("RecycleWorkshopVariableAssigner was completed");
        }
    }
}