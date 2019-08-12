using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("Midpoint", CustomMethodType.Value)]
    [Parameter("point1", ValueType.Vector, null)]
    [Parameter("point2", ValueType.Vector, null)]
    class Midpoint : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element point1 = (Element)Parameters[0];
            Element point2 = (Element)Parameters[1];
            return new MethodResult(null, Element.Part<V_Divide>(Element.Part<V_Add>(point1, point2), new V_Number(2)));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("Midpoint", "The midpoint between 2 vectors.", null);
        }
    }
}
