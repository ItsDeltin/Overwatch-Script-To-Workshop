using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("Pi", CustomMethodType.Value)]
    class Pi : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            return new MethodResult(null, Math.PI);
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki("Represents the ratio of the circumference of a circle to its diameter, specified by the constant π. Equal to " + Math.PI);
        }
    }
}