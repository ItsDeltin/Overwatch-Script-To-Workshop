using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("MinWait", CustomMethodType.Action)]
    class MinWait : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(new Element[] { A_Wait.MinimumWait }, null);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki("Waits for " + Constants.MINIMUM_WAIT + " milliseconds.");
        }
    }
}