using System;
using System.Text;

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
}