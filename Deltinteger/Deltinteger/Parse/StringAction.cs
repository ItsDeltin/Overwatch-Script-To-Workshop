using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class StringAction : IExpression
    {
        public string Value { get; }
        public bool Localized { get; }
        public IExpression[] FormatParameters { get; }
        private CustomStringParse[] Sectioned;
        private DocRange StringRange { get; }

        // Normal
        public StringAction(ScriptFile script, DeltinScriptParser.StringContext stringContext, bool parse = true)
        {
            Value = Extras.RemoveQuotes(stringContext.STRINGLITERAL().GetText());
            Localized = stringContext.LOCALIZED() != null;
            StringRange = DocRange.GetRange(stringContext.STRINGLITERAL());
            if (parse) ParseString(script);
        }
        // Formatted
        public StringAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Formatted_stringContext stringContext) : this(script, stringContext.@string(), false)
        {
            FormatParameters = new IExpression[stringContext.expr().Length];
            for (int i = 0; i < FormatParameters.Length; i++)
                FormatParameters[i] = DeltinScript.GetExpression(script, translateInfo, scope, stringContext.expr(i));
            ParseString(script);
        }

        private void ParseString(ScriptFile script)
        {
            if (!Localized)
            {
                try
                {
                    Sectioned = ParseCustomString(Value, FormatParameters);
                }
                catch (StringParseFailedException ex)
                {
                    int errorStart = StringRange.start.character + 1 + ex.StringIndex;
                    script.Diagnostics.Error(ex.Message, new DocRange(
                        new Pos(StringRange.start.line, errorStart),
                        new Pos(StringRange.start.line, errorStart + ex.Length)
                    ));
                }
            }
            else throw new NotImplementedException();
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

        public Scope ReturningScope() => null;
        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            if (!Localized)
            {
                IWorkshopTree[] parsed = new IWorkshopTree[Sectioned.Length];
                for (int i = 0; i < parsed.Length; i++)
                    parsed[i] = Sectioned[i].Parse(actionSet);
                
                return V_CustomString.Join(parsed);
            }
            else throw new NotImplementedException();
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
}