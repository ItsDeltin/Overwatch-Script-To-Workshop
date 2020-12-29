using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class StructAssigner : IGettableAssigner
    {
        private readonly IVariableInstance[] _variables;

        public StructAssigner(IVariableInstance[] variables)
        {
            _variables = variables;
        }

        public StructAssigner(StructInstance structInstance)
        {
            _variables = structInstance.Variables;
        }

        public GettableAssignerResult GetResult(GettableAssignerValueInfo info)
        {
            IGettable[] children = new IGettable[_variables.Length];
            for (int i = 0; i < children.Length; i++)
                children[i] = _variables[i].GetAssigner().GetValue(info);
            
            return new GettableAssignerResult(new StructAssignerValue(children), null);
        }

        public IGettable AssignClassStacks(GetClassStacks info)
        {
            int offset = 0;
            IGettable[] children = new IGettable[_variables.Length];
            for (int i = 0; i < children.Length; i++)
            {
                var assigner = _variables[i].GetAssigner();
                children[i] = assigner.AssignClassStacks(new GetClassStacks(info.DeltinScript, offset));
                offset += assigner.StackDelta();
            }
            
            return new StructAssignerValue(children);
        }

        public int StackDelta()
        {
            int delta = 0;
            for (int i = 0; i < _variables.Length; i++)
                delta += _variables[i].GetAssigner().StackDelta();
            return delta;
        }
    }

    class StructAssignerValue : IGettable
    {
        private readonly IGettable[] _children;

        public StructAssignerValue(IGettable[] children)
        {
            _children = children;
        }

        public IWorkshopTree GetVariable(Element eventPlayer = null) => new StructValue(_children);

        public void Set(ActionSet actionSet, IWorkshopTree value, Element target, Element[] index)
        {
            var structValue = (StructValue)value;
            
            for (int i = 0; i < _children.Length; i++)
                _children[i].Set(actionSet, structValue.Children[i].GetVariable(target), target, index);
        }

        public void Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, Element target, Element[] index)
        {
            var structValue = (StructValue)value;
            
            for (int i = 0; i < _children.Length; i++)
                _children[i].Modify(actionSet, operation, structValue.Children[i].GetVariable(target), target, index);
        }

        public IGettable ChildFromClassReference(IWorkshopTree reference)
        {
            var newChildren = new IGettable[_children.Length];

            for (int i = 0; i < newChildren.Length; i++)
                newChildren[i] = _children[i].ChildFromClassReference(reference);
            
            return new StructAssignerValue(newChildren);
        }
    }

    public class StructValue : IWorkshopTree
    {
        public IGettable[] Children { get; }

        public StructValue(IGettable[] children)
        {
            Children = children;
        }

        public bool EqualTo(IWorkshopTree other) => throw new System.NotImplementedException();
        public void ToWorkshop(WorkshopBuilder b, ToWorkshopContext context) => throw new System.NotImplementedException();
    }
}