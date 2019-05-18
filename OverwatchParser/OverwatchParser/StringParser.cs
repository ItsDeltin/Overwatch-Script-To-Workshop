using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using OverwatchParser.Elements;
using Antlr4;
using Antlr4.Runtime;
using System.Reflection;

namespace OverwatchParser
{
    class StringParser
    {
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
            if (depth == 0)
                Log.Write($"\"{value}\"");

            string debug = new string(' ', depth * 4);

            for (int i = 0; i < searchOrder.Length; i++)
            {
                string searchString = searchOrder[i];
                string escapedValue = Escape(value);

                string regex =  
                    Regex.Replace(Escape(searchString)
                    , "({[0-9]})", "(.+)");  // Converts {0} {1} {2} to (.+) (.+) (.+)
                var match = Regex.Match(escapedValue, regex);

                if (match.Success)
                {
                    Log.Write(debug + searchString);
                    V_String str = new V_String(searchString);

                    List<Element> parsedParameters = new List<Element>();
                    for (int g = 1; g < match.Groups.Count; g++)
                    {
                        string currentParameterValue = match.Groups[g].Captures[0].Value;

                        Match parameterString = Regex.Match(currentParameterValue, "^<([0-9]+)>$");
                        if (parameterString.Success)
                        {
                            int index = int.Parse(parameterString.Groups[1].Value);
                            Log.Write($"{debug}    <param {index}>");
                            parsedParameters.Add(parameters[index]);
                        }
                        else
                            parsedParameters.Add(ParseString(currentParameterValue, parameters, depth + 1));
                    }
                    str.ParameterValues = parsedParameters.ToArray();

                    return str;
                }
            }

            throw new InvalidStringException($"Could not desipher the string {value}.");
        }

        private static string Escape(string value)
        {
            return value
                .Replace("?", @"\?")
                .Replace("*", @"\*")
                .Replace("(", @"\(")
                .Replace(")", @"\)")
                .Replace(".", @"\.")
                ;
        }
    }
}
