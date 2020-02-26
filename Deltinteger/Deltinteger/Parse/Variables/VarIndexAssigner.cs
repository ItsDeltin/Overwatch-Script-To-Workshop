using System;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Parse
{
    public class VarIndexAssigner
    {
        private readonly Dictionary<IIndexReferencer, IGettable> references = new Dictionary<IIndexReferencer, IGettable>();
        private readonly List<VarIndexAssigner> children = new List<VarIndexAssigner>();
        private readonly VarIndexAssigner parent = null;

        public VarIndexAssigner() {}
        private VarIndexAssigner(VarIndexAssigner parent)
        {
            this.parent = parent;
        }

        public IGettable Add(VarCollection varCollection, Var var, bool isGlobal, IWorkshopTree referenceValue, bool recursive = false)
        {
            if (varCollection == null) throw new ArgumentNullException(nameof(varCollection));
            if (var == null)           throw new ArgumentNullException(nameof(var          ));

            if (references.ContainsKey(var)) throw new Exception(var.Name + " was already added into the variable index assigner.");

            IGettable assigned;

            // A gettable/settable variable
            if (var.Settable())
            {
                assigned = varCollection.Assign(var, isGlobal);
                if (recursive) assigned = new RecursiveIndexReference((IndexReference)assigned);
                references.Add(var, assigned);
            }
            
            // Element reference
            else if (var.VariableType == VariableType.ElementReference)
            {
                if (referenceValue == null) throw new ArgumentNullException(nameof(referenceValue));
                assigned = new WorkshopElementReference(referenceValue);
                references.Add(var, assigned);
            }
            
            else throw new NotImplementedException();

            return assigned;
        }

        public void Add(IIndexReferencer var, IndexReference reference)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            if (references.ContainsKey(var)) throw new Exception(var.Name + " was already added into the variable index assigner.");
            references.Add(var, reference);
        }

        public void Add(IIndexReferencer var, IWorkshopTree reference)
        {
            if (reference == null) throw new ArgumentNullException(nameof(reference));
            if (references.ContainsKey(var)) throw new Exception(var.Name + " was already added into the variable index assigner.");
            references.Add(var, new WorkshopElementReference(reference));
        }

        public VarIndexAssigner CreateContained()
        {
            VarIndexAssigner newAssigner = new VarIndexAssigner(this);
            children.Add(newAssigner);
            return newAssigner;
        }

        public IGettable this[IIndexReferencer var]
        {
            get {
                VarIndexAssigner current = this;
                while (current != null)
                {
                    if (current.references.ContainsKey(var))
                        return current.references[var];

                    current = current.parent;
                }

                throw new Exception(string.Format("The variable {0} is not assigned to an index.", var.Name));
            }
            private set {}
        }
    }
}