using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class VarIndexAssigner
    {
        private readonly Dictionary<string, IGettable> _references = new Dictionary<string, IGettable>();
        private readonly List<VarIndexAssigner> _children = new List<VarIndexAssigner>();
        private readonly VarIndexAssigner _parent = null;

        public VarIndexAssigner() {}
        private VarIndexAssigner(VarIndexAssigner parent)
        {
            this._parent = parent;
        }

        private void CheckIfAdded(IVariable var)
        {
            if (_references.ContainsKey(var.Name))
                throw new Exception(var.Name + " was already added into the variable index assigner.");
        }

        public void Add(IVariable variable, IGettable value)
        {
            CheckIfAdded(variable);
            _references.Add(variable.Name, value);
        }

        public WorkshopElementReference Add(IVariable variable, IWorkshopTree value)
        {
            CheckIfAdded(variable);
            var created = new WorkshopElementReference(value);
            _references.Add(variable.Name, created);
            return created;
        }

        public VarIndexAssigner CreateContained()
        {
            VarIndexAssigner newAssigner = new VarIndexAssigner(this);
            _children.Add(newAssigner);
            return newAssigner;
        }

        public IGettable this[IVariable variable]
        {
            get => Get(variable);
            private set {}
        }

        public IGettable Get(IVariable variable)
        {
            if (TryGet(variable, out IGettable gettable)) return gettable;
            throw new Exception(string.Format("The variable {0} is not assigned to an index.", variable.Name));
        }

        public bool TryGet(IVariable variable, out IGettable gettable)
        {
            VarIndexAssigner current = this;
            while (current != null)
            {
                if (current._references.ContainsKey(variable.Name))
                {
                    gettable = current._references[variable.Name];
                    return true;
                }

                current = current._parent;
            }
            gettable = null;
            return false;
        }

        public VarIndexAssigner CopyAll(VarIndexAssigner other)
        {
            var current = other;
            while (current != null)
            {
                // Copy references
                foreach (var reference in current._references)
                    if (!_references.ContainsKey(reference.Key))
                        _references.Add(reference.Key, reference.Value);
                current = current._parent;
            }

            return this;
        }
    }
}