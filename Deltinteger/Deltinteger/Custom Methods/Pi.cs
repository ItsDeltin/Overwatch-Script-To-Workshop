using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("Pi", CustomMethodType.Value)]
    class Pi : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(null, new V_Number(Math.PI));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("Pi", "Represents the ratio of the circumference of a circle to its diameter, specified by the constant π. Equal to " + Math.PI, null);
        }
    }
}