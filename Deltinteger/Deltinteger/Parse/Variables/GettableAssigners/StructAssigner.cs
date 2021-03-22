using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class StructAssigner : IGettableAssigner
    {
        private readonly IVariableInstance[] _variables;
        private readonly Var _var;
        private readonly bool _isArray;

        public StructAssigner(StructInstance structInstance, Var var, bool isArray)
        {
            _variables = structInstance.Variables;
            _var = var;
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
            else if (_var?.InitialValue != null)
                initialValue = ValueInArrayToWorkshop.ExtractStructValue(_var.InitialValue.Parse(info.ActionSet));
            // 'initialValue' may still be null.

            bool inline = info.Inline || (_var != null && _var.StoreType == StoreType.None);

            var values = new Dictionary<string, IGettable>();
            foreach (var var in _variables)
                // Get the child gettable.
                values.Add(var.Name, var.GetAssigner(info.ActionSet).GetValue(new GettableAssignerValueInfo(info.ActionSet) {
                    InitialValueOverride = initialValue?.GetValue(var.Name),
                    Inline = inline
                }));
            
            return new GettableAssignerResult(new StructAssignerValue(values), null);
        }

        public LinkedStructAssigner GetValues(ActionSet actionSet)
        {
            // Create an array linking variable names and their values.
            var values = new Dictionary<string, IWorkshopTree>();

            // Link the variable values to their names.
            foreach (var variable in _variables)
                values.Add(variable.Name, variable.GetAssigner(actionSet).GetValue(new GettableAssignerValueInfo(actionSet) { Inline = true }).GetVariable());
            
            return new LinkedStructAssigner(values);
        }

        public IGettable AssignClassStacks(GetClassStacks info)
        {
            int offset = 0;
            var values = new Dictionary<string, IGettable>();
            foreach (var var in _variables)
            {
                var assigner = var.GetAssigner(null);
                values.Add(var.Name, assigner.AssignClassStacks(new GetClassStacks(info.DeltinScript, offset)));
                offset += assigner.StackDelta();
            }
            
            return new StructAssignerValue(values);
        }

        public int StackDelta()
        {
            int delta = 0;
            for (int i = 0; i < _variables.Length; i++)
                delta += _variables[i].GetAssigner(null).StackDelta();
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
        IGettable IStructValue.GetGettable(string variableName) => _children[variableName];

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

        public IWorkshopTree GetArbritraryValue() => _children.First().Value.GetVariable();
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
        IGettable GetGettable(string variableName);
        IWorkshopTree GetArbritraryValue();
        BridgeGetStructValue Bridge(Func<IWorkshopTree, IWorkshopTree> bridge) => new BridgeGetStructValue(this, bridge);
        BridgeGetStructValue BridgeArbritrary(Func<IWorkshopTree, IWorkshopTree> bridge) => Bridge(bridge);
        bool IWorkshopTree.EqualTo(IWorkshopTree other) => throw new NotImplementedException();
        void IWorkshopTree.ToWorkshop(WorkshopBuilder b, ToWorkshopContext context) => throw new NotImplementedException();

        public static IWorkshopTree ExtractArbritraryValue(IStructValue structValue)
        {
            IWorkshopTree current = structValue;
            while (current is IStructValue step)
                current = step.GetArbritraryValue();
            
            return current;
        }
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
        public IWorkshopTree GetArbritraryValue() => Values.First().Value;
        public IGettable GetGettable(string variableName) => new WorkshopElementReference(Values[variableName]);

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

        public IGettable GetGettable(string variableName) => new WorkshopElementReference(GetValue(variableName));

        public IWorkshopTree GetArbritraryValue() => Children[0];
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
            // Get the struct value.
            var value = _structValue.GetValue(variableName);

            // Check if we need to do a value-in-array subsection.
            if (value is IInlineStructDictionary subvalue)
                return new ValueInStructArray(subvalue, _index);

            // Otherwise, get the value in the array normally.
            return Element.ValueInArray(value, _index);
        }

        public IGettable GetGettable(string variableName) => _structValue.GetGettable(variableName).ChildFromClassReference(_index);

        public IWorkshopTree GetArbritraryValue() => _structValue;
    }

    public class BridgeGetStructValue : IStructValue
    {
        private readonly IStructValue _structValue;
        private readonly Func<IWorkshopTree, IWorkshopTree> _bridge;

        public BridgeGetStructValue(IStructValue structValue, Func<IWorkshopTree, IWorkshopTree> bridge)
        {
            _structValue = structValue;
            _bridge = bridge;
        }

        public IWorkshopTree GetValue(string variableName)
        {
            // Get the struct value.
            var value = _structValue.GetValue(variableName);

            // Check if we need to do a subsection.
            if (value is IInlineStructDictionary subvalue)
                return new BridgeGetStructValue(subvalue, _bridge);

            return _bridge(value);
        }

        public IGettable GetGettable(string variableName) => new WorkshopElementReference(GetValue(variableName));

        public IWorkshopTree GetArbritraryValue() => _structValue;

        public IWorkshopTree GetWorkshopValue()
        {
            IWorkshopTree current = _structValue;
            while (current is IStructValue structValue)
                current = structValue.GetArbritraryValue();

            return _bridge(current);
        }
    }

    class IndexedStructArray : IStructValue
    {
        public IStructValue StructArray { get; }
        public Element IndexedArray { get; private set; }
        readonly bool _operationModifiesLength;

        public IndexedStructArray(IStructValue structArray, Element indexedArray, bool operationModifiesLength)
        {
            StructArray = structArray;
            IndexedArray = indexedArray;
            _operationModifiesLength = operationModifiesLength;
        }

        public IWorkshopTree GetArbritraryValue() => StructArray;

        public IGettable GetGettable(string variableName) => new WorkshopElementReference(IndexedArray);

        public IWorkshopTree GetValue(string variableName)
        {
            // Get the struct value.
            var value = StructArray.GetValue(variableName);

            // Check if we need to do a subsection.
            if (value is IInlineStructDictionary subvalue)
                return new IndexedStructArray(subvalue, IndexedArray, _operationModifiesLength);
            
            return value;
        }

        public void AppendModification(Func<(IStructValue structArray, Element indexArray), Element> append) =>
            IndexedArray = append((new ValueInStructArray(StructArray, Element.ArrayElement()), IndexedArray));

        // Hook the bridge so that it is being used with the indexed array rather than the struct value 'v'.
        public BridgeGetStructValue Bridge(Func<IWorkshopTree, IWorkshopTree> bridge) => new BridgeGetStructValue(this, v => Element.ValueInArray(v, bridge(IndexedArray)));
        public BridgeGetStructValue BridgeArbritrary(Func<IWorkshopTree, IWorkshopTree> bridge)
        {
            // In typical scenarios, this will equal true.
            if (_operationModifiesLength)
                return new BridgeGetStructValue(this, v => bridge(IndexedArray));

            // It is completely useless for the user to do something like this, but if an arbritrary value is needed and
            // the length won't change, we can optimize and just use the original array.
            return new BridgeGetStructValue(this, bridge);
        }
    }
}