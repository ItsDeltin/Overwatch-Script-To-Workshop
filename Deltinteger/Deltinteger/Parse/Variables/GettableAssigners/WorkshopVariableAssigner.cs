using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    // Assigns workshop variables to be assigned to by types.
    public class WorkshopVariableAssigner
    {
        protected VarCollection VarCollection { get; }

        public WorkshopVariableAssigner(VarCollection collection)
        {
            VarCollection = collection;
        }

        public virtual IndexReference Create(Var var, bool isGlobal) => VarCollection.Assign(
            name: var.Name,
            variableType: var.VariableType,
            isGlobal: isGlobal,
            extended: var.InExtendedCollection,
            id: var.ID);
    }

    // This allows workshop-assigned variables to be reused.
    class RecycleWorkshopVariableAssigner : WorkshopVariableAssigner
    {
        int _current = 0;
        List<IndexReference> _created = new List<IndexReference>();
        public IndexReference[] Created => _created.ToArray();

        public RecycleWorkshopVariableAssigner(VarCollection collection) : base(collection) {}

        public override IndexReference Create(Var var, bool isGlobal)
        {
            // New workshop variable must be assigned.
            if (_current == _created.Count)
                _created.Add(base.Create(var, isGlobal));

            return _created[_current++];
        }

        public void Reset()
        {
            _current = 0;
        }
    }
}