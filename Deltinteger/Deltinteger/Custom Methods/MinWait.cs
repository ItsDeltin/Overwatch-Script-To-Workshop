using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("MinWait", "Waits for 0.016 seconds.", CustomMethodType.Action)]
    class MinWait : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => null;

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            actionSet.AddAction(A_Wait.MinimumWait);
            return null;
        }
    }
}