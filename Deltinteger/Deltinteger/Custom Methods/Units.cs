using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("ToKPH", CustomMethodType.Value)]
    [Parameter("metersPerSecond", ValueType.Number, null)]
    class ToKPH : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(null, Element.Part<V_Multiply>((Element)Parameters[0], new V_Number(3.6)));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki("Converts from meters per second to kilometers per hour.", "Meters per second to convert.");
        }
    }

    [CustomMethod("ToMPH", CustomMethodType.Value)]
    [Parameter("metersPerSecond", ValueType.Number, null)]
    class ToMPH : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(null, Element.Part<V_Multiply>((Element)Parameters[0], new V_Number(2.237)));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki("Converts from meters per second to miles per hour.", "Meters per second to convert.");
        }
    }

    [CustomMethod("ToFt", CustomMethodType.Value)]
    [Parameter("meters", ValueType.Number, null)]
    class ToFt : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(null, Element.Part<V_Multiply>((Element)Parameters[0], new V_Number(2.381)));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki("Converts from meters to feet.", "Meters to convert.");
        }
    }

    [CustomMethod("ToYards", CustomMethodType.Value)]
    [Parameter("meters", ValueType.Number, null)]
    class ToYards : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(null, Element.Part<V_Multiply>((Element)Parameters[0], new V_Number(1.094)));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki("Converts from meters to yards.", "Meters to convert.");
        }
    }

    [CustomMethod("ToInches", CustomMethodType.Value)]
    [Parameter("meters", ValueType.Number, null)]
    class ToInches : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(null, Element.Part<V_Multiply>((Element)Parameters[0], new V_Number(39.37)));
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki("Converts meters to inches.", "Meters to convert.");
        }
    }
}
