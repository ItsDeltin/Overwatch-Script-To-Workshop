using Deltin.Deltinteger.Parse;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;

namespace Deltin.Deltinteger.Animation
{
    class RootAnimationType : CodeType
    {
        private readonly Scope _scope = new Scope("animation root");
        private readonly AnimationObjectVariable[] _sceneObjects;

        public RootAnimationType(DeltinScript deltinScript, BlendFile blendFile) : base("animation root")
        {
            _sceneObjects = new AnimationObjectVariable[blendFile.Objects.Length];
            for (int i = 0; i < _sceneObjects.Length; i++)
            {
                _sceneObjects[i] = new AnimationObjectVariable(blendFile.Objects[i], new AnimationObjectType(deltinScript, blendFile, blendFile.Objects[i]));
                _scope.AddNativeVariable(_sceneObjects[i]);
            }
        }

        public override void AddObjectVariablesToAssigner(IWorkshopTree reference, VarIndexAssigner assigner)
        {
            foreach (var sceneObject in _sceneObjects)
                assigner.Add(sceneObject, new Elements.V_Null());
        }

        public override Scope GetObjectScope() => _scope;
        public override bool IsConstant() => true;
        public override CompletionItem GetCompletion() => throw new System.NotImplementedException();
        public override Scope ReturningScope() => throw new System.NotImplementedException();
    }

    class AnimationObjectType : CodeType
    {
        public BlendObject Object { get; }
        private readonly BlendFile _blendFile;
        private readonly Scope _scope;

        public AnimationObjectType(DeltinScript deltinScript, BlendFile blendFile, BlendObject obj) : base("animation " + (obj is BlendArmature ? "armature" : "mesh"))
        {
            Object = obj;
            _blendFile = blendFile;
            _scope = new Scope("object " + obj.Name);
            _scope.AddNativeMethod(new CreateAnimationObjectInstance(deltinScript, blendFile, Object));

            // Armature-specific functions.
            if (obj is BlendArmature armature)
                _scope.AddNativeMethod(AnimationRangeIniterFunctions.ArmatureBones(deltinScript, blendFile, armature));
        }

        public override Scope GetObjectScope() => _scope;
        public override bool IsConstant() => true;
        public override Scope ReturningScope() => throw new System.NotImplementedException();
        public override CompletionItem GetCompletion() => throw new System.NotImplementedException();
    }

    class AnimationObjectVariable : IndexReferencer
    {
        public BlendObject BlendObject { get; }

        public AnimationObjectVariable(BlendObject blendObject, AnimationObjectType type) : base(blendObject.Name)
        {
            BlendObject = blendObject;
            CodeType = type;
        }
    }
}