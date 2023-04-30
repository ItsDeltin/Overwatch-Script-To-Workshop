using System;
using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.I18n;

namespace Deltin.Deltinteger
{
    public class WorkshopBuilder
    {
        public bool Tab { get; } = false;
        public OutputLanguage OutputLanguage { get; }
        public bool CStyle { get; }
        public bool IncludeComments { get; }

        private readonly StringBuilder _builder;
        private int _indentCount;
        private bool _space = false;

        public WorkshopBuilder(OutputLanguage outputLanguage, bool cStyle, bool includeComments)
        {
            OutputLanguage = outputLanguage;
            CStyle = cStyle;
            IncludeComments = includeComments;
            _builder = new StringBuilder();
        }

        public WorkshopBuilder Append(string text)
        {
            if (_space)
            {
                if (_indentCount != 0)
                    _builder.Append(new string(Tab ? '\t' : ' ', _indentCount * (Tab ? 1 : 4)));
                _space = false;
            }
            _builder.Append(text);
            return this;
        }
        public WorkshopBuilder AppendLine()
        {
            _builder.AppendLine();
            _space = true;
            return this;
        }
        public WorkshopBuilder AppendLine(string text) => Append(text).AppendLine();
        public WorkshopBuilder AppendKeyword(string keyword) => Append(Kw(keyword));
        public WorkshopBuilder AppendKeywordLine(string keyword) => AppendLine(Kw(keyword));

        public string Kw(string keyword) => LanguageInfo.Translate(OutputLanguage, keyword);

        public WorkshopBuilder Indent()
        {
            _indentCount++;
            return this;
        }

        public WorkshopBuilder Outdent()
        {
            _indentCount--;
            return this;
        }

        public string Translate(string keyword) => LanguageInfo.Translate(OutputLanguage, keyword);

        public string GetResult() => _builder.ToString();
    }
}