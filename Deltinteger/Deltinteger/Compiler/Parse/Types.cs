using System.Text;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Compiler.Parse
{
    public class TokenCapture
    {
        public int StartToken { get; }
        public int Length { get; private set; }
        public object Node { get; private set; }
        public bool HasError { get; set; }

        public TokenCapture(int startToken)
        {
            StartToken = startToken;
        }

        public void Finish(int position, object node)
        {
            Length = position - StartToken;
            Node = node;
        }

        public bool IsValid => Length > 0 && !HasError;
    }

    public class VersionInstance
    {
        public string Text { get; }
        readonly List<int> _newlines = new List<int>();

        public VersionInstance(string text)
        {
            Text = text;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\n')
                    _newlines.Add(i);
        }

        public int GetLine(int index)
        {
            int r;
            for (r = 0; r < _newlines.Count && _newlines[r] < index; r++) ;
            return r;
        }
        public int GetColumn(int index) => index - GetLineIndex(GetLine(index));
        public int IndexOf(DocPos pos) => GetLineIndex(pos.Line) + pos.Character;
        public int GetLineIndex(int line) => line == 0 ? 0 : (_newlines[line - 1] + 1);

        public void UpdatePosition(DocPos pos, int index)
        {
            pos.Line = GetLine(index);
            pos.Character = GetColumn(index);
        }

        public VersionInstance Update(params UpdateRange[] updateRanges)
        {
            StringBuilder builder = new StringBuilder(Text);

            foreach (var updateRange in updateRanges)
            {
                int start = IndexOf(updateRange.Range.Start);
                int length = IndexOf(updateRange.Range.End) - start;

                builder.Remove(start, length);
                builder.Insert(start, updateRange.Text);
            }

            return new VersionInstance(builder.ToString());
        }
    }
}