using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("MinWait", "Waits for 0.016 seconds.", CustomMethodType.Action, typeof(NullType))]
    class MinWait : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("waitBehavior", ValueGroupType.GetEnumType("WaitBehavior"), new ExpressionOrWorkshopValue(ElementRoot.Instance.GetEnumValueFromWorkshop("WaitBehavior", "Ignore Condition")))
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            actionSet.AddAction(Element.Wait());
            return null;
        }
    }
}
