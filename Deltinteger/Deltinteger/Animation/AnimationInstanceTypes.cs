using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

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
            _scope.AddNativeMethod(AnimationPlayFunctions.Play);
            _scope.AddNativeMethod(AnimationPlayFunctions.Stop);
            _scope.AddNativeMethod(AnimationPlayFunctions.StopAll);
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
        public ObjectVariable BonePointParents { get; private set; }
        public ObjectVariable BoneNames { get; private set; }
        public ObjectVariable BoneMatrices { get; private set; }
        /// <summary>The bone a point is assigned to.</summary>
        public ObjectVariable BonePointsBone { get; private set; }
        public ObjectVariable BoneRootPoint { get; private set; }

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
            BonePointParents = AddPrivateObjectVariable();
            BoneNames = AddPrivateObjectVariable();
            BoneMatrices = AddPrivateObjectVariable();
            BonePointsBone = AddPrivateObjectVariable();
            BoneRootPoint = AddPrivateObjectVariable();
        }

        public void Init(ActionSet actionSet, Element reference, BoneStructure boneStructure)
        {
            BoneVertexLinks.Set(actionSet, reference, boneStructure.GetBoneVertexData()); // Set the vertex links.
            BoneDescendants.Set(actionSet, reference, boneStructure.GetBoneDescendents()); // Set the descendants.
            BoneInitialPositions.Set(actionSet, reference, boneStructure.GetInitialBonePositions()); // Set the initial bone positions.
            BonePositions.Set(actionSet, reference, BoneInitialPositions.Get(reference)); // Set the current bone positions to the initial positions array.
            BoneLocalPositions.Set(actionSet, reference, boneStructure.GetLocalArmaturePositions());
            BoneParents.Set(actionSet, reference, boneStructure.GetParents());
            BonePointParents.Set(actionSet, reference, boneStructure.GetPointParents());
            BoneNames.Set(actionSet, reference, boneStructure.GetNameArray());
            BoneMatrices.Set(actionSet, reference, boneStructure.GetBoneMatrices());
            BonePointsBone.Set(actionSet, reference, boneStructure.GetBonePointsBone());
            BoneRootPoint.Set(actionSet, reference, boneStructure.GetRootBonePoint());
        }
    }
}