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
        int _current = 0;
        List<IndexReference> _created = new List<IndexReference>();
        public IndexReference[] Created => _created.ToArray();

        public RecycleWorkshopVariableAssigner(VarCollection collection) : base(collection) {}

        public override IndexReference Create(AssigningAttributes attributes)
        {
            // New workshop variable must be assigned.
            if (_current == _created.Count)
                _created.Add(base.Create(attributes));

            return _created[_current++];
        }

        public void Reset()
        {
            _current = 0;
        }
    }
}