using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Deltin.Deltinteger.Parse;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;
using MarkedStringsOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.MarkedStringsOrMarkupContent;
using MarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.MarkupContent;
using MarkupKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.MarkupKind;
using DocumentUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;


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
                return Path.GetFullPath(combined);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string RemoveQuotes(this string str) => str.Length >= 2 &&
            ((str[0] == '"' && str[str.Length - 1] == '"') || (str[0] == '\'' && str[str.Length - 1] == '\'')) ? str.Substring(1, str.Length - 2) : str;

        public static string FilePath(this Uri uri) => uri.LocalPath;
        public static bool Compare(this Uri uri, Uri other) => uri.LocalPath == other.LocalPath;
        public static DocumentUri ToDefinition(this Uri uri)
        {
            string enc = uri.LocalPath.Replace('\\', '/').Replace(" ", "%20").Replace(":", "%3A");
            return DocumentUri.File(uri.LocalPath);
        }

        public static Uri Clean(this Uri uri)
        {
            return new Uri(uri.FilePath());
        }

        public static string GetNameOrVoid(this CodeType type) => type?.GetName() ?? "void";
        public static string GetNameOrAny(this CodeType type) => type?.GetName() ?? "Any";

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

        public static V GetValueOrAddKey<T, V>(this Dictionary<T, V> dictionary, T key) where V : class, new()
        {
            if (!dictionary.TryGetValue(key, out V value))
            {
                value = new V();
                dictionary.Add(key, value);
            }
            return value;
        }

        public static int TextIndexFromPosition(string text, Position pos)
        {
            if (pos.Line == 0 && pos.Character == 0) return 0;

            int line = 0;
            int character = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    character = 0;
                }
                else
                {
                    character++;
                }

                if (pos.Line == line && pos.Character == character)
                    return i + 1;

                if (line > pos.Line)
                    throw new Exception($"Surpassed position {pos} in text: {text}");
            }
            throw new Exception($"Failed to locate position {pos} in text: {text}");
        }
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

        public MarkupBuilder Add(string text)
        {
            result.Append(text);
            noMarkup.Append(text);
            return this;
        }
        public MarkupBuilder Add(MarkupBuilder markupBuilder)
        {
            result.Append(markupBuilder.result);
            noMarkup.Append(markupBuilder.noMarkup);
            return this;
        }
        public MarkupBuilder Italicize(string text)
        {
            result.Append("*" + text + "*");
            noMarkup.Append(text);
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
            result.Append("\n");
            noMarkup.Append("\n");
            return this;
        }
        public MarkupBuilder StartCodeLine(string language = "ostw")
        {
            inCodeLine = true;
            result.Append($"```{language}\n");
            return this;
        }
        public MarkupBuilder EndCodeLine()
        {
            inCodeLine = false;
            result.Append("\n```");
            return this;
        }
        public MarkupBuilder NewSection()
        {
            result.Append("\n ----- \n");
            noMarkup.Append("\n");
            return this;
        }
        public MarkupBuilder Indent() => Add("    ");
        public MarkupBuilder If(bool condition, Action<MarkupBuilder> modify)
        {
            if (condition) modify(this);
            return this;
        }

        public override string ToString() => noMarkup.ToString();
        public string ToString(bool markup) => markup ? result.ToString() : noMarkup.ToString();
        public MarkupContent ToMarkup() => new MarkupContent()
        {
            Kind = MarkupKind.Markdown,
            Value = ToString()
        };

        public static implicit operator MarkupBuilder(string value) => value == null ? null : new MarkupBuilder(value);
        public static implicit operator string(MarkupBuilder builder) => builder?.ToString(false);
        public static implicit operator StringOrMarkupContent(MarkupBuilder builder) => builder == null ? null : new StringOrMarkupContent((MarkupContent)builder);
        public static implicit operator MarkupContent(MarkupBuilder builder) => builder == null ? null : new MarkupContent() { Kind = MarkupKind.Markdown, Value = builder.ToString(true) };
        public static implicit operator MarkedStringsOrMarkupContent(MarkupBuilder builder) => new MarkedStringsOrMarkupContent(builder);
    }
}
