using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deltin.OverwatchParser.Elements
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
    [Parameter("Player",    ValueType.Player,          typeof(EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(Vector))     ]
    [Parameter("Speed",     ValueType.Number,          typeof(Number))     ]
    [Parameter("Relative",  typeof(Relative))      ]
    [Parameter("Motion",    typeof(ContraryMotion))]
    public class ApplyImpulse : Element {}

    [ElementData("Big Message")]
    [Parameter("Visible To", ValueType.Player, null)]
    [Parameter("Header", ValueType.String, typeof(OverwatchParser.Elements.String))]
    public class BigMessage : Element {}

    [ElementData("Chase Global Variable At Rate")]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(Number))]
    [Parameter("Rate", ValueType.Number, typeof(Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    public class ChaseGlobalVariableAtRate : Element {}

    [ElementData("Chase Global Variable Over Time")]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(Number))]
    [Parameter("Duration", ValueType.Number, typeof(Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    public class ChaseGlobalVariableOverTime : Element {}

    [ElementData("Chase Player Variable At Rate")]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(Number))]
    [Parameter("Rate", ValueType.Number, typeof(Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    public class ChasePlayerVariableAtRate : Element {}

    [ElementData("Chase Player Variable Over Time")]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(Number))]
    [Parameter("Duration", ValueType.Number, typeof(Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    public class ChasePlayerVariableOverTime : Element {}

    [ElementData("Clear Status")]
    [Parameter("Player", ValueType.Player, typeof(EventPlayer))]
    [Parameter("Status", typeof(Status))]
    public class ClearStatus : Element {}
}
