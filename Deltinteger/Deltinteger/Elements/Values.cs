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
using Deltin.Deltinteger.Models;

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

            if (ParameterValues[0] is V_Number a)
                return Math.Abs(a.Value);
            
            return this;
        }
    }

    [ElementData("Add", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class V_Add : Element
    {
        override public Element Optimize()
        {
            return OptimizeAddOperation(
                op       : (a, b) => a + b,
                areEqual : (a, b) => a * 2,
                true
            );
        }
    }

    [ElementData("All Dead Players", ValueType.Players)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_AllDeadPlayers : Element {}

    [ElementData("All Heroes", ValueType.Hero)]
    public class V_AllHeroes : Element {}

    [ElementData("All Living Players", ValueType.Players)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_AllLivingPlayers : Element {}

    [ElementData("All Players", ValueType.Players)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_AllPlayers : Element {}

    [ElementData("All Players Not On Objective", ValueType.Players)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_AllPlayersNotOnObjective : Element {}

    [ElementData("All Players On Objective", ValueType.Players)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_AllPlayersOnObjective : Element {}

    [ElementData("All Tank Heroes", ValueType.Hero)]
    public class V_AllTankHeroes : Element {}

    [ElementData("All Damage Heroes", ValueType.Hero)]
    public class V_AllDamageHeroes : Element {}

    [ElementData("All Support Heroes", ValueType.Hero)]
    public class V_AllSupportHeroes : Element {}

    [ElementData("Allowed Heroes", ValueType.Hero)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_AllowedHeroes : Element {}

    [ElementData("Altitude Of", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_AltitudeOf : Element {}

    [ElementData("And", ValueType.Boolean)]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    [HideElement]
    public class V_And : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a.ConstantSupported<bool>() && b.ConstantSupported<bool>())
                return (bool)a.GetConstant() && (bool)b.GetConstant();

            if (a.EqualTo(b))
                return a;

            // todo: check
            if (a is V_Not)
            {
                if (b.EqualTo(a.ParameterValues[0]))
                    return false;
            }
            else if (b is V_Not)
            {
                if (a.EqualTo(b.ParameterValues[0]))
                    return false;
            }

            return this;
        }
    }

    [ElementData("Angle Between Vectors", ValueType.Vector)]
    [Parameter("Vector", ValueType.Vector, typeof(V_Vector))]
    [Parameter("Vector", ValueType.Vector, typeof(V_Vector))]
    public class V_AngleBetweenVectors : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a.ConstantSupported<Vertex>() && b.ConstantSupported<Vertex>())
            {
                Vertex vertexA = (Vertex)a.GetConstant();
                Vertex vertexB = (Vertex)b.GetConstant();

                return Math.Acos(vertexA.DotProduct(vertexB) / (vertexA.Length * vertexB.Length)) * (180 / Math.PI);
            }

            return this;
        }
    }

    [ElementData("Angle Difference", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_AngleDifference : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            if (ParameterValues[0] is V_Number a && ParameterValues[1] is V_Number b)
            {
                double diff = Math.Abs(a.Value - b.Value) % 360;
                if (diff > 180) diff = 360 - diff;
                return diff;
            }

            return this;
        }
    }

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

            if (ParameterValues[0] is V_Number a)
                return Math.Acos(a.Value) * (180.0 / Math.PI);

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

            if (ParameterValues[0] is V_Number a)
                return Math.Acos(a.Value);

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

            if (ParameterValues[0] is V_Number a)
                return Math.Asin(a.Value) * (180 / Math.PI);

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

            if (ParameterValues[0] is V_Number a)
                return Math.Asin(a.Value);

            return this;
        }
    }

    [ElementData("Arctangent In Degrees", ValueType.Number)]
    [Parameter("Numerator", ValueType.Number, typeof(V_Number))]
    [Parameter("Denominator", ValueType.Number, typeof(V_Number))]
    public class V_ArctangentInDegrees : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            if (ParameterValues[0] is V_Number a && ParameterValues[1] is V_Number b)
                return Math.Atan2(a.Value, b.Value) * (180 / Math.PI);

            return this;
        }
    }

    [ElementData("Arctangent In Radians", ValueType.Number)]
    [Parameter("Numerator", ValueType.Number, typeof(V_Number))]
    [Parameter("Denominator", ValueType.Number, typeof(V_Number))]
    public class V_ArctangentInRadians : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            if (ParameterValues[0] is V_Number a && ParameterValues[1] is V_Number b)
                return Math.Atan2(a.Value, b.Value);

            return this;
        }
    }

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
    [Restricted(RestrictedCallType.Attacker)]
    public class V_Attacker : Element {}

    [ElementData("Backward", ValueType.Vector)]
    public class V_Backward : Element
    {
        public override bool ConstantSupported<T>() =>
            typeof(T) == typeof(Vertex);

        public override object GetConstant() =>
            new Vertex(0, 0, -1);
    }

    [ElementData("Closest Player To", ValueType.Player)]
    [Parameter("Center", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_ClosestPlayerTo : Element {}

    [ElementData("Compare", ValueType.Boolean)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [EnumParameter("", typeof(Operators))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class V_Compare : Element 
    {
        public V_Compare() : base() {}

        public V_Compare(IWorkshopTree left, Operators op, IWorkshopTree right) : base(left, EnumData.GetEnumValue(op), right) {}

        public override Element Optimize()
        {
            OptimizeChildren();

            Element left = (Element)ParameterValues[0];
            Operators op = (Operators)((EnumMember)ParameterValues[1]).Value;
            Element right = (Element)ParameterValues[2];

            if (op == Operators.Equal)
            {
                if (left.EqualTo(right))
                    return true;
                if (left is V_Number a && right is V_Number b)
                    return a.Value == b.Value;
            }
            else if (op == Operators.GreaterThan)
            {
                if (left.EqualTo(right))
                    return false;
                if (left is V_Number a && right is V_Number b)
                    return a.Value > b.Value;
            }
            else if (op == Operators.GreaterThanOrEqual)
            {
                if (left.EqualTo(right))
                    return true;
                if (left is V_Number a && right is V_Number b)
                    return a.Value >= b.Value;
            }
            else if (op == Operators.LessThan)
            {
                if (left.EqualTo(right))
                    return false;
                if (left is V_Number a && right is V_Number b)
                    return a.Value < b.Value;
            }
            else if (op == Operators.LessThanOrEqual)
            {
                if (left.EqualTo(right))
                    return true;
                if (left is V_Number a && right is V_Number b)
                    return a.Value <= b.Value;
            }
            else if (op == Operators.NotEqual)
            {
                if (left is V_Number a && right is V_Number b)
                    return a.Value != b.Value;
                if (left.EqualTo(right))
                    return false;
            }

            return this;
        }
    }

    [ElementData("Control Mode Scoring Percentage", ValueType.Number)]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_ControlModeScoringPercentage : Element {}

    [ElementData("Control Mode Scoring Team", ValueType.Team)]
    public class V_ControlModeScoringTeam : Element {}

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

            if (ParameterValues[0] is V_Number a)
                return Math.Cos(a.Value * (Math.PI / 180));

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

            if (ParameterValues[0] is V_Number a)
                return Math.Cos(((V_Number)a).Value);

            return this;
        }
    }

    [ElementData("Cross Product", ValueType.Vector)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_CrossProduct : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a.ConstantSupported<Vertex>() && b.ConstantSupported<Vertex>())
            {
                Vertex vertexA = (Vertex)a.GetConstant();
                Vertex vertexB = (Vertex)b.GetConstant();

                return vertexA.CrossProduct(vertexB).RemoveNaNs().ToVector();
            }

            return this;
        }
    }

    [ElementData("Current Map", ValueType.Map)]
    public class V_CurrentMap : Element {}

    [ElementData("Current Array Element", ValueType.Any)]
    public class V_ArrayElement : Element {}

    [ElementData("Direction From Angles", ValueType.Vector)]
    [Parameter("Horizontal Angle", ValueType.Number, typeof(V_Number))]
    [Parameter("Vertical Angle", ValueType.Number, typeof(V_Number))]
    public class V_DirectionFromAngles : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            if (ParameterValues[0] is V_Number a && ParameterValues[1] is V_Number b)
            {
                double h = a.Value;
                double v = b.Value;

                double x = Math.Sin(h * (Math.PI / 180));
                double y = -Math.Sin(v * (Math.PI / 180));
                double z = Math.Cos(h * (Math.PI / 180));

                if (y == -0)
                    y = 0;

                return new V_Vector(x, y, z);
            }

            return this;
        }
    }

    [ElementData("Direction Towards", ValueType.Vector)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_DirectionTowards : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a.ConstantSupported<Vertex>() && b.ConstantSupported<Vertex>())
            {
                Vertex vertexA = (Vertex)a.GetConstant();
                Vertex vertexB = (Vertex)b.GetConstant();

                return vertexA.DirectionTowards(vertexB).RemoveNaNs().ToVector();
            }

            return this;
        }
    }

    [ElementData("Distance Between", ValueType.Number)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_DistanceBetween : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a.ConstantSupported<Vertex>() && b.ConstantSupported<Vertex>())
            {
                Vertex vertexA = (Vertex)a.GetConstant();
                Vertex vertexB = (Vertex)b.GetConstant();

                return vertexA.DistanceTo(vertexB);
            }

            return this;
        }
    }

    [ElementData("Divide", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class V_Divide : Element
    {
        override public Element Optimize()
        {
            return OptimizeMultiplyOperation(
                op      : (a, b) => a / b,
                areEqual: (a, b) => 1,
                false
            );
        }
    }

    [ElementData("Dot Product", ValueType.Number)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_DotProduct : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a.ConstantSupported<Vertex>() && b.ConstantSupported<Vertex>())
            {
                Vertex vertexA = (Vertex)a.GetConstant();
                Vertex vertexB = (Vertex)b.GetConstant();

                return vertexA.DotProduct(vertexB);
            }

            return this;
        }
    }

    [ElementData("Down", ValueType.Vector)]
    public class V_Down : Element
    {
        public override bool ConstantSupported<T>() =>
            typeof(T) == typeof(Vertex);

        public override object GetConstant() =>
            new Vertex(0, -1, 0);
    }

    [ElementData("Empty Array", ValueType.Any)]
    public class V_EmptyArray : Element {}

    [ElementData("Entity Exists", ValueType.Boolean)]
    [Parameter("Entity", ValueType.Player, null)]
    public class V_EntityExists : Element {}

    [ElementData("Event Damage", ValueType.Number)]
    [Restricted(RestrictedCallType.Attacker)]
    public class V_EventDamage : Element {}

    [ElementData("Event Healing", ValueType.Number)]
    [Restricted(RestrictedCallType.Healer)]
    public class V_EventHealing : Element {}

    [ElementData("Event Player", ValueType.Player)]
    [Restricted(RestrictedCallType.EventPlayer)]
    public class V_EventPlayer : Element {}

    [ElementData("Event Was Critical Hit", ValueType.Boolean)]
    [Restricted(RestrictedCallType.Attacker)]
    public class V_EventWasCriticalHit : Element {}

    [ElementData("Event Was Health Pack", ValueType.Boolean)]
    [Restricted(RestrictedCallType.Healer)]
    public class V_EventWasHealthPack : Element {}

    [ElementData("Eye Position", ValueType.Vector)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_EyePosition : Element {}

    [ElementData("Facing Direction Of", ValueType.Vector)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_FacingDirectionOf : Element {}

    [ElementData("False", ValueType.Boolean)]
    public class V_False : Element
    {
        public override bool ConstantSupported<T>() =>
            typeof(T) == typeof(bool);

        public override object GetConstant() => false;
    }

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
    public class V_Forward : Element
    {
        public override bool ConstantSupported<T>() =>
            typeof(T) == typeof(Vertex);

        public override object GetConstant() =>
            new Vertex(0, 0, 1);
    }

    [ElementData("Game Mode", ValueType.Gamemode)]
    [EnumParameter("Gamemode", typeof(GameMode))]
    [HideElement]
    public class V_GameModeVar : Element {}

    [ElementData("Current Game Mode", ValueType.Gamemode)]
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
    [Restricted(RestrictedCallType.Healer)]
    public class V_Healee : Element {}

    [ElementData("Healer", ValueType.Player)]
    [Restricted(RestrictedCallType.Healer)]
    public class V_Healer : Element {}

    [ElementData("Health", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_Health : Element {}

    [ElementData("Hero", ValueType.Hero)]
    [EnumParameter("Hero", typeof(Hero))]
    [HideElement]
    public class V_HeroVar : Element {}

    [ElementData("Hero Icon String", ValueType.Any)]
    [Parameter("Value", ValueType.String, typeof(V_HeroVar))]
    public class V_HeroIconString : Element {}

    [ElementData("Hero Of", ValueType.Hero)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_HeroOf : Element {}

    [ElementData("Horizontal Angle From Direction", ValueType.Number)]
    [Parameter("Direction", ValueType.Vector, typeof(V_Vector))]
    public class V_HorizontalAngleFromDirection : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a.ConstantSupported<Vertex>())
            {
                Vertex vert = (Vertex)a.GetConstant();
                double gradient = vert.X / vert.Z;
                if (double.IsNaN(gradient))
                    gradient = 0;
                double result = Math.Atan(gradient) * (180 / Math.PI);
                if (result == -0) //thank you c# for -0 being a thing
                    result = 180;
                return result;
            }

            return this;
        }
    }

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

    [ElementData("Index Of Array Value", ValueType.Number)]
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

    [ElementData("Last Healing Modification ID", ValueType.Number)]
    public class V_LastHealingModificationID : Element {}

    [ElementData("Last Of", ValueType.Any)]
    [Parameter("Array", ValueType.Any, null)]
    public class V_LastOf : Element {}

    [ElementData("Last Text ID", ValueType.Number)]
    public class V_LastTextID : Element {}

    [ElementData("Left", ValueType.Vector)]
    public class V_Left : Element
    {
        public override bool ConstantSupported<T>() =>
            typeof(T) == typeof(Vertex);

        public override object GetConstant() =>
            new Vertex(1, 0, 0);
    }

    [ElementData("Local Vector Of", ValueType.Vector)]
    [Parameter("World Vector", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("Relative Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Transformation", typeof(Transformation))]
    public class V_LocalVectorOf : Element {}

    [ElementData("Map", ValueType.Map)]
    [EnumParameter("Map", typeof(Map))]
    [HideElement]
    public class V_MapVar : Element {}

    [ElementData("Match Round", ValueType.Number)]
    public class V_MatchRound : Element {}

    [ElementData("Match Time", ValueType.Number)]
    public class V_MatchTime : Element {}

    [ElementData("Max", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_Max : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a is V_Number aNum && b is V_Number bNum)
                return Math.Max(aNum.Value, bNum.Value);

            return this;
        }
    }

    [ElementData("Max Health", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_MaxHealth : Element {}

    [ElementData("Min", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_Min : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a is V_Number aNum && b is V_Number bNum)
                return Math.Min(aNum.Value, bNum.Value);

            return this;
        }
    }

    [ElementData("Modulo", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_Modulo : Element
    {
        override public Element Optimize()
        {
            OptimizeChildren();

            V_Number a = ParameterValues[0] as V_Number;
            V_Number b = ParameterValues[1] as V_Number;

            if (a != null && b != null)
                return a.Value % b.Value;

            if (a != null)
            {
                if (a.Value == 0) return 0;
                if (a.Value == 1) return 1;
            }

            if (b != null && b.Value == 0) return 0;

            if (((Element)ParameterValues[0]).EqualTo(ParameterValues[1])) return 0;
            
            return this;
        }
    }

    [ElementData("Multiply", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class V_Multiply : Element
    {
        override public Element Optimize()
        {
            return OptimizeMultiplyOperation(
                op      : (a, b) => a * b,
                areEqual: (a, b) => Element.Part<V_RaiseToPower>(a, new V_Number(2)),
                true
            );
        }
    }

    [ElementData("Nearest Walkable Position", ValueType.Vector)]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_NearestWalkablePosition : Element {}

    [ElementData("Normalize", ValueType.Number)]
    [Parameter("Vector", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_Normalize : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a.ConstantSupported<Vertex>())
            {
                Vertex vertexA = (Vertex)a.GetConstant();

                return vertexA.Normalize().ToVector();
            }

            return this;
        }
    }

    [ElementData("Normalized Health", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_NormalizedHealth : Element {}

    [ElementData("Not", ValueType.Boolean)]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    public class V_Not : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a is V_True)
                return false;

            if (a is V_False)
                return true;

            if (a is V_Not)
                return (Element)a.ParameterValues[0];

            if (a is V_Compare)
            {
                Operators op = (Operators)((EnumMember)a.ParameterValues[1]).Value;
                IWorkshopTree left = a.ParameterValues[0];
                IWorkshopTree right = a.ParameterValues[2];
                if (op == Operators.Equal)
                    return new V_Compare(left, Operators.NotEqual, right);
                else if (op == Operators.GreaterThan)
                    return new V_Compare(left, Operators.LessThanOrEqual, right);
                else if (op == Operators.GreaterThanOrEqual)
                    return new V_Compare(left, Operators.LessThan, right);
                else if (op == Operators.LessThan)
                    return new V_Compare(left, Operators.GreaterThanOrEqual, right);
                else if (op == Operators.LessThanOrEqual)
                    return new V_Compare(left, Operators.GreaterThan, right);
                else if (op == Operators.NotEqual)
                    return new V_Compare(left, Operators.Equal, right);
            }

            return this;
        }
    }

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

        public override string ToWorkshop(OutputLanguage language, ToWorkshopContext context)
        {
            if (double.IsNaN(Value))
                Value = 0;
            return Math.Round(Value, MAX_LENGTH).ToString();
        }

        override public bool ConstantSupported<T>() =>
            typeof(T) == typeof(double);

        override public object GetConstant() => Value;

        protected override bool OverrideEquals(IWorkshopTree other)
        {
            return ((V_Number)other).Value == Value;
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
    [HideElement]
    public class V_Or : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a.ConstantSupported<bool>() && b.ConstantSupported<bool>())
                return (bool)a.GetConstant() || (bool)b.GetConstant();

            // If either condition is already true, return true. This may or may not work due to short-circuiting.
            if (a is V_True || b is V_True) return true;
            
            if (a.EqualTo(b)) return a;

            if (a is V_Not)
                if (b.EqualTo(a.ParameterValues[0]))
                    return true;

            if (b is V_Not)
                if (a.EqualTo(b.ParameterValues[0]))
                    return true;

            return this;
        }
    }

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

    [ElementData("Players In Slot", ValueType.Players)]
    [Parameter("Slot", ValueType.Number, typeof(V_Number))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_PlayersInSlot : Element {}

    [ElementData("Players In View Angle", ValueType.Players)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    [Parameter("View Angle", ValueType.Number, typeof(V_Number))]
    public class V_PlayersInViewAngle : Element {}

    [ElementData("Players On Hero", ValueType.Players)]
    [Parameter("Hero", ValueType.Hero, typeof(V_HeroVar))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    public class V_PlayersOnHero : Element {}

    [ElementData("Players Within Radius", ValueType.Players)]
    [Parameter("Center", ValueType.Vector, typeof(V_Vector))]
    [Parameter("Radius", ValueType.Number, typeof(V_Number))]
    [Parameter("Team", ValueType.Team, typeof(V_TeamVar))]
    [EnumParameter("LOS Check", typeof(RadiusLOS))]
    public class V_PlayersWithinRadius : Element {}

    [ElementData("Point Capture Percentage", ValueType.Number)]
    public class V_PointCapturePercentage : Element {}

    [ElementData("Position Of", ValueType.Vector)]
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

            V_Number a = ParameterValues[0] as V_Number;
            V_Number b = ParameterValues[1] as V_Number;

            if (a != null && b != null)
                return Math.Pow(
                    a.Value,
                    b.Value
                );

            if (a != null)
            {
                if (a.Value == 0) return 0;
                if (a.Value == 1) return 1;
                
                // ! Workshop Bug: Pow on values less than 0 always equals 0.
                if (a.Value < 0) return 0;
            }

            if (b != null)
            {
                if (b.Value == 0) return 1;
                if (b.Value == 1) return a;
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
    public class V_Right : Element
    {
        public override bool ConstantSupported<T>() =>
            typeof(T) == typeof(Vertex);

        public override object GetConstant() =>
            new Vertex(-1, 0, 0);
    }

    [ElementData("Round To Integer", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [EnumParameter("Rounding Type", typeof(Rounding))]
    public class V_RoundToInteger : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Rounding type = (Rounding)((EnumMember)ParameterValues[1]).Value;

            if (ParameterValues[0] is V_Number a)
            {
                double num = a.Value;
                if (type == Rounding.Down) return Math.Floor(num);
                if (type == Rounding.Nearest) return Math.Round(num);
                if (type == Rounding.Up) return Math.Ceiling(num);
            }

            return this;
        }
    }

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

            if (ParameterValues[0] is V_Number a)
                return Math.Sin(a.Value * (Math.PI / 180));

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

            if (ParameterValues[0] is V_Number a)
                return Math.Sin(a.Value);

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

            if (ParameterValues[0] is V_Number a)
                return Math.Sqrt(a.Value);

            return this;
        }
    }

    [ElementData("String", ValueType.String)]
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

        protected override bool OverrideEquals(IWorkshopTree other)
        {
            return ((V_String)other).Text == Text;
        }
    }

    [ElementData("Custom String", ValueType.String)]
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

        protected override bool OverrideEquals(IWorkshopTree other)
        {
            return ((V_CustomString)other).Text == Text;
        }
    }

    [ElementData("Icon String", ValueType.String)]
    [EnumParameter("Icon", typeof(Icon))]
    public class V_IconString : Element {}

    [ElementData("Subtract", ValueType.Any)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [HideElement]
    public class V_Subtract : Element
    {
        override public Element Optimize()
        {
            return OptimizeAddOperation(
                op       : (a, b) => a - b,
                areEqual : (a, b) => 0,
                false
            );
        }
    }

    [ElementData("Tangent From Degrees", ValueType.Number)]
    [Parameter("Angle", ValueType.Number, typeof(V_Number))]
    public class V_TangentFromDegrees : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            if (ParameterValues[0] is V_Number a)
                return Math.Tan(a.Value * (Math.PI / 180));

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

            if (ParameterValues[0] is V_Number a)
                return Math.Tan(a.Value);

            return this;
        }
    }

    [ElementData("Team", ValueType.Team)]
    [EnumParameter("Team", typeof(Team))]
    [HideElement]
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
    public class V_True : Element
    {
        public override bool ConstantSupported<T>() =>
            typeof(T) == typeof(bool);

        public override object GetConstant() => true;
    }

    [ElementData("Ultimate Charge Percent", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_UltimateChargePercent : Element {}

    [ElementData("Up", ValueType.Vector)]
    public class V_Up : Element
    {
        public override bool ConstantSupported<T>() =>
            typeof(T) == typeof(Vertex);

        public override object GetConstant() =>
            new Vertex(0, 1, 0);
    }

    [ElementData("Value In Array", ValueType.Any)]
    [Parameter("Array", ValueType.Any, null)]
    [Parameter("Index", ValueType.Number, typeof(V_Number))]
    public class V_ValueInArray : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element array = (Element)ParameterValues[0];
            Element index = (Element)ParameterValues[1];

            if (index is V_Number num)
            if (num.Value == 0) // Needs to be in a seperate if or else the compiler will complain.
            {
                return Element.Part<V_FirstOf>(array);
            }

            return this;
        }
    }

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
        public V_Vector(IWorkshopTree x, IWorkshopTree y, IWorkshopTree z)
        {
            ParameterValues = new IWorkshopTree[] { x, y, z };
        }
        public V_Vector(double x, double y, double z) : this(new V_Number(x), new V_Number(y), new V_Number(z))
        {
        }
        public V_Vector()
        {
        }

        public override bool ConstantSupported<T>()
        {
            if (typeof(T) != typeof(Vertex)) return false;

            for (int i = 0; i < ParameterValues.Length && i < 3; i++)
            {
                if (ParameterValues[i] is Element == false)
                    return false;
                
                if (!((Element)ParameterValues[i]).ConstantSupported<double>())
                    return false;
            }

            return true;
        }

        public override object GetConstant()
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
            
            return new Vertex(x, y, z);
        }

        public override Element Optimize()
        {
            OptimizeChildren();

            if (X is V_Number xNum && Y is V_Number yNum && Z is V_Number zNum)
            {
                double x = xNum.Value, y = yNum.Value, z = zNum.Value;

                if (x == 0  && y == 1  && z == 0 ) return new V_Up();
                if (x == 0  && y == -1 && z == 0 ) return new V_Down();
                if (x == -1 && y == 0  && z == 0 ) return new V_Right();
                if (x == 1  && y == 0  && z == 0 ) return new V_Left();
                if (x == 0  && y == 0  && z == 1 ) return new V_Forward();
                if (x == 0  && y == 0  && z == -1) return new V_Backward();
            }

            Element oX = X;
            Element oY = Y;
            Element oZ = Z;
            if (oX is V_Number oXNum && oXNum.Value == 0) oX = new V_EmptyArray();
            if (oY is V_Number oYNum && oYNum.Value == 0) oY = new V_EmptyArray();
            if (oZ is V_Number oZNum && oZNum.Value == 0) oZ = new V_EmptyArray();
            if (oX != X || oY != Y || oZ != Z) return Element.Part<V_Vector>(oX, oY, oZ);

            return this;
        }

        public Element X => (Element)ParameterValues[0];
        public Element Y => (Element)ParameterValues[1];
        public Element Z => (Element)ParameterValues[2];

        public static V_Vector Zero => new V_Vector(0, 0, 0);
    }

    [ElementData("Vector Towards", ValueType.Vector)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_VectorTowards : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];
            Element b = (Element)ParameterValues[1];

            if (a.ConstantSupported<Vertex>() && b.ConstantSupported<Vertex>())
            {
                Vertex vertA = (Vertex)a.GetConstant();
                Vertex vertB = (Vertex)b.GetConstant();

                return vertA.VectorTowards(vertB).ToVector();
            }

            return this;
        }
    }

    [ElementData("Velocity Of", ValueType.Vector)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_VelocityOf : Element {}

    [ElementData("Vertical Angle From Direction", ValueType.Number)]
    [Parameter("Direction", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_VerticalAngleFromDirection : Element
    {
        public override Element Optimize()
        {
            OptimizeChildren();

            Element a = (Element)ParameterValues[0];

            if (a.ConstantSupported<Vertex>())
            {
                double result = -Math.Asin(((Vertex)a.GetConstant()).Y) * (180 / Math.PI);
                if (result == -0)
                    result = 0;
                return result;
            }

            return this;
        }
    }

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
    [Restricted(RestrictedCallType.Attacker)]
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

            if (a.ConstantSupported<Vertex>())
                return ((Vertex)a.GetConstant()).X;

            if (a is V_Vector aVect)
                return aVect.X;

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

            if (a.ConstantSupported<Vertex>())
                return ((Vertex)a.GetConstant()).Y;

            if (a is V_Vector aVect)
                return aVect.Y;

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

            if (a.ConstantSupported<Vertex>())
                return ((Vertex)a.GetConstant()).Z;

            if (a is V_Vector aVect)
                return aVect.Z;

            return this;
        }
    }

    [ElementData("Array", ValueType.Any)]
    [HideElement]
    public class V_Array : Element
    {
        public V_Array()
        {
            AlwaysShowParentheses = true;
        }
    }

    [ElementData("If-Then-Else", ValueType.Any)]
    [Parameter("If", ValueType.Boolean, null)]
    [Parameter("Then", ValueType.Boolean, null)]
    [Parameter("Else", ValueType.Boolean, null)]
    [HideElement]
    public class V_IfThenElse : Element
    {
        public override string ToWorkshop(OutputLanguage language, ToWorkshopContext context)
        {
            AddMissingParameters();
            string result = ParameterValues[0].ToWorkshop(language, ToWorkshopContext.NestedValue) + " ? " + ParameterValues[1].ToWorkshop(language, ToWorkshopContext.NestedValue) + " : " + ParameterValues[2].ToWorkshop(language, ToWorkshopContext.NestedValue);
            if (context == ToWorkshopContext.ConditionValue) result = "(" + result + ")";
            return result;
        }
    }

    [ElementData("Is Meleeing", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsMeleeing : Element {}

    [ElementData("Is Jumping", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsJumping : Element {}

    [ElementData("Event Direction", ValueType.Vector)]
    [Restricted(RestrictedCallType.Knockback)]
    public class V_EventDirection : Element {}

    [ElementData("Button", ValueType.Button)]
    [EnumParameter("Button", typeof(Button))]
    public class V_ButtonValue : Element {}

    [ElementData("Event Ability", ValueType.Button)]
    [Restricted(RestrictedCallType.Ability)]
    public class V_EventAbility : Element {}

    [ElementData("Ability Cooldown", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    public class V_AbilityCooldown : Element {}

    [ElementData("Ability Icon String", ValueType.String)]
    [Parameter("Hero", ValueType.Hero, typeof(V_HeroVar))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    public class V_AbilityIconString : Element {}

    [ElementData("Is In Alternate Form", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsInAlternateForm : Element {}

    [ElementData("Is Duplicating", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsDuplicating : Element {}

    [ElementData("Hero Being Duplicated", ValueType.Hero)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_HeroBeingDuplicated : Element {}

    [ElementData("Ammo", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Clip", ValueType.Number, typeof(V_Number))]
    public class V_Ammo : Element {}

    [ElementData("Max Ammo", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Clip", ValueType.Number, typeof(V_Number))]
    public class V_MaxAmmo : Element {}

    [ElementData("Weapon", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_Weapon : Element {}

    [ElementData("Is Reloading", ValueType.Boolean)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsReloading : Element {}

    [ElementData("Event Was Environment", ValueType.Boolean)]
    [Restricted(RestrictedCallType.Attacker)]
    public class V_EventWasEnvironment : Element {}

    [ElementData("Current Array Index", ValueType.Number)]
    public class V_CurrentArrayIndex : Element {}

    [ElementData("Input Binding String", ValueType.String)]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    public class V_InputBindingString : Element {}

    [ElementData("Ability Charge", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    public class V_AbilityCharge : Element {}

    [ElementData("Ability Resource", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", ValueType.Button, typeof(V_ButtonValue))]
    public class V_AbilityResource : Element {}

    [ElementData("Mapped Array", ValueType.Number)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Mapping Expression", ValueType.Any, typeof(V_ArrayElement))]
    public class V_MappedArray : Element {}

    [ElementData("Workshop Setting Integer", ValueType.Number)]
    [Parameter("Category", ValueType.String, null)]
    [Parameter("Name", ValueType.String, null)]
    [Parameter("Default", ValueType.Number, null)]
    [Parameter("Min", ValueType.Number, null)]
    [Parameter("Max", ValueType.Number, null)]
    public class V_WorkshopSettingInteger : Element {}

    [ElementData("Workshop Setting Real", ValueType.Number)]
    [Parameter("Category", ValueType.String, null)]
    [Parameter("Name", ValueType.String, null)]
    [Parameter("Default", ValueType.Number, null)]
    [Parameter("Min", ValueType.Number, null)]
    [Parameter("Max", ValueType.Number, null)]
    public class V_WorkshopSettingReal : Element {}

    [ElementData("Workshop Setting Toggle", ValueType.Boolean)]
    [Parameter("Category", ValueType.String, null)]
    [Parameter("Name", ValueType.String, null)]
    [Parameter("Default", ValueType.Boolean, null)]
    public class V_WorkshopSettingToggle : Element {}

    [ElementData("Last Created Health Pool", ValueType.Any)]
    public class V_LastCreatedHealthPool : Element {}

    [ElementData("Health Of Type", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Health Type", typeof(HealthType))]
    public class V_HealthOfType : Element {}

    [ElementData("Max Health Of Type", ValueType.Number)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [EnumParameter("Health Type", typeof(HealthType))]
    public class V_MaxHealthOfType : Element {}
}
