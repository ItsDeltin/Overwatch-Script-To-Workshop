using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("ToRadians", CustomMethodType.Value)]
    [Parameter("degrees", ValueType.Number, null)]
    class ToRadians : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(null, Element.Part<V_Divide>((Element)Parameters[0], new V_Number(180 / Math.PI)));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("ToRadians", "Converts from degress to radians.", null);
        }
    }

    [CustomMethod("ToDegrees", CustomMethodType.Value)]
    [Parameter("radians", ValueType.Number, null)]
    class ToDegrees : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(null, Element.Part<V_Multiply>((Element)Parameters[0], new V_Number(180 / Math.PI)));
        }

        public override WikiMethod Wiki()
        {
            return new WikiMethod("ToDegrees", "Converts from radians to degrees.", null);
        }
    }
}
