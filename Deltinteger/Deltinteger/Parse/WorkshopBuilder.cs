using System;
using System.Text;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.I18n;

namespace Deltin.Deltinteger
{
    public class WorkshopBuilder
    {
        public OutputLanguage OutputLanguage { get; }
        public bool CStyle { get; }
        public bool IncludeComments { get; }

        private readonly StringBuilder _builder;
        private int _indentCount;
        private bool _space = false;
        readonly bool _useTabs = false;

        public WorkshopBuilder(OutputLanguage outputLanguage, bool cStyle, bool includeComments, bool useTabs)
        {
            OutputLanguage = outputLanguage;
            CStyle = cStyle;
            IncludeComments = includeComments;
            _useTabs = useTabs;
            _builder = new StringBuilder();
        }

        public WorkshopBuilder Append(string text)
        {
            if (_space)
            {
                if (_indentCount != 0)
                    _builder.Append(new string(_useTabs ? '\t' : ' ', _indentCount * (_useTabs ? 1 : 4)));
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

        public WorkshopBuilder Outdent(int min = 0)
        {
            _indentCount = Math.Max(_indentCount - 1, min);
            return this;
        }

        public int GetCurrentIndent() => _indentCount;
        public void SetCurrentIndent(int count) => Math.Max(_indentCount = count, 0);


        public string GetResult() => _builder.ToString();
    }
}