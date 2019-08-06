using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deltin.Deltinteger.Elements
{
    [ElementData("Abort")]
    [UsageDiagnostic("This workshop method can leave the workshop in an invalid state. Method callers will also be aborted.", 2)]
    public class A_Abort : Element {}

    [ElementData("Abort If")]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [UsageDiagnostic("This workshop method can leave the workshop in an invalid state. Method callers will also be aborted.", 2)]
    public class A_AbortIf : Element {}

    [ElementData("Abort If Condition Is False")]
    [UsageDiagnostic("This workshop method can leave the workshop in an invalid state. Method callers will also be aborted.", 2)]
    public class A_AbortIfConditionIsFalse : Element {}

    [ElementData("Abort If Condition Is True")]
    [UsageDiagnostic("This workshop method can leave the workshop in an invalid state. Method callers will also be aborted.", 2)]
    public class A_AbortIfConditionIsTrue : Element {}

    [ElementData("Allow Button")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Button", typeof(Button))]
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

    [ElementData("Chase Global Variable At Rate")]
    [EnumParameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Rate", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(RateChaseReevaluation))]
    [UsageDiagnostic("Use ChaseVariable instead.", 3)]
    public class A_ChaseGlobalVariableAtRate : Element {}

    [ElementData("Chase Global Variable Over Time")]
    [EnumParameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(TimeChaseReevaluation))]
    public class A_ChaseGlobalVariableOverTime : Element {}

    [ElementData("Chase Player Variable At Rate")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Rate", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(RateChaseReevaluation))]
    [UsageDiagnostic("Use ChaseVariable instead.", 3)]
    public class A_ChasePlayerVariableAtRate : Element {}

    [ElementData("Chase Player Variable Over Time")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(TimeChaseReevaluation))]
    public class A_ChasePlayerVariableOverTime : Element {}

    [ElementData("Clear Status")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Status", typeof(Status))]
    public class A_ClearStatus : Element {}

    [ElementData("Communicate")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Type", typeof(Communication))]
    public class A_Communicate : Element {}

    [ElementData("Create Effect")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [EnumParameter("Type", typeof(Effect))]
    [EnumParameter("Color", typeof(Color))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Radius", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    public class A_CreateEffect : Element {}

    [ElementData("Create Hud Text")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_String))]
    [Parameter("Subheader", ValueType.Any, typeof(V_Null))]
    [Parameter("Text", ValueType.Any, typeof(V_Null))]
    [EnumParameter("Location", typeof(Location))]
    [Parameter("Sort Order", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Header Color", typeof(Color))]
    [EnumParameter("Subheader Color", typeof(Color))]
    [EnumParameter("Text Color", typeof(Color))]
    [EnumParameter("Reevaluation", typeof(StringRev))]
    public class A_CreateHudText : Element {}

    [ElementData("Create Icon")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [EnumParameter("Icon", typeof(Icon))]
    [EnumParameter("Reevaluation", typeof(IconRev))]
    [EnumParameter("Icon Color", typeof(Color))]
    [Parameter("Show When Offscreen", ValueType.Boolean, typeof(V_True))]
    public class A_CreateIcon : Element {}

    [ElementData("Create In-World Text")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_String))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Scale", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Clipping", typeof(Clipping))]
    [EnumParameter("Reevaluation", typeof(InworldTextRev))]
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

    [ElementData("Destroy All Effects")]
    public class A_DestroyAllEffects : Element {}

    [ElementData("Destroy All HUD Text")]
    public class A_DestroyAllHudText : Element {}

    [ElementData("Destroy All Icons")]
    public class A_DestroyAllIcons : Element {}

    [ElementData("Destroy All In-World Text")]
    public class A_DestroyAllInworldText : Element {}

    [ElementData("Destroy Effect")]
    [Parameter("Effect ID", ValueType.Player, null)]
    public class A_DestroyEffect : Element {}

    [ElementData("Destroy Hud Text")]
    [Parameter("Text ID", ValueType.Number, null)]
    public class A_DestroyHudText : Element {}

    [ElementData("Destroy Effect")]
    [Parameter("Effect ID", ValueType.Player, null)]
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

    [ElementData("Disallow Button")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Button", typeof(Button))]
    public class A_DisallowButton : Element {}



    [ElementData("Enable Built-In Game Mode Announcer")]
    public class A_EnableAnnouncer : Element {}

    [ElementData("Enable Built-In Game Mode Completion")]
    public class A_EnableCompletion : Element {}

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


    [ElementData("Go To Assemble Heroes")]
    public class A_AssembleHeroes : Element {}

    [ElementData("Heal")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Healer", ValueType.Player, typeof(V_Null))]
    [Parameter("Amount", ValueType.Number, typeof(V_Number))]
    public class A_Heal : Element {}

    [ElementData("Kill")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Killer", ValueType.Player, typeof(V_Null))]
    public class A_Kill : Element {}

    [ElementData("Loop")]
    [UsageDiagnostic("This workshop method can leave the workshop in an invalid state. Method callers will also be looped.", 2)]
    public class A_Loop : Element {}

    [ElementData("Loop If")]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [UsageDiagnostic("This workshop method can leave the workshop in an invalid state. Method callers will also be looped.", 2)]
    public class A_LoopIf : Element {}

    [ElementData("Loop If Condition Is False")]
    [UsageDiagnostic("This workshop method can leave the workshop in an invalid state. Method callers will also be looped.", 2)]
    public class A_LoopIfConditionIsFalse : Element {}

    [ElementData("Loop If Condition Is True")]
    [UsageDiagnostic("This workshop method can leave the workshop in an invalid state. Method callers will also be looped.", 2)]
    public class A_LoopIfConditionIsTrue : Element { }

    [ElementData("Modify Global Variable")]
    [EnumParameter("Variable", typeof(Variable))]
    [EnumParameter("Operation", typeof(Operation))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_ModifyGlobalVariable : Element {}

    [ElementData("Modify Player Score")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Score", ValueType.Number, typeof(V_Number))]
    public class A_ModifyPlayerScore : Element {}

    [ElementData("Modify Player Variable")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Variable", typeof(Variable))]
    [EnumParameter("Operation", typeof(Operation))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_ModifyPlayerVariable : Element {}

    [ElementData("Modify Team Score")]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    [Parameter("Score", ValueType.Number, typeof(V_Number))]
    public class A_ModifyTeamScore : Element {}

    [ElementData("Pause Match Time")]
    public class A_PauseMatchTime : Element {}

    [ElementData("Play Effect")]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [EnumParameter("Type", typeof(PlayEffect))]
    [EnumParameter("Color", typeof(Color))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Radius", ValueType.Number, typeof(V_Number))]
    public class A_PlayEffect : Element {}

    [ElementData("Preload Hero")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Hero", ValueType.Hero, typeof(V_HeroVar))]
    public class A_PreloadHero : Element {}

    [ElementData("Press Button")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Button", typeof(Button))]
    public class A_PressButton : Element {}

    [ElementData("Reset Player Hero Availability")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class A_ResetHeroRoster : Element {}

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
    [EnumParameter("Variable", typeof(Variable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_SetGlobalVariable : Element {}

    [ElementData("Set Global Variable At Index")]
    [EnumParameter("Variable", typeof(Variable))]
    [Parameter("Index", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
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
    [EnumParameter("Reevaluation", typeof(StringRev))]
    public class A_SetObjectiveDescription : Element {}

    [ElementData("Set Player Allowed Heroes")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Hero", ValueType.Hero, null)]
    public class A_SetHeroRoster : Element {}

    [ElementData("Set Player Score")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Score", ValueType.Number, typeof(V_Number))]
    public class A_SetPlayerScore : Element {}

    [ElementData("Set Player Variable")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Variable", typeof(Variable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class A_SetPlayerVariable : Element {}

    [ElementData("Set Player Variable At Index")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Variable", typeof(Variable))]
    [Parameter("Index", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
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
    [UsageDiagnostic("This workshop method can leave the workshop in an invalid state.", 2)]
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
    [UsageDiagnostic("This workshop method can leave the workshop in an invalid state.", 2)]
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
    [EnumParameter("Reevaluation", typeof(ModRev))]
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
    [Parameter("Hero", ValueType.Hero, typeof(V_HeroVar))]
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

    [ElementData("Start Heal Over Time")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Healer", ValueType.Player, typeof(V_Null))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [Parameter("Healing Per Second", ValueType.Number, typeof(V_Number))]
    public class A_StartHealOverTime : Element {}

    [ElementData("Start Holding Button")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Button", typeof(Button))]
    public class A_StartHoldingButton : Element {}

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
    [EnumParameter("Variable", typeof(Variable))]
    public class A_StopChasingGlobalVariable : Element {}

    [ElementData("Stop Chasing Player Variable")]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Variable", typeof(Variable))]
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
    [EnumParameter("Button", typeof(Button))]
    public class A_StopHoldingButton : Element {}

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
}