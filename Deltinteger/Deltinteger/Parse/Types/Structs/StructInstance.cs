using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class StructInstance : CodeType, IAdditionalArray
    {
        public IVariableInstance[] Variables { get; }
        private readonly StructInitializer _provider;

        public StructInstance(StructInitializer provider, InstanceAnonymousTypeLinker genericsLinker) : base(provider.Name)
        {
            _provider = provider;

            Variables = new IVariableInstance[provider.Variables.Count];
            for (int i = 0; i < Variables.Length; i++)
                Variables[i] = provider.Variables[i].GetInstance(genericsLinker);
        }

        public override IGettableAssigner GetGettableAssigner(IVariable variable) => new StructAssigner(this);
        public IGettableAssigner GetArrayAssigner(IVariable variable) => new StructAssigner(this);
        public void OverrideArray(ArrayType array) {}
        public override CompletionItem GetCompletion() => throw new System.NotImplementedException();
    }
}