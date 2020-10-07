using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Animation
{
    public class AnimationTick : IComponent
    {
        public DeltinScript DeltinScript { get; set; }
        private BaseAnimationInstanceType _objectType;
        /// <summary>An array of object references.</summary>
        private IndexReference _animationReferenceList;
        private IndexReference _animationInfoList;

        public AnimationTick() {}

        public void Init()
        {
            _objectType = DeltinScript.Types.GetInstance<BaseAnimationInstanceType>();
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
            var actionLoop = new ForRangeBuilder(actionSet, "animation_action_loop", 0, ReferenceListCount, 1);

            actionLoop.Finish();
            actionSet.AddAction(A_Wait.MinimumWait);
            actionSet.AddAction(new A_LoopIfConditionIsTrue());
        }

        public void AddAnimation(ActionSet actionSet, Element objectReference, Element actionIdentifier)
        {
            Element actionIndex = Element.Part<V_IndexOfArrayValue>(_objectType.ActionNames.Get(objectReference), actionIdentifier);

            var objectIndexInAnimationList = actionSet.AssignAndSave("animation_existing_obj_push", Element.Part<V_IndexOfArrayValue>(_animationReferenceList.Get(), objectReference));
            
            // If the _animationList array does not contain the object reference, add it to the list.
            actionSet.AddAction(Element.Part<A_If>(new V_Compare(objectIndexInAnimationList.Get(), Operators.Equal, new V_Number(-1))));

            // Empty index.
            actionSet.AddAction(_animationInfoList.SetVariable(new V_EmptyArray(), index: ReferenceListCount));
            
            // Push the objectReference.
            actionSet.AddAction(_animationReferenceList.ModifyVariable(Operation.AppendToArray, objectReference));

            // End the if
            actionSet.AddAction(new A_End());

            actionSet.AddAction(_animationInfoList.ModifyVariable(Operation.AppendToArray, Element.CreateArray(actionIndex, new V_TotalTimeElapsed()), index: objectIndexInAnimationList.Get()));
        }

        Element ReferenceListCount => Element.Part<V_CountOf>(_animationReferenceList.Get());
    }
}