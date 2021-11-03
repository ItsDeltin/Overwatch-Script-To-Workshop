using System.Linq;

namespace DS.Analysis.Utility
{
    static class Extensions
    {
        public static string RemoveQuotes(this string value)
        {
            if (StartsAndEndsWith(value, '\'') || StartsAndEndsWith(value, '"'))
                return value.Substring(1, value.Length - 2);
            return value;
        }

        static bool StartsAndEndsWith(string str, char value)
        {
            return str.Length >= 2 && str.First() == value && str.Last() == value;
        }
    }
}