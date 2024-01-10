using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Workshop;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class StructInstance : CodeType, ITypeArrayHandler, IScopeAppender
    {
        public IVariableInstance[] Variables
        {
            get
            {
                SetupMeta();
                return _variables;
            }
        }
        protected IVariableInstance[] _variables { get; private set; }

        protected Scope ObjectScope { get; }
        protected Scope StaticScope { get; }
        readonly IStructProvider _provider;
        readonly InstanceAnonymousTypeLinker _typeLinker;

        bool _instanceReady;
        bool _contentReady;

        public StructInstance(IStructProvider provider, InstanceAnonymousTypeLinker typeLinker) : base(provider.Name)
        {
            ObjectScope = new Scope("struct " + Name);
            StaticScope = new Scope("struct " + Name);

            Generics = typeLinker.SafeTypeArgsFromAnonymousTypes(provider.GenericTypes);
            Attributes = new StructAttributes(this, provider.Parallel);
            TypeSemantics = new StructSemantics(this);
            _provider = provider;
            _typeLinker = typeLinker;
            ArrayHandler = this;
            NeedsArrayProtection = !provider.Parallel;

            Operations.AddAssignmentOperator();
            Operations.DefaultAssignment = false;
        }

        protected virtual void Setup()
        {
            _instanceReady = true;

            // Variables
            _variables = new IVariableInstance[_provider.Variables.Length];
            for (int i = 0; i < _variables.Length; i++)
            {
                _variables[i] = _provider.Variables[i].GetInstance(this, _typeLinker);
                ObjectScope.AddNativeVariable(_variables[i]);
            }

            // Static variables
            foreach (var variable in _provider.StaticVariables)
                variable.AddInstance(this, _typeLinker);

            // Functions
            foreach (var method in _provider.Methods)
                method.AddInstance(this, _typeLinker);
        }

        protected virtual void Content()
        {
            _contentReady = true;
            if (Attributes.IsStruct)
                Attributes.StackLength = _variables.Select(v => v.GetAssigner(new GetVariablesAssigner(_typeLinker)).StackDelta()).Sum();
            else
                Attributes.StackLength = 1;
        }

        public override bool CompatibleWith(CodeType type)
        {
            int stackDelta = GetGettableAssigner(AssigningAttributes.Empty).StackDelta();

            return type is StructInstance structInstance
                ? stackDelta == structInstance.GetGettableAssigner(AssigningAttributes.Empty).StackDelta()
                : stackDelta == 1;
        }

        public override CodeType GetRealType(InstanceAnonymousTypeLinker instanceInfo)
        {
            // Similiar to DefinedClass.GetRealType
            var newLinker = InstanceAnonymousTypeLinker.Empty;

            for (int i = 0; i < Generics.Length; i++)
            {
                if (Generics[i] is AnonymousType at && instanceInfo.Links.ContainsKey(at))
                    newLinker.Add(_provider.GenericTypes[i], instanceInfo.Links[at]);
                else
                    newLinker.Add(_provider.GenericTypes[i], Generics[i].GetRealType(instanceInfo));
            }

            return _provider.GetInstance(newLinker);
        }

        public override Scope GetObjectScope()
        {
            SetupMeta();
            return ObjectScope;
        }

        public override Scope ReturningScope()
        {
            SetupMeta();
            return StaticScope;
        }

        public override AccessLevel LowestAccessLevel(CodeType other)
        {
            if (other != null && other is StructInstance structInstance && _provider == structInstance._provider)
                return AccessLevel.Private;
            else
                return AccessLevel.Public;
        }

        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, SourceIndexReference source, VarIndexAssigner assigner)
        {
            // Add the struct variables to the assigner.
            // Parallel
            if (Attributes.IsStruct)
            {
                var structValue = (IStructValue)source.Value;

                foreach (var variable in Variables)
                    assigner.Add(variable.Provider, structValue.GetGettable(variable.Name));
            }
            // Unparalleled
            else
            {
                var stacks = UnparalleledStructStack.StacksFromType(source.Reference, this);
                foreach (var stack in stacks)
                    assigner.Add(stack.Variable.Provider, stack.SteppedValue);
            }
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, WorkshopParameter[] parameters)
            => GetGettableAssigner(new AssigningAttributes()).GetValue(new GettableAssignerValueInfo(actionSet)).GetVariable();

        void SetupMeta()
        {
            _provider.DependMeta();
            if (!_instanceReady) Setup();
        }

        void SetupContent()
        {
            SetupMeta();
            _provider.DependContent();
            if (!_contentReady) Content();
        }

        public override IGettableAssigner GetGettableAssigner(AssigningAttributes attributes)
        {
            if (Attributes.IsStruct)
                return new StructAssigner(this, new StructAssigningAttributes(attributes), false);
            else
                return base.GetGettableAssigner(attributes);
        }

        IGettableAssigner ITypeArrayHandler.GetArrayAssigner(AssigningAttributes attributes)
        {
            if (Attributes.IsStruct)
                return new StructAssigner(this, new StructAssigningAttributes(attributes), true);
            else
                return new DataTypeAssigner(attributes);
        }
        ArrayFunctionHandler ITypeArrayHandler.GetFunctionHandler()
        {
            if (Attributes.IsStruct)
                return new StructArrayFunctionHandler();
            else
                return new ArrayFunctionHandler();
        }
        void ITypeArrayHandler.OverrideArray(ArrayType array) { }

        void IScopeAppender.AddObjectBasedScope(IMethod function) => ObjectScope.AddNativeMethod(function);
        void IScopeAppender.AddStaticBasedScope(IMethod function) => StaticScope.AddNativeMethod(function);
        void IScopeAppender.AddObjectBasedScope(IVariableInstance variable) => ObjectScope.AddNativeVariable(variable);
        void IScopeAppender.AddStaticBasedScope(IVariableInstance variable) => StaticScope.AddNativeVariable(variable);

        // Overrides default array function implementation to support structs, since the default won't work with parallel variables.
        class StructArrayFunctionHandler : ArrayFunctionHandler
        {
            // Since all variables in a struct array will be of the same length, we can use BridgeArbritrary instead.
            public override IWorkshopTree Length(IWorkshopTree reference) => str(reference).BridgeArbritrary(v => Element.CountOf(v)).GetWorkshopValue();
            public override IWorkshopTree FirstOf(IWorkshopTree reference) => str(reference).Bridge(v => Element.FirstOf(v.Value));
            public override IWorkshopTree LastOf(IWorkshopTree reference) => str(reference).Bridge(v => Element.LastOf(v.Value));
            public override IWorkshopTree Contains(IWorkshopTree reference, IWorkshopTree value) => SearchStructArray(reference, value, SearchType.Contains);
            public override IWorkshopTree IndexOf(IWorkshopTree reference, IWorkshopTree value) => SearchStructArray(reference, value, SearchType.IndexOf);

            IWorkshopTree SearchStructArray(IWorkshopTree reference, IWorkshopTree value, SearchType searchType)
            {
                // Extract the values from the 'reference' struct array.
                IWorkshopTree[] arrayValues; // Flattened struct values.
                IWorkshopTree iterator; // An arbritrary pointer to an array that will be iterated on.
                IWorkshopTree mappedIterator; // The iterator as an array of indexes like [0, 1, 2, 3...]
                IWorkshopTree targetter; // The workshop element that will retrieve the current array value while iterating.
                bool allowFirstElementQuickSkip; // If the struct has only one value, we can use normal means to do the search.

                // 'Contains' is being executed on an indexed struct array.
                if (reference is IndexedStructArray indexedStructArray)
                {
                    arrayValues = indexedStructArray.StructArray.GetAllValues();
                    targetter = Element.ArrayElement();
                    allowFirstElementQuickSkip = false;
                    iterator = mappedIterator = indexedStructArray.IndexedArray;
                }
                else
                {
                    arrayValues = str(reference).GetAllValues();
                    targetter = Element.ArrayIndex();
                    allowFirstElementQuickSkip = true;
                    iterator = arrayValues[0];
                    mappedIterator = Element.Map(arrayValues[0], Element.ArrayIndex());
                }

                // Extract values from the 'value' struct.
                IWorkshopTree[] contains = str(value).GetAllValues();

                if (arrayValues.Length != contains.Length)
                    throw new Exception("Lengths of struct pair do not match.");

                // If the struct only has one value, we can just use the default Contains.
                if (arrayValues.Length == 1 && allowFirstElementQuickSkip)
                {
                    if (searchType == SearchType.Contains)
                        return Element.Contains(iterator, contains[0]);
                    else
                        return Element.IndexOfArrayValue(iterator, contains[0]);
                }

                // The list of struct comparisons.
                var comparisons = new Queue<IWorkshopTree>();

                // Add the comparisons to the queue.
                for (int i = 0; i < arrayValues.Length; i++)
                {
                    // Later in this function, 'Is True For Any's array is arrayValues[0], so for the first struct value, just use Array Element.
                    // Otherwise, it will result in 'Is True For Any(x, x[Array Index] == ...' when 'Array Element' would suffice for 'x[Array Index]'
                    // Do not use shortcut if the search type is IndexOf because the iterator will be indexes.
                    var compareFrom = allowFirstElementQuickSkip && i == 0 && searchType == SearchType.Contains ?
                        Element.ArrayElement() :
                        Element.ValueInArray(arrayValues[i], targetter);

                    comparisons.Enqueue(Element.Compare(compareFrom, Operator.Equal, contains[i]));
                }

                // Convert the list of comparisons into an && sequence.
                var condition = comparisons.Dequeue();
                while (comparisons.Count > 0)
                    condition = Element.And(condition, comparisons.Dequeue());

                if (searchType == SearchType.Contains)
                    return Element.Any(iterator, condition);
                else
                    // -1 is added to the list of indexes. When no structs match the value being searched for, -1 will be chosen.
                    return Element.FirstOf(Element.Append(Element.Filter(mappedIterator, condition), Element.Num(-1)));
            }

            public override IWorkshopTree Append(IWorkshopTree reference, IWorkshopTree value) =>
                str(reference).Bridge(v => Element.Append(v.Value, IStructValue.GetValueWithPath(str(value), v.Path)));
            public override IWorkshopTree Remove(IWorkshopTree reference, IWorkshopTree value) =>
                str(reference).Bridge(v => Element.Remove(v.Value, IStructValue.GetValueWithPath(str(value), v.Path)));
            public override IWorkshopTree Slice(IWorkshopTree reference, IWorkshopTree start, IWorkshopTree count) =>
                str(reference).Bridge(v => Element.Slice(v.Value, start, count));
            public override ISortFunctionExecutor SortedArray() => new StructSortExecutorReturnsArray(Element.SORTED_ARRAY, false);
            public override ISortFunctionExecutor FilteredArray() => new StructSortExecutorReturnsArray(Element.FILTERED_ARRAY, true);
            public override ISortFunctionExecutor All() => new StructSortExecutorReturnsBoolean(Element.IS_TRUE_FOR_ALL);
            public override ISortFunctionExecutor Any() => new StructSortExecutorReturnsBoolean(Element.IS_TRUE_FOR_ANY);
            public override ISortFunctionExecutor Map() => new StructMap();

            public StructArrayFunctionHandler()
            {
                AllowUnhandled = false;
            }

            // Used by the SearchStructArray method.
            private enum SearchType
            {
                Contains,
                IndexOf
            }

            // For struct array iterators that returns a struct array (Filtered Array, Sorted Array).
            private class StructSortExecutorReturnsArray : ISortFunctionExecutor
            {
                readonly string _functionName;
                // If the user does something like array.SortedArray(...).Length, we can just optimize the SortedArray away.
                readonly bool _operationModifiesLength;

                public StructSortExecutorReturnsArray(string functionName, bool operationModifiesLength)
                {
                    _functionName = functionName;
                    _operationModifiesLength = operationModifiesLength;
                }

                public IWorkshopTree GetResult(
                    ActionSet actionSet,
                    Func<IWorkshopTree, IWorkshopTree> invoke)
                {
                    // Extract the struct array from the action set's current object.
                    var currentStructArray = (IStructValue)actionSet.CurrentObject;

                    if (currentStructArray is IndexedStructArray appendToExistingNode)
                    {
                        appendToExistingNode.AppendModification(args => Element.Part(
                            _functionName,
                            args.indexArray,
                            invoke(args.structArray)
                        ));
                        return appendToExistingNode;
                    }

                    // Step into the struct array with the array index. Since the array in this context is the
                    // struct array mapped into a normal array of indices, either ArrayElement or ArrayIndex will
                    // work here.
                    var arrayed = new ValueInStructArray(currentStructArray, Element.ArrayElement());

                    var indexedArray = Element.Part(
                        _functionName,
                        Element.Map(StructHelper.ExtractArbritraryValue(currentStructArray), Element.ArrayIndex()),
                        invoke(arrayed));

                    // Return the struct array.
                    return new IndexedStructArray(
                        structArray: currentStructArray,
                        // The struct array converted into an array of indices.
                        indexedArray: indexedArray,
                        operationModifiesLength: _operationModifiesLength);
                }
            }

            // For struct array iterators that returns a boolean (Is True For Any, Is True For All).
            private class StructSortExecutorReturnsBoolean : ISortFunctionExecutor
            {
                readonly string _functionName;

                public StructSortExecutorReturnsBoolean(string functionName) => _functionName = functionName;

                public IWorkshopTree GetResult(
                    ActionSet actionSet,
                    Func<IWorkshopTree, IWorkshopTree> invoke)
                {
                    // Extract the struct array from the action set's current object.
                    var currentStructArray = (IStructValue)actionSet.CurrentObject;

                    if (currentStructArray is IndexedStructArray appendToExistingNode)
                        return Element.Part(
                            _functionName,
                            appendToExistingNode.IndexedArray,
                            invoke(appendToExistingNode.StructArray)
                        );

                    // Step into the struct array with the current array index.
                    var arrayed = new ValueInStructArray(currentStructArray, Element.ArrayIndex());

                    // Return the workshop function.
                    return Element.Part(
                        _functionName,
                        StructHelper.ExtractArbritraryValue(currentStructArray),
                        invoke(arrayed));
                }
            }

            // Struct mapping.
            private class StructMap : ISortFunctionExecutor
            {
                public IWorkshopTree GetResult(ActionSet actionSet, Func<IWorkshopTree, IWorkshopTree> invoke) => Map(actionSet, invoke);

                IWorkshopTree Map(ActionSet actionSet, Func<IWorkshopTree, IWorkshopTree> invoke)
                {
                    // The struct array that is being mapped.
                    var structArray = str(actionSet.CurrentObject);

                    // Mapping indexed struct array
                    if (structArray is IndexedStructArray indexedStructArray)
                        return MapIndexed(actionSet, indexedStructArray, invoke);

                    // The current element's struct values stepped into using ArrayIndex.
                    var structInset = new ValueInStructArray(structArray, Element.ArrayIndex());

                    // Map the value.
                    var value = invoke(structInset);

                    // Optimize by locating the most commonly used inset and replacing it with
                    // the 'Current Array Element' value.
                    var variablePaths = StructHelper.ExtractAllPaths(structInset);
                    var templates = StructHelper.SpreadTemplates(value, innerValue =>
                    {
                        foreach (var path in variablePaths)
                            if (innerValue.EqualTo(IStructValue.GetValueWithPath(structInset, path.Steps)))
                                return path;
                        return null;
                    });

                    // Path to the most commonly used inset.
                    StructPath? mostCommonInset = null;

                    // Apply templates
                    foreach (var template in templates)
                    {
                        if (template.PatternTemplate.Patterns.Count != 0)
                        {
                            // Find name of most common pattern.
                            var mostCommon = template.PatternTemplate.Patterns.OrderByDescending(o => o.Value.Count()).First();
                            mostCommon.Value.ReplaceWith(Element.ArrayElement());
                            mostCommonInset = mostCommon.Key;
                        }
                    }

                    // Value is a struct, bridge it.
                    if (value is IStructValue structValue)
                        return structValue.Bridge(v => CompleteAndOptimize(structArray, structInset, v.Value, mostCommonInset));

                    return CompleteAndOptimize(structArray, structInset, value, mostCommonInset);
                }

                IWorkshopTree CompleteAndOptimize(IStructValue originalArray, ValueInStructArray structInset, IWorkshopTree workshopValue, StructPath? mostCommonInset)
                {
                    /*
                    Optimize situations such as 'values.Map(value => value.b)'. 'b' is stored as an array 'global.values_b'.
                    This will compile like 'Mapped Array(global.values_b, values_b[Current Array Index])', which is the
                    same exact thing as doing just 'global.values_b'.
                    */
                    var extractedInsetValues = structInset.GetAllValues();

                    for (int i = 0; i < extractedInsetValues.Length; i++)
                        // Scan every extracted inset value to see if the pattern matches.
                        if (workshopValue.EqualTo(extractedInsetValues[i]))
                            // If it does, return the respective original array.
                            return originalArray.GetAllValues()[i];

                    // Map using the most common inset if applicable, otherwise use an arbritrary value.
                    var innerArray = mostCommonInset is null ?
                        StructHelper.ExtractArbritraryValue(originalArray) :
                        IStructValue.GetValueWithPath(originalArray, mostCommonInset.Value.Steps);

                    // Do actual map if it cannot be truncated.
                    return Element.Map(innerArray, workshopValue);
                }

                IWorkshopTree MapIndexed(ActionSet actionSet, IndexedStructArray indexedStructArray, Func<IWorkshopTree, IWorkshopTree> invoke)
                {
                    var structInset = new ValueInStructArray(indexedStructArray.StructArray, Element.ArrayElement());
                    var value = invoke(structInset);

                    // Value is struct, append the modification to that struct.
                    if (value is IStructValue structValue)
                    {
                        indexedStructArray.AppendModification(args => Element.Map(args.indexArray, value));
                        return indexedStructArray;
                    }
                    // This case will be true if mapping a filtered/sorted struct array, like so:
                    // `structArray.Filter(s => s.Value).Map((v, i) => i);`
                    // Since the Filter indexes the struct array, we can just return that without adding an
                    // extra, expensive, and redundant map.
                    else if (Element.ArrayIndex().EqualTo(value))
                    {
                        return indexedStructArray.IndexedArray;
                    }

                    return Element.Map(indexedStructArray.IndexedArray, value);
                }
            }

            // Simply casts an IWorkshopTree to an IStructValue.
            private static IStructValue str(IWorkshopTree reference) => (IStructValue)reference;
        }

        class StructAttributes : TypeAttributes
        {
            readonly StructInstance _structInstance;
            int _stackLength = -1;

            public StructAttributes(StructInstance structInstance, bool parallel) : base(parallel, structInstance.Generics.Any(g => g.Attributes.ContainsGenerics), false) =>
                _structInstance = structInstance;

            public override int StackLength
            {
                get
                {
                    _structInstance.SetupContent();
                    return _stackLength;
                }
                set => _stackLength = value;
            }
        }

        class StructSemantics : TypeSemantics
        {
            readonly StructInstance _structInstance;

            public StructSemantics(StructInstance structInstance) => _structInstance = structInstance;

            public override void MakeUnsettable(DeltinScript deltinScript, VariableModifierGroup modifierGroup)
            {
                foreach (var variable in _structInstance.Variables)
                    // Ensures we do not stack overflow in case of recursive structs.
                    if (!modifierGroup.ContainsProvider(variable.Provider))
                        modifierGroup.MakeUnsettable(deltinScript, variable);
            }
        }
    }
}