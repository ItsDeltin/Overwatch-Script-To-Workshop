using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class StructAssigner : IGettableAssigner
    {
        readonly IVariableInstance[] _variables;
        readonly StructAssigningAttributes _attributes;
        readonly bool _isArray;

        public StructAssigner(StructInstance structInstance, StructAssigningAttributes attributes, bool isArray)
        {
            _variables = structInstance.Variables;
            _attributes = attributes;
            _isArray = isArray;
        }

        public GettableAssignerResult GetResult(GettableAssignerValueInfo info)
        {
            IStructValue initialValue = null;

            // Set the initial value.
            // If an initial value is provided, use that.
            if (info.InitialValueOverride != null)
                initialValue = StructHelper.ExtractStructValue(info.InitialValueOverride);
            // Otherwise, use the default initial value if it exists.
            else if (_attributes.DefaultValue != null)
                initialValue = StructHelper.ExtractStructValue(_attributes.DefaultValue.GetDefaultValue(info.ActionSet));
            // 'initialValue' may still be null.

            bool inline = info.Inline || _attributes.StoreType == StoreType.None;

            var values = new Dictionary<string, IGettable>();
            foreach (var var in _variables)
                // Get the child gettable.
                values.Add(
                    var.Name,
                    var.GetAssigner(new(info.ActionSet, _attributes.Name + "_", _attributes.Persist))
                        .GetValue(new GettableAssignerValueInfo(
                            actionSet: info.ActionSet,
                            setInitialValue: info.SetInitialValue,
                            initialValue: initialValue?.GetValue(var.Name),
                            inline: inline,
                            indexReferenceCreator: info.IndexReferenceCreator,
                            isGlobal: info.IsGlobal,
                            isRecursive: info.IsRecursive,
                            forceNonpersistentClear: info.ForceNonpersistentClear)));

            return new GettableAssignerResult(new StructAssignerValue(values), null);
        }

        public LinkedStructValue GetValues(ActionSet actionSet)
        {
            // Create an array linking variable names and their values.
            var values = new Dictionary<string, IWorkshopTree>();

            // Link the variable values to their names.
            foreach (var variable in _variables)
                values.Add(variable.Name, variable.GetAssigner(new(actionSet)).GetValue(new GettableAssignerValueInfo(actionSet) { Inline = true }).GetVariable());

            return new LinkedStructValue(values);
        }

        public IGettable AssignClassStacks(GetClassStacks info)
        {
            if (_attributes.StoreType == StoreType.None)
                return null;

            int offset = info.StackOffset;
            var values = new Dictionary<string, IGettable>();
            foreach (var var in _variables)
            {
                var assigner = var.GetAssigner();
                var stack = assigner.AssignClassStacks(new GetClassStacks(info.ClassData, offset));
                if (stack != null)
                {
                    values.Add(var.Name, stack);
                    offset += assigner.StackDelta();
                }
            }

            return new StructAssignerValue(values);
        }

        public int StackDelta()
        {
            if (_attributes.StoreType == StoreType.None)
                return 0;

            int delta = 0;
            for (int i = 0; i < _variables.Length; i++)
                delta += _variables[i].GetAssigner().StackDelta();
            return delta;
        }

        public IGettable Unfold(IUnfoldGettable unfolder)
        {
            var values = new Dictionary<string, IGettable>();
            foreach (var var in _variables)
                values.Add(var.Name, var.GetAssigner().Unfold(unfolder));

            return new StructAssignerValue(values);
        }
    }

    public struct StructAssigningAttributes
    {
        public string Name;
        public StoreType StoreType;
        public IVariableDefault DefaultValue;
        public bool Persist;

        public StructAssigningAttributes(AssigningAttributes attributes)
        {
            Name = attributes.Name;
            StoreType = attributes.StoreType;
            DefaultValue = attributes.DefaultValue;
            Persist = attributes.Persist;
        }
    }

    class StructAssignerValue : IGettable, IStructValue
    {
        private readonly Dictionary<string, IGettable> _children;
        IWorkshopTree IStructValue.GetValue(string variableName) => _children[variableName].GetVariable();
        IGettable IStructValue.GetGettable(string variableName) => _children[variableName];

        public StructAssignerValue(Dictionary<string, IGettable> children)
        {
            _children = children;
        }

        // *** IGettable ***

        // Encode target player into struct.
        IWorkshopTree IGettable.GetVariable(Element eventPlayer) => new TargetPlayerStruct(eventPlayer, this);

        void IGettable.Set(ActionSet actionSet, IWorkshopTree value, Element target, Element[] index)
        {
            var structValue = StructHelper.ExtractStructValue(value);

            foreach (var child in _children)
                child.Value.Set(actionSet, structValue.GetValue(child.Key), target, index);
        }

        void IGettable.Modify(ActionSet actionSet, Operation operation, IWorkshopTree value, Element target, Element[] index)
        {
            switch (operation)
            {
                case Operation.RemoveFromArrayByIndex:
                    foreach (var child in _children)
                        child.Value.Modify(actionSet, operation, value, target, index);
                    break;

                default:
                    var structValue = StructHelper.ExtractStructValue(value);

                    foreach (var child in _children)
                        child.Value.Modify(actionSet, operation, structValue.GetValue(child.Key), target, index);
                    break;
            }
        }

        void IGettable.Push(ActionSet actionSet, IWorkshopTree value)
        {
            var structValue = StructHelper.ExtractStructValue(value);

            foreach (var child in _children)
                child.Value.Push(actionSet, structValue.GetValue(child.Key));
        }

        void IGettable.Pop(ActionSet actionSet)
        {
            foreach (var child in _children)
                child.Value.Pop(actionSet);
        }

        IGettable IGettable.ChildFromClassReference(IWorkshopTree reference)
        {
            var values = new Dictionary<string, IGettable>();

            foreach (var child in _children)
                values.Add(child.Key, child.Value.ChildFromClassReference(reference));

            return new StructAssignerValue(values);
        }

        bool IGettable.CanBeSet() => _children.All(c => c.Value.CanBeSet());

        // *** IStructValue ***
        IWorkshopTree IStructValue.GetArbritraryValue() => GetArbritraryGettable().GetVariable();

        IWorkshopTree[] IStructValue.GetAllValues() => GetAllValuesWithTarget(null);

        private IGettable GetArbritraryGettable() => _children.First().Value;
        private IWorkshopTree[] GetAllValuesWithTarget(Element target) => IStructValue.ExtractAllValues(_children.Select(child => child.Value.GetVariable(target)));

        /// <summary>Targets a player struct variable with a known player.</summary>
        class TargetPlayerStruct : IStructValue
        {
            readonly StructAssignerValue _parent;
            readonly Element _target;

            public TargetPlayerStruct(Element target, StructAssignerValue parent) => (_parent, _target) = (parent, target);

            // Wrap the parent with the known target.
            public IWorkshopTree[] GetAllValues() => _parent.GetAllValuesWithTarget(this._target);
            public IWorkshopTree GetArbritraryValue() => _parent.GetArbritraryGettable().GetVariable(_target);
            public IGettable GetGettable(string variableName) => new TargetGettable(_parent._children[variableName], _target);
            public IWorkshopTree GetValue(string variableName) => _parent._children[variableName].GetVariable(_target);
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
        IGettable GetGettable(string variableName);
        IWorkshopTree GetArbritraryValue();
        IWorkshopTree[] GetAllValues();
        BridgeGetStructValue Bridge(Func<BridgeArgs, IWorkshopTree> bridge) => new BridgeGetStructValue(this, bridge);
        BridgeGetStructValue BridgeArbritrary(Func<IWorkshopTree, IWorkshopTree> bridge) => Bridge(b => bridge(b.Value));
        bool IWorkshopTree.EqualTo(IWorkshopTree other) => throw new NotImplementedException();
        void IWorkshopTree.ToWorkshop(WorkshopBuilder b, ToWorkshopContext context) => throw new NotImplementedException();

        /// <summary>Flattens structs within an array of workshop values.</summary>
        public static IWorkshopTree[] ExtractAllValues(IEnumerable<IWorkshopTree> children)
        {
            var values = new List<IWorkshopTree>();

            foreach (var child in children)
            {
                if (child is IStructValue structValue)
                    values.AddRange(structValue.GetAllValues());
                else
                    values.Add(child);
            }

            return values.ToArray();
        }

        /// <summary>Steps into a struct to get the specified value. For example, the struct <code>{x: 1, y: { z: 2 }}</code>
        /// with the path <code>"y", "z"</code> will return 2.</summary>
        public static IWorkshopTree GetValueWithPath(IStructValue structValue, IEnumerable<string> path)
        {
            IWorkshopTree current = structValue;
            foreach (var step in path)
                current = ((IStructValue)current).GetValue(step);

            return current;
        }
    }

    /// <summary>Struct variables linked to workshop values.</summary>
    public class LinkedStructValue : IStructValue
    {
        public Dictionary<string, IWorkshopTree> Values { get; }
        public IWorkshopTree GetValue(string variableName) => Values[variableName];
        public IWorkshopTree GetArbritraryValue() => Values.First().Value;
        public IWorkshopTree[] GetAllValues() => IStructValue.ExtractAllValues(Values.Select(v => v.Value));
        public IGettable GetGettable(string variableName) => new WorkshopElementReference(Values[variableName]);

        public LinkedStructValue(Dictionary<string, IWorkshopTree> values)
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
            if (Children.Any(child => child.GetValue(variableName) is IStructValue))
            {
                // If we do, create a new StructArray with the target variable.
                // This will convert the data structure like so:
                //
                // variableName == 'b'
                //   [{a: 0, b: {c: 0}}, {a: 0, b: {c: 0}}]
                //   to
                //   [b: {c: 0}, b: {c: 0}]
                var childrenAsSubstructList = Children.Select(c => StructHelper.ExtractStructValue(c.GetValue(variableName))).ToArray();
                return new StructArray(childrenAsSubstructList);
            }

            // Otherwise, create a normal workshop array.
            return Element.CreateArray(Children.Select(c => c.GetValue(variableName)).ToArray());
        }

        public IGettable GetGettable(string variableName) => new WorkshopElementReference(GetValue(variableName));
        public IWorkshopTree GetArbritraryValue()
        {
            // Shouldn't be possible, but just in case.
            if (Children.Length == 0) return Element.EmptyArray();
            return GetAllValues()[0];
        }
        public IWorkshopTree[] GetAllValues()
        {
            // This isn't possible, but if it was, we would return an empty array.
            if (Children.Length == 0) return new IWorkshopTree[0];

            // The first child is used as a reference for the other children, 'Children[x].GetAllValues().Length' will all equal the same thing.
            var primaryStructValues = Children[0].GetAllValues();
            // The number of values in each array.
            int valueCount = primaryStructValues.Length;
            // The number of arrays.
            int arrayCount = Children.Length;

            // 'transposed' is the struct values shifted into their respective arrays.
            // The data we recieve from the children's GetAllValues will look something like this:
            // [x1, y1], [x2, y2]
            // We want to change that into:
            // [x1, x2], [y1, y2]
            var transposed = new IWorkshopTree[valueCount, arrayCount];

            // Add the primary values (Children[0]).
            for (int v = 0; v < valueCount; v++)
                transposed[v, 0] = primaryStructValues[v];

            // Add other values. Start at 1 since 0 was added earlier.
            for (int c = 1; c < arrayCount; c++)
            {
                var childValues = Children[c].GetAllValues();
                for (int v = 0; v < valueCount; v++)
                    transposed[v, c] = childValues[v];
            }

            // 'transposed' is now a 2d array where the first dimension represents the workshop array
            // and the second dimension represents that array's values.

            // Return transposed converted to a range of workshop arrays.
            var arrays = new IWorkshopTree[valueCount];
            for (int v = 0; v < valueCount; v++)
            {
                // Convert the v'th dimension into its own array.
                var array = new IWorkshopTree[arrayCount];
                for (int a = 0; a < arrayCount; a++)
                    array[a] = transposed[v, a];

                // Turn that array into a singular element.
                arrays[v] = Element.CreateArray(array);
            }

            return arrays;
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
            // Get the struct value.
            var value = _structValue.GetValue(variableName);

            // Check if we need to do a value-in-array subsection.
            if (value is IStructValue subvalue)
                return new ValueInStructArray(subvalue, _index);

            // Otherwise, get the value in the array normally.
            return Element.ValueInArray(value, _index);
        }

        public IGettable GetGettable(string variableName) => _structValue.GetGettable(variableName).ChildFromClassReference(_index);
        public IWorkshopTree GetArbritraryValue() => _structValue;
        public IWorkshopTree[] GetAllValues() => _structValue.GetAllValues().Select(value => Element.ValueInArray(value, _index)).ToArray();
    }

    /// <summary>Applies a modification to a struct value.</summary>
    public class BridgeGetStructValue : IStructValue
    {
        private readonly IStructValue _structValue;
        private readonly Func<BridgeArgs, IWorkshopTree> _bridge;
        readonly IEnumerable<string> _path;

        public BridgeGetStructValue(IStructValue structValue, Func<BridgeArgs, IWorkshopTree> bridge, IEnumerable<string> path = null)
        {
            _structValue = structValue;
            _bridge = bridge;
            _path = path ?? Enumerable.Empty<string>();
        }

        public IWorkshopTree GetValue(string variableName)
        {
            // Get the struct value.
            var value = _structValue.GetValue(variableName);

            var newPath = _path.Append(variableName);

            // Check if we need to do a subsection.
            if (value is IStructValue subvalue)
                return new BridgeGetStructValue(subvalue, _bridge, newPath);

            return _bridge(new BridgeArgs(value, newPath));
        }

        public IWorkshopTree GetWorkshopValue()
        {
            IWorkshopTree current = _structValue;
            while (current is IStructValue structValue)
                current = structValue.GetArbritraryValue();

            return _bridge(new BridgeArgs(current));
        }

        public IGettable GetGettable(string variableName) => new WorkshopElementReference(GetValue(variableName));
        public IWorkshopTree GetArbritraryValue() => _structValue;
        public IWorkshopTree[] GetAllValues() => _structValue.GetAllValues().Select(v => _bridge(new BridgeArgs(v))).ToArray();
    }

    /// <summary>Arguments from bridging struct values.</summary>
    public struct BridgeArgs
    {
        /// <summary>The bridge's workshop value to be modified by the receiver.</summary>
        public IWorkshopTree Value;
        /// <summary>The path used to obtain the value.</summary>
        public IEnumerable<string> Path;

        public BridgeArgs(IWorkshopTree value, IEnumerable<string> path)
        {
            Value = value;
            Path = path;
        }

        public BridgeArgs(IWorkshopTree value)
        {
            Value = value;
            Path = null;
        }
    }

    /// <summary>Represents a struct array converted into a single array of indices with the same length of the struct array.</summary>
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
        public IWorkshopTree[] GetAllValues() => throw new NotImplementedException();

        public IWorkshopTree GetValue(string variableName)
        {
            // Get the struct value.
            var value = StructArray.GetValue(variableName);

            // Check if we need to do a subsection.
            if (value is IStructValue subvalue)
                return new IndexedStructArray(subvalue, IndexedArray, _operationModifiesLength);

            return value;
        }

        public void AppendModification(Func<(IStructValue structArray, Element indexArray), Element> append) =>
            IndexedArray = append((new ValueInStructArray(StructArray, Element.ArrayElement()), IndexedArray));

        // Hook the bridge so that it is being used with the indexed array rather than the struct value 'v'.
        public BridgeGetStructValue Bridge(Func<BridgeArgs, IWorkshopTree> bridge) => new BridgeGetStructValue(this, v => Element.ValueInArray(v.Value, bridge(new BridgeArgs(IndexedArray))));
        public BridgeGetStructValue BridgeArbritrary(Func<IWorkshopTree, IWorkshopTree> bridge)
        {
            // In typical scenarios, this will equal true.
            if (_operationModifiesLength)
                return new BridgeGetStructValue(this, v => bridge(IndexedArray));

            // It is completely useless for the user to do something like this, but if an arbritrary value is needed and
            // the length won't change, we can optimize and just use the original array.
            return new BridgeGetStructValue(this, b => bridge(b.Value));
        }
    }
}