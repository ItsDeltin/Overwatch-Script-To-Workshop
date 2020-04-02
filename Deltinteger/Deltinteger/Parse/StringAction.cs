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

        private ParseInfo _parseInfo;
        private DocRange _stringRange;
        public string Value { get; private set; }
        public bool Localized { get; private set; }
        public IExpression[] FormatParameters { get; }
        private IStringParse String;

        // Normal
        public StringAction(ParseInfo parseInfo, DeltinScriptParser.StringContext stringContext)
        {
            Init(parseInfo, stringContext);
            FormatParameters = new IExpression[0];
            ParseString();
        }

        // Formatted
        public StringAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Formatted_stringContext stringContext)
        {
            Init(parseInfo, stringContext.@string());
            FormatParameters = new IExpression[stringContext.expr().Length];
            for (int i = 0; i < FormatParameters.Length; i++)
                FormatParameters[i] = DeltinScript.GetExpression(parseInfo, scope, stringContext.expr(i));
            ParseString();
        }

        private void Init(ParseInfo parseInfo, DeltinScriptParser.StringContext stringContext)
        {
            _parseInfo = parseInfo;
            Value = Extras.RemoveQuotes(stringContext.STRINGLITERAL().GetText());
            Localized = stringContext.LOCALIZED() != null;
            _stringRange = DocRange.GetRange(stringContext.STRINGLITERAL());

            if (Localized)
                _parseInfo.Script.AddCompletionRange(new CompletionRange(StringCompletion, _stringRange, CompletionRangeKind.ClearRest));
        }

        private void ParseString()
        {
            String = GetCachedString(Value, Localized);
            if (String == null)
            {
                try
                {
                    if (!Localized)
                        String = CustomStringGroup.ParseCustomString(Value, FormatParameters.Length);
                    else
                        String = LocalizedString.ParseLocalizedString(Value, 0, Value, FormatParameters.Length);
                    
                    lock (_cacheLock) _cache.Add(String);
                }
                catch (StringParseFailedException ex)
                {
                    if (ex.StringIndex == -1)
                    {
                        _parseInfo.Script.Diagnostics.Error(ex.Message, _stringRange);
                    }
                    else
                    {
                        int errorStart = _stringRange.start.character + 1 + ex.StringIndex;
                        _parseInfo.Script.Diagnostics.Error(ex.Message, new DocRange(
                            new Pos(_stringRange.start.line, errorStart),
                            new Pos(_stringRange.start.line, errorStart + ex.Length)
                        ));
                    }
                }
            }
            _parseInfo.TranslateInfo.GetComponent<StringSaverComponent>().Strings.Add(String);
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
        public IWorkshopTree Parse(ActionSet actionSet) => String.Parse(actionSet, FormatParameters.Select(fp => fp.Parse(actionSet)).ToArray());

        private static readonly object _cacheLock = new object();
        private static readonly List<IStringParse> _cache = new List<IStringParse>();

        private static IStringParse GetCachedString(string str, bool localized)
        {
            lock (_cacheLock)
                foreach(var cachedString in _cache)
                    if ((localized && cachedString is LocalizedString) || (!localized && cachedString is CustomStringGroup))
                        return cachedString;
            return null;
        }

        public static void RemoveUnused(List<IStringParse> strings)
        {
            lock (_cacheLock)
                for (int i = _cache.Count - 1; i >= 0; i--)
                    if (strings.Contains(_cache[i]))
                        _cache.RemoveAt(i);
        }
    }

    class StringSaverComponent : IComponent, IDisposable
    {
        public DeltinScript DeltinScript { get; set; }
        public List<IStringParse> Strings { get; } = new List<IStringParse>();
        
        public void Init() {}

        public void Dispose()
        {
            StringAction.RemoveUnused(Strings);
        }
    }

    public interface IStringParse
    {
        string Original { get; }
        IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameters);
    }

    class CustomStringGroup : IStringParse
    {
        public string Original { get; }
        public CustomStringSegment[] Segments { get; private set; }

        CustomStringGroup(string original)
        {
            Original = original;
        }

        public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            IWorkshopTree[] parsed = new IWorkshopTree[Segments.Length];
            for (int i = 0; i < parsed.Length; i++)
                parsed[i] = Segments[i].Parse(actionSet, parameters);
            
            return V_CustomString.Join(parsed);
        }

        public static CustomStringGroup ParseCustomString(string value, int parameterCount)
        {
            // Look for <#>s
            var formats = Regex.Matches(value, "<([0-9]+)>").ToArray();

            CustomStringGroup customStringGroup = new CustomStringGroup(value);

            // If there are no formats, return the custom string normally.
            if (formats.Length == 0)
            {
                customStringGroup.Segments = new CustomStringSegment[] {
                    new CustomStringSegment(value)
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
                FormatParameter parameter = new FormatParameter(formats[i]);

                // If the format id is more than the number of parameters, throw a syntax error.
                if (parameter.Parameter >= parameterCount)
                    throw new StringParseFailedException(
                        $"Can't set the <{parameter.Parameter}> format, there are only {parameterCount} parameters.",
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
            customStringGroup.Segments = new CustomStringSegment[stringGroups.Count];
            for (int i = 0; i < stringGroups.Count; i++)
            {
                // start is either the start of the string or the end of the last section.
                int start = i == 0                      ? 0            : stringGroups[i - 1].EndIndex;
                // end is the index of last format in the section unless this is the last section, then it will be the end of the string.
                int end   = i == stringGroups.Count - 1 ? value.Length : stringGroups[i]    .EndIndex;

                string groupString = value.Substring(start, end - start);
                
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
                    groupParameters[g] = g;
                }
                customStringGroup.Segments[i] = new CustomStringSegment(groupString, groupParameters);
            }
            return customStringGroup;
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

        public V_CustomString Parse(ActionSet actionSet, IWorkshopTree[] parameters)
        {
            IWorkshopTree[] resultingParameters = new IWorkshopTree[ParameterIndexes.Length];
            for (int i = 0; i < resultingParameters.Length; i++)
                resultingParameters[i] = parameters[ParameterIndexes[i]];
            
            return new V_CustomString(Text, resultingParameters);
        }
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

    class LocalizedString : IStringParse
    {
        public string Original { get; }
        public string String { get; }
        public LocalizedStringOrExpression[] ParameterValues { get; set; }

        public LocalizedString(string original, string str)
        {
            Original = original;
            String = str;
        }

        private static readonly string[] searchOrder = Constants.Strings
            .OrderByDescending(str => str.Contains("{0}"))
            .ThenByDescending(str => str.IndexOfAny("-></*-+=()!?".ToCharArray()) != -1)
            .ThenByDescending(str => str.Length)
            .ToArray();

        public static LocalizedString ParseLocalizedString(string original, int charOffset, string value, int parameterCount, int depth = 0)
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
                    LocalizedString str = new LocalizedString(original, searchString);

                    bool valid = true; // Confirms that the arguments were able to successfully parse.
                    List<LocalizedStringOrExpression> formatParameters = new List<LocalizedStringOrExpression>(); // The parameters that were successfully parsed.

                    // Iterate through the parameters.
                    for (int g = 1; g < match.Groups.Count; g+=3)
                    {
                        Capture capture = match.Groups[g].Captures[0];
                        string currentParameterValue = capture.Value;

                        // Test if the parameter is a format parameter, for example <0>, <1>, <2>, <3>...
                        Match parameterString = Regex.Match(currentParameterValue, "^<([0-9]+)>$");
                        if (parameterString.Success)
                        {
                            int index = int.Parse(parameterString.Groups[1].Value);

                            // Throw syntax error if the number of parameters is less than the parameter index being set.
                            if (index >= parameterCount)
                                throw new StringParseFailedException($"Can't set the <{index}> format, there are only {parameterCount} parameters.", charOffset + capture.Index, parameterString.Length);

                            formatParameters.Add(new LocalizedStringOrExpression(index));
                        }
                        else
                        {
                            // Parse the parameter. If it fails it will return null and the string being checked is probably false.
                            var p = ParseLocalizedString(original, charOffset + capture.Index, currentParameterValue, parameterCount, depth + 1);
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

        public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameters) => new V_String(String, ParameterValues.Select(pv => (Element)pv.Parse(actionSet, parameters)).ToArray());
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