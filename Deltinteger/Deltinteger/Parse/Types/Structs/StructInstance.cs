using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class StructInstance : CodeType, IAdditionalArray
    {
        public IVariableInstance[] Variables { get; }
        private readonly StructInitializer _provider;
        private readonly IGettableAssigner _assigner;

        public StructInstance(StructInitializer provider, InstanceAnonymousTypeLinker genericsLinker) : base(provider.Name)
        {
            _provider = provider;

            Variables = new IVariableInstance[provider.Variables.Count];
            for (int i = 0; i < Variables.Length; i++)
                Variables[i] = provider.Variables[i].GetInstance(genericsLinker);
            
            _assigner = new StructAssigner(this);
        }

        public override bool Is(CodeType other)
        {
            if (Name != other.Name || (Generics == null) != (other.Generics == null))
                return false;
            
            if (Generics != null)
            {
                if (Generics.Length != other.Generics.Length)
                    return false;

                for (int i = 0; i < Generics.Length; i++)
                    if (!Generics[i].Is(other.Generics[i]))
                        return false;
            }
            
            return true;
        }

        public override IWorkshopTree New(ActionSet actionSet, Constructor constructor, IWorkshopTree[] constructorValues, object[] additionalParameterData)
            => _assigner.GetValue(new GettableAssignerValueInfo(actionSet)).GetVariable();

        public override IGettableAssigner GetGettableAssigner(IVariable variable) => _assigner;
        IGettableAssigner IAdditionalArray.GetArrayAssigner(IVariable variable) => _assigner;
        void IAdditionalArray.OverrideArray(ArrayType array) {}
        public override CompletionItem GetCompletion() => throw new System.NotImplementedException();
    }
}