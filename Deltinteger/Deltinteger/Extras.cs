using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Deltin.Deltinteger.Parse;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;
using MarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.MarkupContent;
using MarkupKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.MarkupKind;

namespace Deltin.Deltinteger
{
    public static class Extras
    {
        public static string Indent(int indent, bool tab)
        {
            if (tab)
            {
                string result = "";
                for (int i = 0; i < indent; i++)
                    result += "\t";
                return result;
            }
            else return new string(' ', indent * 4);
        }

        public static string AddSpacesToSentence(string text, bool preserveAcronyms)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                        i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        public static string CombinePathWithDotNotation(string referenceDirectory, string file)
        {
            if (file.Length > 0 && file[0] == '!')
            {
                referenceDirectory = Path.Combine(Program.ExeFolder, "Modules" + Path.DirectorySeparatorChar);
                file = file.Substring(1);
            }

            try
            {
                string directory = Path.GetDirectoryName(referenceDirectory);
                string combined = Path.Combine(directory, file);
                if (file == "") combined += Path.DirectorySeparatorChar;
                if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    combined = "/" + combined;
                return Path.GetFullPath(combined);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string Lines(params string[] lines)
        {
            return string.Join("\n", lines);
        }

        public static string RemoveQuotes(this string str) => str.Length >= 2 && str[0] == '"' && str[str.Length - 1] == '"' ? str.Substring(1, str.Length - 2) : str;

        public static string FilePath(this Uri uri)
        {
			var path = uri.LocalPath.TrimStart('\\').TrimStart('/');
			if(!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            	return "/" + path.Replace('\\', '/');
			else
				return path;
        }

        public static Uri Clean(this Uri uri)
        {
			return new Uri(uri.FilePath());
        }

        public static bool Compare(this Uri uri, Uri other) => uri.Clean().FilePath() == other.Clean().FilePath();

        public static string GetNameOrDefine(this CodeType type) => type?.GetName() ?? "define";

        public static bool CodeTypeParameterInvalid(this CodeType parameterType, CodeType valueType) =>
            parameterType != null && ((parameterType.IsConstant() && valueType == null) || (valueType != null && !valueType.Implements(parameterType)));

        public static Uri Definition(string path)
        {
            string enc = "file:///" + path.Replace('\\', '/').Replace(" ", "%20").Replace(":", "%3A");
            return new Uri(enc);
        }

        public static StringOrMarkupContent GetMarkupContent(string text) => new StringOrMarkupContent(new MarkupContent()
        {
            Kind = MarkupKind.Markdown,
            Value = text
        });

        public static string ListJoin(string collectionName, params string[] elements)
        {
            if (elements.Length == 0) return collectionName;
            if (elements.Length == 1) return elements[0] + " " + collectionName;

            string result = "";
            for (int i = 0; i < elements.Length; i++)
            {
                if (i < elements.Length - 2) result += elements[i] + ", ";
                else if (i < elements.Length - 1) result += elements[i] + " and ";
                else result += elements[i];
            }
            result += " " + collectionName + "s";
            return result;
        }

        public static string RemoveStructuralChars(this string str) => str.Replace(",", "").Replace("(", "").Replace(")", "");
    }

    class ArrayBuilder<T>
    {
        private readonly T[] values;

        public ArrayBuilder(T[] values)
        {
            this.values = values;
        }

        public static implicit operator ArrayBuilder<T>(T value)
        {
            return new ArrayBuilder<T>(new T[] { value });
        }

        public static implicit operator ArrayBuilder<T>(T[] value)
        {
            return new ArrayBuilder<T>(value);
        }

        public static T[] Build(params ArrayBuilder<T>[] values)
        {
            List<T> valueList = new List<T>();

            foreach (var val in values)
                if (val?.values != null)
                    valueList.AddRange(val.values);

            return valueList.ToArray();
        }
    }

    public class MarkupBuilder
    {
        StringBuilder result = new StringBuilder();
        StringBuilder noMarkup = new StringBuilder();
        bool inCodeLine = false;

        public MarkupBuilder() { }

        public MarkupBuilder(string value)
        {
            result.Append(value);
            noMarkup.Append(value);
        }

        public MarkupBuilder Add(string line)
        {
            result.Append(line);
            noMarkup.Append(line);
            return this;
        }
        public MarkupBuilder Code(string line)
        {
            result.Append("`" + line + "`");
            noMarkup.Append(line);
            return this;
        }
        public MarkupBuilder NewLine()
        {
            if (inCodeLine)
            {
                result.Append("\n");
                noMarkup.Append("\n");
            }
            else
            {
                result.Append("\n\r");
                noMarkup.Append("\n\r");
            }
            return this;
        }
        public MarkupBuilder StartCodeLine()
        {
            inCodeLine = true;
            result.Append("```ostw\n");
            return this;
        }
        public MarkupBuilder EndCodeLine()
        {
            inCodeLine = false;
            result.Append("\n\r```");
            return this;
        }
        public MarkupBuilder NewSection()
        {
            result.Append("\n\r ----- \n\r");
            noMarkup.Append("\n\r");
            return this;
        }

        public override string ToString() => result.ToString();
        public string ToString(bool markup) => markup ? result.ToString() : noMarkup.ToString();
        public MarkupContent ToMarkup() => new MarkupContent()
        {
            Kind = MarkupKind.Markdown,
            Value = ToString()
        };
        
        public static implicit operator MarkupBuilder(string value) => new MarkupBuilder(value);
        public static implicit operator StringOrMarkupContent(MarkupBuilder builder) => new StringOrMarkupContent(builder);
        public static implicit operator MarkupContent(MarkupBuilder builder) => new MarkupContent() { Kind = MarkupKind.Markdown, Value = builder.ToString(true) };
    }
}
