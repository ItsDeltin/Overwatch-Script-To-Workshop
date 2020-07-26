using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Csv;
using TextCopy;

namespace Deltin.Deltinteger.Debugger
{
    class DebuggerServer
    {
        public bool IsListening { get; private set; }
        private readonly Func<DeltinScript> _deltinScriptResolver;

        public DebuggerServer(Func<DeltinScript> deltinScriptResolver)
        {
            _deltinScriptResolver = deltinScriptResolver;
        }

        public async Task Listen()
        {
            string last = null;
            while (IsListening)
            {
                await Task.Delay(500);

                string clipboard = Clipboard.GetText(); // Get clipboard.
                if (clipboard == last || clipboard == null) continue; // Clipboard did not change.
                last = clipboard;

                // Clipboard changed.

                // As workshop actions
                DebuggerActionStream actionStream = new DebuggerActionStream(clipboard);
                if (actionStream.Run()) // Determines if the clipboard is an action list.
                {
                    // Action list successfully parsed.
                    // Get the DeltinScript.
                    DeltinScript deltinScript = _deltinScriptResolver.Invoke();
                }
                else
                {
                    // As CSV
                    try
                    {
                        CsvFrame csv = CsvFrame.ParseOne(clipboard);
                        // TODO
                    }
                    catch (CsvParseFailedException) {}
                    catch (Exception) {}
                }
            }
        }
    }

    class DebuggerActionStream
    {
        public DebuggerActionStreamSet Set { get; private set; }
        public List<DebuggerVariable> Variables { get; } = new List<DebuggerVariable>();
        private readonly string _text;
        private readonly DebuggerActionStreamKeywords _keywords;
        private int _position;
        private bool ReachedEnd => _position < _text.Length;

        public DebuggerActionStream(string text, DebuggerActionStreamKeywords keywords = null)
        {
            _text = text;
            _keywords = keywords ?? new DebuggerActionStreamKeywords();
        }

        public bool Run()
        {
            bool visitVariables = VisitVariables();
            bool visitActions = VisitActions();
            return visitVariables || visitActions;
        }

        public DebuggerActionResult GetResult() => new DebuggerActionResult(Variables.ToArray());

        private char Current() => _text[_position];
        private bool IsAny(params char[] characters) => !ReachedEnd && characters.Contains(Current());
        private bool IsAny(string characters) => IsAny(characters.ToCharArray());
        private bool IsNumeric() => IsAny("0123456789");
        private bool IsAlpha() => IsAny("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        private bool IsAlphaNumeric() => IsNumeric() || IsAlpha();
        private bool Is(int pos, char character) => _position + pos < _text.Length && _text[_position + pos] == character;
        private bool IsWhitespace() => IsAny(' ', '\t', '\r', '\n');
        private void Accept(int length = 0) => _position = Math.Min(_text.Length, _position + length);
        private void SkipWhitespace()
        {
            while (IsWhitespace()) Accept();
        }

        private bool Visit(string text)
        {
            for (int i = 0; i < text.Length; i++)
                if (!Is(i, text[i]))
                    return false;
                
            Accept(text.Length);
            SkipWhitespace();
            return true;
        }

        private bool VisitNumber(out int number)
        {
            string str = "";

            while (IsNumeric())
            {
                str += Current();
                Accept();
            }

            if (str == "")
            {
                number = 0;
                return false;
            }
            number = int.Parse(str);
            return true;
        }

        private bool VisitDouble(out double number)
        {
            string str = "";

            if (Visit("-")) str += "-";

            while (IsNumeric())
            {
                str += Current();
                Accept();
            }

            if (Visit("."))
            {
                str += ".";
                while (IsNumeric())
                {
                    str += Current();
                    Accept();
                }
            }

            if (str == "")
            {
                number = 0;
                return false;
            }
            number = double.Parse(str);
            return true;
        }

        private bool VisitString(out string value)
        {
            if (!Visit("\""))
            {
                value = null;
                return false;
            }

            value = "";
            bool escaped = false;
            do
            {
                value += Current();
                if (escaped) escaped = false;
                else if (Is(0, '"')) escaped = true;
                Accept();
            }
            while (escaped || !Is(0, '"'));
            Accept();

            return true;
        }

        private bool VisitIdentifier(out string identifier)
        {
            if (!IsAlpha())
            {
                identifier = null;
                return false;
            }

            identifier = "";
            while (IsAlphaNumeric())
            {
                identifier += Current();
                Accept();
            }
            return true;
        }

        private bool VisitVariables()
        {
            if (!Visit(_keywords.Variables)) return false;
            Visit("{");

            // Global variable list
            if (Visit(_keywords.Global))
            {
                Set = DebuggerActionStreamSet.Global;
                VisitVariableList();
            }
            // Player variable list
            else if (Visit(_keywords.Player))
            {
                Set = DebuggerActionStreamSet.Player;
                VisitVariableList();
            }

            Visit("}");
            return true;
        }

        private void VisitVariableList()
        {
            while (VisitNumber(out int index))
            {
                Visit(":");
                VisitIdentifier(out string name);
                Variables.Add(new DebuggerVariable(index, name ?? "?"));
            }
        }

        private bool VisitActions()
        {
            if (!Visit(_keywords.Actions)) return false;
            Visit("{");

            while (Visit(_keywords.GlobalIdentifier) || Visit(_keywords.PlayerIdentifier))
            {
                // Global.Name
                Visit(".");

                // Get the name
                VisitIdentifier(out string name);

                // Get the value
                SetVariable(name ?? "?", VisitExpression());

                // Statement end
                Visit(";");
            }

            Visit("}");
            return true;
        }

        private void SetVariable(string name, CsvPart value)
        {
            DebuggerVariable set = Variables.FirstOrDefault(v => v.Name == name);
            if (set == null)
            {
                set = new DebuggerVariable(-1, name);
                Variables.Add(set);
            }
            set.Value = value;
        }

        CsvPart VisitExpression()
        {
            if (Is(0, ',') || Visit(_keywords.Null))
            {
                return new CsvNull();
            }
            // Arrays
            else if (Visit(_keywords.Array))
            {
                List<CsvPart> elements = new List<CsvPart>();

                Visit("(");
                do
                {
                    elements.Add(VisitExpression());
                }
                while (Visit(","));
                Visit(")");

                return new CsvArray(elements.ToArray());
            }
            // Number
            else if (VisitDouble(out double value))
            {
                return new CsvNumber(value);
            }
            // Vector
            else if (Visit(_keywords.Vector))
            {
                Visit("(");
                VisitDouble(out double x);
                Visit(",");
                VisitDouble(out double y);
                Visit(",");
                VisitDouble(out double z);
                Visit(")");

                return new CsvVector(new Models.Vertex(x, y, z));
            }
            // Strings
            else if (VisitString(out string str))
            {
                return new CsvString(str);
            }
            // todo: all the types
            else throw new NotImplementedException();
        }
    }

    class DebuggerActionStreamKeywords
    {
        public string Variables = "variables";
        public string Global = "global";
        public string Player = "player";
        public string Actions = "actions";
        public string GlobalIdentifier = "Global";
        public string PlayerIdentifier = "Event Player";
        public string Array = "Array";
        public string Null = "Null";
        public string Vector = "Vector";
    }

    class DebuggerActionResult
    {
        public DebuggerVariable[] Variables { get; }

        public DebuggerActionResult(DebuggerVariable[] variables)
        {
            Variables = variables;
        }
    }

    class DebuggerVariable
    {
        public int Index { get; }
        public string Name { get; }
        public CsvPart Value { get; set; }

        public DebuggerVariable(int index, string name)
        {
            Index = index;
            Name = name;
        }
    }

    enum DebuggerActionStreamSet
    {
        Unknown,
        Global,
        Player
    }
}