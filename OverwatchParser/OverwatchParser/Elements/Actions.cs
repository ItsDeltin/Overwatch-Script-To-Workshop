using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverwatchParser.Elements
{
    [ElementData("Abort")]
    public class A_Abort : Element {}

    [ElementData("Abort If")]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    public class A_AbortIf : Element {}

    [ElementData("Abort If Condition Is False")]
    public class A_AbortIfConditionIsFalse : Element {}

    [ElementData("Abort If Condition Is True")]
    public class A_AbortIfConditionIsTrue : Element {}

    [ElementData("Allow Button")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", typeof(Button))]
    public class A_AllowButton : Element {}

    [ElementData("Apply Impulse")]
    [Parameter("Player",    ValueType.Player,          typeof(V_EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))     ]
    [Parameter("Speed",     ValueType.Number,          typeof(V_Number))     ]
    [Parameter("Relative",  typeof(Relative))      ]
    [Parameter("Motion",    typeof(ContraryMotion))]
    public class A_ApplyImpulse : Element {}

    [ElementData("Big Message")]
    [Parameter("Visible To", ValueType.Player, null)]
    [Parameter("Header", ValueType.String, typeof(OverwatchParser.Elements.V_String))]
    public class A_BigMessage : Element {}

    [ElementData("Chase Global Variable At Rate")]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Rate", ValueType.Number, typeof(V_Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    public class A_ChaseGlobalVariableAtRate : Element {}

    [ElementData("Chase Global Variable Over Time")]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    public class A_ChaseGlobalVariableOverTime : Element {}

    [ElementData("Chase Player Variable At Rate")]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Rate", ValueType.Number, typeof(V_Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    public class A_ChasePlayerVariableAtRate : Element {}

    [ElementData("Chase Player Variable Over Time")]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    public class A_ChasePlayerVariableOverTime : Element {}

    [ElementData("Clear Status")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Status", typeof(Status))]
    public class A_ClearStatus : Element {}

    [ElementData("Kill", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Killer", ValueType.Player, typeof(V_Null))]
    public class A_Kill : Element {}

    [ElementData("Set Global Variable", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_SetGlobalVariable : Element {}

    [ElementData("Set Player Variable", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_SetPlayerVariable : Element {}
}
