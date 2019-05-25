using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Elements
{
    [ElementData("Absolute Value", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_AbsoluteValue : Element {}

    [ElementData("Add", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Add : Element {}

    [ElementData("All Dead Players", ValueType.Player)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_AllDeadPlayers : Element {}

    [ElementData("All Heroes", ValueType.Hero)]
    public class V_AllHeroes : Element {}

    [ElementData("All Living Players", ValueType.Player)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_AllLivingPlayers : Element {}

    [ElementData("All Players", ValueType.Player)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_AllPlayers : Element {}

    [ElementData("All Players Not On Objective", ValueType.Player)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_AllPlayersNotOnObjective : Element {}

    [ElementData("All Players On Objective", ValueType.Player)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_AllPlayersOnObjective : Element {}

    [ElementData("Allowed Heroes", ValueType.Hero)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_AllowedHeroes : Element {}

    [ElementData("Altitude Of", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_AltitudeOf : Element {}

    [ElementData("And", ValueType.Boolean)]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    public class V_And : Element {}

    [ElementData("Angle Difference", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_AngleDifference : Element {}

    [ElementData("Append To Array", ValueType.Any)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Append : Element {}

    [ElementData("Array Contains", ValueType.Boolean)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_ArrayContains : Element {}

    [ElementData("Array Slice", ValueType.Any)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Parameter("Start Index", ValueType.Number, typeof(V_Number))]
    [Parameter("Count", ValueType.Number, typeof(V_Number))]
    public class V_ArraySlice : Element {}

    [ElementData("Attacker", ValueType.Player)]
    public class V_Attacker : Element {}

    [ElementData("Backward", ValueType.Vector)]
    public class V_Backward : Element {}

    [ElementData("Closest Player To", ValueType.Player)]
    [Parameter("Center", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_ClosestPlayerTo : Element {}

    [ElementData("Compare", ValueType.Boolean)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("", typeof(Operators))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Compare : Element {}

    [ElementData("Control Point Scoring Percentage", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_ControlPointScoringPercentage : Element {}

    [ElementData("Control Point Scoring Team", ValueType.Team)]
    public class V_ControlPointScoringTeam : Element {}

    [ElementData("Count Of", ValueType.Number)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    public class V_CountOf : Element {}

    [ElementData("Cross Product", ValueType.Vector)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_CrossProduct : Element {}

    [ElementData("Current Array Element", ValueType.Any)]
    public class V_ArrayElement : Element {}

    [ElementData("Direction From Angles", ValueType.Vector)]
    [Parameter("Horizontal Angle", ValueType.Number, typeof(V_Number))]
    [Parameter("Vertical Angle", ValueType.Number, typeof(V_Number))]
    public class V_DirectionFromAngles : Element {}

    [ElementData("Direction Towards", ValueType.Vector)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_DirectionTowards : Element {}

    [ElementData("Distance Between", ValueType.Number)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_DistanceBetween : Element {}

    [ElementData("Divide", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Divide : Element {}

    [ElementData("Dot Product", ValueType.Number)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_DotProduct : Element {}

    [ElementData("Down", ValueType.Vector)]
    public class V_Down : Element {}

    [ElementData("Empty Array", ValueType.Any)]
    public class V_EmptyArray : Element {}

    [ElementData("Entity Exists", ValueType.Boolean)]
    [Parameter("Entity", ValueType.Player, null)]
    public class V_EntityExists : Element {}

    [ElementData("Event Player", ValueType.Player)]
    public class V_EventPlayer : Element {}

    [ElementData("Eye Position", ValueType.Vector)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_EyePosition : Element {}

    [ElementData("Facing Direction Of", ValueType.Vector)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_FacingDirectionOf : Element {}

    [ElementData("False", ValueType.Boolean)]
    public class V_False : Element {}

    [ElementData("Farthest Player From", ValueType.Player)]
    [Parameter("Center", ValueType.Vector, typeof(V_Vector))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_FarthestPlayerFrom : Element {}

    [ElementData("Filtered Array", ValueType.Any)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
#warning check default type
    [Parameter("Condition", ValueType.Boolean, null)]
    public class V_FilteredArray : Element {}

    [ElementData("First Of", ValueType.Any)]
#warning check default type
    [Parameter("Array", ValueType.Any, null)]
    public class V_FirstOf : Element {}

    [ElementData("Flag Position", ValueType.Vector)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_FlagPosition : Element {}

    [ElementData("Forward", ValueType.Vector)]
    public class V_Forward : Element {}

    [ElementData("Global Variable", ValueType.Any)]
    [Parameter("Variable", typeof(Variable))]
    public class V_GlobalVariable : Element {}

    [ElementData("Has Spawned", ValueType.Boolean)]
    [Parameter("Entity", ValueType.Player, typeof(V_EventPlayer))]
    public class V_HasSpawned : Element {}

    [ElementData("Has Status", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Status", typeof(Status))]
    public class V_HasStatus : Element {}

#warning check health pos
    [ElementData("Health", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_Health : Element {}

    [ElementData("Health Percent", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_HealthPercent : Element {}

#warning check hero pos
    [ElementData("Hero", ValueType.Hero)]
    [Parameter("Hero", typeof(Hero))]
    public class V_Hero : Element {}

    [ElementData("Hero Icon String", ValueType.Any)]
    [Parameter("Value", ValueType.Hero, typeof(V_Hero))]
    public class V_HeroIconString : Element {}

    [ElementData("Hero Of", ValueType.Hero)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_HeroOf : Element {}

    [ElementData("Horizontal Angle From Direction", ValueType.Number)]
    [Parameter("Direction", ValueType.Vector, typeof(V_Vector))]
    public class V_HorizontalAngleFromDirection : Element {}

    [ElementData("Horizontal Angle Towards", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Position", ValueType.Vector, typeof(V_Vector))]
    public class V_HorizontalAngleTowards : Element {}

    [ElementData("Horizontal Facing Angle Of", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_HorizontalFacingAngleOf : Element {}

    [ElementData("Horizontal Speed Of", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_HorizontalSpeedOf : Element {}

    [ElementData("Index Of Array Value", ValueType.Any)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_IndexOfArrayValue : Element {}

    [ElementData("Is Alive", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsAlive : Element {}

    [ElementData("Is Assembling Heroes", ValueType.Boolean)]
    public class V_IsAssemblingHeroes : Element {}

    [ElementData("Is Between Rounds", ValueType.Boolean)]
    public class V_IsBetweenRounds : Element {}

    [ElementData("Is Button Held", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", typeof(Button))]
    public class V_IsButtonHeld : Element {}

#warning check order
    [ElementData("Is Communicating", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Type", typeof(Communication))]
    public class V_IsCommunicating : Element {}

    [ElementData("Is Communicating Any", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsCommunicatingAny : Element {}

    [ElementData("Is Communicating Any Emote", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsCommunicatingAnyEmote : Element {}

    [ElementData("Is Communicating Any Voice Line", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsCommunicatingAnyVoiceLine : Element {}

    [ElementData("Is Control Mode Point Locked", ValueType.Boolean)]
    public class V_IsControlModePointLocked : Element {}

    [ElementData("Is Crouching", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsCrouching : Element {}

    [ElementData("Is CTF Mode In Sudden Death", ValueType.Boolean)]
    public class V_IsInSuddenDeath : Element {}

    [ElementData("Is Dead", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsDead : Element {}

    [ElementData("Is Firing Primary", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsFiringPrimary : Element {}

    [ElementData("Is Firing Secondary", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsFiringSecondary : Element {}

    [ElementData("Is Flag At Base", ValueType.Boolean)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_IsFlagAtBase : Element {}

    [ElementData("Is Game In Progress", ValueType.Boolean)]
    public class V_IsGameInProgress : Element {}

    [ElementData("Is Hero Being Played", ValueType.Boolean)]
    [Parameter("Hero", ValueType.Hero, typeof(V_Hero))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_IsHeroBeingPlayed : Element {}

    [ElementData("Is In Air", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsInAir : Element {}

    [ElementData("Is In Line Of Sight", ValueType.Boolean)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Barriers", typeof(BarrierLOS))]
    public class V_IsInLineOfSight : Element {}

    [ElementData("Is In Setup", ValueType.Boolean)]
    public class V_IsInSetup : Element {}

    [ElementData("Is In Spawn Room", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsInSpawnRoom : Element {}

    [ElementData("Is In View Angle", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Location", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("View Angle", ValueType.Number, typeof(V_Number))]
    public class V_IsInViewAngle : Element {}

    [ElementData("Is Match Complete", ValueType.Boolean)]
    public class V_IsMatchComplete : Element {}

    [ElementData("Is Moving", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsMoving : Element {}

    [ElementData("Is Objective Complete", ValueType.Boolean)]
    [Parameter("Number", ValueType.Number, typeof(V_Number))]
    public class V_IsObjectiveComplete : Element {}

    [ElementData("Is On Ground", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsOnGround : Element { }

    [ElementData("Is On Objective", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsOnObjective : Element {}

    [ElementData("Is On Wall", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsOnWall : Element {}

    [ElementData("Is Portrait On Fire", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsPortraitOnFire : Element {}

    [ElementData("Is Standing", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsStanding : Element {}

    [ElementData("Is Team On Defense", ValueType.Boolean)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_IsTeamOnDefense : Element {}

    [ElementData("Is Team On Offense", ValueType.Boolean)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_IsTeamOnOffense : Element {}

    [ElementData("Is True For All", ValueType.Boolean)]
#warning check default
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    public class V_IsTrueForAll : Element {}

    [ElementData("Is True For Any", ValueType.Boolean)]
#warning check default
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    public class V_IsTrueForAny : Element {}

    [ElementData("Is Using Ability 1", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsUsingAbility1 : Element {}

    [ElementData("Is Using Ability 2", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsUsingAbility2 : Element {}

    [ElementData("Is Using Ultimate", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsUsingUltimate : Element {}

    [ElementData("Is Waiting For Players", ValueType.Boolean)]
    public class V_IsWaitingForPlayers : Element {}

    [ElementData("Last Created Entity", ValueType.Player)]
    public class V_LastCreatedEntity : Element {}

    [ElementData("Last Damage Over Time ID", ValueType.Number)]
    public class V_LastDamageOverTime : Element {}

    [ElementData("Last Heal Over Time ID", ValueType.Number)]
    public class V_LastHealOverTime : Element {}

    [ElementData("Last Of", ValueType.Any)]
#warning check default
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    public class V_LastOf : Element {}

    [ElementData("Last Text ID", ValueType.Number)]
    public class V_LastTextID : Element {}

#warning check order
    [ElementData("Left", ValueType.Number)]
    public class V_Left : Element {}

    [ElementData("Local Vector Of", ValueType.Vector)]
    [Parameter("World Vector", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Relative Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Transformation", typeof(Transformation))]
    public class V_LocalVectorOf : Element {}

    [ElementData("Match Round", ValueType.Number)]
    public class V_MatchRound : Element {}

    [ElementData("Match Time", ValueType.Number)]
    public class V_MatchTime : Element {}

#warning Check order
    [ElementData("Max", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_Max : Element {}

    [ElementData("Max Health", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_MaxHealth : Element {}

#warning Check order
    [ElementData("Min", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_Min : Element {}

    [ElementData("Modulo", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_Modulo : Element {}

    [ElementData("Multiply", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Multiply : Element {}

    [ElementData("Nearest Walkable Position", ValueType.Vector)]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_NearestWalkablePosition : Element {}

    [ElementData("Normalize", ValueType.Number)]
    [Parameter("Vector", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_Normalize : Element {}

    [ElementData("Not", ValueType.Boolean)]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    public class V_Not : Element {}

    [ElementData("Null", ValueType.Any)]
    public class V_Null : Element {}

    [ElementData("Number", ValueType.Number)]
    public class V_Number : Element
    {
        public V_Number(double value)
        {
            this.value = value;
        }
        public V_Number() : this(0) {}

        double value;

        public override string ToWorkshop()
        {
            return value.ToString();
        }

        protected override string Info()
        {
            return $"{ElementData.ElementName} {value}";
        }
    }

    [ElementData("Number Of Dead Players", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_NumberOfDeadPlayers : Element {}

    [ElementData("Number Of Deaths", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_NumberOfDeaths : Element {}

    [ElementData("Number Of Eliminations", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_NumberOfEliminations : Element {}

    [ElementData("Number Of Final Blows", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_NumberOfFinalBlows : Element {}

    [ElementData("Number Of Heroes", ValueType.Number)]
    [Parameter("Hero", ValueType.Hero, typeof(V_Hero))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_NumberOfHeroes : Element {}

    [ElementData("Number Of Living Players", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_NumberOfLivingPlayers : Element {}

    [ElementData("Number Of Players", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_NumberOfPlayers : Element {}

    [ElementData("Number Of Players On Objective", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_NumberOfPlayersOnObjective : Element {}

    [ElementData("Objective Index", ValueType.Number)]
    public class V_ObjectiveIndex : Element {}

    [ElementData("Objective Position", ValueType.Number)]
    [Parameter("Number", ValueType.Number, typeof(V_Number))]
    public class V_ObjectivePosition : Element {}

    [ElementData("Opposite Team Of", ValueType.Team)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_OppositeTeamOf : Element {}

    [ElementData("Or", ValueType.Boolean)]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    public class V_Or : Element {}

    [ElementData("Payload Position", ValueType.Vector)]
    public class V_PayloadPosition : Element {}

    [ElementData("Payload Progress Percentage", ValueType.Number)]
    public class V_PayloadProgressPercentage : Element {}

    [ElementData("Player Carrying Flag", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_PlayerCarryingFlag : Element {}

    [ElementData("Player Closest To Reticle", ValueType.Player)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_PlayerClosestToReticle : Element {}

    [ElementData("Player Variable", ValueType.Any)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Variable", typeof(Variable))]
    public class V_PlayerVariable : Element {}

    [ElementData("Players In Slot", ValueType.Player)]
    [Parameter("Slot", ValueType.Number, typeof(V_Number))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_PlayersInSlot : Element {}

    [ElementData("Players In View Angle", ValueType.Player)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Parameter("View Angle", ValueType.Number, typeof(V_Number))]
    public class V_PlayersInViewAngle : Element {}

    [ElementData("Players On Hero", ValueType.Player)]
    [Parameter("Hero", ValueType.Hero, typeof(V_Hero))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_PlayersOnHero : Element {}

    [ElementData("Players Within Radius", ValueType.Player)]
    [Parameter("Center", ValueType.Vector, typeof(V_Vector))]
    [Parameter("Radius", ValueType.Number, typeof(V_Number))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Parameter("LOS Check", typeof(RadiusLOS))]
    public class V_PlayersWithinRadius : Element {}

    [ElementData("Point Capture Percentage", ValueType.Number)]
    public class V_PointCapturePercentage : Element {}

    [ElementData("Position of", ValueType.Vector)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_PositionOf : Element {}  

    [ElementData("Raise To Power", ValueType.Number)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_RaiseToPower : Element {}

    [ElementData("Random Integer", ValueType.Number)]
    [Parameter("Min", ValueType.Number, typeof(V_Number))]
    [Parameter("Max", ValueType.Number, typeof(V_Number))]
    public class V_RandomInteger : Element {}

    [ElementData("Random Real", ValueType.Number)]
    [Parameter("Min", ValueType.Number, typeof(V_Number))]
    [Parameter("Max", ValueType.Number, typeof(V_Number))]
    public class V_RandomReal : Element {}

    [ElementData("Random Value In Array", ValueType.Any)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    public class V_RandomValueInArray : Element {}

    [ElementData("Randomized Array", ValueType.Number)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    public class V_RandomizedArray : Element {}

    [ElementData("Ray Cast Hit Normal", ValueType.Vector)]
    [Parameter("Start POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Players To Include", ValueType.Player, typeof(V_Null))]
    [Parameter("Players To Exclude", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Include Player Owned Objects", ValueType.Boolean, typeof(V_True))]
    public class V_RayCastHitNormal : Element {}

    [ElementData("Ray Cast Hit Player", ValueType.Player)]
    [Parameter("Start POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Players To Include", ValueType.Player, typeof(V_Null))]
    [Parameter("Players To Exclude", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Include Player Owned Objects", ValueType.Boolean, typeof(V_True))]
    public class V_RayCastHitPlayer : Element {}

    [ElementData("Ray Cast Hit Position", ValueType.Vector)]
    [Parameter("Start POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Players To Include", ValueType.Player, typeof(V_Null))]
    [Parameter("Players To Exclude", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Include Player Owned Objects", ValueType.Boolean, typeof(V_True))]
    public class V_RayCastHitPosition : Element {}

    [ElementData("Remove From Array", ValueType.Any)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_RemoveFromArray : Element {}

    [ElementData("Right", ValueType.Vector)]
    public class V_Right : Element {}

    [ElementData("Round To Integer", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Rounding Type", typeof(Rounding))]
    public class V_RoundToInteger : Element {}

    [ElementData("Score Of", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_ScoreOf : Element {}

    [ElementData("Sine From Degrees", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_SineFromDegrees : Element {}

    [ElementData("Sine From Radians", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_SineFromRadians : Element {}

    [ElementData("Slot Of", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_SlotOf : Element {}

    [ElementData("Sorted Array", ValueType.Number)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Parameter("Value Rank", ValueType.Number, typeof(V_ArrayElement))]
    public class V_SortedArray : Element {}

    [ElementData("Speed Of", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_SpeedOf : Element {}

    [ElementData("Speed Of In Direction", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_SpeedOfInDirection : Element {}

    [ElementData("Square Root", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_SquareRoot : Element {}

    [ElementData("String", ValueType.Any)]
    [Parameter("{0}", ValueType.Any, typeof(V_Null))]
    [Parameter("{1}", ValueType.Any, typeof(V_Null))]
    [Parameter("{2}", ValueType.Any, typeof(V_Null))]
    public class V_String : Element
    {
        public V_String(IToken token, string text, params Element[] stringValues) : base(NullifyEmptyValues(stringValues))
        {
            TextID = Array.IndexOf(Constants.Strings, text);
            if (TextID == -1)
                throw new SyntaxErrorException($"{text} is not a valid string.", token);
        }
        public V_String() : this(null, Constants.DEFAULT_STRING) {}

        public int TextID { get; private set; }

        protected override string[] AdditionalParameters()
        {
            return new string[] { "\"" + Constants.Strings[TextID].Replace("_", " ") + "\"" };
        }

        protected override string Info()
        {
            return $"{ElementData.ElementName} {Constants.Strings[TextID]}";
        }

        private static Log Log = new Log("String Parse");

        /*
         The order of string search:
         - Has Parameters?
         - Has a symbol?
         - Length
        */
        private static string[] searchOrder = Constants.Strings
            .OrderByDescending(str => str.Contains("{0}"))
            .ThenByDescending(str => str.IndexOfAny("-></*-+=()!?".ToCharArray()) != -1)
            .ThenByDescending(str => str.Length)
            .ToArray();

        public static Element ParseString(IToken token, string value, Element[] parameters, int depth = 0)
        {
            value = value.ToLower();

            if (depth == 0)
                Log.Write(LogLevel.Verbose, $"String \"{value}\"");

            string debug = new string(' ', depth * 4);

            // Loop through every string to search for.
            for (int i = 0; i < searchOrder.Length; i++)
            {
                string searchString = searchOrder[i];

                // Converts string parameters ({0}, {1}, {2}) to regex expressions to get the values.
                // {#} -> (.+)
                string regex =
                    Regex.Replace(Escape(searchString)
                    , "({[0-9]})", @"(([a-z_.<>0-9-]+ ?)|(.+))");

                // Add the regex expressions start-of-line and end-of-line to ensure that the entire string is parsed.
                regex = "^" + regex + "$";

                // Match
                var match = Regex.Match(value, regex);
                if (match.Success)
                {
                    Log.Write(LogLevel.Verbose, debug + searchString);

                    // Create a string element with the found string.
                    V_String str = new V_String(token, searchString);

                    bool valid = true; // Confirms that the arguments were able to successfully parse.
                    List<Element> parsedParameters = new List<Element>(); // The parameters that were successfully parsed.

                    // Iterate through the parameters.
                    for (int g = 1; g < match.Groups.Count; g+=3)
                    {
                        string currentParameterValue = match.Groups[g].Captures[0].Value;

                        // Test if the parameter is a format parameter, for example <0>, <1>, <2>, <3>...
                        Match parameterString = Regex.Match(currentParameterValue, "^<([0-9]+)>$");
                        if (parameters != null && parameterString.Success)
                        {
                            int index = int.Parse(parameterString.Groups[1].Value);

                            if (index >= parameters.Length)
                                throw new SyntaxErrorException($"Tried to set the <{index}> format, but there are only {parameters.Length} parameters. Check your string.", token);

                            Log.Write(LogLevel.Verbose, $"{debug}    <param {index}>");
                            parsedParameters.Add(parameters[index]);
                        }
                        else
                        {
                            // Parse the parameter. If it fails it will return null and the string being checked is probably false.
                            var p = ParseString(token, currentParameterValue, parameters, depth + 1);
                            if (p == null)
                            {
                                Log.Write(LogLevel.Verbose, $"{debug}{searchString} combo fail");
                                valid = false;
                                break;
                            }
                            parsedParameters.Add(p);
                        }
                    }
                    str.ParameterValues = parsedParameters.ToArray();

                    if (!valid)
                        continue;
                    return str;
                }
            }

            if (depth > 0)
                return null;
            else
                throw new SyntaxErrorException($"Could not parse the string {value}.", token);
        }

        private static string Escape(string value)
        {
            return value
                .Replace("?", @"\?")
                .Replace("*", @"\*")
                .Replace("(", @"\(")
                .Replace(")", @"\)")
                .Replace(".", @"\.")
                .Replace("/", @"\/")
                ;
        }

        private static Element[] NullifyEmptyValues(Element[] stringValues)
        {
            var stringList = stringValues.ToList();
            while (stringList.Count < 3)
                stringList.Add(new V_Null());

            return stringList.ToArray();
        }
    }

    [ElementData("Subtract", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Subtract : Element {}

    [ElementData("Team", ValueType.Team)]
    [Parameter("Team", typeof(TeamSelector))]
    public class V_Team : Element {}

#warning check order
    [ElementData("TeamOf", ValueType.Team)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_TeamOf : Element {}

    [ElementData("TeamScore", ValueType.Team)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_TeamScore : Element {}

    [ElementData("Throttle Of", ValueType.Vector)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_ThrottleOf : Element {}

    [ElementData("Total Time Elapsed", ValueType.Number)]
    public class V_TotalTimeElapsed : Element {}

    [ElementData("True", ValueType.Boolean)]
    public class V_True : Element {}

    [ElementData("Ultimate Charge Percent", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_UltimateChargePercent : Element {}

#warning check order
    [ElementData("Up", ValueType.Vector)]
    public class V_Up : Element {}

    [ElementData("Value In Array", ValueType.Any)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Parameter("Index", ValueType.Number, typeof(V_EventPlayer))]
    public class V_ValueInArray : Element {}

    [ElementData("Vector", ValueType.Vector)]
    [Parameter("X", ValueType.Number, typeof(V_Number))]
    [Parameter("Y", ValueType.Number, typeof(V_Number))]
    [Parameter("Z", ValueType.Number, typeof(V_Number))]
    public class V_Vector : Element {}

#warning check order
    [ElementData("Vector Towards", ValueType.Vector)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_VectorTowards : Element {}

    [ElementData("Velocity Of", ValueType.Vector)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_VelocityOf : Element {}

    [ElementData("Vertical Angle From Direction", ValueType.Vector)]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_VerticalAngleFromDirection : Element {}

    [ElementData("Vertical Angle Towards", ValueType.Number)]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_VerticalAngleTowards : Element {}

    [ElementData("Vertical Facing Angle Of", ValueType.Number)]
    public class V_VerticalFacingAngleOf : Element {}

    [ElementData("Vertical Speed Of", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_VerticalSpeedOf : Element {}

    [ElementData("Victim", ValueType.Player)]
    public class V_Victim : Element {}

    [ElementData("World Vector Of", ValueType.Vector)]
    [Parameter("Local vector", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Relative Player", ValueType.Player, typeof(V_Vector))]
    [Parameter("Local Vector", typeof(LocalVector))]
    public class V_WorldVectorOf : Element {}

    [ElementData("X Component Of", ValueType.Number)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_XOf : Element {}

    [ElementData("Y Component Of", ValueType.Number)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_YOf : Element {}

    [ElementData("Z Component Of", ValueType.Number)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_ZOf : Element {}
}
