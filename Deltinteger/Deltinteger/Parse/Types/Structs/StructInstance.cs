using System;
using System.Linq;
using Deltin.Deltinteger.Elements;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class StructInstance : CodeType, IAdditionalArray, IScopeAppender
    {
        public IVariableInstance[] Variables { get; private set; }
        public IMethod[] Methods { get; private set; }
        protected Scope ObjectScope { get; }
        protected Scope StaticScope { get; }
        private bool _isReady;

        public StructInstance(IStructProvider provider, InstanceAnonymousTypeLinker genericsLinker) : base(provider.Name)
        {
            ObjectScope = new Scope("struct " + Name);

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

        IGettableAssigner IAdditionalArray.GetArrayAssigner(IVariable variable) => new StructAssigner(this, ((Var)variable), true);
        void IAdditionalArray.OverrideArray(ArrayType array) {}
        ArrayFunctionHandler IAdditionalArray.GetFunctionHandler() => new StructArrayFunctionHandler();

        void IScopeAppender.AddObjectBasedScope(IMethod function) => ObjectScope.AddNativeMethod(function);
        void IScopeAppender.AddStaticBasedScope(IMethod function) => StaticScope.AddNativeMethod(function);
        void IScopeAppender.AddObjectBasedScope(IVariableInstance variable) => ObjectScope.AddNativeVariable(variable);
        void IScopeAppender.AddStaticBasedScope(IVariableInstance variable) => StaticScope.AddNativeVariable(variable);

        class StructArrayFunctionHandler : ArrayFunctionHandler
        {
            public override void AssignLength(IVariable lengthVariable, VarIndexAssigner assigner, IWorkshopTree reference)
                => assigner.Add(lengthVariable, ((IStructValue)reference).BridgeArbritrary(v => Element.CountOf(v)).GetWorkshopValue());

            public override void AssignFirstOf(IVariable firstOfVariable, VarIndexAssigner assigner, IWorkshopTree reference)
                => assigner.Add(firstOfVariable, ((IStructValue)reference).Bridge(v => Element.FirstOf(v)));

            public override void AssignLastOf(IVariable lastOfVariable, VarIndexAssigner assigner, IWorkshopTree reference)
                => assigner.Add(lastOfVariable, ((IStructValue)reference).Bridge(v => Element.LastOf(v)));

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
        }
    }
}