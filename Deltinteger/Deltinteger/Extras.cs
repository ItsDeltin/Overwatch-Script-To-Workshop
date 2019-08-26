using System;
using System.Text;
using System.Collections.Generic;
using System.IO;

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
    }

    public class TabStringBuilder
    {
        public int Indent { get; set; }
        public bool Tab { get; private set; }
        public int WhitespaceCount { get; set; } = 4;

        private readonly StringBuilder StringBuilder = new StringBuilder();

        public TabStringBuilder(bool tab)
        {
            Tab = tab;
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
}