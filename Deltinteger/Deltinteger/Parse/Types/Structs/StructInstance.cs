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
        void IAdditionalArray.AssignLength(IVariable lengthVariable, VarIndexAssigner assigner, IWorkshopTree reference)
            => assigner.Add(lengthVariable, new BridgeGetStructValue((IStructValue)reference, v => Element.CountOf(v)).GetWorkshopValue());
        // {
        //     while (reference is IStructValue structValue)
        //         reference = structValue.GetArbritraryValue();

        //     assigner.Add(lengthVariable, reference);
        // }

        void IAdditionalArray.AssignFirstOf(IVariable firstOfVariable, VarIndexAssigner assigner, IWorkshopTree reference)
            => assigner.Add(firstOfVariable, new BridgeGetStructValue((IStructValue)reference, v => Element.FirstOf(v)));

        void IAdditionalArray.AssignLastOf(IVariable lastOfVariable, VarIndexAssigner assigner, IWorkshopTree reference)
            => assigner.Add(lastOfVariable, new BridgeGetStructValue((IStructValue)reference, v => Element.LastOf(v)));

        void IScopeAppender.AddObjectBasedScope(IMethod function) => ObjectScope.AddNativeMethod(function);
        void IScopeAppender.AddStaticBasedScope(IMethod function) => StaticScope.AddNativeMethod(function);
        void IScopeAppender.AddObjectBasedScope(IVariableInstance variable) => ObjectScope.AddNativeVariable(variable);
        void IScopeAppender.AddStaticBasedScope(IVariableInstance variable) => StaticScope.AddNativeVariable(variable);
    }
}