using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Model;

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

        protected string MatchParameter(int parameter)
        {
            if (ClassicFormatSyntax)
                return "<" + parameter + ">";
            else
                return @"(?:(?<!{)({{)*)\{" + parameter + "}";
        }

        public abstract Result<IStringParse, StringParseError> Parse();
    }

    public struct StringParseError
    {
        public string Message;
        public int StringIndex;
        public int Length;

        public StringParseError()
        {
            Message = "Failed to parse the string.";
            StringIndex = -1;
            Length = 0;
        }
    }
}