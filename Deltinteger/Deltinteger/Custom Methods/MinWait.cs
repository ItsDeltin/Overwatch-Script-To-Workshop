using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("MinWait", "Waits for 0.016 seconds.", CustomMethodType.Action)]
    class MinWait : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("waitBehavior", ValueGroupType.GetEnumType<WaitBehavior>(), new ExpressionOrWorkshopValue(EnumData.GetEnumValue(WaitBehavior.IgnoreCondition)))
        }

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            actionSet.AddAction(A_Wait.MinimumWait);
            return null;
        }
    }
}