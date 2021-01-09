using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Strings
{
    class ParseLocalizedString : StringParseBase
    {
        private static readonly string[] searchOrder = Constants.Strings
            .OrderByDescending(str => str.Contains("{0}"))
            .ThenByDescending(str => str.IndexOfAny("-></*-+=()!?".ToCharArray()) != -1)
            .ThenByDescending(str => str.Length)
            .ToArray();

        public ParseLocalizedString(StringParseInfo stringParseinfo) : base(stringParseinfo) {}

        protected override IStringParse DoParse()
        {
            return RecursiveParse(0, Value);
        }

        LocalizedString RecursiveParse(int charOffset, string value, int depth = 0)
        {
            value = value.ToLower();
            
            //if (depth == 0)
                //foreach(string multiword in multiwordStrings)
                    //value = value.Replace(multiword.Replace('_', ' '), multiword);

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
                    // Create a string element with the found string.
                    LocalizedString str = new LocalizedString(Value, searchString);

                    bool valid = true; // Confirms that the arguments were able to successfully parse.
                    List<LocalizedStringOrExpression> formatParameters = new List<LocalizedStringOrExpression>(); // The parameters that were successfully parsed.

                    // Iterate through the parameters.
                    for (int g = 1; g < match.Groups.Count; g+=3)
                    {
                        Capture capture = match.Groups[g].Captures[0];
                        string currentParameterValue = capture.Value;

                        // Test if the parameter is a format parameter, for example <0>, <1>, <2>, <3>...
                        Match parameterString = Regex.Match(currentParameterValue, "^" + FormatMatch + "$");
                        if (parameterString.Success)
                        {
                            int index = int.Parse(parameterString.Groups[FormatGroupNumber].Value);
                            formatParameters.Add(new LocalizedStringOrExpression(index));
                        }
                        else
                        {
                            // Parse the parameter. If it fails it will return null and the string being checked is probably false.
                            var p = RecursiveParse(charOffset + capture.Index, currentParameterValue, depth + 1);
                            if (p == null)
                            {
                                valid = false;
                                break;
                            }
                            formatParameters.Add(new LocalizedStringOrExpression(p));
                        }
                    }
                    str.ParameterValues = formatParameters.ToArray();

                    if (!valid)
                        continue;
                    
                    return str;
                }
            }

            if (depth > 0)
                return null;
            else
                // If the depth is 0, throw a syntax error.
                throw new StringParseFailedException("Failed to parse the string.");
        }

        static string Escape(string value)
        {
            return value
                .Replace("?", @"\?")
                .Replace("*", @"\*")
                .Replace("(", @"\(")
                .Replace(")", @"\)")
                .Replace(".", @"\.")
                .Replace("/", @"\/")
                .Replace("+", @"\+")
                ;
        }
    }

    class LocalizedString : IStringParse
    {
        public string Original { get; }
        public string String { get; }
        public LocalizedStringOrExpression[] ParameterValues { get; set; }
        public int ArgCount => ParameterValues.Length;

        public LocalizedString(string original, string str)
        {
            Original = original;
            String = str;
        }

        public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameters) => new StringElement(String, true, ParameterValues.Select(pv => (Element)pv.Parse(actionSet, parameters)).ToArray());
    }

    class LocalizedStringOrExpression
    {
        public LocalizedString LocalizedString { get; }
        public int ParameterIndex { get; }

        public LocalizedStringOrExpression(LocalizedString str)
        {
            LocalizedString = str;
        }
        public LocalizedStringOrExpression(int parameterIndex)
        {
            ParameterIndex = parameterIndex;
        }

        public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            if (LocalizedString != null) return LocalizedString.Parse(actionSet, parameters);
            else return parameters[ParameterIndex];
        }
    }
}