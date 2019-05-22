using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deltin.Deltinteger.Elements
{
    [ElementData("Abort", 0)]
    [Serializable]
    public class A_Abort : Element {}

    [ElementData("Abort If", 0)]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [Serializable]
    public class A_AbortIf : Element {}

    [ElementData("Abort If Condition Is False", 0)]
    [Serializable]
    public class A_AbortIfConditionIsFalse : Element {}

    [ElementData("Abort If Condition Is True", 0)]
    [Serializable]
    public class A_AbortIfConditionIsTrue : Element {}

    [ElementData("Allow Button", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", typeof(Button))]
    [Serializable]
    public class A_AllowButton : Element {}

    [ElementData("Apply Impulse", 0)]
    [Parameter("Player",    ValueType.Player,          typeof(V_EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))     ]
    [Parameter("Speed",     ValueType.Number,          typeof(V_Number))     ]
    [Parameter("Relative",  typeof(Relative))      ]
    [Parameter("Motion",    typeof(ContraryMotion))]
    [Serializable]
    public class A_ApplyImpulse : Element {}

    [ElementData("Big Message", 0)]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_String))]
    [Serializable]
    public class A_BigMessage : Element {}

    [ElementData("Chase Global Variable At Rate", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Rate", ValueType.Number, typeof(V_Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    [Serializable]
    public class A_ChaseGlobalVariableAtRate : Element {}

    [ElementData("Chase Global Variable Over Time", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    [Serializable]
    public class A_ChaseGlobalVariableOverTime : Element {}

    [ElementData("Chase Player Variable At Rate", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Rate", ValueType.Number, typeof(V_Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    [Serializable]
    public class A_ChasePlayerVariableAtRate : Element {}

    [ElementData("Chase Player Variable Over Time", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Destination", ValueType.Any, typeof(V_Number))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [Parameter("Variable", typeof(ChaseReevaluation))]
    [Serializable]
    public class A_ChasePlayerVariableOverTime : Element {}

    [ElementData("Clear Status", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Status", typeof(Status))]
    [Serializable]
    public class A_ClearStatus : Element {}

    [ElementData("Communicate", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Type", typeof(Communication))]
    [Serializable]
    public class A_Communicate : Element {}

    [ElementData("Create Effect", 0)]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Type", typeof(Effect))]
    [Parameter("Color", typeof(Color))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Radius", ValueType.Number, typeof(V_Number))]
    [Parameter("Reevaluation", typeof(EffectRev))]
    [Serializable]
    public class A_CreateEffect : Element {}

    [ElementData("Create Hud Text", 0)]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_String))]
    [Parameter("Subheader", ValueType.Any, typeof(V_Null))]
    [Parameter("Text", ValueType.Any, typeof(V_Null))]
    [Parameter("Location", typeof(Location))]
    [Parameter("Sort Order", ValueType.Number, typeof(V_Number))]
    [Parameter("Header Color", typeof(Color))]
    [Parameter("Subheader Color", typeof(Color))]
    [Parameter("Text Color", typeof(Color))]
    [Parameter("Reevaluation", typeof(StringRev))]
    [Serializable]
    public class A_CreateHudText : Element {}

    [ElementData("Create Icon", 0)]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Icon", typeof(Icon))]
    [Parameter("Reevaluation", typeof(IconRev))]
    [Parameter("Icon Color", typeof(Color))]
    [Parameter("Show When Offscreen", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class A_CreateIcon : Element {}

    [ElementData("Create In-World Text", 0)]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_String))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Scale", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_CreateInWorldText : Element {}

    [ElementData("Damage", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Damager", ValueType.Player, typeof(V_Null))]
    [Parameter("Amount", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_Damage : Element {}

    [ElementData("Declare Match Draw", 0)]
    [Serializable]
    public class A_DeclareMatchDraw : Element {}

    [ElementData("Declare Player Victory", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_DeclarePlayerVictory : Element {}

    [ElementData("Declare Round Victory", 0)]
    [Parameter("Round Winning Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class A_DeclareRoundVictory : Element {}

    [ElementData("Declare team Victory", 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class A_DeclareTeamVictory : Element {}

    [ElementData("Destroy All Effects", 0)]
    [Serializable]
    public class A_DestroyAllEffects : Element {}

    [ElementData("Destroy All HUD Text", 0)]
    [Serializable]
    public class A_DestroyAllHudText : Element {}

    [ElementData("Destroy All Icons", 0)]
    [Serializable]
    public class A_DestroyAllIcons : Element {}

    [ElementData("Destroy Effect", 0)]
    [Parameter("Effect ID", ValueType.Player, null)]
    [Serializable]
    public class A_DestroyEffect : Element {}

    [ElementData("Destroy Hud Text", 0)]
    [Parameter("Text ID", ValueType.Number, null)]
    [Serializable]
    public class A_DestroyHudtext : Element {}

    [ElementData("Destroy Effect", 0)]
    [Parameter("Effect ID", ValueType.Player, null)]
    [Serializable]
    public class A_DestroyIcon : Element {}

    [ElementData("Destroy In-World Text", 0)]
    [Parameter("Text ID", ValueType.Number, null)]
    [Serializable]
    public class A_DestroyInWorldText : Element {}

    [ElementData("Disable Built-In Game Mode Announcer", 0)]
    [Serializable]
    public class A_DisableAnnouncer : Element {}

    [ElementData("Disable Built-In Game Mode Completion", 0)]
    [Serializable]
    public class A_DisableCompletion : Element {}

    [ElementData("Disable Built-In Game Mode Music", 0)]
    [Serializable]
    public class A_DisableMusic : Element {}

    [ElementData("Disable Built-In Game Mode Respawning", 0)]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_DisableRespawning : Element {}

    [ElementData("Disable Built-In Game Mode Scoring", 0)]
    [Serializable]
    public class A_DisableScoring : Element {}

    [ElementData("Disable Death Spectate All Players", 0)]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_DisableSpectate : Element {}

    [ElementData("Disable Death Spectate Target HUD", 0)]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_DisableSpectateHUD : Element {}

    [ElementData("Disallow Button", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", typeof(Button))]
    [Serializable]
    public class A_DisallowButton : Element {}



    [ElementData("Enable Built-In Game Mode Announcer", 0)]
    [Serializable]
    public class A_EnableAnnouncer : Element {}

    [ElementData("Enable Built-In Game Mode Completion", 0)]
    [Serializable]
    public class A_EnableCompletion : Element {}

    [ElementData("Enable Built-In Game Mode Music", 0)]
    [Serializable]
    public class A_EnableMusic : Element {}

    [ElementData("Enable Built-In Game Mode Respawning", 0)]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_EnableRespawning : Element {}

    [ElementData("Enable Built-In Game Mode Scoring", 0)]
    [Serializable]
    public class A_EnableScoring : Element {}

    [ElementData("Enable Death Spectate All Players", 0)]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_EnableSpectate : Element {}

    [ElementData("Enable Death Spectate Target HUD", 0)]
    [Parameter("Players", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_EnableSpectateHUD : Element {}


    [ElementData("Go To Assemble Heroes", 0)]
    [Serializable]
    public class A_AssembleHeroes : Element {}

    [ElementData("Heal", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Healer", ValueType.Player, typeof(V_Null))]
    [Parameter("Amount", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_Heal : Element {}

    [ElementData("Kill", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Killer", ValueType.Player, typeof(V_Null))]
    [Serializable]
    public class A_Kill : Element {}

    [ElementData("Loop", 0)]
    [Serializable]
    public class A_Loop : Element {}

    [ElementData("Loop If", 0)]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [Serializable]
    public class A_LoopIf : Element {}

    [ElementData("Loop If Condition Is False", 0)]
    [Serializable]
    public class A_LoopIfConditionIsFalse : Element {}

    [ElementData("Loop If Condition Is True", 0)]
    [Serializable]
    public class A_LoopIfConditionIsTrue : Element { }

    [ElementData("Modify Global Variable", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Operation", typeof(Operation))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class A_ModifyGlobalVariable : Element {}

    [ElementData("Modify Player Score", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Score", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_ModifyPlayerScore : Element {}

    [ElementData("Modify Player Variable", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Operation", typeof(Operation))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class A_ModifyPlayerVariable : Element {}

    [ElementData("Modify Team Score", 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Parameter("Score", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_ModifyTeamScore : Element {}

    [ElementData("Pause Match Time", 0)]
    [Serializable]
    public class A_PauseMatchTime : Element {}

    [ElementData("Play Effect", 0)]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Type", typeof(PlayEffect))]
    [Parameter("Color", typeof(PlayEffect))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_EventPlayer))]
    [Parameter("Radius", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_PlayEffect : Element {}

    [ElementData("Preload Hero", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
#warning confirm this parameter when the workshop is live
    [Parameter("Hero", typeof(Hero))]
    [Serializable]
    public class A_PreloadHero : Element {}

    [ElementData("Press Button", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", typeof(Button))]
    [Serializable]
    public class A_PressButton : Element {}

    [ElementData("Reset Player Hero Availability", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_ResetHeroRoster : Element {}

    [ElementData("Respawn", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_Respawn : Element {}

    [ElementData("Resurrect", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_Resurrect : Element {}

    [ElementData("Set Ability 1 Enabled", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class A_SetAbility1Enabled : Element {}

    [ElementData("Set Ability 2 Enabled", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class A_SetAbility2Enabled : Element {}

    [ElementData("Set Aim Speed", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Turn Speed Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetAimSpeed : Element {}

    [ElementData("Set Damage Dealt", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Damage Dealt Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetDamageDealt : Element {}

    [ElementData("Set Damage Recieved", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Damage Dealt Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetDamageRecieved : Element {}

    [ElementData("Set Facing", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Relative", typeof(Relative))]
    [Serializable]
    public class A_SetFacing : Element {}

    [ElementData("Set Global Variable", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class A_SetGlobalVariable : Element {}

    [ElementData("Set Global Variable At Index", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Index", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class A_SetGlobalVariableAtIndex : Element {}

    [ElementData("Set Gravity", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Gravity Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetGravity : Element {}

    [ElementData("Set Healing Dealt", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Healing Dealt Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetHealingDealt : Element {}

    [ElementData("Set Healing Received", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Healing Received Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetHealingReceived : Element {}

    [ElementData("Set Invisible", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Invisible To", typeof(InvisibleTo))]
    [Serializable]
    public class A_SetInvisible : Element {}

    [ElementData("Set Match Time", 0)]
    [Parameter("Match Time", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetMatchTime : Element {}

    [ElementData("Set Max Health", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Health Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetMaxHealth : Element {}

    [ElementData("Set Move Speed", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Move Speed Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetMoveSpeed : Element {}

    [ElementData("Set Objective Description", 0)]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_String))]
    [Parameter("Reevaluation", typeof(StringRev))]
    [Serializable]
    public class A_SetObjectiveDescription : Element {}

    [ElementData("Set Player Allowed Heroes", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Hero", ValueType.Hero, null)]
    [Serializable]
    public class A_SetHeroRoster : Element {}

    [ElementData("Set Player Score", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Score", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetPlayerScore : Element {}

    [ElementData("Set Player Variable", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class A_SetPlayerVariable : Element {}

    [ElementData("Set Player Variable At Index", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Variable", typeof(Variable))]
    [Parameter("Index", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class A_SetPlayerVariableAtIndex : Element {}

    [ElementData("Set Primary Fire Enabled", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class A_SetPrimaryFireEnabled : Element {}

    [ElementData("Set Projectile Gravity", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Projectile Gravity Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetProjectileGravity : Element {}

    [ElementData("Set Projectile Speed", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Projectile Speed Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetProjectileSpeed : Element {}

    [ElementData("Set Respawn Max Time", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Time", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetRespawnMaxTime : Element {}

    [ElementData("Set Secondary Fire Enabled", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class A_SetSecondaryFireEnabled : Element {}

    [ElementData("Set Slow Motion", 0)]
    [Parameter("Speed Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetSlowMotion : Element {}

    [ElementData("Set Status", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Assister", ValueType.Player, typeof(V_Null))]
    [Parameter("Status", typeof(Status))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetStatus : Element {}

    [ElementData("Set Team Score", 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Parameter("Score", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetTeamScore : Element {}

    [ElementData("Set Ultimate Ability Enabled", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Enabled", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class A_SetUltimateAbilityEnabled : Element {}

    [ElementData("Set Ultimate Charge", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Charge Percent", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SetUltimateCharge : Element {}

    [ElementData("Skip", 0)]
    [Parameter("Number Of Actions", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_Skip : Element {}

    [ElementData("Skip If", 0)]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [Parameter("Number Of Actions", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_SkipIf : Element {}

    [ElementData("Small Message", 0)]
    [Parameter("Visible To", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Header", ValueType.Any, typeof(V_String))]
    [Serializable]
    public class A_SmallMessage : Element {}

    [ElementData("Start Accelerating", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Rate", ValueType.Number, typeof(V_Number))]
    [Parameter("Max Speed", ValueType.Number, typeof(V_Number))]
    [Parameter("Relative", typeof(Relative))]
    [Parameter("Reevaluation", typeof(AccelerateRev))]
    [Serializable]
    public class A_StartAccelerating : Element {}

    [ElementData("Start Camera", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Eye Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Look At Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Blend Speed", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_StartCamera : Element {}

    [ElementData("Start Damage Modification", 0)]
#warning check defaults when workshop is live
    [Parameter("Receivers", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Damagers", ValueType.Player, typeof(V_AllPlayers))]
    [Parameter("Damage Percent", ValueType.Number, typeof(V_Number))]
    [Parameter("Reevaluation", typeof(ModRev))]
    [Serializable]
    public class A_StartDamageModification : Element {}

    [ElementData("Start Damage Over Time", 0)]
    [Parameter("Receivers", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Damagers", ValueType.Player, typeof(V_Null))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [Parameter("Damage Per Second", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_StartDamageOverTime : Element {}

    [ElementData("Start Facing", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Turn Rate", ValueType.Number, typeof(V_Number))]
    [Parameter("Relative", typeof(Relative))]
    [Parameter("Reevaluation", typeof(FacingRev))]
    [Serializable]
    public class A_StartFacing : Element {}

    [ElementData("Start Forcing Player To Be Hero", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
#warning check defaults when workshop is live
    [Parameter("Hero", ValueType.Hero, typeof(V_AllHeroes))]
    [Serializable]
    public class A_ForcePlayerHero : Element {}

    [ElementData("Start Forcing Spawn Room", 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Parameter("Room", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_ForceSpawnRoom : Element {}

    [ElementData("Start Forcing Throttle", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Min Forward", ValueType.Number, typeof(V_Number))]
    [Parameter("Max Forward", ValueType.Number, typeof(V_Number))]
    [Parameter("Min Backward", ValueType.Number, typeof(V_Number))]
    [Parameter("Max Backward", ValueType.Number, typeof(V_Number))]
    [Parameter("Min Sideways", ValueType.Number, typeof(V_Number))]
    [Parameter("Max Sideways", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_ForceThrottle : Element {}

    [ElementData("Start Heal Over Time", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Healer", ValueType.Player, typeof(V_Null))]
    [Parameter("Duration", ValueType.Number, typeof(V_Number))]
    [Parameter("Healing Per Second", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class A_StartHealOverTime : Element {}

    [ElementData("Start Holding Button", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", typeof(Button))]
    [Serializable]
    public class A_StartHoldingButton : Element {}

    [ElementData("Stop Accelerating", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_StopAccelerating : Element {}

    [ElementData("Stop All Damage Modifications", 0)]
    [Serializable]
    public class A_StopAllDamageModifications : Element {}

    [ElementData("Stop All Damage Over Time", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_StopAllDamageOverTime : Element {}

    [ElementData("Stop All Heal Over Time", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_StopAllHealOverTime : Element {}

    [ElementData("Stop Camera", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_StopCamera : Element {}

    [ElementData("Stop Chasing Global Variable", 0)]
    [Parameter("Variable", typeof(Variable))]
    [Serializable]
    public class A_StopChasingGlobalVariable : Element {}

    [ElementData("Stop Chasing Player Variable", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Variable", typeof(Variable))]
    [Serializable]
    public class A_StopChasingPlayerVariable : Element {}

    [ElementData("Stop Damage Modifications", 0)]
    [Parameter("Damage modification ID", ValueType.Number, null)]
    [Serializable]
    public class A_StopDamageModifications : Element {}

    [ElementData("Stop Damage Over Time", 0)]
    [Parameter("Damage Over Time ID", ValueType.Number, null)]
    [Serializable]
    public class A_StopDamageOverTime : Element {}

    [ElementData("Stop Facing", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_StopFacing : Element {}

    [ElementData("Stop Forcing Player To Be Hero", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_StopForcingHero : Element {}

    [ElementData("Stop Forcing Spawn Room", 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class A_StopForcingSpawnRoom : Element {}

    [ElementData("Stop Forcing Throttle", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_StopForcingThrottle : Element {}

    [ElementData("Stop Heal Over Time", 0)]
    [Parameter("Stop Heal Over Time", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_StopHealOverTime : Element {}

    [ElementData("Stop Holding Button", 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class A_StopHoldingButton : Element {}

    [ElementData("Teleport", 0)]
    [Parameter("Player", ValueType.Any, typeof(V_Number))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class A_Teleport : Element {}

    [ElementData("Unpause Match Time", 0)]
    [Serializable]
    public class A_UnpauseMatchTime : Element {}

    [ElementData("Wait", 0)]
    [Parameter("Time", ValueType.Number, typeof(V_Number))]
    [Parameter("Wait Behavior", typeof(WaitBehavior))]
    [Serializable]
    public class A_Wait : Element {}
}
