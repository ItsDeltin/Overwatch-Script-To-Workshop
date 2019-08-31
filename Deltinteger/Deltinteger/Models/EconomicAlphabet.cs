using System;
using System.Drawing;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Deltin.Deltinteger.Models.Import;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Models
{
    public class Letter
    {
        public static void Generate(string folder)
        {
            string[] files = Directory.GetFiles(folder).Where(file => Path.GetExtension(file) == ".obj").Where(file => Path.GetFileNameWithoutExtension(file).Length == 1).ToArray();
            List<Letter> letters = new List<Letter>();
            foreach(string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string content = File.ReadAllText(file);
                Line[] lines = ObjModel.Import(content).GetLines();

                Letter letter = new Letter(name[0], lines.ToArray());

                double xoffset = -letter.Lines.Min(line => Math.Min(line.Vertex1.X, line.Vertex2.X));
                double yoffset = -letter.Lines.Min(line => Math.Min(line.Vertex1.Y, line.Vertex2.Y));
                List<Vertex> offsetted = new List<Vertex>();
                foreach (Line line in letter.Lines)
                {
                    if (!offsetted.Contains(line.Vertex1))
                        line.Vertex1.Offset(xoffset, yoffset, 0);
                    if (!offsetted.Contains(line.Vertex2))
                        line.Vertex2.Offset(xoffset, yoffset, 0);
                    offsetted.Add(line.Vertex1);
                    offsetted.Add(line.Vertex2);
                }

                letters.Add(letter);
            }

            foreach(Letter letter in letters)
                Console.WriteLine( 
                    "new Letter('" + letter.Character + "', " + 
                    string.Join(", ", 
                        letter.Lines.Select(line => "new Line(new Vertex(" + line.Vertex1.X + ", " + line.Vertex1.Y + "), new Vertex(" + line.Vertex2.X + ", " + line.Vertex2.Y + "))")
                    )
                    + "),"
                );
            Console.ReadLine();
        }

        public static Line[] Create(string text, bool exactLetter, Location location)
        {
            double offset = 0;
            List<Line> result = new List<Line>();

            foreach (char character in text)
            {
                Letter letter = GetLetter(character, exactLetter, location);

                if (letter.Lines != null)
                    foreach (var line in letter.Lines)
                    {
                        Line newLine = (Line)line.Clone();
                        newLine.Offset(offset, 0, 0);
                        result.Add(newLine);
                    }

                offset += letter.Width;
            }

            if (result.Count > 0)
            {
                double xOffset = -(result.Max(line => Math.Max(line.Vertex1.X, line.Vertex2.X)) / 2);

                foreach (Line line in result)
                    line.Offset(xOffset, 0, 0);
            }
            
            return result.ToArray();
        }

        private static Letter GetLetter(char character, bool exactLetter, Location location)
        {
            foreach (Letter letter in Alphabet)
                if (character == letter.Character)
                    return letter;
                
            if (!exactLetter)
                foreach (Letter letter in Alphabet)
                    if (Char.ToLower(character) == char.ToLower(letter.Character))
                        return letter;
            
            throw new SyntaxErrorException(character + " is not a valid character.", location);
        }

        private static Letter[] Alphabet { get; } = new Letter[]
        {
            new Letter(' ', 1),
            new Letter('A', new Line(new Vertex(0, 0), new Vertex(0.5, 2)), new Line(new Vertex(0.5, 2), new Vertex(1, 0)), new Line(new Vertex(0.247541, 1), new Vertex(0.752459, 1))),
            new Letter('b', new Line(new Vertex(0, 1), new Vertex(0.999999, 0)), new Line(new Vertex(0.999999, 0), new Vertex(0, 0)), new Line(new Vertex(0, 0), new Vertex(0, 2))),
            new Letter('C', new Line(new Vertex(0, 0.5), new Vertex(1, 0)), new Line(new Vertex(1, 1), new Vertex(0, 0.5))),
            new Letter('d', new Line(new Vertex(0, 2), new Vertex(1, 1)), new Line(new Vertex(0, 0), new Vertex(0, 2)), new Line(new Vertex(1, 1), new Vertex(0, 0))),
            new Letter('E', new Line(new Vertex(0, 1), new Vertex(1, 2)), new Line(new Vertex(1, 0), new Vertex(0, 1)), new Line(new Vertex(0, 1), new Vertex(0.832833000000001, 1))),
            new Letter('F', new Line(new Vertex(0, 0), new Vertex(0.807810999999999, 2)), new Line(new Vertex(0.342711, 0.833885), new Vertex(0.910667999999999, 1.02276))),
            new Letter('G', new Line(new Vertex(0, 0), new Vertex(0.807811000000001, 2)), new Line(new Vertex(1.449066, 0.346098), new Vertex(0, 0)), new Line(new Vertex(0.791757, 0.588264), new Vertex(1.449066, 0.346098))),
            new Letter('h', new Line(new Vertex(0, 0.00241), new Vertex(0.257947, 2.00241)), new Line(new Vertex(0.779656999999998, 0), new Vertex(0.108321, 0.798539))),
            new Letter('I', new Line(new Vertex(0, 0), new Vertex(0, 2))),
            new Letter('J', new Line(new Vertex(0.373264000000001, 0), new Vertex(1.128898, 2)), new Line(new Vertex(0, 0.564448), new Vertex(0.373264000000001, 0))),
            new Letter('K', new Line(new Vertex(0, 0), new Vertex(0, 2)), new Line(new Vertex(0.6, 0), new Vertex(0, 1)), new Line(new Vertex(0.6, 2), new Vertex(0, 1))),
            new Letter('L', new Line(new Vertex(0, 0), new Vertex(0, 2)), new Line(new Vertex(0.700001, 0), new Vertex(0, 0))),
            new Letter('M', new Line(new Vertex(1, 2), new Vertex(2, 0)), new Line(new Vertex(0, 0), new Vertex(1, 2)), new Line(new Vertex(1, 2), new Vertex(1, 0))),
            new Letter('N', new Line(new Vertex(0, 2), new Vertex(1, 0)), new Line(new Vertex(0, 2), new Vertex(0, 0))),
            new Letter('O', new Line(new Vertex(0, 0), new Vertex(0.521623999999999, 0.896356)), new Line(new Vertex(1.043245, 0), new Vertex(0, 0)), new Line(new Vertex(0.521623999999999, 0.896356), new Vertex(1.043245, 0))),
            new Letter('P', new Line(new Vertex(0, 0), new Vertex(0, 2)), new Line(new Vertex(0.00214599999999976, 1.075178), new Vertex(0.697593999999999, 1.574635)), new Line(new Vertex(0.697593999999999, 1.574635), new Vertex(0, 2))),
            new Letter('Q', new Line(new Vertex(0, 0.085734), new Vertex(0.521623999999999, 1.878446)), new Line(new Vertex(1.043246, 0.085734), new Vertex(0, 0.085734)), new Line(new Vertex(0.521623999999999, 1.878446), new Vertex(1.043246, 0.085734)), new Line(new Vertex(1.415478, 0), new Vertex(0.620592000000002, 0.375546))),
            new Letter('R', new Line(new Vertex(0, 1), new Vertex(0.663150000000002, 0.910405)), new Line(new Vertex(0, 1), new Vertex(0, 0))),
            new Letter('S', new Line(new Vertex(0.469768000000002, 0), new Vertex(0.469768000000002, 1)), new Line(new Vertex(0, 0.294971), new Vertex(0.469768000000002, 0)), new Line(new Vertex(0.469768000000002, 1), new Vertex(1.005086, 0.721416))),
            new Letter('T', new Line(new Vertex(0.274403, 0), new Vertex(0.274403, 1.316822)), new Line(new Vertex(0, 0.976804), new Vertex(0.548802999999999, 0.976804))),
            new Letter('U', new Line(new Vertex(0.314983000000002, 0), new Vertex(0, 1.161329)), new Line(new Vertex(1.08025, 0), new Vertex(0.314983000000002, 0)), new Line(new Vertex(1.395233, 1.161329), new Vertex(1.08025, 0))),
            new Letter('V', new Line(new Vertex(0.542964000000005, 0), new Vertex(0, 1.161329)), new Line(new Vertex(1.085929, 1.161329), new Vertex(0.542964000000005, 0))),
            new Letter('W', new Line(new Vertex(1.171715, 0), new Vertex(0, 1.136849)), new Line(new Vertex(2.650383, 1.136849), new Vertex(1.171715, 0)), new Line(new Vertex(1.171715, 0), new Vertex(1.171719, 0.834798))),
            new Letter('X', new Line(new Vertex(0, 0), new Vertex(0.667632999999995, 1)), new Line(new Vertex(0.667632999999995, 0), new Vertex(0, 1))),
            new Letter('Y', new Line(new Vertex(0.679511999999995, 1.945375), new Vertex(0.294109999999996, 0)), new Line(new Vertex(0, 1.911369), new Vertex(0.452838999999997, 0.912541))),
            new Letter('Z', new Line(new Vertex(0, 0), new Vertex(0.5, 1)), new Line(new Vertex(0.5, 0), new Vertex(0, 0)), new Line(new Vertex(0.5, 1), new Vertex(0, 1))),
        };

        public char Character { get; }
        public Line[] Lines { get; }
        public double Width { get; }

        public Letter(char character, params Line[] lines)
        {
            Character = character;
            Lines = lines;
            Width = Lines.Max(line => Math.Max(line.Vertex1.X, line.Vertex2.X)) + .25;
        }
        public Letter(char character, double width)
        {
            Character = character;
            Width = width;
        }
    }
}