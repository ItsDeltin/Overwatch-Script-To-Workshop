using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Deltin.Deltinteger.Assets
{
    class ObjModel
    {
        public List<Vertex> Vertices { get; } = new List<Vertex>();

        public List<Face> Faces { get; } = new List<Face>();

        public List<Line> Lines { get; } = new List<Line>();

        private static ObjImportParser[] LineParsers = new ObjImportParser[]
        {
            new VertexParser(),
            new FaceParser(),
            new LineParser()
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
            lines.AddRange(this.Lines);

            foreach (Face face in Faces)
                lines.AddRange(face.GetLines());

            Line.RemoveDuplicateLines(lines);
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
            foreach (Capture capture in match.Groups[2].Captures)
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

    class LineParser : ObjImportParser
    {
        protected override string Match { get; } = @"l ([0-9]+) ([0-9]+)";

        protected override void Add(Match match, ObjModel model)
        {
            int id1 = int.Parse(match.Groups[1].Value) - 1;
            int id2 = int.Parse(match.Groups[2].Value) - 1;
            model.Lines.Add(new Line(model.Vertices[id1], model.Vertices[id2]));
        }
    }
}