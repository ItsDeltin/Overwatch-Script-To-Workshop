using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse.Strings
{
    public abstract class StringParseBase
    {
        public IStringParse Result { get; private set; }
        protected StringParseInfo StringParseInfo { get; }
        protected string FormatMatch { get; }
        protected int FormatGroupNumber { get; }

        protected string Value => StringParseInfo.Value;
        protected bool ClassicFormatSyntax => StringParseInfo.ClassicFormatSyntax;

        protected StringParseBase(StringParseInfo stringParseInfo)
        {
            StringParseInfo = stringParseInfo;
            FormatMatch = ClassicFormatSyntax ? "<([0-9]+)>" : "(?:(?<!{)({{)*){([0-9]+)}";
            FormatGroupNumber = ClassicFormatSyntax ? 1 : 2;
        }

        public IStringParse Parse()
        {
            Result = DoParse();
            return Result;
        }

        protected abstract IStringParse DoParse();
    }
}