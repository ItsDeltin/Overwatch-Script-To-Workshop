using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.Strings
{
    class ParseCustomString : StringParseBase
    {
        public ParseCustomString(StringParseInfo stringParseInfo) : base(stringParseInfo) {}

        protected override IStringParse DoParse()
        {
            // Look for <#>s
            var formats = Regex.Matches(Value, FormatMatch).ToArray();

            CustomStringGroup customStringGroup = new CustomStringGroup(Value, formats.Length);

            // If there are no formats, return the custom string normally.
            if (formats.Length == 0)
            {
                customStringGroup.Segments = new CustomStringSegment[] {
                    new CustomStringSegment(Value)
                };
                return customStringGroup;
            }
            
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
                FormatParameter parameter = new FormatParameter(formats[i], FormatGroupNumber);

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
            customStringGroup.Segments = new CustomStringSegment[stringGroups.Count];
            for (int i = 0; i < stringGroups.Count; i++)
            {
                // start is either the start of the string or the end of the last section.
                int start = i == 0                      ? 0            : stringGroups[i - 1].EndIndex;
                // end is the index of last format in the section unless this is the last section, then it will be the end of the string.
                int end   = i == stringGroups.Count - 1 ? Value.Length : stringGroups[i]    .EndIndex;

                string groupString = Value.Substring(start, end - start);
                
                // Returns an array of all unique formats in the current section.
                var formatGroups = stringGroups[i].Formats
                    .GroupBy(g => g.Parameter)
                    .Select(g => g.First())
                    .ToArray();
                
                // groupParameters is {0}, {1}, and {2}. Length should be between 1 and 3.
                int[] groupParameters = new int[formatGroups.Length];
                for (int g = 0; g < formatGroups.Length; g++)
                {
                    int parameter = formatGroups[g].Parameter;
                    groupString = groupString.Replace("<" + parameter + ">", "{" + g + "}");
                    groupParameters[g] = parameter;
                }
                customStringGroup.Segments[i] = new CustomStringSegment(groupString, groupParameters);
            }
            return customStringGroup;
        }
    }

    class CustomStringGroup : IStringParse
    {
        public string Original { get; }
        public int ArgCount { get; }
        public CustomStringSegment[] Segments { get; set; }

        public CustomStringGroup(string original, int argCount)
        {
            Original = original;
            ArgCount = argCount;
        }
    
        public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            IWorkshopTree[] parsed = new IWorkshopTree[Segments.Length];
            for (int i = 0; i < parsed.Length; i++)
                parsed[i] = Segments[i].Parse(actionSet, parameters);
            
            return StringElement.Join(parsed);
        }
    }

    class CustomStringSegment
    {
        public string Text { get; }
        public int[] ParameterIndexes { get; } = new int[0];

        public CustomStringSegment(string text)
        {
            Text = text;
        }
        public CustomStringSegment(string text, int[] parameterIndexes)
        {
            Text = text;
            ParameterIndexes = parameterIndexes;
        }

        public StringElement Parse(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            IWorkshopTree[] resultingParameters = new IWorkshopTree[ParameterIndexes.Length];
            for (int i = 0; i < resultingParameters.Length; i++)
                resultingParameters[i] = parameters[ParameterIndexes[i]];
            
            return new StringElement(Text, resultingParameters);
        }
    }

    class FormatParameter
    {
        public Match Match { get; }
        public int Parameter { get; } 

        public FormatParameter(Match match, int group)
        {
            Match = match;
            Parameter = int.Parse(match.Groups[group].Value);
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