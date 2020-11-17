using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse.Strings
{
    public abstract class StringParseBase
    {
        public IStringParse Result { get; private set; }
        protected ParseInfo ParseInfo { get; }
        protected string Value { get; }
        protected int ArgCount { get; }
        private readonly DocRange _stringRange;
        private bool _awaitingUsage = false;
        private bool _addedToCache = false;

        protected StringParseBase(ParseInfo parseInfo, string value, DocRange stringRange, int argCount)
        {
            ParseInfo = parseInfo;
            Value = value;
            _stringRange = stringRange;
            ArgCount = argCount;
        }

        public IStringParse Parse()
        {
            Result = DoParse();
            AddToCache();
            return Result;
        }

        protected abstract IStringParse DoParse();

        protected void StringFormatCountError(int argID, DocRange range)
        {
            // If there is no current usage resolver, add the error.
            if (ParseInfo.CurrentUsageResolver == null)
                AddStringFormatCountError(argID, range);
            else // Otherwise, wait for the usage to be resolved before deciding if the error should be added.
            {
                _awaitingUsage = true;
                ParseInfo.CurrentUsageResolver.OnResolve(usage => {
                    _awaitingUsage = false;
                    // Add the error if the usage is not StringFormat.
                    if (usage != UsageType.StringFormat)
                        AddStringFormatCountError(argID, range);
                    else
                        AddToCache();
                });
            }
        }

        private void AddStringFormatCountError(int argID, DocRange range)
        {
            ParseInfo.Script.Diagnostics.Error($"Can't set the {{{argID}}} format, there are only {ArgCount} parameters", range);
        }

        protected DocRange RangeFromMatch(int index, int length)
        {
            int start = _stringRange.Start.Character + 1 + index;
            return new DocRange(
                new DocPos(_stringRange.Start.Line, start),
                new DocPos(_stringRange.Start.Line, start + length)
            );
        }

        private void AddToCache()
        {
            if (_awaitingUsage || _addedToCache) return;
            _addedToCache = true;
            StringSaverComponent.Add(Result);
        }
    }
}