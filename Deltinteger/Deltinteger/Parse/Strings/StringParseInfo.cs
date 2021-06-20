using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse.Strings
{
    public class StringParseInfo
    {
        public string Value { get; }
        public bool ClassicFormatSyntax { get; }

        public StringParseInfo(string value, bool classicFormatSyntax)
        {
            Value = value;
            ClassicFormatSyntax = classicFormatSyntax;
        }
    }
}