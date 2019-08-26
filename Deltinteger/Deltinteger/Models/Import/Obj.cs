using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Models.Import
{
    class ObjModel : IModelLoader
    {
        public List<Vertex> Vertices { get; } = new List<Vertex>();

        public List<Face> Faces { get; } = new List<Face>();

        private static ObjImportParser[] LineParsers = new ObjImportParser[] 
        {
            new VertexParser(),
            new FaceParser()
        };

        public static ObjModel Import(string obj)
        {
            string[] lines = obj.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            ObjModel model = new ObjModel();

            foreach (string line in lines)
                foreach (ObjImportParser parser in LineParsers)
                    if (parser.ParseLine(line, model))
                        break;
            
            return model;
        }

        public Line[] GetLines()
        {
            List<Line> lines = new List<Line>();
            foreach (Face face in Faces)
                lines.AddRange(face.GetLines());

            for (int a = 0; a < lines.Count; a++)
            {
                var pointsA = new (double X, double Y, double Z)[] {
                    (lines[a].Vertex1.X, lines[a].Vertex1.Y, lines[a].Vertex1.Z),
                    (lines[a].Vertex2.X, lines[a].Vertex2.Y, lines[a].Vertex2.Z)
                };

                for (int b = 0; b < lines.Count; b++)
                {
                    var pointsB = new (double X, double Y, double Z)[] {
                        (lines[b].Vertex1.X, lines[b].Vertex1.Y, lines[b].Vertex1.Z),
                        (lines[b].Vertex2.X, lines[b].Vertex2.Y, lines[b].Vertex2.Z)
                    };

                    if ((a != b) && (
                        (pointsA[0] == pointsB[0] && pointsA[0] == pointsB[1]) ||
                        (pointsA[0] == pointsB[1] && pointsA[1] == pointsB[0])
                    ))
                    {
                        lines.RemoveAt(a);
                        a--;
                        break;
                    }
                }
            }

            return lines.ToArray();
        }
    }

    abstract class ObjImportParser
    {
        protected const string Reg_Double = @"-?[0-9]+(\.[0-9]+)?";

        public bool ParseLine(string text, ObjModel model)
        {
            Match match = Regex.Match(text, Match);
            if (!match.Success) return false;

            Add(match, model);
            return true;
        }

        protected abstract string Match { get; }
        protected abstract void Add(Match match, ObjModel model);
    }

    class VertexParser : ObjImportParser
    {
        protected override string Match { get; } = $@"v( {Reg_Double}){{3,4}}";

        protected override void Add(Match match, ObjModel model)
        {
            double x = double.Parse(match.Groups[1].Captures[0].Value);
            double y = double.Parse(match.Groups[1].Captures[1].Value);
            double z = double.Parse(match.Groups[1].Captures[2].Value);
            double w = 0;
            if (match.Groups.Count == 8)
                w = double.Parse(match.Groups[1].Captures[3].Value);
            model.Vertices.Add(new Vertex(x, y, z, w));
        }
    }

    class FaceParser : ObjImportParser
    {
        protected override string Match { get; } = @"f( ([0-9]+)(\/[0-9]*){0,2}){3,}";

        protected override void Add(Match match, ObjModel model)
        {
            List<Vertex> vertices = new List<Vertex>();
            foreach(Capture capture in match.Groups[2].Captures)
            {
                int index = int.Parse(capture.Value);
                if (index > 0)
                    index = index - 1;
                else if (index < 0)
                    index = model.Vertices.Count - index;
                vertices.Add(model.Vertices[index]);
            }
            model.Faces.Add(new Face(vertices.ToArray()));
        }
    }
}