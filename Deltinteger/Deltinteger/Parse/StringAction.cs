using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class StringAction : IExpression
    {
        private static readonly CompletionItem[] StringCompletion = Constants.Strings.Select(str => new CompletionItem() {
            Label = str,
            Kind = CompletionItemKind.Text
        }).ToArray();

        public string Value { get; }
        public bool Localized { get; }
        public IExpression[] FormatParameters { get; }
        private DocRange StringRange { get; }
        private CustomStringParse[] Sectioned;
        private LocalizedString LocalizedString;

        // Normal
        public StringAction(ScriptFile script, DeltinScriptParser.StringContext stringContext, bool parse = true)
        {
            Value = Extras.RemoveQuotes(stringContext.STRINGLITERAL().GetText());
            Localized = stringContext.LOCALIZED() != null;
            StringRange = DocRange.GetRange(stringContext.STRINGLITERAL());
            if (parse) ParseString(script);

            if (Localized)
            {
                script.AddCompletionRange(new CompletionRange(StringCompletion, StringRange, CompletionRangeKind.ClearRest));
            }
        }
        // Formatted
        public StringAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Formatted_stringContext stringContext) : this(parseInfo.Script, stringContext.@string(), false)
        {
            FormatParameters = new IExpression[stringContext.expr().Length];
            for (int i = 0; i < FormatParameters.Length; i++)
                FormatParameters[i] = DeltinScript.GetExpression(parseInfo, scope, stringContext.expr(i));
            ParseString(parseInfo.Script);
        }

        private void ParseString(ScriptFile script)
        {
            try
            {
                if (!Localized)
                {
                    Sectioned = ParseCustomString(Value, FormatParameters);
                }
                else
                {
                    LocalizedString = ParseLocalizedString(Value, FormatParameters);
                }
            }
            catch (StringParseFailedException ex)
            {
                if (ex.StringIndex == -1)
                {
                     script.Diagnostics.Error(ex.Message, StringRange);
                }
                else
                {
                    int errorStart = StringRange.start.character + 1 + ex.StringIndex;
                    script.Diagnostics.Error(ex.Message, new DocRange(
                        new Pos(StringRange.start.line, errorStart),
                        new Pos(StringRange.start.line, errorStart + ex.Length)
                    ));
                }
            }
        }

        static CustomStringParse[] ParseCustomString(string value, IExpression[] parameters)
        {
            // Look for <#>s
            var formats = Regex.Matches(value, "<([0-9]+)>").ToArray();

            // If there are no formats, return the custom string normally.
            if (formats.Length == 0)
                return new CustomStringParse[] { new CustomStringParse(value) };
            
            // The Overwatch workshop only supports 3 formats in a string.
            // The following code will split the string into multiple sections so it can support more.
            // Split the string after every 3 unique formats, for example:
            //                        v split here
            // <0> this <1> <0> is a <3> custom <4> string <5>

            List<FormatParameter> stringGroupParameters = new List<FormatParameter>(); // The current group of formats.
            List<StringGroup> stringGroups = new List<StringGroup>(); // Stores information about each section in the string.
            List<int> unique = new List<int>(); // Stores the list of each unique format id. The count shouldn't go above 3.
            for (int i = 0; i < formats.Length; i++)
            {
                FormatParameter parameter = new FormatParameter(formats[i]);

                // If the format id is more than the number of parameters, throw a syntax error.
                if (parameter.Parameter >= parameters.Length)
                    throw new StringParseFailedException(
                        $"Can't set the <{parameter.Parameter}> format, there are only {parameters.Length} parameters.",
                        parameter.Match.Index,
                        parameter.Match.Length
                    );

                // If there is already 3 unique IDs, create a new section.
                if (unique.Count == 3 && !unique.Contains(parameter.Parameter))
                {
                    stringGroups.Add(new StringGroup(stringGroupParameters.ToArray()));
                    stringGroupParameters.Clear();
                    unique.Clear();
                }

                stringGroupParameters.Add(parameter);

                // If the current format ID is new, add it to the unique list.
                if (!unique.Contains(parameter.Parameter))
                    unique.Add(parameter.Parameter);
            }

            // Add tailing formats to a new section.
            stringGroups.Add(new StringGroup(stringGroupParameters.ToArray()));

            // Convert each section to a custom string.
            CustomStringParse[] strings = new CustomStringParse[stringGroups.Count];
            for (int i = 0; i < strings.Length; i++)
            {
                // start is either the start of the string or the end of the last section.
                int start = i == 0                  ? 0            : stringGroups[i - 1].EndIndex;
                // end is the index of last format in the section unless this is the last section, then it will be the end of the string.
                int end   = i == strings.Length - 1 ? value.Length : stringGroups[i]    .EndIndex;

                string groupString = value.Substring(start, end - start);
                
                // Returns an array of all unique formats in the current section.
                var formatGroups = stringGroups[i].Formats
                    .GroupBy(g => g.Parameter)
                    .Select(g => g.First())
                    .ToArray();
                
                // groupParameters is {0}, {1}, and {2}. Length should be between 1 and 3.
                IExpression[] groupParameters = new IExpression[formatGroups.Length];
                for (int g = 0; g < formatGroups.Length; g++)
                {
                    int parameter = formatGroups[g].Parameter;
                    groupString = groupString.Replace("<" + parameter + ">", "{" + g + "}");
                    groupParameters[g] = parameters[parameter];
                }
                strings[i] = new CustomStringParse(groupString, groupParameters);
            }
            return strings;
        }
        
        private static readonly string[] searchOrder = Constants.Strings
            .OrderByDescending(str => str.Contains("{0}"))
            .ThenByDescending(str => str.IndexOfAny("-></*-+=()!?".ToCharArray()) != -1)
            .ThenByDescending(str => str.Length)
            .ToArray();

        static LocalizedString ParseLocalizedString(string value, IExpression[] parameters, int depth = 0)
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
                    LocalizedString str = new LocalizedString(searchString);

                    bool valid = true; // Confirms that the arguments were able to successfully parse.
                    List<LocalizedStringOrExpression> formatParameters = new List<LocalizedStringOrExpression>(); // The parameters that were successfully parsed.

                    // Iterate through the parameters.
                    for (int g = 1; g < match.Groups.Count; g+=3)
                    {
                        string currentParameterValue = match.Groups[g].Captures[0].Value;

                        // Test if the parameter is a format parameter, for example <0>, <1>, <2>, <3>...
                        Match parameterString = Regex.Match(currentParameterValue, "^<([0-9]+)>$");
                        if (parameters != null && parameterString.Success)
                        {
                            int index = int.Parse(parameterString.Groups[1].Value);

                            // Throw syntax error if the number of parameters is less than the parameter index being set.
                            if (index >= parameters.Length)
                                throw new StringParseFailedException($"Can't set the <{index}> format, there are only {parameters.Length} parameters.");

                            formatParameters.Add(new LocalizedStringOrExpression(parameters[index]));
                        }
                        else
                        {
                            // Parse the parameter. If it fails it will return null and the string being checked is probably false.
                            var p = ParseLocalizedString(currentParameterValue, parameters, depth + 1);
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

        public Scope ReturningScope() => null;
        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            if (!Localized)
            {
                IWorkshopTree[] parsed = new IWorkshopTree[Sectioned.Length];
                for (int i = 0; i < parsed.Length; i++)
                    parsed[i] = Sectioned[i].Parse(actionSet);
                
                return V_CustomString.Join(parsed);
            }
            else
            {
                return LocalizedString.Parse(actionSet);
            }
        }
    }

    class CustomStringParse
    {
        public string Text { get; }
        public IExpression[] Parameters { get; } = new IExpression[0];

        public CustomStringParse(string text)
        {
            Text = text;
        }
        public CustomStringParse(string text, IExpression[] parameters) : this(text)
        {
            Parameters = parameters;
        }

        public V_CustomString Parse(ActionSet actionSet) => new V_CustomString(Text, Parameters.Select(p => p.Parse(actionSet)).ToArray());
    }

    class FormatParameter
    {
        public Match Match { get; }
        public int Parameter { get; } 

        public FormatParameter(Match match)
        {
            Match = match;
            Parameter = int.Parse(match.Groups[1].Value);
        }
    }

    class StringGroup
    {
        public FormatParameter[] Formats { get; }
        public int EndIndex { get; }

        public StringGroup(FormatParameter[] formats)
        {
            Formats = formats;
            EndIndex = formats.Last().Match.Index + formats.Last().Match.Length;
        }
    }

    class LocalizedString
    {
        public string String { get; }
        public LocalizedStringOrExpression[] ParameterValues { get; set; }

        public LocalizedString(string str)
        {
            String = str;
        }

        public IWorkshopTree Parse(ActionSet actionSet) => new V_String(String, ParameterValues.Select(pv => (Element)pv.Parse(actionSet)).ToArray());
    }

    class LocalizedStringOrExpression
    {
        public LocalizedString LocalizedString { get; }
        public IExpression Expression { get; }

        public LocalizedStringOrExpression(LocalizedString str)
        {
            LocalizedString = str;
        }
        public LocalizedStringOrExpression(IExpression expression)
        {
            Expression = expression;
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            if (LocalizedString != null) return LocalizedString.Parse(actionSet);
            else return Expression.Parse(actionSet);
        }
    }
}