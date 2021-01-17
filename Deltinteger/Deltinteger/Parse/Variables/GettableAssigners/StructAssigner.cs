using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class StructAssigner : IGettableAssigner
    {
        private readonly IVariableInstance[] _variables;
        private readonly IExpression _defaultInitialValue;
        private readonly bool _isArray;

        public StructAssigner(StructInstance structInstance, IExpression initialValue, bool isArray)
        {
            _variables = structInstance.Variables;
            _defaultInitialValue = initialValue;
            _isArray = isArray;
        }

        public GettableAssignerResult GetResult(GettableAssignerValueInfo info)
        {
            IStructValue initialValue = null;
            
            // Set the initial value.
            // If an initial value is provided, use that.
            if (info.InitialValueOverride != null)
                initialValue = ValueInArrayToWorkshop.ExtractStructValue(info.InitialValueOverride);
            // Otherwise, use the default initial value if it exists.
            else if (_defaultInitialValue != null)
                initialValue = ValueInArrayToWorkshop.ExtractStructValue(_defaultInitialValue.Parse(info.ActionSet));
            // 'initialValue' may still be null.

            var values = new Dictionary<string, IGettable>();
            foreach (var var in _variables)
                // Get the child gettable.
                values.Add(var.Name, var.GetAssigner().GetValue(new GettableAssignerValueInfo(info.ActionSet) {
                    InitialValueOverride = initialValue?.GetValue(var.Name),
                    Inline = info.Inline // Copy inline status
                }));
            
            return new GettableAssignerResult(new StructAssignerValue(values), null);
        }

        public LinkedStructAssigner GetValues(ActionSet actionSet)
        {
            // Create an array linking variable names and their values.
            var values = new Dictionary<string, IWorkshopTree>();

            // Link the variable values to their names.
            foreach (var variable in _variables)
                values.Add(variable.Name, variable.GetAssigner().GetValue(new GettableAssignerValueInfo(actionSet) { Inline = true }).GetVariable());
            
            return new LinkedStructAssigner(values);
        }

        public IGettable AssignClassStacks(GetClassStacks info)
        {
            int offset = 0;
            var values = new Dictionary<string, IGettable>();
            foreach (var var in _variables)
            {
                var assigner = var.GetAssigner();
                values.Add(var.Name, assigner.AssignClassStacks(new GetClassStacks(info.DeltinScript, offset)));
                offset += assigner.StackDelta();
            }
            
            return new StructAssignerValue(values);
        }

        public int StackDelta()
        {
            int delta = 0;
            for (int i = 0; i < _variables.Length; i++)
                delta += _variables[i].GetAssigner().StackDelta();
            return delta;
        }
    }

    class StructAssignerValue : IGettable, IAssignedStructDictionary
    {
        private readonly Dictionary<string, IGettable> _children;
        IGettable[] IAssignedStructDictionary.ChildGettables => _children.Select(c => c.Value).ToArray();
        IWorkshopTree IInlineStructDictionary.this[string variableName] => _children[variableName].GetVariable();
        IGettable IAssignedStructDictionary.this[string variableName] => _children[variableName];
        IWorkshopTree IStructValue.GetValue(string variableName) => _children[variableName].GetVariable();

        public StructAssignerValue(Dictionary<string, IGettable> children)
        {
            _children = children;
        }

        public IWorkshopTree GetVariable(Element eventPlayer = null) => this;

        public void Set(ActionSet actionSet, IWorkshopTree value, Element target, Element[] index)
        {
            var structValue = ValueInArrayToWorkshop.ExtractStructValue(value);

            foreach (var child in _children)
                child.Value.Set(actionSet, structValue.GetValue(child.Key), target, index);
        }

        public void Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, Element target, Element[] index)
        {
            var structValue = ValueInArrayToWorkshop.ExtractStructValue(value);

            foreach (var child in _children)
                child.Value.Modify(actionSet, Operation.AppendToArray, structValue.GetValue(child.Key), target, index);
        }

        public IGettable ChildFromClassReference(IWorkshopTree reference)
        {
            var values = new Dictionary<string, IGettable>();

            foreach (var child in _children)
                values.Add(child.Key, child.Value.ChildFromClassReference(reference));
            
            return new StructAssignerValue(values);
        }
    }

    /*
    Hierarchy tree for struct types.

    IStructValue    - An interface that represents any type of struct value.
        StructArray    - Creates an array of struct values.
        ValueInStructArray    - Gets a value in a struct array.
        IInlineStructDictionary    - A dictionary linking variable names and values.
            (LinkedStructAssigner)    - Default IInlineStructDictionary implementation.
            IAssignedStructDictionary    - A dictionary linking variable names and workshop variables.
                (StructAssignerValue)    - Assigns struct to workshop variables. Default IAssignedStructDictionary implementation.
    */

    /// <summary>Represents a struct value or a struct array.</summary>
    public interface IStructValue : IWorkshopTree
    {
        IWorkshopTree GetValue(string variableName);
        bool IWorkshopTree.EqualTo(IWorkshopTree other) => throw new NotImplementedException();
        void IWorkshopTree.ToWorkshop(WorkshopBuilder b, ToWorkshopContext context) => throw new NotImplementedException();
    }

    /// <summary>The interface for variable-linked struct values. 'this[variableName]' will get the struct variable's value.</summary>
    public interface IInlineStructDictionary : IStructValue
    {
        IWorkshopTree this[string variableName] { get; }
    }

    /// <summary>A struct value that has assigned indices.</summary>
    public interface IAssignedStructDictionary : IInlineStructDictionary
    {
        IGettable[] ChildGettables { get; }
        new IGettable this[string variableName] { get; }
    }

    /// <summary>Struct variables linked to workshop values.</summary>
    public class LinkedStructAssigner : IInlineStructDictionary
    {
        public Dictionary<string, IWorkshopTree> Values { get; }
        public IWorkshopTree this[string variableName] => Values[variableName];
        public IWorkshopTree GetValue(string variableName) => Values[variableName];

        public LinkedStructAssigner(Dictionary<string, IWorkshopTree> values)
        {
            Values = values;
        }
    }

    /// <summary>Represents an array of struct values.</summary>
    public class StructArray : IStructValue
    {
        public IStructValue[] Children { get; }

        public StructArray(IStructValue[] children)
        {
            Children = children;
        }

        public IWorkshopTree GetValue(string variableName)
        {
            // Check if we need to do an array subsection.
            if (Children.Length > 0 && Children[0].GetValue(variableName) is IInlineStructDictionary)
            {
                // If we do, create a new StructArray with the target variable.
                // This will convert the data structure like so:
                //
                // variableName == 'b'
                //   [{a: 0, b: {c: 0}}, {a: 0, b: {c: 0}}]
                //   to
                //   [b: {c: 0}, b: {c: 0}]
                var childrenAsSubstructList = Children.Select(c => (IStructValue)c.GetValue(variableName)).ToArray();
                return new StructArray(childrenAsSubstructList);
            }

            // Otherwise, create a normal workshop array.
            return Element.CreateArray(Children.Select(c => c.GetValue(variableName)).ToArray());
        }
    }

    /// <summary>Gets a value in a struct array by an index.</summary>
    public class ValueInStructArray : IStructValue
    {
        private readonly IStructValue _structValue;
        private readonly IWorkshopTree _index;

        public ValueInStructArray(IStructValue structValue, IWorkshopTree index)
        {
            _structValue = structValue;
            _index = index;
        }

        public IWorkshopTree GetValue(string variableName)
        {
            return Element.ValueInArray(_structValue.GetValue(variableName), _index);
        }
    }
}