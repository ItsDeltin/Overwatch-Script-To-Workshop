using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace OverwatchParser.Elements
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

    [ElementData("All Players", ValueType.Player, 2)]
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

    [ElementData("Append To Array", ValueType.Any, 0)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_AppendToArray : Element {}

    [ElementData("Array Contains", ValueType.Boolean)]
    [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_ArrayContains : Element {}

    [ElementData("Array Slice", ValueType.Any, 0)]
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

    [ElementData("Count Of", ValueType.Number, 0)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    public class V_CountOf : Element {}

    [ElementData("Distance Between", ValueType.Number, 0)]
    [Parameter("Start Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    [Parameter("End Pos", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_DistanceBetween : Element {}

    [ElementData("Divide", ValueType.Any, 0)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Divide : Element {}

    [ElementData("Empty Array", ValueType.Any, 0)]
    public class V_EmptyArray : Element {}

    [ElementData("Event Player", ValueType.Player, 0)]
    public class V_EventPlayer : Element {}

    [ElementData("Facing Direction Of", ValueType.Vector, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_FacingDirectionOf : Element {}

    [ElementData("False", ValueType.Boolean, 0)]
    public class V_False : Element {}

    [ElementData("Global Variable", ValueType.Any, 0)]
    [Parameter("Variable", typeof(Variable))]
    public class V_GlobalVariable : Element {}

    [ElementData("Index Of Array Value", 0)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_IndexOfArrayValue : Element {}

    [ElementData("Is Alive", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsAlive : Element {}

    [ElementData("Is Button Held", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Button", typeof(Button))]
    public class V_IsButtonHeld : Element {}

    [ElementData("Is Dead", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsDead : Element {}

    [ElementData("Is On Objective", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsOnObjective : Element {}

    [ElementData("Is On Ground", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsOnGround : Element {}

    [ElementData("Is In Air", ValueType.Boolean, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_IsInAir : Element {}

    [ElementData("Nearest Walkable Position", ValueType.Vector, 0)]
    [Parameter("Position", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_NearestWalkablePosition : Element {}

    [ElementData("Not", ValueType.Boolean, 1)]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    public class V_Not : Element {}

    [ElementData("Null", ValueType.Player, 0)]
    public class V_Null : Element {}

    [ElementData("Number", ValueType.Number, 0)]
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

        protected override double GetWeight()
        {
            return 2;
        }
    }

    [ElementData("Number Of Players", ValueType.Number, 2)]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_NumberOfPlayers : Element {}

    [ElementData("Players In Slot", ValueType.Player, 0)]
    [Parameter("Slot", ValueType.Number, typeof(V_Number))]
    [Parameter("Team", ValueType.Team, typeof(V_Team))]
    public class V_PlayersInSlot : Element {}

    [ElementData("Modulo", ValueType.Number, 0)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_Modulo : Element {}

    [ElementData("Multiply", ValueType.Any, 0)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Multiply : Element {}

    [ElementData("Or", ValueType.Boolean, 13)]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    [Parameter("Value", ValueType.Boolean, typeof(V_True))]
    public class V_Or : Element {}

    [ElementData("Player Variable", ValueType.Any, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    [Parameter("Variable", typeof(Variable))]
    public class V_PlayerVariable : Element {}

    [ElementData("Position of", ValueType.Vector, 0)]
    [Parameter("Player", ValueType.Player, typeof(V_EventPlayer))]
    public class V_PositionOf : Element {}  

    [ElementData("Raise To Power", ValueType.Number)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_RaiseToPower : Element {}

    [ElementData("Round To Integer", ValueType.Number, 0)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    [Parameter("Rounding Type", typeof(Rounding))]
    public class V_RoundToInteger : Element {}

    [ElementData("Square Root", ValueType.Number)]
    [Parameter("Value", ValueType.Number, typeof(V_Number))]
    public class V_SquareRoot : Element {}

    [ElementData("String", ValueType.String, 1)]
    [Parameter("{0}", ValueType.Any, typeof(V_Null))]
    [Parameter("{1}", ValueType.Any, typeof(V_Null))]
    [Parameter("{2}", ValueType.Any, typeof(V_Null))]
    public class V_String : Element
    {
        public V_String(string text, params Element[] stringValues) : base(NullifyEmptyValues(stringValues))
        {
            TextID = Array.IndexOf(Constants.Strings, text);
            if (TextID == -1)
                throw new InvalidStringException(text);
        }
        public V_String() : this(Constants.DEFAULT_STRING) {}

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
            InputSim.Repeat(Keys.Down, Wait.Short, before);

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

        public static Element ParseString(string value, Element[] parameters, int depth = 0)
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
                    V_String str = new V_String(searchString);

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
                                throw new InvalidStringException($"Tried to get the {index} format, but there are {parameters.Length} parameters. Check your string.");

                            Log.Write($"{debug}    <param {index}>");
                            parsedParameters.Add(parameters[index]);
                        }
                        else
                        {
                            var p = ParseString(currentParameterValue, parameters, depth + 1);
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
                throw new InvalidStringException($"Could not parse the string {value}.");
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

        protected override double GetWeight()
        {
            return 3;
        }
    }

    [ElementData("Subtract", ValueType.Any, 0)]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    [Parameter("Value", ValueType.Any, typeof(V_Number))]
    public class V_Subtract : Element {}

    [ElementData("Team", ValueType.Team, 4)]
    [Parameter("Team", typeof(TeamSelector))]
    public class V_Team : Element {}

    [ElementData("Total Time Elapsed", ValueType.Number, 0)]
    public class V_TotalTimeElapsed : Element {}

    [ElementData("True", ValueType.Boolean, 2)]
    public class V_True : Element {}

    [ElementData("Value In Array", ValueType.Vector, 2)]
    [Parameter("Array", ValueType.Any, typeof(V_GlobalVariable))]
    [Parameter("Index", ValueType.Number, typeof(V_EventPlayer))]
    public class V_ValueInArray : Element {}

    [ElementData("Vector", ValueType.Vector, 1)]
    [Parameter("X", ValueType.Number, typeof(V_Number))]
    [Parameter("Y", ValueType.Number, typeof(V_Number))]
    [Parameter("Z", ValueType.Number, typeof(V_Number))]
    public class V_Vector : Element {}

    [ElementData("X Component Of", ValueType.Number, 0)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_XComponentOf : Element {}

    [ElementData("Y Component Of", ValueType.Number, 0)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_YComponentOf : Element {}

    [ElementData("Z Component Of", ValueType.Number, 0)]
    [Parameter("Value", ValueType.VectorAndPlayer, typeof(V_Vector))]
    public class V_ZComponentOf : Element {}
}
