using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

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
                            position + armatureType.BoneLocalPositions.Get(armatureInstance)[boneData.Tail],
                            EnumData.GetEnumValue(color),
                            EnumData.GetEnumValue(EffectRev.VisibleToPositionAndRadius)
                        ));
                    }
                    
                    return null;
                }
            };
        }
    }
}