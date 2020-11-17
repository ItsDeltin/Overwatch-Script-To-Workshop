using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse.Strings;
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
        public IStringParse StringParseInfo { get; private set; }
        private bool _shouldParse = true;

        public StringAction(ParseInfo parseInfo, Scope scope, StringExpression stringContext)
        {
            _parseInfo = parseInfo;
            Value = stringContext.Value;
            Localized = stringContext.Localized;
            _stringRange = stringContext.Token.Range;

            // Add completion if the string is localized.
            if (Localized)
                _parseInfo.Script.AddCompletionRange(new CompletionRange(StringCompletion, _stringRange, CompletionRangeKind.ClearRest));

            // Get the format parameters.
            if (stringContext.Formats == null)
                // No formats.
                FormatParameters = new IExpression[0];
            else
            {
                // Has formats.
                FormatParameters = new IExpression[stringContext.Formats.Count];
                for (int i = 0; i < FormatParameters.Length; i++)
                    FormatParameters[i] = parseInfo.GetExpression(scope, stringContext.Formats[i]);
            }
            
            parseInfo.CurrentUsageResolver?.OnResolve(usage => _shouldParse = usage != UsageType.StringFormat);

            ParseString();
        }

        private void ParseString()
        {
            StringParseInfo = StringSaverComponent.GetCachedString(Value, Localized);
            if (StringParseInfo == null)
            {
                try
                {
                    StringParseBase parser = Localized ?
                        (StringParseBase)new ParseLocalizedString(_parseInfo, Value, _stringRange, FormatParameters.Length) :
                        new ParseCustomString(_parseInfo, Value, _stringRange, FormatParameters.Length);
                    StringParseInfo = parser.Parse();
                }
                catch (StringParseFailedException ex)
                {
                    if (ex.StringIndex == -1)
                    {
                        _parseInfo.Script.Diagnostics.Error(ex.Message, _stringRange);
                    }
                    else
                    {
                        int errorStart = _stringRange.Start.Character + 1 + ex.StringIndex;
                        _parseInfo.Script.Diagnostics.Error(ex.Message, new DocRange(
                            new DocPos(_stringRange.Start.Line, errorStart),
                            new DocPos(_stringRange.Start.Line, errorStart + ex.Length)
                        ));
                    }
                }
            }
            _parseInfo.TranslateInfo.GetComponent<StringSaverComponent>().Strings.Add(StringParseInfo);
        }

        public Scope ReturningScope() => Type().GetObjectScope();
        public CodeType Type() => _parseInfo.TranslateInfo.Types.String();
        public IWorkshopTree Parse(ActionSet actionSet) => _shouldParse ? StringParseInfo.Parse(actionSet, FormatParameters.Select(fp => fp.Parse(actionSet)).ToArray()) : null;

        public static DocRange RangeFromMatch(DocRange stringRange, int index, int length)
        {
            int start = stringRange.Start.Character + 1 + index;
            return new DocRange(
                new DocPos(stringRange.Start.Line, start),
                new DocPos(stringRange.Start.Line, start + length)
            );
        }
    }
}