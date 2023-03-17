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
        private static readonly CompletionItem[] StringCompletion = Constants.Strings.Select(str => new CompletionItem()
        {
            Label = str,
            Kind = CompletionItemKind.Text
        }).ToArray();

        public string Value { get; private set; }
        public bool Localized { get; private set; }
        public IExpression[] FormatParameters { get; }
        public IStringParse StringParseInfo { get; private set; }
        private readonly ParseInfo _parseInfo;
        private readonly DocRange _stringRange;
        private readonly bool _classicFormatSyntax;
        private bool _shouldParse = true;

        public StringAction(ParseInfo parseInfo, Scope scope, StringExpression stringContext)
        {
            _parseInfo = parseInfo;
            _stringRange = stringContext.Token.Range;
            _classicFormatSyntax = stringContext.ClassicFormatSyntax;
            Value = stringContext.Value;
            Localized = stringContext.Localized;

            // Add completion if the string is localized.
            if (Localized)
                _parseInfo.Script.AddCompletionRange(new CompletionRange(parseInfo.TranslateInfo, StringCompletion, _stringRange, CompletionRangeKind.ClearRest));

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

            // The string does not exist in the cache.
            if (StringParseInfo == null)
            {
                // Parse the string.
                // String parse info
                var stringParseInfo = new StringParseInfo(Value, _classicFormatSyntax);

                // Create the parser.
                var parser = Localized ? (StringParseBase)new ParseLocalizedString(stringParseInfo) : new ParseCustomString(stringParseInfo);

                parser.Parse().Match(
                    ok =>
                    {
                        StringParseInfo = ok;
                        // Cache the string.
                        _parseInfo.TranslateInfo.GetComponent<StringSaverComponent>().Strings.Add(StringParseInfo);
                    },
                    err =>
                    {
                        // Convert the exception to an error.
                        if (err.StringIndex == -1)
                        {
                            _parseInfo.Script.Diagnostics.Error(err.Message, _stringRange);
                        }
                        else
                        {
                            int errorStart = _stringRange.Start.Character + 1 + err.StringIndex;
                            _parseInfo.Script.Diagnostics.Error(err.Message, new DocRange(
                                new DocPos(_stringRange.Start.Line, errorStart),
                                new DocPos(_stringRange.Start.Line, errorStart + err.Length)
                            ));
                        }
                    }
                );
            }

            if (StringParseInfo != null)
            {
                // If there is no current usage resolver, add the error.
                if (_parseInfo.CurrentUsageResolver == null)
                    AddStringFormatCountError();
                else // Otherwise, wait for the usage to be resolved before deciding if the error should be added.
                {
                    _parseInfo.CurrentUsageResolver.OnResolve(usage =>
                    {
                        // Add the error if the usage is not StringFormat.
                        if (usage != UsageType.StringFormat)
                            AddStringFormatCountError();
                    });
                }
            }
        }

        void AddStringFormatCountError()
        {
            if (FormatParameters.Length != StringParseInfo.ArgCount)
                _parseInfo.Script.Diagnostics.Error($"String format requires {StringParseInfo.ArgCount} arguments, got {FormatParameters.Length} values", _stringRange);
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