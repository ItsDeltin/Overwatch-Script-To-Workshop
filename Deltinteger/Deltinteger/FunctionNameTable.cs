using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger
{
    public class NameTable
    {
        public static void MakeNameTable()
        {
            var elements = new List<ElementBaseJson>();

            // Add the actions.
            foreach (var element in ElementRoot.Instance.Actions)
                if (!element.IsHidden && element.CodeName().ToLower() != element.Name.Replace(" ", "").Replace("-", "").ToLower())
                    elements.Add(element);
            
            // Add the values.
            foreach (var element in ElementRoot.Instance.Values)
                if (!element.IsHidden && element.CodeName().ToLower() != element.Name.Replace(" ", "").Replace("-", "").ToLower())
                    elements.Add(element);
            
            TableElement[,] table = new TableElement[2, elements.Count + 2];
            table[0, 0] = new TextElement("Workshop Name");
            table[1, 0] = new TextElement("Function Name");
            table[0, 1] = new SeperatorElement();
            table[1, 1] = new SeperatorElement();

            for (int i = 0; i < elements.Count; i++)
            {
                table[0, i + 2] = new TextElement(elements[i].Name);
                table[1, i + 2] = new TextElement(elements[i].CodeName());
            }

            Program.WorkshopCodeResult(TableToString(table));
        }

        private static string TableToString(TableElement[,] table)
        {
            StringBuilder builder = new StringBuilder();
            for (int r = 0; r < table.GetLength(1); r++)
            {
                for (int c = 0; c < table.GetLength(0); c++)
                {
                    builder.Append("|");
                    builder.Append(table[c,r].GetText(ColumnWidth(table, c)));
                }
                builder.Append("|");
                builder.AppendLine();
            }
            return builder.ToString();
        }

        private static int ColumnWidth(TableElement[,] table, int column)
        {
            int width = 0;
            for (int r = 0; r < table.GetLength(1); r++)
            {
                int elementWidth = table[column,r].Width();
                if (elementWidth > width) width = elementWidth;
            }
            return width;
        }

        abstract class TableElement
        {
            public abstract string GetText(int columnLength);
            public abstract int Width();
        }
        class TextElement : TableElement
        {
            private readonly string _text;
            
            public TextElement(string text)
            {
                _text = text;
            }

            public override string GetText(int columnLength) => _text + new string(' ', columnLength - _text.Length);
            public override int Width() => _text.Length;
        }
        class SeperatorElement : TableElement
        {
            public SeperatorElement() {}
            public override string GetText(int columnLength) => new string('-', columnLength);
            public override int Width() => 0;
        }
    }
}