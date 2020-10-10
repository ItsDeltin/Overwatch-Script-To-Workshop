using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Animation
{
    public class BaseAnimationInstanceType : ClassType
    {
        public ObjectVariable Location { get; private set; }
        public ObjectVariable Children { get; private set; }
        public ObjectVariable ActionNames { get; private set; }
        public ObjectVariable Actions { get; private set; }
        
        public BaseAnimationInstanceType() : base("AnimationObject")
        {
            Description = "The base class for animation objects.";
        }

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();

            Location = AddPrivateObjectVariable();
            Children = AddPrivateObjectVariable();
            ActionNames = AddPrivateObjectVariable();
            Actions = AddPrivateObjectVariable();
        }
    }

    public abstract class BaseObjectExtender : ClassType
    {
        protected readonly Scope _scope = new Scope();

        protected BaseObjectExtender(DeltinScript deltinScript, string name) : base(name)
        {
            Inherit(deltinScript.Types.GetInstance<BaseAnimationInstanceType>());
            _scope.AddNativeMethod(new PlayAnimationFunction());
        }

        public override Scope GetObjectScope() => _scope;

        public ObjectVariable Location => ((BaseAnimationInstanceType)Extends).Location;
        public ObjectVariable Children => ((BaseAnimationInstanceType)Extends).Children;
        public ObjectVariable ActionNames => ((BaseAnimationInstanceType)Extends).ActionNames;
        public ObjectVariable Actions => ((BaseAnimationInstanceType)Extends).Actions;
    }

    public class MeshInstanceType : BaseObjectExtender
    {
        public ObjectVariable Vertices { get; private set; }

        public MeshInstanceType(DeltinScript deltinScript) : base(deltinScript, "AnimationMesh") {}

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();

            Vertices = AddObjectVariable(new InternalVar("Vertices"));
        }
    }

    public class ArmatureInstanceType : BaseObjectExtender
    {
        public ObjectVariable BoneVertexLinks { get; private set; }
        public ObjectVariable BoneDescendants { get; private set; }
        /// <summary>The initial bone positions relative to the bone's parent.</summary>
        public ObjectVariable BoneInitialPositions { get; private set; }
        /// <summary>The current bone position relative to the bone's parent.</summary>
        public ObjectVariable BonePositions { get; private set; }
        /// <summary>The current bone position relative to the bone's armature. If unused, this can be removed after debugging.</summary>
        public ObjectVariable BoneLocalPositions { get; private set; }
        /// <summary>The bone's parents.</summary>
        public ObjectVariable BoneParents { get; private set; }

        public ArmatureInstanceType(DeltinScript deltinScript) : base(deltinScript, "AnimationArmature") {}

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();

            BoneVertexLinks = AddPrivateObjectVariable();
            BoneDescendants = AddPrivateObjectVariable();
            BoneInitialPositions = AddPrivateObjectVariable();
            BonePositions = AddPrivateObjectVariable();
            BoneLocalPositions = AddPrivateObjectVariable();
            BoneParents = AddPrivateObjectVariable();
        }
    }
}