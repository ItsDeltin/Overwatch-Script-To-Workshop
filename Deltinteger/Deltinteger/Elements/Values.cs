using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Elements
{
    [ElementData("Absolute Value", ValueType.Number, 0)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_AbsoluteValue : Element {}

    [ElementData("Add", ValueType.Any, 0)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class V_Add : Element {}

    [ElementData("All Dead Players", ValueType.Player, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_AllDeadPlayers : Element {}

    [ElementData("All Heroes", ValueType.Hero, 0)]
    [Serializable]
    public class V_AllHeroes : Element {}

    [ElementData("All Living Players", ValueType.Player, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_AllLivingPlayers : Element {}

    [ElementData("All Players", ValueType.Player, 2)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_AllPlayers : Element {}

    [ElementData("All Players Not On Objective", ValueType.Player, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_AllPlayersNotOnObjective : Element {}

    [ElementData("All Players On Objective", ValueType.Player, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_AllPlayersOnObjective : Element {}

    [ElementData("Allowed Heroes", ValueType.Hero, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_AllowedHeroes : Element {}

    [ElementData("Altitude Of", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_AltitudeOf : Element {}

    [ElementData("And", ValueType.Boolean, 0)]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class V_And : Element {}

    [ElementData("Angle Difference", ValueType.Number, 0)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_AngleDifference : Element {}

    [ElementData("Append To Array", ValueType.Any, 0)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class V_AppendToArray : Element {}

    [ElementData("Array Contains", ValueType.Boolean, 0)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class V_ArrayContains : Element {}

    [ElementData("Array Slice", ValueType.Any, 0)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Parameter("Start Index", ValueType.Number, typeof(V_Number))]
    [Parameter("Count", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_ArraySlice : Element {}

    [ElementData("Attacker", ValueType.Player, 0)]
    [Serializable]
    public class V_Attacker : Element {}

    [ElementData("Backward", ValueType.Vector, 0)]
    [Serializable]
    public class V_Backward : Element {}

    [ElementData("Closest Player To", ValueType.Player, 0)]
    [Parameter("Center", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_ClosestPlayerTo : Element {}

    [ElementData("Compare", ValueType.Boolean, 0)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("", typeof(Operators))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class V_Compare : Element {}

    [ElementData("Control Point Scoring Percentage", ValueType.Number, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_ControlPointScoringPercentage : Element {}

    [ElementData("Control Point Scoring Team", ValueType.Team, 0)]
    [Serializable]
    public class V_ControlPointScoringTeam : Element {}

    [ElementData("Count Of", ValueType.Number, 0)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Serializable]
    public class V_CountOf : Element {}

    [ElementData("Cross Product", ValueType.Vector, 0)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_CrossProduct : Element {}

    [ElementData("Current Array Element", ValueType.Any, 0)]
    [Serializable]
    public class V_CurrentArrayElement : Element {}

    [ElementData("Direction From Angles", ValueType.Vector, 0)]
    [Parameter("Horizontal Angle", ValueType.Number, typeof(V_Number))]
    [Parameter("Vertical Angle", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_DirectionFromAngles : Element {}

    [ElementData("Direction Towards", ValueType.Vector, 0)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_DirectionTowards : Element {}

    [ElementData("Distance Between", ValueType.Number, 0)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_DistanceBetween : Element {}

    [ElementData("Divide", ValueType.Any, 0)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class V_Divide : Element {}

    [ElementData("Dot Product", ValueType.Number, 0)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class V_DotProduct : Element {}

    [ElementData("Down", ValueType.Vector, 0)]
    [Serializable]
    public class V_Down : Element {}

    [ElementData("Empty Array", ValueType.Any, 0)]
    [Serializable]
    public class V_EmptyArray : Element {}

    [ElementData("Entity Exists", ValueType.Boolean, 0)]
    [Parameter("Entity", ValueType.Player, null)]
    [Serializable]
    public class V_EntityExists : Element {}

    [ElementData("Event Player", ValueType.Player, 0)]
    [Serializable]
    public class V_EventPlayer : Element {}

    [ElementData("Facing Direction Of", ValueType.Vector, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_FacingDirectionOf : Element {}

    [ElementData("False", ValueType.Boolean, 0)]
    [Serializable]
    public class V_False : Element {}

    [ElementData("Farthest Player From", ValueType.Player, 0)]
    [Parameter("Center", ValueType.Vector, typeof(V_Vector))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_FarthestPlayerFrom : Element {}

    [ElementData("Filtered Array", ValueType.Any, 0)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
#warning check default type
    [Parameter("Condition", ValueType.Boolean, null)]
    [Serializable]
    public class V_FilteredArray : Element {}

    [ElementData("First Of", ValueType.Any, 0)]
#warning check default type
    [Parameter("Array", ValueType.Any, null)]
    [Serializable]
    public class V_FirstOf : Element {}

    [ElementData("Flag Position", ValueType.Vector, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_FlagPosition : Element {}

    [ElementData("Forward", ValueType.Vector, 0)]
    [Serializable]
    public class V_Forward : Element {}

    [ElementData("Global Variable", ValueType.Any, 0)]
    [Parameter("Variable", typeof(Variable))]
    [Serializable]
    public class V_GlobalVariable : Element {}

    [ElementData("Has Spawned", ValueType.Boolean, 0)]
#warning default type
    [Parameter("Entity", ValueType.Player, null)]
    [Serializable]
    public class V_HasSpawned : Element {}

    [ElementData("Has Status", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Status", typeof(Status))]
    [Serializable]
    public class V_HasStatus : Element {}

#warning check health pos
    [ElementData("Health", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_Health : Element {}

    [ElementData("Health Percent", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_HealthPercent : Element {}

#warning check hero pos
    [ElementData("Hero", ValueType.Hero, 0)]
    [Parameter("Hero", typeof(Hero))]
    [Serializable]
    public class V_Hero : Element {}

    [ElementData("Hero Icon String", ValueType.String, 0)]
    [Parameter("Value", ValueType.Hero, typeof(V_Hero))]
    [Serializable]
    public class V_HeroIconString : Element {}

    [ElementData("Hero Of", ValueType.Hero, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_HeroOf : Element {}

    [ElementData("Horizontal Angle From Direction", ValueType.Number, 0)]
    [Parameter("Direction", ValueType.Vector, typeof(V_Vector))]
    [Serializable]
    public class V_HorizontalAngleFromDirection : Element {}

    [ElementData("Horizontal Angle Towards", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Position", ValueType.Vector, typeof(V_Vector))]
    [Serializable]
    public class V_HorizontalAngleTowards : Element {}

    [ElementData("Horizontal Facing Angle Of", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_HorizontalFacingAngleOf : Element {}

    [ElementData("Horizontal Speed Of", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_HorizontalSpeedOf : Element {}

    [ElementData("Index Of Array Value", 0)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class V_IndexOfArrayValue : Element {}

    [ElementData("Is Alive", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsAlive : Element {}

    [ElementData("Is Assembling Heroes", ValueType.Boolean, 0)]
    [Serializable]
    public class V_IsAssemblingHeroes : Element {}

    [ElementData("Is Between Rounds", ValueType.Boolean, 0)]
    [Serializable]
    public class V_IsBetweenRounds : Element {}

    [ElementData("Is Button Held", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", typeof(Button))]
    [Serializable]
    public class V_IsButtonHeld : Element {}

#warning check order
    [ElementData("Is Communicating", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Type", typeof(Communication))]
    [Serializable]
    public class V_IsCommunicating : Element {}

    [ElementData("Is Communicating Any", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsCommunicatingAny : Element {}

    [ElementData("Is Communicating Any Emote", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsCommunicatingAnyEmote : Element {}

    [ElementData("Is Communicating Any Voice Line", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsCommunicatingAnyVoiceLine : Element {}

    [ElementData("Is Control Mode Point Locked", ValueType.Boolean, 0)]
    [Serializable]
    public class V_IsControlModePointLocked : Element {}

    [ElementData("Is Crouching", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsCrouching : Element {}

    [ElementData("Is CTF Mode In Sudden Death", ValueType.Boolean, 0)]
    [Serializable]
    public class V_IsInSuddenDeath : Element {}

    [ElementData("Is Dead", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsDead : Element {}

    [ElementData("Is Firing Primary", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsFiringPrimary : Element {}

    [ElementData("Is Firing Secondary", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsFiringSecondary : Element {}

    [ElementData("Is Flag At Base", ValueType.Boolean, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_IsFlagAtBase : Element {}

    [ElementData("Is Game In Progress", ValueType.Boolean, 0)]
    [Serializable]
    public class V_IsGameInProgress : Element {}

    [ElementData("Is Hero Being Played", ValueType.Boolean, 0)]
    [Parameter("Hero", ValueType.Hero, typeof(V_Hero))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_IsHeroBeingPlayed : Element {}

    [ElementData("Is In Air", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsInAir : Element {}

    [ElementData("Is In Line Of Sight", ValueType.Boolean, 0)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Barriers", typeof(BarrierLOS))]
    [Serializable]
    public class V_IsInLineOfSight : Element {}

    [ElementData("Is In Setup", ValueType.Boolean, 0)]
    [Serializable]
    public class V_IsInSetup : Element {}

    [ElementData("Is In Spawn Room", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsInSpawnRoom : Element {}

    [ElementData("Is In View Angle", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Location", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("View Angle", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_IsInViewAngle : Element {}

    [ElementData("Is Match Complete", ValueType.Boolean, 0)]
    [Serializable]
    public class V_IsMatchComplete : Element {}

    [ElementData("Is Moving", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsMoving : Element {}

    [ElementData("Is Objective Complete", ValueType.Boolean, 0)]
    [Parameter("Number", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_IsObjectiveComplete : Element {}

    [ElementData("Is On Ground", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsOnGround : Element { }

    [ElementData("Is On Objective", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsOnObjective : Element {}

    [ElementData("Is On Wall", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsOnWall : Element {}

    [ElementData("Is Portrait On Fire", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsPortraitOnFire : Element {}

    [ElementData("Is Standing", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsStanding : Element {}

    [ElementData("Is Team On Defense", ValueType.Boolean, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_IsTeamOnDefense : Element {}

    [ElementData("Is Team On Offense", ValueType.Boolean, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_IsTeamOnOffense : Element {}

    [ElementData("Is True For All", ValueType.Boolean, 0)]
#warning check default
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [Serializable]
    public class V_IsTrueForAll : Element {}

    [ElementData("Is True For Any", ValueType.Boolean, 0)]
#warning check default
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    [Serializable]
    public class V_IsTrueForAny : Element {}

    [ElementData("Is Using Ability 1", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsUsingAbility1 : Element {}

    [ElementData("Is Using Ability 2", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsUsingAbility2 : Element {}

    [ElementData("Is Using Ultimate", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_IsUsingUltimate : Element {}

    [ElementData("Is Waiting For Players", ValueType.Boolean, 0)]
    [Serializable]
    public class V_IsWaitingForPlayers : Element {}

    [ElementData("Last Created Entity", ValueType.Player, 0)]
    [Serializable]
    public class V_LastCreatedEntity : Element {}

    [ElementData("Last Damage Over Time ID", ValueType.Number, 0)]
    [Serializable]
    public class V_LastDamageOverTime : Element {}

    [ElementData("Last Heal Over Time ID", ValueType.Number, 0)]
    [Serializable]
    public class V_LastHealOverTime : Element {}

    [ElementData("Last Of", ValueType.Any, 0)]
#warning check default
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Serializable]
    public class V_LastOf : Element {}

    [ElementData("Last Text ID", ValueType.Number, 0)]
    [Serializable]
    public class V_LastTextID : Element {}

#warning check order
    [ElementData("Left", ValueType.Number, 0)]
    [Serializable]
    public class V_Left : Element {}

    [ElementData("Local Vector Of", ValueType.Vector, 0)]
    [Parameter("World Vector", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Relative Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Transformation", typeof(Transformation))]
    [Serializable]
    public class V_LocalVectorOf : Element {}

    [ElementData("Match Round", ValueType.Number, 0)]
    [Serializable]
    public class V_MatchRound : Element {}

    [ElementData("Match Time", ValueType.Number, 0)]
    [Serializable]
    public class V_MatchTime : Element {}

#warning Check order
    [ElementData("Max", ValueType.Number, 0)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_Max : Element {}

    [ElementData("Max Health", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_MaxHealth : Element {}

#warning Check order
    [ElementData("Min", ValueType.Number, 0)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_Min : Element {}

    [ElementData("Modulo", ValueType.Number, 0)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_Modulo : Element {}

    [ElementData("Multiply", ValueType.Any, 0)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class V_Multiply : Element {}

    [ElementData("Nearest Walkable Position", ValueType.Vector, 0)]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_NearestWalkablePosition : Element {}

    [ElementData("Normalize", ValueType.Number, 0)]
    [Parameter("Vector", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_Normalize : Element {}

    [ElementData("Not", ValueType.Boolean, 1)]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class V_Not : Element {}

    [ElementData("Null", ValueType.Any, 0)]
    [Serializable]
    public class V_Null : Element {}

    [ElementData("Number", ValueType.Number, 0)]
    [Serializable]
    public class V_Number : Element
    {
        public V_Number(double value)
        {
            this.value = value;
        }
        public V_Number() : this(0) {}

        double value;

        protected override void AfterParameters()
        {
            InputSim.Press(Keys.Down, Wait.Short);

            // Clear the text
            InputSim.Press(Keys.D0, Wait.Short);
            InputSim.Press(Keys.Back, Wait.Short);

            InputSim.NumberInput(value);

            InputSim.Press(Keys.Enter, Wait.Short);
        }

        protected override string Info()
        {
            return $"{ElementData.ElementName} {value}";
        }

        protected override bool AdditionalEquals(Element other)
        {
            return (other as V_Number).value == value;
        }

        protected override int AdditionalGetHashCode()
        {
            return value.GetHashCode();
        }
    }

    [ElementData("Number Of Dead Players", ValueType.Number, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_NumberOfDeadPlayers : Element {}

    [ElementData("Number Of Deaths", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_NumberOfDeaths : Element {}

    [ElementData("Number Of Eliminations", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_NumberOfEliminations : Element {}

    [ElementData("Number Of Final Blows", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_NumberOfFinalBlows : Element {}

    [ElementData("Number Of Heroes", ValueType.Number, 0)]
    [Parameter("Hero", ValueType.Hero, typeof(V_Hero))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_NumberOfHeroes : Element {}

    [ElementData("Number Of Living Players", ValueType.Number, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_NumberOfLivingPlayers : Element {}

    [ElementData("Number Of Players", ValueType.Number, 2)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_NumberOfPlayers : Element {}

    [ElementData("Number Of Players On Objective", ValueType.Number, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_NumberOfPlayersOnObjective : Element {}

    [ElementData("Objective Index", ValueType.Number, 0)]
    [Serializable]
    public class V_ObjectiveIndex : Element {}

    [ElementData("Objective Position", ValueType.Number, 0)]
    [Parameter("Number", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_ObjectivePosition : Element {}

    [ElementData("Opposite Team Of", ValueType.Team, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_OppositeTeamOf : Element {}

    [ElementData("Or", ValueType.Boolean, 13)]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class V_Or : Element {}

    [ElementData("Payload Position", ValueType.Vector, 0)]
    [Serializable]
    public class V_PayloadPosition : Element {}

    [ElementData("Payload Progress Percentage", ValueType.Number, 0)]
    [Serializable]
    public class V_PayloadProgressPercentage : Element {}

    [ElementData("Player Carrying Flag", ValueType.Number, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_PlayerCarryingFlag : Element {}

    [ElementData("Player Closest To Reticle", ValueType.Player, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_PlayerClosestToReticle : Element {}

    [ElementData("Player Variable", ValueType.Any, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Variable", typeof(Variable))]
    [Serializable]
    public class V_PlayerVariable : Element {}

    [ElementData("Players In Slot", ValueType.Player, 0)]
    [Parameter("Slot", ValueType.Number, typeof(V_Number))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_PlayersInSlot : Element {}

    [ElementData("Players In View Angle", ValueType.Player, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Parameter("View Angle", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_PlayersInViewAngle : Element {}

    [ElementData("Players On Hero", ValueType.Player, 0)]
    [Parameter("Hero", ValueType.Hero, typeof(V_Hero))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_PlayersOnHero : Element {}

    [ElementData("Players Within Radius", ValueType.Player, 0)]
    [Parameter("Center", ValueType.Vector, typeof(V_Vector))]
    [Parameter("Radius", ValueType.Number, typeof(V_Number))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Parameter("LOS Check", typeof(RadiusLOS))]
    [Serializable]
    public class V_PlayersWithinRadius : Element {}

    [ElementData("Point Capture Percentage", ValueType.Number, 0)]
    [Serializable]
    public class V_PointCapturePercentage : Element {}

    [ElementData("Position of", ValueType.Vector, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_PositionOf : Element {}  

    [ElementData("Raise To Power", ValueType.Number, 0)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class V_RaiseToPower : Element {}

    [ElementData("Random Integer", ValueType.Number, 0)]
    [Parameter("Min", ValueType.Number, typeof(V_Number))]
    [Parameter("Max", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_RandomInteger : Element {}

    [ElementData("Random Real", ValueType.Number, 0)]
    [Parameter("Min", ValueType.Number, typeof(V_Number))]
    [Parameter("Max", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_RandomReal : Element {}

    [ElementData("Random Value In Arary", ValueType.Any, 0)]
#warning check default type
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Serializable]
    public class V_RandomValueInArray : Element {}

    [ElementData("Randomized Array", ValueType.Number, 0)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Serializable]
    public class V_RandomizedArray : Element {}

    [ElementData("Ray Cast Hit Normal", ValueType.Vector, 0)]
    [Parameter("Start POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Players To Include", ValueType.Player, typeof(V_Null))]
    [Parameter("Players To Exclude", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Include Player Owned Objects", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class V_RayCastHitNormal : Element {}

    [ElementData("Ray Cast Hit Player", ValueType.Player, 0)]
    [Parameter("Start POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Players To Include", ValueType.Player, typeof(V_Null))]
    [Parameter("Players To Exclude", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Include Player Owned Objects", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class V_RayCastHitPlayer : Element {}

    [ElementData("Ray Cast Hit Position", ValueType.Vector, 0)]
    [Parameter("Start POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End POS", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Players To Include", ValueType.Player, typeof(V_Null))]
    [Parameter("Players To Exclude", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Include Player Owned Objects", ValueType.Boolean, typeof(V_True))]
    [Serializable]
    public class V_RayCastHitPosition : Element {}

#warning check order and default
    [ElementData("Remove From Array", ValueType.Any, 0)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class V_RemoveFromArray : Element {}

    [ElementData("Right", ValueType.Vector, 0)]
    [Serializable]
    public class V_Right : Element {}

    [ElementData("Round To Integer", ValueType.Number, 0)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Rounding Type", typeof(Rounding))]
    [Serializable]
    public class V_RoundToInteger : Element {}

    [ElementData("Score Of", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_ScoreOf : Element {}

    [ElementData("Sine From Degrees", ValueType.Number, 0)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_SineFromDegrees : Element {}

    [ElementData("Sine From Radians", ValueType.Number, 0)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_SineFromRadians : Element {}

    [ElementData("Slot Of", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_SlotOf : Element {}

#warning check default
    [ElementData("Sorted Array", ValueType.Number, 0)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Parameter("Value Rank", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_SortedArray : Element {}

#warning check order
    [ElementData("Speed Of", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_SpeedOf : Element {}

    [ElementData("Speed Of In Direction", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_SpeedOfInDirection : Element {}

    [ElementData("Square Root", ValueType.Number, 0)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_SquareRoot : Element {}

    [ElementData("String", ValueType.String, 1)]
    [Parameter("{0}", ValueType.Any, typeof(V_Null))]
    [Parameter("{1}", ValueType.Any, typeof(V_Null))]
    [Parameter("{2}", ValueType.Any, typeof(V_Null))]
    [Serializable]
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

        protected override void BeforeParameters()
        {
            string value = Constants.Strings[TextID]
                .Replace('_', ' ');

            // Select "string" option
            InputSim.Press(Keys.Down, Wait.Short);

            // Open the string list
            InputSim.Press(Keys.Space, Wait.Long);

            // Search the string
            InputSim.TextInput(value, Wait.Long);

            // Leave the search field input
            InputSim.Press(Keys.Enter, Wait.Short);

            /*
            Searching for "Down" results in:
            - Cooldown
            - Cooldowns
            - Down
            - Download
            - Downloaded
            - Downloading
            */
            var conflicting = Constants.Strings.Where(@string => value.Split(' ').All(valueWord => @string.Split(' ').Any(stringWord => stringWord.Contains(valueWord)))).ToList();

            int before = conflicting.IndexOf(value);
            if (before == -1)
                before = 0;

            // Select the selected string by textID.
            InputSim.Press(Keys.Down, Wait.Short, before);

            // Select the string
            InputSim.Press(Keys.Space, Wait.Long);
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
                Log.Write($"\"{value}\"");

            string debug = new string(' ', depth * 4);

            for (int i = 0; i < searchOrder.Length; i++)
            {
                string searchString = searchOrder[i];

                string regex =
                    Regex.Replace(Escape(searchString)
                    , "({[0-9]})", @"(([a-z_.]+ ?)|(.+))");  // Converts {0} {1} {2} to (.+) (.+) (.+)
                regex = $"^{regex}$";
                var match = Regex.Match(value, regex);

                if (match.Success)
                {
                    Log.Write(debug + searchString);
                    V_String str = new V_String(token, searchString);

                    bool valid = true;
                    List<Element> parsedParameters = new List<Element>();
                    for (int g = 1; g < match.Groups.Count; g+=3)
                    {
                        string currentParameterValue = match.Groups[g].Captures[0].Value;

                        Match parameterString = Regex.Match(currentParameterValue, "^<([0-9]+)>$");
                        if (parameters != null && parameterString.Success)
                        {
                            int index = int.Parse(parameterString.Groups[1].Value);

                            if (index >= parameters.Length)
                                throw new SyntaxErrorException($"Tried to set the <{index}> format, but there are only {parameters.Length} parameters. Check your string.", token);

                            Log.Write($"{debug}    <param {index}>");
                            parsedParameters.Add(parameters[index]);
                        }
                        else
                        {
                            var p = ParseString(token, currentParameterValue, parameters, depth + 1);
                            if (p == null)
                            {
                                Log.Write($"{debug}{searchString} combo fail");
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

        protected override bool AdditionalEquals(Element other)
        {
            return (other as V_String).TextID == TextID;
        }

        protected override int AdditionalGetHashCode()
        {
            return Constants.Strings[TextID].GetHashCode();
        }
    }

    [ElementData("Subtract", ValueType.Any, 0)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Serializable]
    public class V_Subtract : Element {}

    [ElementData("Team", ValueType.Team, 4)]
    [Parameter("Team", typeof(TeamSelector))]
    [Serializable]
    public class V_Team : Element {}

#warning check order
    [ElementData("TeamOf", ValueType.Team, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_TeamOf : Element {}

    [ElementData("TeamScore", ValueType.Team, 0)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    [Serializable]
    public class V_TeamScore : Element {}

    [ElementData("Throttle Of", ValueType.Vector, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_ThrottleOf : Element {}

    [ElementData("Total Time Elapsed", ValueType.Number, 0)]
    [Serializable]
    public class V_TotalTimeElapsed : Element {}

    [ElementData("True", ValueType.Boolean, 2)]
    [Serializable]
    public class V_True : Element {}

    [ElementData("Ultimate Charge Percent", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_UltimateChargePercent : Element {}

#warning check order
    [ElementData("Up", ValueType.Vector, 0)]
    [Serializable]
    public class V_Up : Element {}

    [ElementData("Value In Array", ValueType.Any, 2)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Parameter("Index", ValueType.Number, typeof(V_EventPlayer))]
    [Serializable]
    public class V_ValueInArray : Element {}

    [ElementData("Vector", ValueType.Vector, 1)]
    [Parameter("X", ValueType.Number, typeof(V_Number))]
    [Parameter("Y", ValueType.Number, typeof(V_Number))]
    [Parameter("Z", ValueType.Number, typeof(V_Number))]
    [Serializable]
    public class V_Vector : Element {}

#warning check order
    [ElementData("Vector Towards", ValueType.Vector, 0)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_VectorTowards : Element {}

    [ElementData("Velocity Of", ValueType.Vector, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_VelocityOf : Element {}

    [ElementData("Vertical Angle From Direction", ValueType.Vector, 0)]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_VerticalAngleFromDirection : Element {}

    [ElementData("Vertical Angle Towards", ValueType.Number, 0)]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_VerticalAngleTowards : Element {}

    [ElementData("Vertical Facing Angle Of", ValueType.Number, 0)]
    [Serializable]
    public class V_VerticalFacingAngleOf : Element {}

    [ElementData("Vertical Speed Of", ValueType.Number, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Serializable]
    public class V_VerticalSpeedOf : Element {}

    [ElementData("Victim", ValueType.Player, 0)]
    [Serializable]
    public class V_Victim : Element {}

    [ElementData("World Vector Of", ValueType.Vector, 0)]
    [Parameter("Local vector", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Relative Player", ValueType.Player, typeof(V_Vector))]
    [Parameter("Local Vector", typeof(LocalVector))]
    [Serializable]
    public class V_WorldVectorOf : Element {}

    [ElementData("X Component Of", ValueType.Number, 0)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_XOf : Element {}

    [ElementData("Y Component Of", ValueType.Number, 0)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_YOf : Element {}

    [ElementData("Z Component Of", ValueType.Number, 0)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Serializable]
    public class V_ZOf : Element {}
}
