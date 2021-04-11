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
        public IVariableInstance[] Variables { get; private set; }
        public IMethod[] Methods { get; private set; }
        protected Scope ObjectScope { get; }
        protected Scope StaticScope { get; }
        private bool _isReady;

        public StructInstance(IStructProvider provider, InstanceAnonymousTypeLinker genericsLinker) : base(provider.Name)
        {
            ObjectScope = new Scope("struct " + Name);
            Attributes = new TypeAttributes(true, Generics.Any(g => g.Attributes.ContainsGenerics));

            provider.OnReady.OnReady(() => {
                // Variables
                Variables = new IVariableInstance[provider.Variables.Length];
                for (int i = 0; i < Variables.Length; i++)
                {
                    Variables[i] = provider.Variables[i].GetInstance(genericsLinker);
                    ObjectScope.AddNativeVariable(Variables[i]);
                }

                // Functions
                foreach (var method in provider.Methods)
                    method.AddInstance(this, genericsLinker);

                _isReady = true;
            });

            ArrayHandler = this;
        }

        public override bool Is(CodeType other)
        {
            ThrowIfNotReady();

            if (Name != other.Name || Generics.Length != other.Generics.Length)
                return false;

            for (int i = 0; i < Generics.Length; i++)
                if (!Generics[i].Is(other.Generics[i]))
                    return false;

            return true;
        }

        public override bool Implements(CodeType type)
        {
            ThrowIfNotReady();

            foreach(var utype in type.UnionTypes())
            {
                if (!(utype is StructInstance other && other.Variables.Length == Variables.Length && Generics.Length == other.Generics.Length))
                    continue;
                
                bool structVariablesMatch = true;
            
                for (int i = 0; i < Variables.Length; i++)
                {
                    var matchingVariable = other.Variables.FirstOrDefault(v => Variables[i].Name == v.Name);
                    if (matchingVariable == null || !((CodeType)Variables[i].CodeType).Implements((CodeType)matchingVariable.CodeType))
                    {
                        structVariablesMatch = false;
                        break;
                    }
                }

                return structVariablesMatch;
            }
            return false;
        }

        public override Scope GetObjectScope()
        {
            ThrowIfNotReady();
            return ObjectScope;
        }

        public override void AddObjectVariablesToAssigner(ToWorkshop toWorkshop, IWorkshopTree reference, VarIndexAssigner assigner)
        {
            var structValue = (IStructValue)reference;

            foreach (var variable in Variables)
                assigner.Add(variable.Provider, structValue.GetGettable(variable.Name));
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
            => GetGettableAssigner(null).GetValue(new GettableAssignerValueInfo(actionSet)).GetVariable();

        public override CompletionItem GetCompletion() => throw new System.NotImplementedException();
        void ThrowIfNotReady()
        {
            if (!_isReady) throw new Exception("You are but a fool.");
        }

        public override IGettableAssigner GetGettableAssigner(IVariable variable) => new StructAssigner(this, ((Var)variable), false);

        IGettableAssigner ITypeArrayHandler.GetArrayAssigner(IVariable variable) => new StructAssigner(this, ((Var)variable), true);
        void ITypeArrayHandler.OverrideArray(ArrayType array) {}
        ArrayFunctionHandler ITypeArrayHandler.GetFunctionHandler() => new StructArrayFunctionHandler();

        void IScopeAppender.AddObjectBasedScope(IMethod function) => ObjectScope.AddNativeMethod(function);
        void IScopeAppender.AddStaticBasedScope(IMethod function) => StaticScope.AddNativeMethod(function);
        void IScopeAppender.AddObjectBasedScope(IVariableInstance variable) => ObjectScope.AddNativeVariable(variable);
        void IScopeAppender.AddStaticBasedScope(IVariableInstance variable) => StaticScope.AddNativeVariable(variable);

        // Overrides default array function implementation to support structs, since the default won't work with parallel variables.
        class StructArrayFunctionHandler : ArrayFunctionHandler
        {
            // Since all variables in a struct array will be of the same length, we can use BridgeArbritrary instead.
            public override IWorkshopTree Length(IWorkshopTree reference) => str(reference).BridgeArbritrary(v => Element.CountOf(v)).GetWorkshopValue();
            public override IWorkshopTree FirstOf(IWorkshopTree reference) => str(reference).Bridge(v => Element.FirstOf(v));
            public override IWorkshopTree LastOf(IWorkshopTree reference) => str(reference).Bridge(v => Element.LastOf(v));
            public override IWorkshopTree Contains(IWorkshopTree reference, IWorkshopTree value)
            {
                // Extract the values from both structs.
                var arrayValues = str(reference).GetAllValues();
                var contains = str(value).GetAllValues();

                if (arrayValues.Length != contains.Length)
                    throw new Exception("Lengths of struct pair do not match.");

                // The list of struct comparisons.
                var comparisons = new Queue<IWorkshopTree>();

                // Add the comparisons to the queue.
                for (int i = 0; i < arrayValues.Length; i++)
                {
                    // Later in this function, 'Is True For Any's array is arrayValues[0], so for the first struct value, just use Array Element.
                    // Otherwise, it will result in 'Is True For Any(x, x[Array Index] == ...' when 'Array Element' would suffice for 'x[Array Index]'
                    var compareFrom = i == 0 ?
                        Element.ArrayElement() :
                        Element.ValueInArray(arrayValues[i], Element.ArrayIndex());

                    comparisons.Enqueue(Element.Compare(compareFrom, Operator.Equal, contains[i]));
                }

                // Convert the list of comparisons into an && sequence.
                var condition = comparisons.Dequeue();
                while (comparisons.Count > 0)
                    condition = Element.And(condition, comparisons.Dequeue());

                return Element.Any(arrayValues[0], condition);
            }
            public override ISortFunctionExecutor SortedArray() => new StructSortExecutorReturnsArray(false);
            public override ISortFunctionExecutor FilteredArray() => new StructSortExecutorReturnsArray(true);
            public override ISortFunctionExecutor All() => new StructSortExecutorReturnsBoolean();
            public override ISortFunctionExecutor Any() => new StructSortExecutorReturnsBoolean();

            // For struct array iterators that returns a struct array (Filtered Array, Sorted Array).
            private class StructSortExecutorReturnsArray : ISortFunctionExecutor
            {
                // If the user does something like array.SortedArray(...).Length, we can just optimize the SortedArray away.
                readonly bool _operationModifiesLength;

                public StructSortExecutorReturnsArray(bool operationModifiesLength)
                {
                    _operationModifiesLength = operationModifiesLength;
                }

                public IWorkshopTree GetResult(
                    string function,
                    ActionSet actionSet,
                    Func<IWorkshopTree, IWorkshopTree> invoke)
                {
                    // Extract the struct array from the action set's current object.
                    var currentStructArray = (IStructValue)actionSet.CurrentObject;

                    if (currentStructArray is IndexedStructArray appendToExistingNode)
                    {
                        appendToExistingNode.AppendModification(args => Element.Part(
                            function,
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
                        function,
                        Element.Map(IStructValue.ExtractArbritraryValue(currentStructArray), Element.ArrayIndex()),
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
                public IWorkshopTree GetResult(
                    string function,
                    ActionSet actionSet,
                    Func<IWorkshopTree, IWorkshopTree> invoke)
                {
                    // Extract the struct array from the action set's current object.
                    var currentStructArray = (IStructValue)actionSet.CurrentObject;

                    if (currentStructArray is IndexedStructArray appendToExistingNode)
                        return Element.Part(
                            function,
                            appendToExistingNode.IndexedArray,
                            invoke(appendToExistingNode.StructArray)
                        );

                    // Step into the struct array with the current array index.
                    var arrayed = new ValueInStructArray(currentStructArray, Element.ArrayIndex());

                    // Return the workshop function.
                    return Element.Part(
                        function,
                        IStructValue.ExtractArbritraryValue(currentStructArray),
                        invoke(arrayed));
                }
            }
        
            // Simply casts an IWorkshopTree to an IStructValue.
            private static IStructValue str(IWorkshopTree reference) => (IStructValue)reference;
        }
    }
}