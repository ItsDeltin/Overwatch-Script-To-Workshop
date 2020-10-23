using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deltin.Deltinteger.Elements
{
    [ElementData("Abort")]
    [UsageDiagnostic("This workshop method can leave the workshop in an unexpected state. Method callers will also be aborted.", 2)]
    public class A_Abort : Element {}

    [ElementData("Abort If")]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [UsageDiagnostic("This workshop method can leave the workshop in an unexpected state. Method callers will also be aborted.", 2)]
    public class A_AbortIf : Element {}

    [ElementData("Abort If Condition Is False")]
    [UsageDiagnostic("This workshop method can leave the workshop in an unexpected state. Method callers will also be aborted.", 2)]
    public class A_AbortIfConditionIsFalse : Element {}

    [ElementData("Abort If Condition Is True")]
    [UsageDiagnostic("This workshop method can leave the workshop in an unexpected state. Method callers will also be aborted.", 2)]
    public class A_AbortIfConditionIsTrue : Element {}

    [ElementData("Allow Button")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    public class A_AllowButton : Element {}

    [ElementData("Apply Impulse")]
    [Parameter("Player",    ValueType.Player,          typeof(V_EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))     ]
    [Parameter("Speed",     ValueType.Number,          typeof(V_Number))     ]
    [EnumParameter("Relative",  typeof(Relative))]
    [EnumParameter("Motion", typeof(ContraryMotion))]
    public class A_ApplyImpulse : Element {}

    [ElementData("Big Message")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_String))]
    public class A_BigMessage : Element {}

    [ElementData("Call Subroutine")]
    [Parameter("Subroutine", ValueType.Any, null)]
    [HideElement]
    public class A_CallSubroutine : Element {}

    [ElementData("Start Rule")]
    [Parameter("Subroutine", ValueType.Any, null)]
    [EnumParameter("If Already Executing", typeof(IfAlreadyExecuting))]
    [HideElement]
    public class A_StartRule : Element {}

    [ElementData("Chase Global Variable At Rate")]
    [VarRefParameter("Variable", true)]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Rate", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(RateChaseReevaluation))]
    [HideElement]
    public class A_ChaseGlobalVariableAtRate : Element {}

    [ElementData("Chase Global Variable Over Time")]
    [VarRefParameter("Variable", true)]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(TimeChaseReevaluation))]
    [HideElement]
    public class A_ChaseGlobalVariableOverTime : Element {}

    [ElementData("Chase Player Variable At Rate")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [VarRefParameter("Variable", false)]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Rate", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(RateChaseReevaluation))]
    [HideElement]
    public class A_ChasePlayerVariableAtRate : Element {}

    [ElementData("Chase Player Variable Over Time")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [VarRefParameter("Variable", false)]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(TimeChaseReevaluation))]
    [HideElement]
    public class A_ChasePlayerVariableOverTime : Element {}

    [ElementData("Clear Status")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Status", typeof(Status))]
    public class A_ClearStatus : Element {}

    [ElementData("Communicate")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Type", typeof(Communication))]
    public class A_Communicate : Element {}

    [ElementData("Create Beam Effect")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [EnumParameter("Beam Type", typeof(BeamType))]
    [Parameter("Start Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("End Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Color", ValueType.Color, typeof(V_ColorValue))]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    public class A_CreateBeamEffect : Element {}

    [ElementData("Create Dummy Bot")]
    [Parameter("Hero", ValueType.Hero, typeof(V_HeroValue))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    #warning settable default number
    [Parameter("Slot", ValueType.Number, typeof(V_Number))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Facing", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class A_CreateDummyBot : Element {}

    [ElementData("Create Effect")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [EnumParameter("Type", typeof(Effect))]
    [Parameter("Color", ValueType.Color, typeof(V_ColorValue))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Radius", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    public class A_CreateEffect : Element {}

    [ElementData("Create HUD Text")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_Null))]
    [Parameter("Subheader", ValueType.Any, typeof(V_Null))]
    [Parameter("Text", ValueType.Any, typeof(V_Null))]
    [EnumParameter("Location", typeof(HudLocation))]
    [Parameter("Sort Order", ValueType.Number, typeof(V_Number))]
    [Parameter("Header Color", ValueType.Color, typeof(V_ColorValue))]
    [Parameter("Subheader Color", ValueType.Color, typeof(V_ColorValue))]
    [Parameter("Text Color", ValueType.Color, typeof(V_ColorValue))]
    [EnumParameter("Reevaluation", typeof(HudTextRev))]
    [EnumParameter("Spectators", typeof(Spectators))]
    public class A_CreateHudText : Element {}

    [ElementData("Create Icon")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [EnumParameter("Icon", typeof(Icon))]
    [EnumParameter("Reevaluation", typeof(IconRev))]
    [Parameter("Icon Color", ValueType.Color, typeof(V_ColorValue))]
    [Parameter("Show When Offscreen", ValueType.Boolean, typeof(V_True))]
    public class A_CreateIcon : Element {}

    [ElementData("Create In-World Text")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_String))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Scale", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Clipping", typeof(Clipping))]
    [EnumParameter("Reevaluation", typeof(InworldTextRev))]
    [Parameter("Text Color", ValueType.Color, typeof(V_ColorValue))]
    [EnumParameter("Spectators", typeof(Spectators))]
    public class A_CreateInWorldText : Element {}

    [ElementData("Damage")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Damager", ValueType.Player, typeof(V_Null))]
    [Parameter("Amount", ValueType.Number, typeof(V_Number))]
    public class A_Damage : Element {}

    [ElementData("Declare Match Draw")]
    public class A_DeclareMatchDraw : Element {}

    [ElementData("Declare Player Victory")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DeclarePlayerVictory : Element {}

    [ElementData("Declare Round Victory")]
    [Parameter("Round Winning Team", ValueType.Team, typeof(V_TeamVar))]
    public class A_DeclareRoundVictory : Element {}

    [ElementData("Declare team Victory")]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class A_DeclareTeamVictory : Element {}

    [ElementData("Destroy All Dummy Bots")]
    public class A_DestroyAllDummyBots : Element {}

    [ElementData("Destroy All Effects")]
    public class A_DestroyAllEffects : Element {}

    [ElementData("Destroy All HUD Text")]
    public class A_DestroyAllHudText : Element {}

    [ElementData("Destroy All Icons")]
    public class A_DestroyAllIcons : Element {}

    [ElementData("Destroy All In-World Text")]
    public class A_DestroyAllInworldText : Element {}

    [ElementData("Destroy Dummy Bot")]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    [Parameter("Slot", ValueType.Number, typeof(V_Number))]
    public class A_DestroyDummyBot : Element {}

    [ElementData("Destroy Effect")]
    [Parameter("Effect ID", ValueType.Player, null)]
    public class A_DestroyEffect : Element {}

    [ElementData("Destroy HUD Text")]
    [Parameter("Text ID", ValueType.Number, null)]
    public class A_DestroyHudText : Element {}

    [ElementData("Destroy Icon")]
    [Parameter("Icon ID", ValueType.Number, null)]
    public class A_DestroyIcon : Element {}

    [ElementData("Destroy In-World Text")]
    [Parameter("Text ID", ValueType.Number, null)]
    public class A_DestroyInWorldText : Element {}

    [ElementData("Disable Built-In Game Mode Announcer")]
    public class A_DisableAnnouncer : Element {}

    [ElementData("Disable Built-In Game Mode Completion")]
    public class A_DisableCompletion : Element {}

    [ElementData("Disable Built-In Game Mode Music")]
    public class A_DisableMusic : Element {}

    [ElementData("Disable Built-In Game Mode Respawning")]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DisableRespawning : Element {}

    [ElementData("Disable Built-In Game Mode Scoring")]
    public class A_DisableScoring : Element {}

    [ElementData("Disable Death Spectate All Players")]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DisableSpectate : Element {}

    [ElementData("Disable Death Spectate Target HUD")]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DisableSpectateHUD : Element {}

    [ElementData("Disable Inspector Recording")]
    public class A_DisableInspectorRecording : Element {}

    [ElementData("Disallow Button")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    public class A_DisallowButton : Element {}

    [ElementData("Enable Built-In Game Mode Announcer")]
    public class A_EnableAnnouncer : Element {}

    [ElementData("Enable Built-In Game Mode Completion")]
    public class A_EnableCompletion : Element {}

    [ElementData("Enable Inspector Recording")]
    public class A_EnableInspectorRecording : Element {}

    [ElementData("Enable Built-In Game Mode Music")]
    public class A_EnableMusic : Element {}

    [ElementData("Enable Built-In Game Mode Respawning")]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    public class A_EnableRespawning : Element {}

    [ElementData("Enable Built-In Game Mode Scoring")]
    public class A_EnableScoring : Element {}

    [ElementData("Enable Death Spectate All Players")]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    public class A_EnableSpectate : Element {}

    [ElementData("Enable Death Spectate Target HUD")]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    public class A_EnableSpectateHUD : Element {}

    [ElementData("End")]
    [HideElement]
    public class A_End : Element {}

    [ElementData("For Global Variable")]
    [VarRefParameter("Control Variable", true)]
    [Parameter("Range Start", ValueType.Number, typeof(V_Number))]
    [Parameter("Range Stop", ValueType.Number, typeof(V_Number))]
    [Parameter("Step", ValueType.Number, typeof(V_Number))]
    [HideElement]
    public class A_ForGlobalVariable : Element {}

    [ElementData("For Player Variable")]
    [Parameter("Control Player", ValueType.Player, typeof(V_EventPlayer))]
    [VarRefParameter("Control Variable", false)]
    [Parameter("Range Start", ValueType.Number, typeof(V_Number))]
    [Parameter("Range Stop", ValueType.Number, typeof(V_Number))]
    [Parameter("Step", ValueType.Number, typeof(V_Number))]
    [HideElement]
    public class A_ForPlayerVariable : Element {}

    [ElementData("Go To Assemble Heroes")]
    public class A_GoToAssembleHeroes : Element {}

    [ElementData("Heal")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Healer", ValueType.Player, typeof(V_Null))]
    [Parameter("Amount", ValueType.Number, typeof(V_Number))]
    public class A_Heal : Element {}

    [ElementData("If")]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [HideElement]
    public class A_If : Element {}

    [ElementData("Else If")]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [HideElement]
    public class A_ElseIf : Element {}

    [ElementData("Else")]
    [HideElement]
    public class A_Else : Element {}

    [ElementData("Kill")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Killer", ValueType.Player, typeof(V_Null))]
    public class A_Kill : Element {}

    [ElementData("Loop")]
    [UsageDiagnostic("This workshop method can leave the workshop in an unexpected state. Method callers will also be looped.", 2)]
    public class A_Loop : Element {}

    [ElementData("Loop If")]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [UsageDiagnostic("This workshop method can leave the workshop in an unexpected state. Method callers will also be looped.", 2)]
    public class A_LoopIf : Element {}

    [ElementData("Loop If Condition Is False")]
    [UsageDiagnostic("This workshop method can leave the workshop in an unexpected state. Method callers will also be looped.", 2)]
    public class A_LoopIfConditionIsFalse : Element {}

    [ElementData("Loop If Condition Is True")]
    [UsageDiagnostic("This workshop method can leave the workshop in an unexpected state. Method callers will also be looped.", 2)]
    public class A_LoopIfConditionIsTrue : Element { }

    [ElementData("Modify Global Variable")]
    [VarRefParameter("Variable", true)]
    [EnumParameter("Operation", typeof(Operation))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class A_ModifyGlobalVariable : Element {}

    [ElementData("Modify Global Variable At Index")]
    [VarRefParameter("Variable", true)]
    [Parameter("Index", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Operation", typeof(Operation))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class A_ModifyGlobalVariableAtIndex : Element {}

    [ElementData("Modify Player Score")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Score", ValueType.Number, typeof(V_Number))]
    public class A_ModifyPlayerScore : Element {}

    [ElementData("Modify Player Variable")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [VarRefParameter("Variable", false)]
    [EnumParameter("Operation", typeof(Operation))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class A_ModifyPlayerVariable : Element {}

    [ElementData("Modify Player Variable At Index")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [VarRefParameter("Variable", false)]
    [Parameter("Index", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Operation", typeof(Operation))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class A_ModifyPlayerVariableAtIndex : Element {}

    [ElementData("Modify Team Score")]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    [Parameter("Score", ValueType.Number, typeof(V_Number))]
    public class A_ModifyTeamScore : Element {}

    [ElementData("Pause Match Time")]
    public class A_PauseMatchTime : Element {}

    [ElementData("Play Effect")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [EnumParameter("Type", typeof(PlayEffect))]
    [Parameter("Color", ValueType.Color, typeof(V_ColorValue))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Radius", ValueType.Number, typeof(V_Number))]
    public class A_PlayEffect : Element {}

    [ElementData("Preload Hero")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Hero", ValueType.Hero, typeof(V_HeroValue))]
    public class A_PreloadHero : Element {}

    [ElementData("Press Button")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    public class A_PressButton : Element {}

    [ElementData("Reset Player Hero Availability")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_ResetHeroAvailability : Element {}

    [ElementData("Respawn")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_Respawn : Element {}

    [ElementData("Resurrect")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_Resurrect : Element {}

    [ElementData("Set Ability 1 Enabled")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    public class A_SetAbility1Enabled : Element {}

    [ElementData("Set Ability 2 Enabled")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    public class A_SetAbility2Enabled : Element {}

    [ElementData("Set Aim Speed")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Turn Speed Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetAimSpeed : Element {}

    [ElementData("Set Damage Dealt")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Damage Dealt Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetDamageDealt : Element {}

    [ElementData("Set Damage Received")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Damage Dealt Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetDamageReceived : Element {}

    [ElementData("Set Facing")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [EnumParameter("Relative", typeof(Relative))]
    public class A_SetFacing : Element {}

    [ElementData("Set Global Variable")]
    [VarRefParameter("Variable", true)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class A_SetGlobalVariable : Element {}

    [ElementData("Set Global Variable At Index")]
    [VarRefParameter("Variable", true)]
    [Parameter("Index", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class A_SetGlobalVariableAtIndex : Element {}

    [ElementData("Set Gravity")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Gravity Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetGravity : Element {}

    [ElementData("Set Healing Dealt")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Healing Dealt Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetHealingDealt : Element {}

    [ElementData("Set Healing Received")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Healing Received Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetHealingReceived : Element {}

    [ElementData("Set Invisible")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Invisible To", typeof(InvisibleTo))]
    public class A_SetInvisible : Element {}

    [ElementData("Set Match Time")]
    [Parameter("Match Time", ValueType.Number, typeof(V_Number))]
    public class A_SetMatchTime : Element {}

    [ElementData("Set Max Health")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Health Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetMaxHealth : Element {}

    [ElementData("Set Move Speed")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Move Speed Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetMoveSpeed : Element {}

    [ElementData("Set Objective Description")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_String))]
    [EnumParameter("Reevaluation", typeof(ObjectiveRev))]
    public class A_SetObjectiveDescription : Element {}

    [ElementData("Set Player Allowed Heroes")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Hero", ValueType.Hero, null)]
    public class A_SetAllowedHeroes : Element {}

    [ElementData("Set Player Score")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Score", ValueType.Number, typeof(V_Number))]
    public class A_SetPlayerScore : Element {}

    [ElementData("Set Player Variable")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [VarRefParameter("Variable", false)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class A_SetPlayerVariable : Element {}

    [ElementData("Set Player Variable At Index")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [VarRefParameter("Variable", false)]
    [Parameter("Index", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class A_SetPlayerVariableAtIndex : Element {}

    [ElementData("Set Primary Fire Enabled")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    public class A_SetPrimaryFireEnabled : Element {}

    [ElementData("Set Projectile Gravity")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Projectile Gravity Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetProjectileGravity : Element {}

    [ElementData("Set Projectile Speed")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Projectile Speed Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetProjectileSpeed : Element {}

    [ElementData("Set Respawn Max Time")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Time", ValueType.Number, typeof(V_Number))]
    public class A_SetRespawnMaxTime : Element {}

    [ElementData("Set Secondary Fire Enabled")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    public class A_SetSecondaryFireEnabled : Element {}

    [ElementData("Set Slow Motion")]
    [Parameter("Speed Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetSlowMotion : Element {}

    [ElementData("Set Status")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Assister", ValueType.Player, typeof(V_Null))]
    [EnumParameter("Status", typeof(Status))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    public class A_SetStatus : Element {}

    [ElementData("Set Team Score")]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    [Parameter("Score", ValueType.Number, typeof(V_Number))]
    public class A_SetTeamScore : Element {}

    [ElementData("Set Ultimate Ability Enabled")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    public class A_SetUltimateAbilityEnabled : Element {}

    [ElementData("Set Ultimate Charge")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Charge Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetUltimateCharge : Element {}

    [ElementData("Skip")]
    [Parameter("Number Of Actions", ValueType.Number, typeof(V_Number))]
    [UsageDiagnostic("This workshop method can leave the workshop in an unexpected state.", 2)]
    public class A_Skip : Element, ISkip 
    {
        public int SkipParameterIndex()
        {
            return 0;
        }
    }

    [ElementData("Skip If")]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [Parameter("Number Of Actions", ValueType.Number, typeof(V_Number))]
    [UsageDiagnostic("This workshop method can leave the workshop in an unexpected state.", 2)]
    public class A_SkipIf : Element, ISkip 
    {
        public int SkipParameterIndex()
        {
            return 1;
        }
    }

    [ElementData("Small Message")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_String))]
    public class A_SmallMessage : Element {}

    [ElementData("Start Accelerating")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Rate", ValueType.Number, typeof(V_Number))]
    [Parameter("Max Speed", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Relative", typeof(Relative))]
    [EnumParameter("Reevaluation", typeof(AccelerateRev))]
    public class A_StartAccelerating : Element {}

    [ElementData("Start Camera")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Eye Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Look At Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Blend Speed", ValueType.Number, typeof(V_Number))]
    public class A_StartCamera : Element {}

    [ElementData("Start Damage Modification")]
    [Parameter("Receivers", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Damagers", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Damage Percent", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(DamageModificationRev))]
    public class A_StartDamageModification : Element {}

    [ElementData("Start Damage Over Time")]
    [Parameter("Receivers", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Damagers", ValueType.Player, typeof(V_Null))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [Parameter("Damage Per Second", ValueType.Number, typeof(V_Number))]
    public class A_StartDamageOverTime : Element {}

    [ElementData("Start Facing")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Turn Rate", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Relative", typeof(Relative))]
    [EnumParameter("Reevaluation", typeof(FacingRev))]
    public class A_StartFacing : Element {}

    [ElementData("Start Forcing Player To Be Hero")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Hero", ValueType.Hero, typeof(V_HeroValue))]
    public class A_ForcePlayerHero : Element {}

    [ElementData("Start Forcing Spawn Room")]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    [Parameter("Room", ValueType.Number, typeof(V_Number))]
    public class A_ForceSpawnRoom : Element {}

    [ElementData("Start Forcing Throttle")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Min Forward", ValueType.Number, typeof(V_Number))]
    [Parameter("Max Forward", ValueType.Number, typeof(V_Number))]
    [Parameter("Min Backward", ValueType.Number, typeof(V_Number))]
    [Parameter("Max Backward", ValueType.Number, typeof(V_Number))]
    [Parameter("Min Sideways", ValueType.Number, typeof(V_Number))]
    [Parameter("Max Sideways", ValueType.Number, typeof(V_Number))]
    public class A_ForceThrottle : Element {}

    [ElementData("Start Healing Modification")]
    [Parameter("Recievers", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Healers", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Healing Percent", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(HealingModificationRev))]
    public class A_StartHealingModification : Element {}

    [ElementData("Stop Healing Modification")]
    [Parameter("Healing Modification ID", ValueType.Number, typeof(V_LastHealingModificationID))]
    public class A_StopHealingModification : Element {}

    [ElementData("Start Heal Over Time")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Healer", ValueType.Player, typeof(V_Null))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [Parameter("Healing Per Second", ValueType.Number, typeof(V_Number))]
    public class A_StartHealOverTime : Element {}

    [ElementData("Start Holding Button")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    public class A_StartHoldingButton : Element {}

    [ElementData("Start Throttle In Direction")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Magnitude", ValueType.Number, typeof(V_Number))] // def of 1
    [EnumParameter("Relative", typeof(Relative))]
    [EnumParameter("Behavior", typeof(ThrottleBehavior))]
    [EnumParameter("Reevaluation", typeof(ThrottleRev))]
    public class A_StartThrottleInDirection : Element {}

    [ElementData("Start Transforming Throttle")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("X Axis Scalar", ValueType.Number, typeof(V_Number))]
    [Parameter("Y Axis Scalar", ValueType.Number, typeof(V_Number))]
    [Parameter("Relative Direction", ValueType.Vector, typeof(V_Vector))]
    public class A_StartTransformingThrottle : Element {}

    [ElementData("Stop Accelerating")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopAccelerating : Element {}

    [ElementData("Stop All Damage Modifications")]
    public class A_StopAllDamageModifications : Element {}

    [ElementData("Stop All Damage Over Time")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopAllDamageOverTime : Element {}

    [ElementData("Stop All Heal Over Time")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopAllHealOverTime : Element {}

    [ElementData("Stop Camera")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopCamera : Element {}

    [ElementData("Stop Chasing Global Variable")]
    [VarRefParameter("Variable", true)]
    [HideElement]
    public class A_StopChasingGlobalVariable : Element {}

    [ElementData("Stop Chasing Player Variable")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [VarRefParameter("Variable", false)]
    [HideElement]
    public class A_StopChasingPlayerVariable : Element {}

    [ElementData("Stop Damage Modification")]
    [Parameter("Damage modification ID", ValueType.Number, null)]
    public class A_StopDamageModification : Element {}

    [ElementData("Stop Damage Over Time")]
    [Parameter("Damage Over Time ID", ValueType.Number, null)]
    public class A_StopDamageOverTime : Element {}

    [ElementData("Stop Facing")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopFacing : Element {}

    [ElementData("Stop Forcing Player To Be Hero")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopForcingHero : Element {}

    [ElementData("Stop Forcing Spawn Room")]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class A_StopForcingSpawnRoom : Element {}

    [ElementData("Stop Forcing Throttle")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopForcingThrottle : Element {}

    [ElementData("Stop Heal Over Time")]
    [Parameter("Stop Heal Over Time", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopHealOverTime : Element {}

    [ElementData("Stop Holding Button")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    public class A_StopHoldingButton : Element {}

    [ElementData("Stop Throttle In Direction")]
    [Parameter("Event Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopThrottleInDirection : Element {}

    [ElementData("Stop Transforming Throttle")]
    [Parameter("Event Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopTransformingThrottle : Element {}

    [ElementData("Teleport")]
    [Parameter("Player", ValueType.Any, typeof(V_Number))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class A_Teleport : Element {}

    [ElementData("Unpause Match Time")]
    public class A_UnpauseMatchTime : Element {}

    [ElementData("Wait")]
    [Parameter("Time", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Wait Behavior", typeof(WaitBehavior))]
    public class A_Wait : Element 
    {
        public static A_Wait MinimumWait { get { return Element.Part<A_Wait>(new V_Number(Constants.MINIMUM_WAIT)); } }
    }

    [ElementData("While")]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [HideElement]
    public class A_While : Element {}

    [ElementData("Continue")]
    [HideElement]
    public class A_Continue : Element {}

    [ElementData("Break")]
    [HideElement]
    public class A_Break : Element {}

    [ElementData("Set Crouch Enabled")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    public class A_SetCrouchEnabled : Element {}

    [ElementData("Set Melee Enabled")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    public class A_SetMeleeEnabled : Element {}

    [ElementData("Set Jump Enabled")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    public class A_SetJumpEnabled : Element {}

    [ElementData("Declare Round Draw")]
    public class A_DeclareRoundDraw : Element {}

    [ElementData("Set Ability Cooldown")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    [Parameter("Cooldown", ValueType.Number, typeof(V_Number))]
    public class A_SetAbilityCooldown : Element {}

    [ElementData("Cancel Primary Action")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_CancelPrimaryAction : Element {}

    [ElementData("Start Forcing Player Position")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Position", ValueType.Vector, typeof(V_Vector))]
    [Parameter("Reevaluation", ValueType.Boolean, typeof(V_True))]
    public class A_StartForcingPlayerPosition : Element {}
    
    [ElementData("Stop Forcing Player Position")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopForcingPlayerPosition : Element {}

    [ElementData("Attach Players")]
    [Parameter("Child", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Parent", ValueType.Player, typeof(V_LastCreatedEntity))]
    [Parameter("Offset", ValueType.Player, typeof(V_Vector))]
    public class A_AttachPlayers : Element {}

    [ElementData("Detach Players")]
    [Parameter("Children", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DetachPlayers : Element {}

    [ElementData("Set Ammo")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Clip", ValueType.Number, typeof(V_Number))]
    [Parameter("Ammo", ValueType.Number, typeof(V_Number))]
    public class A_SetAmmo : Element {}

    [ElementData("Set Max Ammo")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Clip", ValueType.Number, typeof(V_Number))]
    [Parameter("Max Ammo", ValueType.Number, typeof(V_Number))]
    public class A_SetMaxAmmo : Element {}

    [ElementData("Set Weapon")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Weapon", ValueType.Number, typeof(V_Number))]
    public class A_SetWeapon : Element {}

    [ElementData("Set Reload enabled")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Number, typeof(V_True))]
    public class A_SetReloadEnabled : Element {}

    [ElementData("Disable Game Mode HUD")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DisableGameModeHud : Element {}

    [ElementData("Enable Game Mode HUD")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_EnableGameModeHud : Element {}

    [ElementData("Disable Game Mode In-World UI")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DisableGameModeInworldUI : Element {}

    [ElementData("Enable Game Mode In-World UI")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_EnableGameModeInworldUI : Element {}

    [ElementData("Disable Hero HUD")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DisableHeroHud : Element {}

    [ElementData("Enable Hero HUD")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_EnableHeroHud : Element {}

    [ElementData("Disable Kill Feed")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DisableKillFeed : Element {}

    [ElementData("Enable Kill Feed")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_EnableKillFeed : Element {}

    [ElementData("Disable Messages")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DisableMessages : Element {}

    [ElementData("Enable Messages")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_EnableMessages : Element {}

    [ElementData("Disable Scoreboard")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DisableScoreboard : Element {}

    [ElementData("Enable Scoreboard")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_EnableScoreboard : Element {}

    [ElementData("Set Ability Charge")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    [Parameter("Charge Count", ValueType.Number, typeof(V_Number))]
    public class A_SetAbilityCharge : Element {}

    [ElementData("Set Ability Resource")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    [Parameter("Resource Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetAbilityResource : Element {}

    [ElementData("Set Jump Vertical Speed")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Jump Vertical Speed Percent", ValueType.Number, typeof(V_Number))]
    public class A_SetJumpVerticalSpeed : Element {}

    [ElementData("Disable Nameplates")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Viewing Players", ValueType.Player, typeof(V_AllPlayers))]
    public class A_DisableNameplates : Element {}

    [ElementData("Enable Nameplates")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Viewing Players", ValueType.Player, typeof(V_AllPlayers))]
    public class A_EnableNameplates : Element {}
    
    [ElementData("Start Forcing Player Outlines")]
    [Parameter("Viewed Players", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Viewing Players", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Visible", ValueType.Boolean, typeof(V_True))]
    [Parameter("Color", ValueType.Color, typeof(V_ColorValue))]
    [EnumParameter("OutlineType", typeof(OutlineType))]
    public class A_StartForcingPlayerOutlines : Element {}

    [ElementData("Stop Forcing Player Outlines")]
    [Parameter("Viewed Players", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Viewing Players", ValueType.Player, typeof(V_AllPlayers))]
    public class A_StopForcingPlayerOutlines : Element {}

    [ElementData("Start Scaling Player")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Scale", ValueType.Number, typeof(V_Number))]
    [Parameter("Reevaluation", ValueType.Boolean, typeof(V_True))]
    public class A_StartScalingPlayer : Element {}

    [ElementData("Stop Scaling Player")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopScalingPlayer : Element {}

    [ElementData("Start Scaling Barriers")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Scale", ValueType.Number, typeof(V_Number))]
    [Parameter("Reevaluation", ValueType.Boolean, typeof(V_True))]
    public class A_StartScalingBarriers : Element {}

    [ElementData("Stop Scaling Barriers")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopScalingBarriers : Element {}

    [ElementData("Enable Movement Collision With Environment")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_EnableMovementCollisionWithEnvironment : Element {}

    [ElementData("Disable Movement Collision With Environment")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Include Floors", ValueType.Boolean, typeof(V_False))]
    public class A_DisableMovementCollisionWithEnvironment : Element {}

    [ElementData("Enable Movement Collision With Players")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_EnableMovementCollisionWithPlayers : Element {}

    [ElementData("Disable Movement Collision With Players")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_DisableMovementCollisionWithPlayers : Element {}

    [ElementData("Start Modifying Hero Voice Lines")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Pitch Scalar", ValueType.Number, typeof(V_Number))]
    [Parameter("Reevaluation", ValueType.Boolean, typeof(V_True))]
    public class A_StartModifyingHeroVoiceLines : Element {}

    [ElementData("Stop Modifying Hero Voice Lines")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_StopModifyingHeroVoiceLines : Element {}

    [ElementData("Add Health Pool To Player")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Health Type", typeof(HealthType))]
    [Parameter("Max Health", ValueType.Number, typeof(V_Number))]
    [Parameter("Recoverable", ValueType.Boolean, typeof(V_True))]
    [Parameter("Reevaluation", ValueType.Boolean, typeof(V_True))]
    public class A_AddHealthPoolToPlayer : Element {}

    [ElementData("Remove Health Pool From Player")]
    [Parameter("Health Pool ID", ValueType.Any, typeof(V_LastCreatedHealthPool))]
    public class A_RemoveHealthPoolFromPlayer : Element {}

    [ElementData("Remove All Health Pools From Player")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_RemoveAllHealthPoolsFromPlayer : Element {}

    [ElementData("Set Player Health")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Amount", ValueType.Number, typeof(V_Number))]
    public class A_SetPlayerHealth : Element {}

    [ElementData("Log To Inspector")]
    [Parameter("Text", ValueType.String, typeof(V_CustomString))]
    public class A_LogToInspector : Element {}

    [ElementData("Wait Until")]
    [Parameter("Continue Condition", ValueType.Boolean, null)]
    [Parameter("Timeout", ValueType.Number, null)]
    public class A_WaitUntil : Element {}

    [ElementData("Set Knockback Dealt")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Knockback Dealt Percent", ValueType.Number, null)]
    public class A_SetKnockbackDealt : Element {}

    [ElementData("Set Knockback Received")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Knockback Received Percent", ValueType.Number, null)]
    public class A_SetKnockbackReceived : Element {}

    [ElementData("Set Environment Credit Player")]
    [Parameter("Target", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Environment Credit Player", ValueType.Player, null)]
    public class A_SetEnvironmentCreditPlayer : Element {}

    [ElementData("Start Assist")]
    [Parameter("Assisters", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Targets", ValueType.Player, typeof(V_AllPlayers))]
    [EnumParameter("Reevaluation", typeof(Assist))]
    public class A_StartAssist : Element {}

    [ElementData("Stop Assist")]
    [Parameter("Assist ID", ValueType.Any, null)]
    public class A_StopAssist : Element {}

    [ElementData("Stop All Assist")]
    public class A_StopAllAssist : Element {}

    [ElementData("Create Progress Bar HUD Text")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Text", ValueType.String, typeof(V_CustomString))]
    [EnumParameter("Location", typeof(HudLocation))]
    [Parameter("Sort Order", ValueType.Number, typeof(V_Number))]
    [Parameter("Progress Bar Color", ValueType.Color, typeof(V_ColorValue))]
    [Parameter("Text Color", ValueType.Color, typeof(V_ColorValue))]
    [EnumParameter("Reevaluation", typeof(ProgressBarEvaluation))]
    public class A_CreateProgressBarHudText : Element {}

    [ElementData("Destroy Progress Bar HUD Text")]
    [Parameter("Text ID", ValueType.Any, null)]
    public class A_DestroyProgressBarHudText : Element {}

    [ElementData("Destroy All Progress Bar HUD Text")]
    public class A_DestroyAllProgressBarHudText : Element {}

    [ElementData("Create Progress Bar In-World Text")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Text", ValueType.String, typeof(V_CustomString))]
    [Parameter("Position", ValueType.Vector, typeof(V_Vector))]
    [Parameter("Scale", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Clipping", typeof(Clipping))]
    [Parameter("Progress Bar Color", ValueType.Color, typeof(V_ColorValue))]
    [Parameter("Text Color", ValueType.Color, typeof(V_ColorValue))]
    [EnumParameter("Reevaluation", typeof(ProgressBarEvaluation))]
    [EnumParameter("Nonteam Spectators", typeof(Spectators))]
    public class A_CreateProgressBarInWorldText : Element {}

    [ElementData("Destroy Progress Bar In-World Text")]
    [Parameter("Text ID", ValueType.Any, null)]
    public class A_DestroyProgressBarInWorldText : Element {}

    [ElementData("Destroy All Progress Bar In-World Text")]
    public class A_DestroyAllProgressBarInWorldText : Element {}
}
