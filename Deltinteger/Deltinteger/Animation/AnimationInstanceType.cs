using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Animation
{
    public class BaseAnimationInstanceType : ClassType
    {
        public ObjectVariable Location { get; private set; }
        public ObjectVariable Children { get; private set; }
        
        public BaseAnimationInstanceType() : base("AnimationObject")
        {
            Description = "The base class for animation objects.";
        }

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();

            Location = AddObjectVariable(new InternalVar("Location"));
            Children = AddObjectVariable(new InternalVar("Children"));
        } 
    }

    public abstract class BaseObjectExtender : ClassType
    {
        protected BaseObjectExtender(DeltinScript deltinScript, string name) : base(name)
        {
            Inherit(deltinScript.Types.GetInstance<BaseAnimationInstanceType>());
        }

        public ObjectVariable Location => ((BaseAnimationInstanceType)Extends).Location;
        public ObjectVariable Children => ((BaseAnimationInstanceType)Extends).Children;
    }

    public class MeshInstanceType : BaseObjectExtender
    {
        public ObjectVariable Vertices { get; private set; }

        public MeshInstanceType(DeltinScript deltinScript) : base(deltinScript, "AnimationMesh") {}

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();

            Vertices = new ObjectVariable(new InternalVar("Vertices"));
        }
    }

    public class ArmatureInstanceType : BaseObjectExtender
    {
        public ArmatureInstanceType(DeltinScript deltinScript) : base(deltinScript, "AnimationArmature") {}

        public override void ResolveElements()
        {
            if (elementsResolved) return;
            base.ResolveElements();
        }
    }
}