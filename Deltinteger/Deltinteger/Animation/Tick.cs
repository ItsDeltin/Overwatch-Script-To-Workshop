using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Animation
{
    public class AnimationTick : IComponent
    {
        public DeltinScript DeltinScript { get; set; }
        private BaseAnimationInstanceType _objectType;
        private MeshInstanceType _meshType;
        private ArmatureInstanceType _armatureType;
        private ClassData _classData;
        /// <summary>An array of object references.</summary>
        private IndexReference _animationReferenceList;
        private IndexReference _animationInfoList;

        public AnimationTick() {}

        public void Init()
        {
            _objectType = DeltinScript.Types.GetInstance<BaseAnimationInstanceType>();
            _meshType = DeltinScript.Types.GetInstance<MeshInstanceType>();
            _armatureType = DeltinScript.Types.GetInstance<ArmatureInstanceType>();
            _classData = DeltinScript.GetComponent<ClassData>();

            _animationReferenceList = DeltinScript.VarCollection.Assign("animation_reference_list", true, false);
            _animationInfoList = DeltinScript.VarCollection.Assign("animation_info_list", true, false);
            DeltinScript.InitialGlobal.ActionSet.AddAction(_animationReferenceList.SetVariable(new V_EmptyArray()));
            DeltinScript.InitialGlobal.ActionSet.AddAction(_animationInfoList.SetVariable(new V_EmptyArray()));

            DeltinScript.WorkshopRules.Add(GetRule());
        }

        Rule GetRule()
        {
            var ruleBuilder = new TranslateRule(DeltinScript, "Animation -> Loop", RuleEvent.OngoingGlobal);

            // When _animationReferenceList.Length != 0
            ruleBuilder.Conditions.Add(new Condition(ReferenceListCount, Operators.NotEqual, 0));
            
            // Get the actions.
            Loop(ruleBuilder.ActionSet);

            return ruleBuilder.GetRule();
        }

        void Loop(ActionSet actionSet)
        {
            var referenceLoop = new ForRangeBuilder(actionSet, "animation_ref_loop", 0, ReferenceListCount, 1);
            referenceLoop.Init();
            Element currentReference = actionSet.AssignAndSave("animation_current_reference", _animationReferenceList.Get()[referenceLoop.Value]).Get();

            // Armature
            actionSet.AddAction(Element.Part<A_If>(_classData.IsInstanceOf(currentReference, _armatureType)));

            // Reset bone positions.
            _armatureType.BonePositions.Set(actionSet, currentReference, _armatureType.BoneInitialPositions.Get(currentReference));
            _armatureType.BoneLocalPositions.Set(actionSet, currentReference, _armatureType.BoneInitialPositions.Get(currentReference));

            // Loop through each bone
            var boneLoop = new ForRangeBuilder(actionSet, "animation_bone_loop", 0, Element.Part<V_CountOf>(_armatureType.BoneVertexLinks.Get(currentReference)), 1);
            boneLoop.Init();

            // Loop through each action.
            var actionLoop = new ForRangeBuilder(actionSet, "animation_action_loop", 0, Element.Part<V_CountOf>(_animationInfoList.Get()[referenceLoop.Value]), 1);
            actionLoop.Init();

            Element currentActionTime = _animationInfoList.Get()[referenceLoop.Value][actionLoop.Value][1];
            currentActionTime = actionSet.AssignAndSave("animation_current_action_time", currentActionTime).Get();
            Element currentActionTimeDelta = actionSet.AssignAndSave("animation_delta", new V_TotalTimeElapsed() - currentActionTime).Get();

            // Get the fcurve related to this bone.
            var fcurve = actionSet.AssignAndSave("animation_curve", Element.Part<V_FilteredArray>(
                // Get the fcurve array for the current object and action.
                _objectType.Actions.Get(currentReference) // The action array
                    [ // Getting a value in the action array will return an array of fcurves.
                        // Get the action index. The action index being used is stored in the _animationInfoList array.
                        // [reference loop index][action loop index][0 = gets real action index]
                        _animationInfoList.Get()[referenceLoop.Value][actionLoop.Value][0]
                    ],
                // Get the f-curve for the bone we are currently iterating on.
                Element.Part<V_And>(
                    // The Fcurve's type is BoneRotation.
                    new V_Compare(new V_ArrayElement()[0], Operators.Equal, (Element)(int)FCurveType.BoneRotation),
                    // The Fcurve's bone index is equal to the target bone.
                    new V_Compare(boneLoop.Value, Operators.Equal, new V_ArrayElement()[1])
                )
            )[0]); // FilteredArray will return an array with 1 value. Use [0] to convert from '[x]' to 'x'.

            // Ew yuck no curve
            actionSet.AddAction(Element.Part<A_SkipIf>(new V_Compare(fcurve.Get(), Operators.NotEqual, new V_Number(0)), new V_Number(1)));
            actionSet.AddAction(new A_Continue());

            // Now we get the A and B keyframes from the fcurve using the current time in the animation.
            // This will occur if all keyframes were surpassed.
            var keyframe_index = actionSet.AssignAndSave("animation_keyframe_index", Element.Part<V_FirstOf>(Element.Part<V_FilteredArray>(
                // We want the index of the keyframe, convert to a range of numbers.
                Element.Part<V_MappedArray>(fcurve.Get(), new V_CurrentArrayIndex()),
                Element.Part<V_And>(
                    // Ignore the first 2 elements, which is fcurve data.
                    new V_Compare(new V_ArrayElement(), Operators.GreaterThanOrEqual, new V_Number(2)),
                    new V_Compare(currentActionTimeDelta, Operators.LessThan, fcurve.Get()[new V_ArrayElement()][0])
                )
            )));

            // Keyframe A is fcurve[i - 1], keyframe B is furve[i].
            var keyframeA = actionSet.AssignAndSave("animation_keyframe_a", fcurve.Get()[Element.Part<V_Max>(keyframe_index.Get() - 1, (Element)2)]).Get(); // Save to variable if needed.
            var keyframeB = actionSet.AssignAndSave("animation_keyframe_b", fcurve.Get()[keyframe_index.Get()]).Get(); // Save to variable if needed.

            // TODO: Use this to determine if there is only one keyframe.
            // actionSet.AddAction(Element.Part<A_If>(new V_Compare(keyframe_index.Get(), Operators.LessThan, Element.Part<V_CountOf>(fcurve.Get()) - 1)));

            // Get the t of the keyframes depending on the current time.
            //  If the action was started at 5 seconds (s),
            //  and keyframe A is set at 2 (7) local seconds (a),
            //  and keyframe B is set at 6 (11) local seconds (b),
            //  and the current time is 9 (c),
            //  (c-s-a) / (b-a) = (9-5-2) / (6-2) = 0.5
            var c = actionSet.AssignAndSave("animation_test_current_time", new V_TotalTimeElapsed()).Get();
            // var t = actionSet.AssignAndSave("animation_t", Element.Part<V_Min>(Element.Part<V_Max>(new V_Number(0), (c - currentActionTime - keyframeA[0] / 60) / (keyframeB[0] / 60 - keyframeA[0] / 60)), new V_Number(1)));
            var t = actionSet.AssignAndSave("animation_t", Element.Part<V_Min>(Element.Part<V_Max>(new V_Number(0), (c - currentActionTime - keyframeA[0]) / (keyframeB[0] - keyframeA[0])), new V_Number(1)));

            // Get the interpolated rotation.
            var slerp = AnimationOperations.Slerp(
                actionSet,
                keyframeA[1], keyframeA[2], // Quaternion A is at keyframeA[1] for XYZ and keyframeA[2] for W.
                keyframeB[1], keyframeB[2], // Quaternion B is at keyframeB[1] for XYZ and keyframeB[2] for W.
                t.Get()
            );

            Element matrix = actionSet.AssignAndSave("animation_matrix", AnimationOperations.Create3x3MatrixFromQuaternion(slerp.V, slerp.W)).Get();

            // Set the current bone's decendant's bone points.
            // Loop through each descendent.
            var descendentLoop = new ForRangeBuilder(actionSet, "animation_descendents", 0, Element.Part<V_CountOf>(_armatureType.BoneDescendants.Get(currentReference)[boneLoop.Value]), 1);
            descendentLoop.Init();

            Element descendentIndex = actionSet.AssignAndSave("animation_descendent_index", _armatureType.BoneDescendants.Get(currentReference)[boneLoop.Value][descendentLoop.Value]).Get();
            Element originalPoint = actionSet.AssignAndSave("animation_rodrique_original", _armatureType.BonePositions.Get(currentReference)[descendentIndex]).Get();
            Element rodriqueResult = actionSet.AssignAndSave("animation_newpoint", AnimationOperations.MultiplyMatrix3x3AndVectorToVector(matrix, originalPoint)).Get();

            actionSet.AddAction(_armatureType.BonePositions.ArrayStore.SetVariable(
                // New point
                rodriqueResult,
                null, // Player
                // Index is the object reference + the descendent loop value.
                currentReference,
                descendentIndex
            ));

            Element parent = actionSet.AssignAndSave("animation_parent", _armatureType.BoneParents.Get(currentReference)[descendentIndex]).Get();

            // World positions
            actionSet.AddAction(_armatureType.BoneLocalPositions.ArrayStore.SetVariable(
                // Add local position to parent
                rodriqueResult // Local position
                    + Element.TernaryConditional(
                        // If the parent index is not equal to -1,
                        new V_Compare(parent, Operators.NotEqual, new V_Number(-1)),
                        // Add the parent's local position.
                        _armatureType.BoneLocalPositions.Get(currentReference)[parent],
                        // Otherwise, add zero.
                        V_Vector.Zero
                    ),
                null, // Player
                // Index is the object reference + the descendent loop value.
                currentReference,
                descendentIndex
            ));

            descendentLoop.Finish();
            actionLoop.Finish(); // End action loop
            boneLoop.Finish(); // End the bone loop
            actionSet.AddAction(new A_End()); // End armature
            referenceLoop.Finish(); // End object loop
            actionSet.AddAction(A_Wait.MinimumWait);
            actionSet.AddAction(A_Wait.MinimumWait);
            actionSet.AddAction(A_Wait.MinimumWait);
            actionSet.AddAction(new A_LoopIfConditionIsTrue());
        }

        public void AddAnimation(ActionSet actionSet, Element objectReference, Element actionIdentifier)
        {
            Element actionIndex = actionSet.AssignAndSave("animation_action_index", Element.Part<V_IndexOfArrayValue>(_objectType.ActionNames.Get(objectReference), actionIdentifier)).Get();

            var objectIndexInAnimationList = actionSet.AssignAndSave("animation_existing_obj_push", Element.Part<V_IndexOfArrayValue>(_animationReferenceList.Get(), objectReference));
            
            // If the _animationList array does not contain the object reference, add it to the list.
            actionSet.AddAction(Element.Part<A_If>(new V_Compare(objectIndexInAnimationList.Get(), Operators.Equal, new V_Number(-1))));

            // Empty index.
            actionSet.AddAction(_animationInfoList.SetVariable(new V_EmptyArray(), index: ReferenceListCount));
            
            // Push the objectReference.
            actionSet.AddAction(_animationReferenceList.ModifyVariable(Operation.AppendToArray, objectReference));

            // End the if
            actionSet.AddAction(new A_End());

            // [i, t]
            actionSet.AddAction(_animationInfoList.ModifyVariable(Operation.AppendToArray, Element.CreateArray(Element.CreateArray(actionIndex, new V_TotalTimeElapsed())), index: objectIndexInAnimationList.Get()));
        }

        Element ReferenceListCount => Element.Part<V_CountOf>(_animationReferenceList.Get());
    }
}