using System;
using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.I18n;

namespace Deltin.Deltinteger
{
    public class WorkshopBuilder
    {
        public bool Tab { get; set; } = false;
        public OutputLanguage OutputLanguage { get; }
        private readonly StringBuilder Builder;
        private int IndentCount;
        private bool InLine = false;

        public WorkshopBuilder(OutputLanguage outputLanguage)
        {
            OutputLanguage = outputLanguage;
            Builder = new StringBuilder();
        }

        public WorkshopBuilder(OutputLanguage outputLanguage, StringBuilder builder)
        {
            OutputLanguage = outputLanguage;
            Builder = builder;
        }

        public WorkshopBuilder Append(string text)
        {
            if (!InLine) Builder.Append(Spacer());
            Builder.Append(text);
            InLine = true;
            return this;
        }

        public WorkshopBuilder AppendLine()
        {
            Builder.AppendLine();
            InLine = false;
            return this;
        }

        public WorkshopBuilder AppendLine(string text)
        {
            Builder.AppendLine(Spacer() + text);
            InLine = false;
            return this;
        }

        public WorkshopBuilder AppendKeywordLine(string keyword)
        {
            AppendLine(LanguageInfo.Translate(OutputLanguage, keyword));
            return this;
        }

        public WorkshopBuilder AppendKeyword(string keyword)
        {
            Append(LanguageInfo.Translate(OutputLanguage, keyword));
            return this;
        }

        public WorkshopBuilder Indent()
        {
            IndentCount++;
            return this;
        }

        public WorkshopBuilder Unindent()
        {
            IndentCount--;
            return this;
        }

        public string Translate(string keyword) => LanguageInfo.Translate(OutputLanguage, keyword);
        private string Spacer() => Extras.Indent(IndentCount, Tab);
        public override string ToString() => Builder.ToString();
    }
}