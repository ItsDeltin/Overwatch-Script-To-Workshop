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
            string[] files = Directory.GetFiles(folder).Where(file => Path.GetExtension(file) == ".obj").ToArray();
            List<Letter> letters = new List<Letter>();
            foreach(string file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string[] split = name.Split('_');

                char character;
                if (split.Length == 2)
                {
                    character = split[1][0];
                    if (split[0] == "lowercase")
                        character = Char.ToLower(character);
                    else
                        character = Char.ToUpper(character);
                }
                else if (split.Length == 1)
                {
                    character = Uri.UnescapeDataString(split[0])[0];
                }
                else continue;

                string content = File.ReadAllText(file);
                Line[] lines = ObjModel.Import(content).GetLines();

                Letter letter = new Letter(character, lines.ToArray());

                double xoffset = -letter.Lines.Min(line => Math.Min(line.Vertex1.X, line.Vertex2.X));
                //double yoffset = -letter.Lines.Min(line => Math.Min(line.Vertex1.Y, line.Vertex2.Y));
                List<Vertex> offsetted = new List<Vertex>();
                foreach (Line line in letter.Lines)
                {
                    if (!offsetted.Contains(line.Vertex1))
                        line.Vertex1.Offset(xoffset, 0, 0);
                    if (!offsetted.Contains(line.Vertex2))
                        line.Vertex2.Offset(xoffset, 0, 0);
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

        public static Line[] Create(string text, bool exactLetter, Location location, double scale)
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

                offset += letter.Width + (.15 * scale);
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
            new Letter('!', new Line(new Vertex(0, 0.917514), new Vertex(0, 0.293021)), new Line(new Vertex(0, 0.144243), new Vertex(0, 0.06782))),
            new Letter(',', new Line(new Vertex(0.147216999999998, 0.074171), new Vertex(0, -0.091914))),
            new Letter('@', new Line(new Vertex(0.397579999999998, 0.431019), new Vertex(0.141663000000001, 0.16622)), new Line(new Vertex(0.141663000000001, 0.16622), new Vertex(0.504554999999996, 0.169297)), new Line(new Vertex(0.504554999999996, 0.169297), new Vertex(0.35698, 0.534764)), new Line(new Vertex(0.35698, 0.534764), new Vertex(0, 0.093171)), new Line(new Vertex(0, 0.093171), new Vertex(0.544541000000002, 0.095003))),
            new Letter('.', new Line(new Vertex(0, 0.144243), new Vertex(0, 0.06782))),
            new Letter('a', new Line(new Vertex(0.478656999999998, 0.359592), new Vertex(0.0995069999999956, 0.058373)), new Line(new Vertex(0.0995069999999956, 0.058373), new Vertex(0.573127999999997, 0)), new Line(new Vertex(0.573127999999997, 0), new Vertex(0.443562, 0.501822)), new Line(new Vertex(0.443562, 0.501822), new Vertex(0, 0.430568))),
            new Letter('b', new Line(new Vertex(0, 0.498007), new Vertex(0.403122000000003, 0)), new Line(new Vertex(0.403122000000003, 0), new Vertex(0, 0)), new Line(new Vertex(0, 0), new Vertex(0, 0.997687))),
            new Letter('c', new Line(new Vertex(0, 0.270344), new Vertex(0.387867, -0.000705)), new Line(new Vertex(0.387867, 0.541393), new Vertex(0, 0.270344))),
            new Letter('d', new Line(new Vertex(0.366948000000001, 0.459883), new Vertex(0, 0)), new Line(new Vertex(0, 0), new Vertex(0.366948000000001, 0)), new Line(new Vertex(0.366948000000001, 0), new Vertex(0.366948000000001, 1.00457))),
            new Letter('e', new Line(new Vertex(0.0618669999999995, 0.169271), new Vertex(0.359744999999997, 0.209127)), new Line(new Vertex(0.359744999999997, 0.209127), new Vertex(0.172450999999995, 0.487279)), new Line(new Vertex(0.172450999999995, 0.487279), new Vertex(0, 0)), new Line(new Vertex(0, 0), new Vertex(0.389111, 0.014858))),
            new Letter('f', new Line(new Vertex(0, 0), new Vertex(0.401859999999999, 1.115237)), new Line(new Vertex(0.207394000000001, 0.538964), new Vertex(0.506455000000003, 0.623749))),
            new Letter('g', new Line(new Vertex(0.398379999999996, 0.47447), new Vertex(0.477702999999998, -0.453388)), new Line(new Vertex(0, -0.010532), new Vertex(0.398379999999996, 0.47447)), new Line(new Vertex(0.445571999999999, 0), new Vertex(0, -0.010532)), new Line(new Vertex(0.477702999999998, -0.453388), new Vertex(0.0432399999999973, -0.368047))),
            new Letter('h', new Line(new Vertex(0, 0), new Vertex(0.163719, 1.02055)), new Line(new Vertex(0.274811, -0.002507), new Vertex(0.0730819999999994, 0.44607))),
            new Letter('i', new Line(new Vertex(0, 0), new Vertex(0, 0.362634)), new Line(new Vertex(0, 0.511412), new Vertex(0, 0.587835))),
            new Letter('j', new Line(new Vertex(0.120311999999998, -0.218948), new Vertex(0.220618999999999, 0.376254)), new Line(new Vertex(0, -0.022061), new Vertex(0.120311999999998, -0.218948))),
            new Letter('k', new Line(new Vertex(0, 0), new Vertex(0, 0.865895)), new Line(new Vertex(0.316513, 0), new Vertex(0, 0.323914)), new Line(new Vertex(0.243827000000003, 0.633292), new Vertex(0, 0.323914))),
            new Letter('l', new Line(new Vertex(0, 0), new Vertex(0, 0.644341)), new Line(new Vertex(0.248748999999997, 0), new Vertex(0, 0))),
            new Letter('m', new Line(new Vertex(0.0888330000000011, 0.423914), new Vertex(0.868492000000003, -0.011083)), new Line(new Vertex(0, -0.011083), new Vertex(0.0888330000000011, 0.423914)), new Line(new Vertex(0.0888330000000011, 0.423914), new Vertex(0.434246000000002, -0.011083))),
            new Letter('n', new Line(new Vertex(0, 0.54952), new Vertex(0.307243, -0.011083)), new Line(new Vertex(0, 0.54952), new Vertex(0, -0.011083))),
            new Letter('o', new Line(new Vertex(0, 0), new Vertex(0.191478999999994, 0.594751)), new Line(new Vertex(0.382953999999998, 0), new Vertex(0, 0)), new Line(new Vertex(0.191478999999994, 0.594751), new Vertex(0.382953999999998, 0))),
            new Letter('p', new Line(new Vertex(0, -0.321805), new Vertex(0, 0.515237)), new Line(new Vertex(0.00214399999999415, 0.014002), new Vertex(0.320929999999997, 0.284698)), new Line(new Vertex(0.320929999999997, 0.284698), new Vertex(0, 0.515237))),
            new Letter('q', new Line(new Vertex(0.334053000000004, -0.439924), new Vertex(0.334053000000004, 0.505142)), new Line(new Vertex(0.331909000000003, 0.003906), new Vertex(0, 0.274603)), new Line(new Vertex(0, 0.274603), new Vertex(0.334053000000004, 0.505142))),
            new Letter('r', new Line(new Vertex(0, 0.618118), new Vertex(0.411465, 0.468044)), new Line(new Vertex(0, 0.618118), new Vertex(0, 0.001634))),
            new Letter('s', new Line(new Vertex(0.272732000000005, 0.015142), new Vertex(0.272732000000005, 0.595708)), new Line(new Vertex(0, 0.186392), new Vertex(0.272732000000005, 0.015142)), new Line(new Vertex(0.272732000000005, 0.595708), new Vertex(0.583519000000003, 0.433972))),
            new Letter('t', new Line(new Vertex(0.180168999999999, 0.010125), new Vertex(0.180168999999999, 0.874723)), new Line(new Vertex(0, 0.651474), new Vertex(0.360332999999997, 0.651474))),
            new Letter('u', new Line(new Vertex(0.106293000000001, 0.010105), new Vertex(0, 0.481901)), new Line(new Vertex(0.364533000000002, 0.010105), new Vertex(0.106293000000001, 0.010105)), new Line(new Vertex(0.470825000000005, 0.481901), new Vertex(0.364533000000002, 0.010105))),
            new Letter('v', new Line(new Vertex(0.197899, 0.002371), new Vertex(0, 0.480187)), new Line(new Vertex(0.395801999999996, 0.480187), new Vertex(0.197899, 0.002371))),
            new Letter('w', new Line(new Vertex(0.303272, 0.016838), new Vertex(0, 0.445467)), new Line(new Vertex(0.716374000000002, 0.445467), new Vertex(0.303272, 0.016838)), new Line(new Vertex(0.303272, 0.016838), new Vertex(0.303272, 0.394109))),
            new Letter('x', new Line(new Vertex(0, 0.002894), new Vertex(0.298584000000005, 0.450125)), new Line(new Vertex(0.298584000000005, 0.002894), new Vertex(0, 0.450125))),
            new Letter('y', new Line(new Vertex(0.360533999999994, 0.432469), new Vertex(0.151080999999998, -0.291618)), new Line(new Vertex(0, 0.42269), new Vertex(0.251093999999995, 0.02))),
            new Letter('z', new Line(new Vertex(0, -0.000581), new Vertex(0.281334000000001, 0.43866)), new Line(new Vertex(0.281334000000001, -0.000581), new Vertex(0, -0.000581)), new Line(new Vertex(0.281334000000001, 0.43866), new Vertex(0, 0.43866))),
            new Letter('A', new Line(new Vertex(0, 0.078339), new Vertex(0.5, 1.1623)), new Line(new Vertex(0.5, 1.1623), new Vertex(1, 0.078339)), new Line(new Vertex(0.247541, 0.620319), new Vertex(0.752459, 0.620319))),
            new Letter('B', new Line(new Vertex(0, 0.530996), new Vertex(0.640885, 0.408596)), new Line(new Vertex(0.640885, 0.408596), new Vertex(0, 0.078339)), new Line(new Vertex(0, 0.078339), new Vertex(0, 1.1623)), new Line(new Vertex(0, 1.1623), new Vertex(0.434282, 0.453937))),
            new Letter('C', new Line(new Vertex(0, 0.620319), new Vertex(0.620168, 0.078339)), new Line(new Vertex(0.620168, 1.1623), new Vertex(0, 0.620319))),
            new Letter('D', new Line(new Vertex(0, 1.151217), new Vertex(0.529732, 0.609236)), new Line(new Vertex(0, 0.067256), new Vertex(0, 1.151217)), new Line(new Vertex(0.529732, 0.609236), new Vertex(0, 0.067256))),
            new Letter('E', new Line(new Vertex(0, 0.620319), new Vertex(1, 1.1623)), new Line(new Vertex(1, 0.078339), new Vertex(0, 0.620319)), new Line(new Vertex(0, 0.620319), new Vertex(0.832833000000001, 0.620319))),
            new Letter('F', new Line(new Vertex(0, 0.078339), new Vertex(0.807810999999999, 1.1623)), new Line(new Vertex(0.342711, 0.530288), new Vertex(0.910667999999999, 0.632655))),
            new Letter('G', new Line(new Vertex(0, 0.078339), new Vertex(0.807811000000001, 1.1623)), new Line(new Vertex(1.449066, 0.265917), new Vertex(0, 0.078339)), new Line(new Vertex(0.791757, 0.397167), new Vertex(1.449066, 0.265917))),
            new Letter('H', new Line(new Vertex(0, 0.067256), new Vertex(0.257947, 1.151217)), new Line(new Vertex(0.779656999999998, 0.06595), new Vertex(0.151489, 0.732705))),
            new Letter('I', new Line(new Vertex(0, 0.067256), new Vertex(0, 1.151217))),
            new Letter('J', new Line(new Vertex(0.373264000000001, 0.067256), new Vertex(0.846285, 1.151217)), new Line(new Vertex(0, 0.373176), new Vertex(0.373264000000001, 0.067256))),
            new Letter('K', new Line(new Vertex(0, 0.066915), new Vertex(0, 1.150876)), new Line(new Vertex(0.399379000000001, 0.066915), new Vertex(0, 0.608895)), new Line(new Vertex(0.399379000000001, 1.150876), new Vertex(0, 0.608895))),
            new Letter('L', new Line(new Vertex(0, 0.067256), new Vertex(0, 1.151217)), new Line(new Vertex(0.365053, 0.067256), new Vertex(0, 0.067256))),
            new Letter('M', new Line(new Vertex(0.745749, 1.151217), new Vertex(1.491498, 0.067256)), new Line(new Vertex(0, 0.067256), new Vertex(0.745749, 1.151217)), new Line(new Vertex(0.745749, 1.151217), new Vertex(0.745749, 0.067256))),
            new Letter('N', new Line(new Vertex(0, 1.151217), new Vertex(0.644116999999998, 0.067256)), new Line(new Vertex(0, 1.151217), new Vertex(0, 0.067256))),
            new Letter('O', new Line(new Vertex(0, 0.123174), new Vertex(0.393044, 1.094789)), new Line(new Vertex(0.786085, 0.123174), new Vertex(0, 0.123174)), new Line(new Vertex(0.393044, 1.094789), new Vertex(0.786085, 0.123174))),
            new Letter('P', new Line(new Vertex(0, 0.067256), new Vertex(0, 1.151217)), new Line(new Vertex(0.00214599999999976, 0.649981), new Vertex(0.446383000000001, 0.920677)), new Line(new Vertex(0.446383000000001, 0.920677), new Vertex(0, 1.151217))),
            new Letter('Q', new Line(new Vertex(0, 0.123174), new Vertex(0.521623999999999, 1.094789)), new Line(new Vertex(0.792034000000001, 0.123174), new Vertex(0, 0.123174)), new Line(new Vertex(0.521623999999999, 1.094789), new Vertex(0.792034000000001, 0.123174)), new Line(new Vertex(1.000979, 0.089268), new Vertex(0.633151999999999, 0.217443))),
            new Letter('R', new Line(new Vertex(0.00910400000000067, 0.067256), new Vertex(0.00910400000000067, 1.151217)), new Line(new Vertex(0.00910400000000067, 1.151217), new Vertex(0.538650000000001, 0.820624)), new Line(new Vertex(0.538650000000001, 0.820624), new Vertex(0, 0.544308)), new Line(new Vertex(0, 0.544308), new Vertex(0.447609, 0.09036))),
            new Letter('S', new Line(new Vertex(0.38616, 0.067256), new Vertex(0.38616, 1.151217)), new Line(new Vertex(0, 0.386993), new Vertex(0.38616, 0.067256)), new Line(new Vertex(0.38616, 1.151217), new Vertex(0.837868, 0.849243))),
            new Letter('T', new Line(new Vertex(0.548804000000001, 0.067256), new Vertex(0.548804000000001, 1.151217)), new Line(new Vertex(0, 1.126072), new Vertex(1.097607, 1.126072))),
            new Letter('U', new Line(new Vertex(0.204003, 0.067256), new Vertex(0, 1.151217)), new Line(new Vertex(0.699637000000003, 0.067256), new Vertex(0.204003, 0.067256)), new Line(new Vertex(0.903641000000004, 1.151217), new Vertex(0.699637000000003, 0.067256))),
            new Letter('V', new Line(new Vertex(0.697616000000004, 0.067256), new Vertex(0, 1.151217)), new Line(new Vertex(1.395233, 1.151217), new Vertex(0.697616000000004, 0.067256))),
            new Letter('W', new Line(new Vertex(0.668151999999999, 0.067256), new Vertex(0, 1.038669)), new Line(new Vertex(1.643257, 1.038669), new Vertex(0.668151999999999, 0.067256)), new Line(new Vertex(0.668151999999999, 0.067256), new Vertex(0.668156000000003, 0.874964))),
            new Letter('X', new Line(new Vertex(0, 0.067256), new Vertex(0.667632999999995, 1.151217)), new Line(new Vertex(0.667632999999995, 0.067256), new Vertex(0, 1.151217))),
            new Letter('Y', new Line(new Vertex(0.679511999999995, 1.135774), new Vertex(0.294109999999996, 0.081419)), new Line(new Vertex(0, 1.117343), new Vertex(0.452838999999997, 0.575998))),
            new Letter('Z', new Line(new Vertex(0, 0.067256), new Vertex(0.552672999999999, 1.151217)), new Line(new Vertex(0.552672999999999, 0.067256), new Vertex(0, 0.067256)), new Line(new Vertex(0.552672999999999, 1.151217), new Vertex(0, 1.151217))),
        };

        public char Character { get; }
        public Line[] Lines { get; }
        public double Width { get; }

        public Letter(char character, params Line[] lines)
        {
            Character = character;
            Lines = lines;
            Width = Lines.Max(line => Math.Max(line.Vertex1.X, line.Vertex2.X));
        }
        public Letter(char character, double width)
        {
            Character = character;
            Width = width;
        }
    }
}