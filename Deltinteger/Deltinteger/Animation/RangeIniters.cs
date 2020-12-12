using System;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Animation
{
    public static class AnimationRangeIniterFunctions
    {
        public static FuncMethod ArmatureBones(DeltinScript deltinScript, BlendFile file, BlendArmature armature)
        {
            var armatureType = deltinScript.Types.GetInstance<ArmatureInstanceType>();
            return new FuncMethodBuilder() {
                Name = "ShowBoneDebug",
                Documentation = "",
                Parameters = new CodeParameter[] {
                    new CodeParameter("position"),
                    new CodeParameter("armature_instance", armatureType)
                },
                Action = (actionSet, methodCall) => {
                    var position = methodCall.Get(0);
                    var armatureInstance = methodCall.Get(1);
                    var boneStructure = new BoneStructure(file, armature);

                    object type = BeamType.GrappleBeam;
                    object color = Color.Red;

                    foreach (var boneData in boneStructure._boneData)
                    {
                        actionSet.AddAction(Element.Part<A_CreateBeamEffect>(
                            new V_AllPlayers(),
                            EnumData.GetEnumValue(type),
                            position + armatureType.BoneLocalPositions.Get(armatureInstance)[boneData.Head],
                            position + armatureType.BoneLocalPositions.Get(armatureInstance)[boneData.Tail] + new V_Vector(0.001, 0, 0),
                            Element.Part<V_ColorValue>(EnumData.GetEnumValue(color)),
                            EnumData.GetEnumValue(EffectRev.VisibleToPositionAndRadius)
                        ));
                    }
                    
                    return null;
                }
            };
        }

        public static FuncMethod ArmatureBoneHeadByName(DeltinScript deltinScript, BlendFile file, BlendArmature armature)
        {
            var armatureType = deltinScript.Types.GetInstance<ArmatureInstanceType>();
            return new FuncMethodBuilder() {
                Name = "BoneHeadFromName",
                Documentation = "Gets the location of a bone's head by its name.",
                Parameters = new CodeParameter[] {
                    new CodeParameter("armatureInstance", "An initialized armature.", armatureType),
                    new BoneParameter(armature, "boneName", "The name of the bone.")
                },
                Action = (actionSet, methodCall) => {
                    var boneStructure = new BoneStructure(file, armature);
                    var armatureInstance = methodCall.Get(0);
                    var bone = (Bone)methodCall.AdditionalParameterData[1];
                    var boneData = boneStructure._boneData[Array.IndexOf(armature.Bones, bone)];
                    
                    return armatureType.BoneLocalPositions.Get(armatureInstance)[boneData.Head];
                }
            };
        }

        public static FuncMethod ArmatureBoneTailByName(DeltinScript deltinScript, BlendFile file, BlendArmature armature)
        {
            var armatureType = deltinScript.Types.GetInstance<ArmatureInstanceType>();
            return new FuncMethodBuilder() {
                Name = "BoneTailFromName",
                Documentation = "Gets the location of a bone's tail by its name.",
                Parameters = new CodeParameter[] {
                    new CodeParameter("armatureInstance", "An initialized armature.", armatureType),
                    new BoneParameter(armature, "boneName", "The name of the bone.")
                },
                Action = (actionSet, methodCall) => {
                    var boneStructure = new BoneStructure(file, armature);
                    var armatureInstance = methodCall.Get(0);
                    var bone = (Bone)methodCall.AdditionalParameterData[1];
                    var boneData = boneStructure._boneData[Array.IndexOf(armature.Bones, bone)];

                    return armatureType.BoneLocalPositions.Get(armatureInstance)[boneData.Tail];
                }
            };
        }

        public static FuncMethod ArmatureEmptyByName(DeltinScript deltinScript, BlendFile file, BlendArmature armature)
        {
            var armatureType = deltinScript.Types.GetInstance<ArmatureInstanceType>();
            return new FuncMethodBuilder() {
                Name = "EmptyFromName",
                Documentation = "Gets the location of an empty by its name.",
                Parameters = new CodeParameter[] {
                    new CodeParameter("armatureInstance", "An initialized armature.", armatureType),
                    new EmptyParameter(armature, "emptyName", "The name of the empty.")
                },
                Action = (actionSet, methodCall) => {
                    var boneStructure = new BoneStructure(file, armature);
                    var armatureInstance = methodCall.Get(0);
                    var empty = (BoneEmpty)methodCall.AdditionalParameterData[1];
                    var emptyData = boneStructure.Empties.First(e => e.Original == empty);

                    return armatureType.BoneLocalPositions.Get(armatureInstance)[emptyData.Point];
                }
            };
        }

        class BoneParameter : ConstStringParameter
        {
            private readonly BlendArmature _armature;

            public BoneParameter(BlendArmature armature, string name, string documentation) : base(name, documentation)
            {
                _armature = armature;
            }

            public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
            {
                string boneName = (string)base.Validate(parseInfo, value, valueRange);

                foreach (var bone in _armature.Bones)
                    if (bone.Name == boneName)
                        return bone;
                
                parseInfo.Script.Diagnostics.Error("The bone '" + boneName + "' does not exist in the '" + _armature.Name + "' armature.", valueRange);
                return null;
            }
        }

        class EmptyParameter : ConstStringParameter
        {
            private readonly BlendArmature _armature;

            public EmptyParameter(BlendArmature armature, string name, string documentation) : base(name, documentation)
            {
                _armature = armature;
            }

            public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
            {
                string emptyName = (string)base.Validate(parseInfo, value, valueRange);

                foreach (var empty in _armature.Empties)
                    if (empty.Name == emptyName)
                        return empty;
                
                parseInfo.Script.Diagnostics.Error("The bone '" + emptyName + "' does not exist in the '" + _armature.Name + "' armature.", valueRange);
                return null;
            }
        }
    }
}