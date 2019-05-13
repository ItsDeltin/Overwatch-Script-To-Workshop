using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverwatchParser.Elements
{
    [ElementData("Abort")]
    public class Abort : Element {}

    [ElementData("Abort If")]
    [Parameter("Condition", ValueType.Boolean, null)]
    public class AbortIf : Element {}

    [ElementData("Abort If Condition Is False")]
    public class AbortIfConditionIsFalse : Element {}

    [ElementData("Abort If Condition Is True")]
    public class AbortIfConditionIsTrue : Element {}

    [ElementData("Allow Button")]
    [Parameter("Player", ValueType.Player, null)]
    public class AllowButton : Element {}

    [ElementData("Apply Impulse")]
    [Parameter("Player", ValueType.Player, typeof(EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(Vector))]
    [Parameter("Speed", ValueType.Number, typeof(Number))]

    public class ApplyImpulse : Element {}
}
