using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Deltin.Deltinteger;
using Deltin.Deltinteger.Models.Import;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Models
{
    public class Model
    {
        const bool VERTICAL_LINE_FIX = true;

        public Line[] Lines { get; }

        private Model(Line[] lines)
        {
            if (VERTICAL_LINE_FIX)
                foreach (Line line in lines)
                    if (line.Vertex1.X == line.Vertex2.X &&
                        line.Vertex1.Z == line.Vertex2.Z)
                    {
                        line.Vertex2.X += 0.0001;
                    }

            Lines = lines;
        }

        public static Model ImportObj(string obj)
        {
            ObjModel result = ObjModel.Import(obj);
            return new Model(result.GetLines());
        }

        public static Model ImportString(string text, FontFamily family, double quality, double angle)
        {
            List<Line> lines = new List<Line>();

            using (var gp = new GraphicsPath())
            using (var flipY = new Matrix(1, 0, 0, -1, 0, gp.GetBounds().Height))
            {
                const double scale = 0.05;

                gp.AddString(text, family, 0, 40f, new Point(0, 0), StringFormat.GenericDefault);
                gp.Flatten(flipY, (float)quality);

                int lastStart = -1;
                for (int i = 0; i < gp.PointCount; i++)
                {
                    if (gp.PathTypes[i] == (byte)PathPointType.Start)
                        lastStart = i;
                    
                    int vec1 = -1, vec2 = -1;

                    if (gp.PathTypes[i] == (byte)PathPointType.Start
                        || gp.PathTypes[i] == (byte)PathPointType.LinePart)
                    {
                        vec1 = i;
                        vec2 = i + 1;
                    }

                    else if (gp.PathTypes[i] == (byte)PathPointType.LastPointInClosedSubpath)
                    {
                        vec1 = i;
                        vec2 = lastStart;
                        lastStart = -1;
                    }

                    if (vec1 != -1 && vec2 != -1)
                    {
                        PointF first = gp.PathPoints[vec1];
                        PointF next = gp.PathPoints[vec2];

                        double xOffset = gp.GetBounds().Width / 2;
                        double yOffset = gp.GetBounds().Height;

                        lines.Add(
                            new Line(
                                new Vertex(first.X - xOffset, first.Y + yOffset, 0).Rotate(angle, 0, 0).Scale(scale),
                                new Vertex(next.X - xOffset, next.Y + yOffset, 0).Rotate(angle, 0, 0).Scale(scale)
                            )
                        );
                    }
                }
            }

            return new Model(lines.ToArray());
        }

        enum PathPointType : byte
        {
            /// Indicates that the point is the start of a figure.
            Start = 0,
            /// Indicates that the point is one of the two endpoints of a line.
            LinePart = 1,
            /// Indicates that the point is an endpoint or control point of a cubic Bézier spline.
            CubicBezierEndpoint = 2,
            /// Masks all bits except for the three low-order bits, which indicate the point type.
            Mask = 0x7,
            /// Specifies that the point is a marker.
            Marker = 0x20,
            /// Specifies that the point is the last point in a closed subpath (figure).
            LastPointInClosedSubpath = 129
        }
    }

    public class Line
    {
        public Vertex Vertex1 { get; }
        public Vertex Vertex2 { get; }

        public Line(Vertex vertex1, Vertex vertex2)
        {
            Vertex1 = vertex1;
            Vertex2 = vertex2;
        }
    }

    public class Vertex
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }

        public Vertex(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        public Vertex(double x, double y, double z) : this(x,y,z,0) {}
        public Vertex() : this(0,0,0,0) {}

        public V_Vector ToVector()
        {
            return Element.Part<V_Vector>(new V_Number(X), new V_Number(Y), new V_Number(Z));
        }

        public Vertex Rotate(double pitch, double roll, double yaw)
        {
            var cosa = Math.Cos(yaw);
            var sina = Math.Sin(yaw);

            var cosb = Math.Cos(pitch);
            var sinb = Math.Sin(pitch);

            var cosc = Math.Cos(roll);
            var sinc = Math.Sin(roll);

            var Axx = cosa*cosb;
            var Axy = cosa*sinb*sinc - sina*cosc;
            var Axz = cosa*sinb*cosc + sina*sinc;

            var Ayx = sina*cosb;
            var Ayy = sina*sinb*sinc + cosa*cosc;
            var Ayz = sina*sinb*cosc - cosa*sinc;

            var Azx = -sinb;
            var Azy = cosb*sinc;
            var Azz = cosb*cosc;

            var newX = Axx*X + Axy*Y + Axz*Z;
            var newY = Ayx*X + Ayy*Y + Ayz*Z;
            var newZ = Azx*X + Azy*Y + Azz*Z;

            return new Vertex(newX, newY, newZ);
        }

        public Vertex Scale(double scale)
        {
            return new Vertex(X * scale, Y * scale, Z * scale);
        }
    }

    class Face
    {
        public Vertex[] Vertices { get; }

        public Face(Vertex[] vertices)
        {
            Vertices = vertices;
        }

        public Line[] GetLines()
        {
            Line[] lines = new Line[Vertices.Length];
            for (int i = 0; i < Vertices.Length; i++)
            {
                int connectedIndex = 0;
                if (i != Vertices.Length - 1)
                    connectedIndex = i + 1;
                lines[i] = new Line(Vertices[i], Vertices[connectedIndex]);
            }
            return lines;
        }
    }

    interface IModelLoader
    {
        Line[] GetLines();
    }
}