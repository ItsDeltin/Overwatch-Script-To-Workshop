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

        public static string RemoveQuotes(string str)
        {
            return str.Substring(1, str.Length - 2);
        }

        public static AccessLevel GetAccessLevel(this DeltinScriptParser.AccessorContext accessorContext)
        {
            if (accessorContext == null) return AccessLevel.Private;
            else if (accessorContext.PUBLIC() != null) return AccessLevel.Public;
            else if (accessorContext.PRIVATE() != null) return AccessLevel.Private;
            else if (accessorContext.PROTECTED() != null) return AccessLevel.Protected;
            else throw new NotImplementedException();
        }

        public static string SerializeToXML<T>(object o)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("","");

            string result;
            using (StringWriter stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, o, ns);
                result = stringWriter.ToString();
            }
            return result;
        }

        public static string FilePath(this Uri uri)
        {
            return uri.LocalPath.TrimStart('/');
        }

        public static Uri Clean(this Uri uri)
        {
            return new Uri(uri.FilePath());
        }

        public static bool Compare(this Uri uri, Uri other) => uri.Clean().FilePath() == other.Clean().FilePath();

        public static Uri Definition(string path)
        {
            string enc = "file:///" + path.Replace('\\', '/').Replace(" ","%20").Replace(":", "%3A");
            return new Uri(enc);
        }

        public static StringOrMarkupContent GetMarkupContent(string text) => new StringOrMarkupContent(new MarkupContent() {
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
    }

    public class TabStringBuilder
    {
        public int Indent { get; set; }
        public bool Tab { get; private set; }
        public int WhitespaceCount { get; set; } = 4;

        private readonly StringBuilder StringBuilder;

        public TabStringBuilder(bool tab)
        {
            Tab = tab;
            StringBuilder = new StringBuilder();
        }
        public TabStringBuilder(StringBuilder builder, bool tab)
        {
            Tab = tab;
            StringBuilder = builder;
        }

        public TabStringBuilder AppendLine()
        {
            StringBuilder.AppendLine(Extras.Indent(Indent, Tab));
            return this;
        }

        public TabStringBuilder AppendLine(string text)
        {
            StringBuilder.AppendLine(Extras.Indent(Indent, Tab) + text);
            return this;
        }

        public TabStringBuilder Append(string text)
        {
            StringBuilder.Append(text);
            return this;
        }

        public override string ToString()
        {
            return StringBuilder.ToString();
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

    class MarkupBuilder
    {
        StringBuilder result = new StringBuilder();

        public MarkupBuilder() {}

        public MarkupBuilder Add(string line)
        {
            result.Append(line);
            return this;
        }
        public MarkupBuilder NewLine()
        {
            result.Append("\n\r");
            return this;
        }
        public MarkupBuilder StartCodeLine()
        {
            result.Append("```ostw\n");
            return this;
        }
        public MarkupBuilder EndCodeLine()
        {
            result.Append("\n\r```");
            return this;
        }
        public MarkupBuilder NewSection()
        {
            result.Append("\n\r ----- \n\r");
            return this;
        }

        public override string ToString() => result.ToString();
    }
}