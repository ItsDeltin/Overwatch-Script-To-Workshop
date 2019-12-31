using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Deltin.Deltinteger.Parse;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Elements
{
    [ElementData("Absolute Value", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_AbsoluteValue : Element
    {
        override public bool ConstantSupported<T>()
        {
            if (ParameterValues.Length == 0) return true;
            return typeof(T) == typeof(double)
                && ParameterValues[0] is Element
                && ((Element)ParameterValues[0]).ConstantSupported<double>();
        }

        override public object GetConstant()
        {
            if (ParameterValues.Length == 0) return 0;
            return Math.Abs((double)((Element)ParameterValues[0]).GetConstant());
        }

        override public Element Optimize()
        {
            OptimizeChildren();

            if (ParameterValues[0] is V_Number)
                return Math.Abs(((V_Number)ParameterValues[0]).Value);
            
            return this;
        }
    }

    [ElementData("Add", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Add : Element
    {
        override public Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a is V_Number && ParameterValues[1] is V_Number)
                return ((V_Number)a).Value + ((V_Number)b).Value;
            
            if (a.ConstantSupported<Models.Vertex>() && b.ConstantSupported<Models.Vertex>())
            {
                var aVertex = (Models.Vertex)a.GetConstant();
                var bVertex = (Models.Vertex)b.GetConstant();

                return new V_Vector(
                    aVertex.X + bVertex.X,
                    aVertex.Y + bVertex.Y,
                    aVertex.Z + bVertex.Z
                );
            }

            if (a is V_Number)
            {
                if (((V_Number)a).Value == 0)
                    return b;
            }
            else if (b is V_Number)
            {
                if (((V_Number)b).Value == 0)
                    return a;
            }

            if (a.Equals(b))
                return a * 2;
            
            return this;
        }
    }

    [ElementData("All Dead Players", ValueType.Player)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_AllDeadPlayers : Element {}

    [ElementData("All Heroes", ValueType.Hero)]
    public class V_AllHeroes : Element {}

    [ElementData("All Living Players", ValueType.Player)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_AllLivingPlayers : Element {}

    [ElementData("All Players", ValueType.Player)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_AllPlayers : Element {}

    [ElementData("All Players Not On Objective", ValueType.Player)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_AllPlayersNotOnObjective : Element {}

    [ElementData("All Players On Objective", ValueType.Player)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
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
    public class V_And : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a is V_False || b is V_False)
                return false;

            if (a is V_True)
                return b;
            else if (b is V_True)
                return a;

            if (a is V_True && b is V_True)
                return true;

            if (a.Equals(b))
                return a;

            if (a is V_Not)
            {
                if (((Element)a.ParameterValues[0]).Equals(b))
                    return false;
            }
            else if (b is V_Not)
            {
                if (((Element)b.ParameterValues[0]).Equals(a))
                    return false;
            }

            return this;
        }
    }

    [ElementData("Angle Between Vectors", ValueType.Vector)]
    [Parameter("Vector", ValueType.Vector, typeof(V_Vector))]
    [Parameter("Vector", ValueType.Vector, typeof(V_Vector))]
    public class V_AngleBetweenVectors : Element {}

    [ElementData("Angle Difference", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_AngleDifference : Element {}

    [ElementData("Append To Array", ValueType.Any)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Append : Element {}

    [ElementData("Arccosine In Degrees", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_ArccosineInDegrees : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Number)
                return Math.Acos(((V_Number)a).Value) * (180 / Math.PI);

            return this;
        }
    }

    [ElementData("Arccosine In Radians", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_ArccosineInRadians : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Number)
                return Math.Acos(((V_Number)a).Value);

            return this;
        }
    }

    [ElementData("Arcsine In Degrees", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_ArcsineInDegrees : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Number)
                return Math.Asin(((V_Number)a).Value) * (180 / Math.PI);

            return this;
        }
    }

    [ElementData("Arcsine In Radians", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_ArcsineInRadians : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Number)
                return Math.Asin(((V_Number)a).Value);

            return this;
        }
    }

    [ElementData("Arctangent In Degrees", ValueType.Number)]
    [Parameter("Numerator", ValueType.Number, typeof(V_Number))]
    [Parameter("Denominator", ValueType.Number, typeof(V_Number))]
    public class V_ArctangentInDegrees : Element {}

    [ElementData("Arctangent In Radians", ValueType.Number)]
    [Parameter("Numerator", ValueType.Number, typeof(V_Number))]
    [Parameter("Denominator", ValueType.Number, typeof(V_Number))]
    public class V_ArctangentInRadians : Element {}

    [ElementData("Array Contains", ValueType.Boolean)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_ArrayContains : Element {}

    [ElementData("Array Slice", ValueType.Any)]
    [Parameter("Array", ValueType.Any, null)]
    [Parameter("Start Index", ValueType.Number, typeof(V_Number))]
    [Parameter("Count", ValueType.Number, typeof(V_Number))]
    public class V_ArraySlice : Element {}

    [ElementData("Attacker", ValueType.Player)]
    public class V_Attacker : Element {}

    [ElementData("Backward", ValueType.Vector)]
    public class V_Backward : Element {}

    [ElementData("Closest Player To", ValueType.Player)]
    [Parameter("Center", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_ClosestPlayerTo : Element {}

    [ElementData("Compare", ValueType.Boolean)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [EnumParameter("", typeof(Operators))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Compare : Element 
    {
        public V_Compare() : base() {}

        public V_Compare(IWorkshopTree left, Operators op, IWorkshopTree right) : base(left, EnumData.GetEnumValue(op), right) {}

        public override Element Optimize()
        {
            OptimizeChildren();

            Element left = (Element)ParameterValues[0];
            Operators op = (Operators)((EnumMember)ParameterValues[1]).Value;
            Element right = (Element)ParameterValues[0];

            if (op == Operators.Equal)
            {
                if (left.Equals(right))
                    return true;
                if (left is V_Number && right is V_Number)
                    return (double)left.GetConstant() == (double)right.GetConstant();
            }
            else if (op == Operators.GreaterThan)
            {
                if (left.Equals(right))
                    return false;
                if (left is V_Number && right is V_Number)
                    return (double)left.GetConstant() > (double)right.GetConstant();
            }
            else if (op == Operators.GreaterThanOrEqual)
            {
                if (left.Equals(right))
                    return true;
                if (left is V_Number && right is V_Number)
                    return (double)left.GetConstant() >= (double)right.GetConstant();
            }
            else if (op == Operators.LessThan)
            {
                if (left.Equals(right))
                    return false;
                if (left is V_Number && right is V_Number)
                    return (double)left.GetConstant() < (double)right.GetConstant();
            }
            else if (op == Operators.LessThanOrEqual)
            {
                if (left.Equals(right))
                    return true;
                if (left is V_Number && right is V_Number)
                    return (double)left.GetConstant() <= (double)right.GetConstant();
            }
            else if (op == Operators.NotEqual)
            {
                if (left.Equals(right))
                    return false;
                if (left is V_Number && right is V_Number)
                    return (double)left.GetConstant() != (double)right.GetConstant();
            }

            return this;
        }
    }

    [ElementData("Control Mode Scoring Percentage", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_ControlPointScoringPercentage : Element {}

    [ElementData("Control Mode Scoring Team", ValueType.Team)]
    public class V_ControlPointScoringTeam : Element {}

    [ElementData("Count Of", ValueType.Number)]
    [Parameter("Array", ValueType.Any, null)]
    public class V_CountOf : Element {}

    [ElementData("Cosine From Degrees", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_CosineFromDegrees : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Number)
                return Math.Cos(((V_Number)a).Value * (Math.PI / 180));

            return this;
        }
    }

    [ElementData("Cosine From Radians", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_CosineFromRadians : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Number)
                return Math.Cos(((V_Number)a).Value);

            return this;
        }
    }

    [ElementData("Cross Product", ValueType.Vector)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_CrossProduct : Element {}

    [ElementData("Current Map", ValueType.Map)]
    public class V_CurrentMap : Element {}

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
    public class V_Divide : Element
    {
        override public Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            // Divide number and number
            if (a is V_Number && b is V_Number)
                return ((V_Number)ParameterValues[0]).Value / ((V_Number)ParameterValues[1]).Value;
            
            // Divide vector and number
            if ((a is V_Vector && b is V_Number) || (a is V_Number && b is V_Vector))
            {
                V_Vector vector = a is V_Vector ? (V_Vector)a : (V_Vector)b;
                V_Number number = a is V_Number ? (V_Number)a : (V_Number)b;

                if (vector.ConstantSupported<Models.Vertex>())
                {
                    Models.Vertex vertex = (Models.Vertex)vector.GetConstant();
                    return new V_Vector(
                        vertex.X / number.Value,
                        vertex.Y / number.Value,
                        vertex.Z / number.Value
                    );
                }
            }

            if (b is V_Number)
            {
                if (((V_Number)b).Value == 1)
                    return a;
                if (((V_Number)b).Value == 0)
                    return 0;
            }

            if (a is V_Number)
                if (((V_Number)a).Value == 0)
                    return 0;

            return this;
        }
    }

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

    [ElementData("Event Damage", ValueType.Number)]
    public class V_EventDamage : Element {}

    [ElementData("Event Healing", ValueType.Number)]
    public class V_EventHealing : Element {}

    [ElementData("Event Player", ValueType.Player)]
    public class V_EventPlayer : Element {}

    [ElementData("Event Was Critical Hit", ValueType.Boolean)]
    public class V_EventWasCriticalHit : Element {}

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
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_FarthestPlayerFrom : Element {}

    [ElementData("Filtered Array", ValueType.Any)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    public class V_FilteredArray : Element {}

    [ElementData("First Of", ValueType.Any)]
    [Parameter("Array", ValueType.Any, null)]
    public class V_FirstOf : Element {}

    [ElementData("Flag Position", ValueType.Vector)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_FlagPosition : Element {}

    [ElementData("Forward", ValueType.Vector)]
    public class V_Forward : Element {}

    [ElementData("Game Mode", ValueType.Vector)]
    [EnumParameter("Gamemode", typeof(GameMode))]
    public class V_GameModeVar : Element {}

    [ElementData("Current Game Mode", ValueType.Vector)]
    public class V_CurrentGameMode : Element {}

    [ElementData("Global Variable", ValueType.Any)]
    [VarRefParameter("Variable", true)]
    public class V_GlobalVariable : Element {}

    [ElementData("Has Spawned", ValueType.Boolean)]
    [Parameter("Entity", ValueType.Player, typeof(V_EventPlayer))]
    public class V_HasSpawned : Element {}

    [ElementData("Has Status", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Status", typeof(Status))]
    public class V_HasStatus : Element {}

    [ElementData("Healee", ValueType.Player)]
    public class V_Healee : Element {}

    [ElementData("Healer", ValueType.Player)]
    public class V_Healer : Element {}

    [ElementData("Health", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_Health : Element {}

    [ElementData("Hero", ValueType.Hero)]
    [EnumParameter("Hero", typeof(Hero))]
    public class V_HeroVar : Element {}

    [ElementData("Hero Icon String", ValueType.Any)]
    [Parameter("Value", ValueType.Hero, typeof(V_HeroVar))]
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

    [ElementData("Host Player", ValueType.Player)]
    public class V_HostPlayer : Element {}

    [ElementData("Index Of Array Value", ValueType.Any)]
    [Parameter("Array", ValueType.Any, null)]
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
    [EnumParameter("Button", typeof(Button))]
    public class V_IsButtonHeld : Element {}

    [ElementData("Is Communicating", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Type", typeof(Communication))]
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

    [ElementData("Is Dummy Bot", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsDummyBot : Element {}

    [ElementData("Is Firing Primary", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsFiringPrimary : Element {}

    [ElementData("Is Firing Secondary", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsFiringSecondary : Element {}

    [ElementData("Is Flag At Base", ValueType.Boolean)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_IsFlagAtBase : Element {}

    [ElementData("Is Game In Progress", ValueType.Boolean)]
    public class V_IsGameInProgress : Element {}

    [ElementData("Is Hero Being Played", ValueType.Boolean)]
    [Parameter("Hero", ValueType.Hero, typeof(V_HeroVar))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_IsHeroBeingPlayed : Element {}

    [ElementData("Is In Air", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsInAir : Element {}

    [ElementData("Is In Line Of Sight", ValueType.Boolean)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [EnumParameter("Barriers", typeof(BarrierLOS))]
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
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_IsTeamOnDefense : Element {}

    [ElementData("Is Team On Offense", ValueType.Boolean)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_IsTeamOnOffense : Element {}

    [ElementData("Is True For All", ValueType.Boolean)]
    [Parameter("Array", ValueType.Any, null)]
    [Parameter("Condition", ValueType.Boolean, typeof(V_Compare))]
    public class V_IsTrueForAll : Element {}

    [ElementData("Is True For Any", ValueType.Boolean)]
    [Parameter("Array", ValueType.Any, null)]
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

    [ElementData("Last Damage Modification ID", ValueType.Number)]
    public class V_LastDamageModificationID : Element {}

    [ElementData("Last Damage Over Time ID", ValueType.Number)]
    public class V_LastDamageOverTime : Element {}

    [ElementData("Last Heal Over Time ID", ValueType.Number)]
    public class V_LastHealOverTime : Element {}

    [ElementData("Last Of", ValueType.Any)]
    [Parameter("Array", ValueType.Any, null)]
    public class V_LastOf : Element {}

    [ElementData("Last Text ID", ValueType.Number)]
    public class V_LastTextID : Element {}

    [ElementData("Left", ValueType.Vector)]
    public class V_Left : Element {}

    [ElementData("Local Vector Of", ValueType.Vector)]
    [Parameter("World Vector", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Relative Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Transformation", typeof(Transformation))]
    public class V_LocalVectorOf : Element {}

    [ElementData("Map", ValueType.Map)]
    [EnumParameter("Map", typeof(Map))]
    public class V_MapVar : Element {}

    [ElementData("Match Round", ValueType.Number)]
    public class V_MatchRound : Element {}

    [ElementData("Match Time", ValueType.Number)]
    public class V_MatchTime : Element {}

    [ElementData("Max", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_Max : Element {}

    [ElementData("Max Health", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_MaxHealth : Element {}

    [ElementData("Min", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_Min : Element {}

    [ElementData("Modulo", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_Modulo : Element
    {
        override public Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a is V_Number && b is V_Number)
                return ((V_Number)a).Value % ((V_Number)b).Value;

            if (a is V_Number)
            {
                if (((V_Number)a).Value == 0)
                    return 0;
                if (((V_Number)a).Value == 1)
                    return 1;
            }

            if (b is V_Number)
            {
                if (((V_Number)b).Value == 0)
                    return 0;
                if (((V_Number)b).Value == 1)
                    return 0;
            }

            if (a.Equals(b))
                return 0;
            
            return this;
        }
    }

    [ElementData("Multiply", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Multiply : Element
    {
        override public Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            // Multiply number and number
            if (a is V_Number && b is V_Number)
                return ((V_Number)ParameterValues[0]).Value * ((V_Number)ParameterValues[1]).Value;
            
            // Multiply vector and number
            if ((a is V_Vector && b is V_Number) || (a is V_Number && b is V_Vector))
            {
                V_Vector vector = a is V_Vector ? (V_Vector)a : (V_Vector)b;
                V_Number number = a is V_Number ? (V_Number)a : (V_Number)b;

                if (vector.ConstantSupported<Models.Vertex>())
                {
                    Models.Vertex vertex = (Models.Vertex)vector.GetConstant();
                    return new V_Vector(
                        vertex.X * number.Value,
                        vertex.Y * number.Value,
                        vertex.Z * number.Value
                    );
                }
            }

            if (a is V_Number)
            {
                if (((V_Number)a).Value == 1)
                    return b;
                if (((V_Number)a).Value == 0)
                    return 0;
            }

            if (b is V_Number)
            {
                if (((V_Number)b).Value == 1)
                    return a;
                if (((V_Number)b).Value == 0)
                    return 0;
            }

            if (a.Equals(b))
                return Element.Part<V_RaiseToPower>(a, new V_Number(2));
            
            return this;
        }
    }

    [ElementData("Nearest Walkable Position", ValueType.Vector)]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_NearestWalkablePosition : Element {}

    [ElementData("Normalize", ValueType.Number)]
    [Parameter("Vector", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_Normalize : Element {}

    [ElementData("Normalized Health", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_NormalizedHealth : Element {}

    [ElementData("Not", ValueType.Boolean)]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    public class V_Not : Element {}

    [ElementData("Null", ValueType.Any)]
    public class V_Null : Element {}

    [ElementData("Number", ValueType.Number)]
    public class V_Number : Element
    {
        public static readonly V_Number LargeArbitraryNumber = new V_Number(9999);

        private const int MAX_LENGTH = 10;

        public double Value { get; set; }

        public V_Number(double value)
        {
            this.Value = value;
        }
        public V_Number() : this(0) {}

        public override string ToWorkshop(OutputLanguage language)
        {
            return Math.Round(Value, MAX_LENGTH).ToString();
        }

        override public bool ConstantSupported<T>()
        {
            return typeof(T) == typeof(double);
        }

        override public object GetConstant()
        {
            return Value;
        }
    }

    [ElementData("Number Of Dead Players", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
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
    [Parameter("Hero", ValueType.Hero, typeof(V_HeroVar))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_NumberOfHeroes : Element {}

    [ElementData("Number Of Living Players", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_NumberOfLivingPlayers : Element {}

    [ElementData("Number Of Players", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_NumberOfPlayers : Element {}

    [ElementData("Number Of Players On Objective", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_NumberOfPlayersOnObjective : Element {}

    [ElementData("Objective Index", ValueType.Number)]
    public class V_ObjectiveIndex : Element {}

    [ElementData("Objective Position", ValueType.Number)]
    [Parameter("Number", ValueType.Number, typeof(V_Number))]
    public class V_ObjectivePosition : Element {}

    [ElementData("Opposite Team Of", ValueType.Team)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
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
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_PlayerCarryingFlag : Element {}

    [ElementData("Player Closest To Reticle", ValueType.Player)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_PlayerClosestToReticle : Element {}

    [ElementData("Player Variable", ValueType.Any)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [VarRefParameter("Variable", false)]
    public class V_PlayerVariable : Element {}

    [ElementData("Players In Slot", ValueType.Player)]
    [Parameter("Slot", ValueType.Number, typeof(V_Number))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_PlayersInSlot : Element {}

    [ElementData("Players In View Angle", ValueType.Player)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    [Parameter("View Angle", ValueType.Number, typeof(V_Number))]
    public class V_PlayersInViewAngle : Element {}

    [ElementData("Players On Hero", ValueType.Player)]
    [Parameter("Hero", ValueType.Hero, typeof(V_HeroVar))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_PlayersOnHero : Element {}

    [ElementData("Players Within Radius", ValueType.Player)]
    [Parameter("Center", ValueType.Vector, typeof(V_Vector))]
    [Parameter("Radius", ValueType.Number, typeof(V_Number))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    [EnumParameter("LOS Check", typeof(RadiusLOS))]
    public class V_PlayersWithinRadius : Element {}

    [ElementData("Point Capture Percentage", ValueType.Number)]
    public class V_PointCapturePercentage : Element {}

    [ElementData("Position of", ValueType.Vector)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_PositionOf : Element {}  

    [ElementData("Raise To Power", ValueType.Number)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_RaiseToPower : Element
    {
        override public Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a is V_Number && b is V_Number)
                return Math.Pow(
                    ((V_Number)a).Value,
                    ((V_Number)b).Value);

            if (a is V_Number)
            {
                if (((V_Number)a).Value == 0)
                    return 0;
                if (((V_Number)a).Value == 1)
                    return 1;
                if (((V_Number)a).Value < 0) //this has to exist because workshop
                    return 0;
            }

            if (b is V_Number)
            {
                if (((V_Number)b).Value == 0)
                    return 1;
                if (((V_Number)b).Value == 1)
                    return a;
            }
            
            return this;
        }
    }

    [ElementData("Random Integer", ValueType.Number)]
    [Parameter("Min", ValueType.Number, typeof(V_Number))]
    [Parameter("Max", ValueType.Number, typeof(V_Number))]
    public class V_RandomInteger : Element {}

    [ElementData("Random Real", ValueType.Number)]
    [Parameter("Min", ValueType.Number, typeof(V_Number))]
    [Parameter("Max", ValueType.Number, typeof(V_Number))]
    public class V_RandomReal : Element {}

    [ElementData("Random Value In Array", ValueType.Any)]
    [Parameter("Array", ValueType.Any, null)]
    public class V_RandomValueInArray : Element {}

    [ElementData("Randomized Array", ValueType.Number)]
    [Parameter("Array", ValueType.Any, null)]
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
    [EnumParameter("Rounding Type", typeof(Rounding))]
    public class V_RoundToInteger : Element {}

    [ElementData("Score Of", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_ScoreOf : Element {}

    [ElementData("Server Load", ValueType.Number)]
    public class V_ServerLoad : Element {}

    [ElementData("Server Load Average", ValueType.Number)]
    public class V_ServerLoadAverage : Element {}

    [ElementData("Server Load Peak", ValueType.Number)]
    public class V_ServerLoadPeak : Element {}

    [ElementData("Sine From Degrees", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_SineFromDegrees : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Number)
                return Math.Sin(((V_Number)a).Value * (Math.PI / 180));

            return this;
        }
    }

    [ElementData("Sine From Radians", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_SineFromRadians : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Number)
                return Math.Sin(((V_Number)a).Value);

            return this;
        }
    }

    [ElementData("Slot Of", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_SlotOf : Element {}

    [ElementData("Sorted Array", ValueType.Number)]
    [Parameter("Array", ValueType.Any, null)]
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
    public class V_SquareRoot : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Number)
                return Math.Sqrt(((V_Number)a).Value);

            return this;
        }
    }

    [ElementData("String", ValueType.Any)]
    [Parameter("{0}", ValueType.Any, typeof(V_Null))]
    [Parameter("{1}", ValueType.Any, typeof(V_Null))]
    [Parameter("{2}", ValueType.Any, typeof(V_Null))]
    public class V_String : Element
    {
        public V_String(string text, params Element[] stringValues) : base(NullifyEmptyValues(stringValues))
        {
            Text = text;
        }
        public V_String() : this(null) {}

        public string Text { get; }

        protected override string[] AdditionalParameters()
        {
            return new string[] { "\"" + (Text?.Replace("_", " ") ?? "Hello") + "\"" };
        }

        private static Element[] NullifyEmptyValues(Element[] stringValues)
        {
            var stringList = stringValues.ToList();
            while (stringList.Count < 3)
                stringList.Add(new V_Null());

            return stringList.ToArray();
        }
    }

    [ElementData("Custom String", ValueType.Any)]
    [Parameter("{0}", ValueType.Any, typeof(V_Null))]
    [Parameter("{1}", ValueType.Any, typeof(V_Null))]
    [Parameter("{2}", ValueType.Any, typeof(V_Null))]
    public class V_CustomString : Element
    {
        public string Text { get; }

        public V_CustomString(string text, params IWorkshopTree[] format) : base(format)
        {
            Text = text;
        }
        public V_CustomString()
        {
            Text = "";
        }

        protected override string[] AdditionalParameters()
        {
            return new string[] { "\"" + Text + "\"" };
        }

        public static IWorkshopTree Join(params IWorkshopTree[] elements)
        {
            if (elements.Length == 0) throw new Exception();

            const string join2 = "{0}{1}";
            const string join3 = "{0}{1}{2}";

            List<IWorkshopTree> list = elements.ToList();
            while (list.Count > 1)
            {
                if (list.Count >= 3)
                {
                    list[0] = new V_CustomString(join3, list[0], list[1], list[2]);
                    list.RemoveRange(1, 2);
                }
                else if (list.Count >= 2)
                {
                    list[0] = new V_CustomString(join2, list[0], list[1], new V_Null());
                    list.RemoveAt(1);
                }
                else throw new Exception();
            }
            return list[0];
        }
    }

    [ElementData("Icon String", ValueType.Any)]
    [EnumParameter("Icon", typeof(Icon))]
    public class V_IconString : Element {}

    [ElementData("Subtract", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Subtract : Element
    {
        override public Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a is V_Number && ParameterValues[1] is V_Number)
                return ((V_Number)a).Value - ((V_Number)b).Value;
            
            if (a.ConstantSupported<Models.Vertex>() && b.ConstantSupported<Models.Vertex>())
            {
                var aVertex = (Models.Vertex)a.GetConstant();
                var bVertex = (Models.Vertex)b.GetConstant();

                return new V_Vector(
                    aVertex.X - bVertex.X,
                    aVertex.Y - bVertex.Y,
                    aVertex.Z - bVertex.Z
                );
            }

            if (b is V_Number)
                if (((V_Number)b).Value == 0)
                    return a;

            if (a.Equals(b))
                return 0;
            
            return this;
        }
    }

    [ElementData("Tangent From Degrees", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_TangentFromDegrees : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Number)
                return Math.Tan(((V_Number)a).Value * (Math.PI / 180));

            return this;
        }
    }

    [ElementData("Tangent From Radians", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_TangentFromRadians : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Number)
                return Math.Tan(((V_Number)a).Value);

            return this;
        }
    }

    [ElementData("Team", ValueType.Team)]
    [EnumParameter("Team", typeof(Team))]
    public class V_TeamVar : Element {}

    [ElementData("Team Of", ValueType.Team)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_TeamOf : Element {}

    [ElementData("Team Score", ValueType.Team)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
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

    [ElementData("Up", ValueType.Vector)]
    public class V_Up : Element {}

    [ElementData("Value In Array", ValueType.Any)]
    [Parameter("Array", ValueType.Any, null)]
    [Parameter("Index", ValueType.Number, typeof(V_EventPlayer))]
    public class V_ValueInArray : Element {}

    [ElementData("Vector", ValueType.Vector)]
    [Parameter("X", ValueType.Number, typeof(V_Number))]
    [Parameter("Y", ValueType.Number, typeof(V_Number))]
    [Parameter("Z", ValueType.Number, typeof(V_Number))]
    public class V_Vector : Element
    {
        public V_Vector(V_Number x, V_Number y, V_Number z)
        {
            ParameterValues = new IWorkshopTree[] { x, y, z };
        }
        public V_Vector(double x, double y, double z) : this(new V_Number(x), new V_Number(y), new V_Number(z))
        {
        }
        public V_Vector()
        {
        }

        override public bool ConstantSupported<T>()
        {
            if (typeof(T) != typeof(Models.Vertex)) return false;

            for (int i = 0; i < ParameterValues.Length && i < 3; i++)
            {
                if (ParameterValues[i] is Element == false)
                    return false;
                
                if (!((Element)ParameterValues[i]).ConstantSupported<double>())
                    return false;
            }

            return true;
        }

        override public object GetConstant()
        {
            double x = 0;
            if (ParameterValues.Length > 0)
                x = (double)((Element)ParameterValues[0]).GetConstant();
            
            double y = 0;
            if (ParameterValues.Length > 1)
                y = (double)((Element)ParameterValues[1]).GetConstant();
            
            double z = 0;
            if (ParameterValues.Length > 2)
                z = (double)((Element)ParameterValues[2]).GetConstant();
            
            return new Models.Vertex(x, y, z);
        }

        public Element X => (Element)ParameterValues[0];
        public Element Y => (Element)ParameterValues[1];
        public Element Z => (Element)ParameterValues[2];

        public static V_Vector Zero => new V_Vector(0, 0, 0);
    }

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
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_VerticalFacingAngleOf : Element {}

    [ElementData("Vertical Speed Of", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_VerticalSpeedOf : Element {}

    [ElementData("Victim", ValueType.Player)]
    public class V_Victim : Element {}

    [ElementData("World Vector Of", ValueType.Vector)]
    [Parameter("Local vector", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Relative Player", ValueType.Player, typeof(V_Vector))]
    [EnumParameter("Local Vector", typeof(LocalVector))]
    public class V_WorldVectorOf : Element {}

    [ElementData("X Component Of", ValueType.Number)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_XOf : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Vector)
                return (Element)a.ParameterValues[0];

            return this;
        }
    }

    [ElementData("Y Component Of", ValueType.Number)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_YOf : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Vector)
                return (Element)a.ParameterValues[1];

            return this;
        }
    }

    [ElementData("Z Component Of", ValueType.Number)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_ZOf : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_Vector)
                return (Element)a.ParameterValues[2];

            return this;
        }
    }
}
