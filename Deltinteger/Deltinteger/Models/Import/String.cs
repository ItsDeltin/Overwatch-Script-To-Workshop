using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Deltin.Deltinteger.Models.Import
{
    class StringModel : IModelLoader
    {
        private List<Line> lines { get; } = new List<Line>();

        public StringModel(string text, FontFamily family, double quality, Vertex angle, double scale, double angleRound)
        {
            scale *= 0.05;
            if (angle == null) angle = new Vertex();

            using (var gp = new GraphicsPath())
            using (var flipY = new Matrix(1, 0, 0, -1, 0, gp.GetBounds().Height))
            {
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

                    else if (gp.PathTypes[i] == (byte)PathPointType.LastPointInClosedSubpath || gp.PathTypes[i] == 161)
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
                                new Vertex(first.X - xOffset, first.Y + yOffset, 0).Rotate(angle).Scale(scale),
                                new Vertex(next.X - xOffset, next.Y + yOffset, 0).Rotate(angle).Scale(scale)
                            )
                        );
                    }
                }
            }

            Line.RemoveDuplicateLines(lines);
            Line.CombineCloseAngles(lines, angleRound);
            Line.RemoveDuplicateLines(lines);
        }

        public Line[] GetLines()
        {
            return lines.ToArray();
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
}