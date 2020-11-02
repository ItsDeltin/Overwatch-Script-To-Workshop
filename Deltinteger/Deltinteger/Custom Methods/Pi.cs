using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("Pi", "Represents the ratio of the circumference of a circle to its diameter, specified by the constant π. Equal to `3.1415926535897931`.", CustomMethodType.Value, typeof(NumberType))]
    class Pi : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => null;

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues) => Element.Num(Math.PI);
    }
}
