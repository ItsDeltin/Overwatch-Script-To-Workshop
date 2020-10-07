using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.FunctionBuilder;

namespace Deltin.Deltinteger.Animation
{
    class AnimationObjectBuilder : IGroupDeterminer, ISubroutineContext, IFunctionLookupTable
    {
        public BlendObject Object { get; }
        private readonly BlendFile _file;
        private readonly DeltinScript _deltinScript;
        private SubroutineInfo _subroutineInfo;

        public AnimationObjectBuilder(DeltinScript deltinScript, BlendFile file, BlendObject blendObject)
        {
            Object = blendObject;
            _file = file;
            _deltinScript = deltinScript;
        }

        // IGroupDeterminer
        public NewRecursiveStack GetExistingRecursiveStack(List<NewRecursiveStack> stack) => throw new NotImplementedException();
        public IFunctionLookupTable GetLookupTable() => this;
        public object GetStackIdentifier() => throw new NotImplementedException();
        public SubroutineInfo GetSubroutineInfo()
        {
            if (_subroutineInfo == null)
            {
                var subroutineBuilder = new SubroutineBuilder(_deltinScript, this);
                subroutineBuilder.SetupSubroutine();
            }
            return _subroutineInfo;
        }
        public string GroupName() => Object.Name;
        public bool IsObject() => false;
        public bool IsRecursive() => false;
        public bool IsSubroutine() => true;
        public bool IsVirtual() => false;
        public bool MultiplePaths() => false;
        public bool ReturnsValue() => true;
        public IParameterHandler[] Parameters() => new IParameterHandler[0];

        // ISubroutineContext
        public string RuleName() => "Animation -> Create Instance -> " + Object.Name;
        public string ElementName() => Object.Name;
        public string ThisArrayName() => Object.Name;
        public bool VariableGlobalDefault() => true;
        public IGroupDeterminer GetDeterminer() => this;
        public CodeType ContainingType() => null;
        public void SetSubroutineInfo(SubroutineInfo subroutineInfo) => _subroutineInfo = subroutineInfo;
        public void Finish(Rule rule) {}

        // IFunctionLookupTable
        public void Build(FunctionBuildController builder)
        {
            IWorkshopTree result = null;

            // Mesh
            if (Object is BlendMesh mesh)
                result = GetMesh(builder.ActionSet);
            // Armature
            else if (Object is BlendArmature armature)
                result = GetArmature(builder.ActionSet);
            
            builder.ActionSet.ReturnHandler.ReturnValue(result);
        }

        IWorkshopTree GetMesh(ActionSet actionSet)
        {
            BlendMesh mesh = (BlendMesh)Object;

            // Get the type.
            var type = actionSet.Translate.DeltinScript.Types.GetInstance<MeshInstanceType>();
            // Create the reference.
            var reference = type.Create(actionSet, actionSet.Translate.DeltinScript.GetComponent<ClassData>());

            SharedSetup(type, actionSet, reference.Get());

            // Set the vertex array.
            type.Vertices.Set(actionSet, reference.Get(), BlendStructureHelper.VerticesToElement(mesh.Vertices));

            return reference.Get();
        }

        IWorkshopTree GetArmature(ActionSet actionSet)
        {
            BlendArmature armature = (BlendArmature)Object;

            // Get the type.
            var type = actionSet.Translate.DeltinScript.Types.GetInstance<ArmatureInstanceType>();
            // Create the reference.
            var reference = type.Create(actionSet, actionSet.Translate.DeltinScript.GetComponent<ClassData>());

            SharedSetup(type, actionSet, reference.Get());

            // Get the bone data.
            var boneStructure = new BoneStructure(_file, armature);

            type.BoneVertexLinks.Set(actionSet, reference.Get(), boneStructure.GetBoneVertexData()); // Set the vertex links.
            type.BoneDescendants.Set(actionSet, reference.Get(), boneStructure.GetBoneDescendents()); // Set the descendants.
            type.BoneInitialPositions.Set(actionSet, reference.Get(), boneStructure.GetInitialBonePositions()); // Set the initial bone positions.
            type.BonePositions.Set(actionSet, reference.Get(), type.BoneInitialPositions.Get(reference.Get())); // Set the current bone positions to the initial positions array.
            
            return reference.Get();
        }

        void SharedSetup(BaseObjectExtender type, ActionSet actionSet, Element reference)
        {
            type.Actions.Set(actionSet, reference, BlendStructureHelper.GetActions(Object));
            type.ActionNames.Set(actionSet, reference, BlendStructureHelper.GetActionNames(new AnimationAction[] { Object.AnimationData }));
        }
    }
}