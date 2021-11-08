using System;
using System.IO;
using System.Linq;
using LSUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri;

namespace DS
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

        public static string Normalize(this Uri uri) => Path.GetFullPath(uri.AbsolutePath);

        public static string Normalize(this LSUri uri) => Path.GetFullPath(uri.GetFileSystemPath());
    }
}