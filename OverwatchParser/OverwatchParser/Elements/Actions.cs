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
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.String, typeof(V_String))]
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

    [ElementData("Create Effect", 0)]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Type", typeof(Effect))]
    [Parameter("Color", typeof(Color))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Radius", ValueType.Number, typeof(V_Number))]
    [Parameter("Reevaluation", typeof(EffectRev))]
    public class A_CreateEffect : Element {}

    [ElementData("Kill", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Killer", ValueType.Player, typeof(V_Null))]
    public class A_Kill : Element {}

    [ElementData("Loop", 0)]
    public class A_Loop : Element {}

    [ElementData("Loop If", 0)]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    public class A_LoopIf : Element {}

    [ElementData("Loop If Condition Is False", 0)]
    public class A_LoopIfConditionIsFalse : Element {}

    [ElementData("Loop If Condition Is True", 0)]
    public class A_LoopIfConditionIsTrue : Element { }

    [ElementData("Modify Global Variable", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Operation", typeof(Operation))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_ModifyGlobalVariable : Element {}

    [ElementData("Modify Player Variable", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Operation", typeof(Operation))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_ModifyPlayerVariable : Element {}

    [ElementData("Set Global Variable", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_SetGlobalVariable : Element {}

    [ElementData("Set Global Variable At Index", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Index", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_SetGlobalVariableAtIndex : Element {}

    [ElementData("Set Player Variable", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_SetPlayerVariable : Element {}

    [ElementData("Set Player Variable At Index", 0)]
    [Parameter("Player", ValueType.Any, typeof(V_EventPlayer))]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Index", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_SetPlayerVariableAtIndex : Element {}

    [ElementData("Skip If", 0)]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [Parameter("Number Of Actions", ValueType.Number, typeof(V_Number))]
    public class A_SkipIf : Element {}

    [ElementData("Skip", 0)]
    [Parameter("Number Of Actions", ValueType.Number, typeof(V_Number))]
    public class A_Skip : Element {}

    [ElementData("Small Message", 0)]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.String, typeof(V_String))]
    public class A_SmallMessage : Element {}

    [ElementData("Start Camera", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Eye Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Look At Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Blend Speed", ValueType.Number, typeof(V_Number))]
    public class A_StartCamera : Element {}

    [ElementData("Stop Camera", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopCamera : Element {}

    [ElementData("Teleport", 0)]
    [Parameter("Player", ValueType.Any, typeof(V_Number))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class A_Teleport : Element {}

    [ElementData("Wait", 0)]
    [Parameter("Time", ValueType.Number, typeof(V_Number))]
    [Parameter("Wait Behavior", typeof(WaitBehavior))]
    public class A_Wait : Element {}
}
