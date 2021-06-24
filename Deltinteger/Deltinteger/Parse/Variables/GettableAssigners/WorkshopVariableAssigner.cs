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
        int _current = 0;

        public RecycleWorkshopVariableAssigner(VarCollection collection) : base(collection) {}
        public RecycleWorkshopVariableAssigner(VarCollection collection, string tagName) : base(collection) => _tagName = tagName;

        public override IndexReference Create(AssigningAttributes attributes)
        {
            // New workshop variable must be assigned.
            if (_current == _created.Count)
            {
                var newAttributes = attributes;
                if (_tagName != null) newAttributes.Name = _tagName + "_" + _current;

                _created.Add(base.Create(newAttributes));
            }

            return _created[_current++];
        }

        public void Reset() => _current = 0;

        public IndexReference[] Created => _created.ToArray();
    }
}