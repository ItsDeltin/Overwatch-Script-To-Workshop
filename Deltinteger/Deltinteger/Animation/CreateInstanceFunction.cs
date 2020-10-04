using Deltin.Deltinteger.Parse;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Animation
{
    public class CreateAnimationObjectInstance : IMethod
    {
        public string Name => "CreateInstance";
        public string Documentation => "Creates an instance of the mesh or armature.";
        public CodeType ReturnType => _type;
        public bool DoesReturnValue => true;
        public bool Static => false;
        public bool WholeContext => true;
        public LanguageServer.Location DefinedAt => null;
        public AccessLevel AccessLevel => AccessLevel.Public;
        public MethodAttributes Attributes { get; } = new MethodAttributes();
        public CodeParameter[] Parameters { get; } = new CodeParameter[0];
        private readonly BlendObject _object;
        private readonly CodeType _type;

        public CreateAnimationObjectInstance(DeltinScript deltinScript, BlendObject blendObject)
        {
            _object = blendObject;
            if (blendObject is BlendMesh)
                _type = deltinScript.Types.GetInstance<MeshInstanceType>();
            else
                _type = deltinScript.Types.GetInstance<ArmatureInstanceType>();
        }

        public CompletionItem GetCompletion() => MethodAttributes.GetFunctionCompletion(this);
        public string GetLabel(bool markdown) => IMethod.GetLabel(this, true);

        public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            throw new System.NotImplementedException();
        }
    }
}