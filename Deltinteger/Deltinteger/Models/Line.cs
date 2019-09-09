using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Models;

namespace Deltin.Deltinteger.Models
{
    public class Line : ICloneable
    {
        public Vertex Vertex1 { get; set; }
        public Vertex Vertex2 { get; set; }

        public Line(Vertex vertex1, Vertex vertex2)
        {
            Vertex1 = vertex1;
            Vertex2 = vertex2;
        }

        public void Offset(double x, double y, double z)
        {
            Vertex1.Offset(x, y, z);
            Vertex2.Offset(x, y, z);
        }

        public object Clone()
        {
            return new Line((Vertex)Vertex1.Clone(), (Vertex)Vertex2.Clone());
        }

        public static void RemoveDuplicateLines(List<Line> lines)
        {
            for (int a = lines.Count - 1; a >= 0; a--)
            for (int b = 0; b < lines.Count; b++)
            if (a != b && (
                (lines[a].Vertex1.EqualTo(lines[b].Vertex1) && lines[a].Vertex2.EqualTo(lines[b].Vertex2)) ||
                (lines[a].Vertex1.EqualTo(lines[b].Vertex2) && lines[a].Vertex2.EqualTo(lines[b].Vertex1))
            ))
            {
                lines.RemoveAt(a);
                break;
            }
            /*
            for (int a = lines.Count - 1; a >= 0; a--)
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
            */
        }

        public static void CombineCloseAngles(List<Line> lines, double maxAngle)
        {
            if (maxAngle == 0) return;

            while (true)
            {
                bool changed = false;
                for (int a = lines.Count - 1; a >= 0; a--)
                for (int b = 0; b < lines.Count; b++)
                if (a != b)
                {
                    Vertex angleA;
                    Vertex angleMid;
                    Vertex angleC;

                    if (lines[a].Vertex1.EqualTo(lines[b].Vertex1))
                    {
                        angleMid = lines[a].Vertex1;
                        angleA = lines[a].Vertex2;
                        angleC = lines[b].Vertex2;
                    }
                    else if (lines[a].Vertex1.EqualTo(lines[b].Vertex2))
                    {
                        angleMid = lines[a].Vertex1;
                        angleA = lines[a].Vertex2;
                        angleC = lines[b].Vertex1;
                    }
                    else if (lines[a].Vertex2.EqualTo(lines[b].Vertex1))
                    {
                        angleMid = lines[a].Vertex2;
                        angleA = lines[b].Vertex2;
                        angleC = lines[a].Vertex1;
                    }
                    else if (lines[a].Vertex2.EqualTo(lines[b].Vertex2))
                    {
                        angleMid = lines[a].Vertex2;
                        angleA = lines[b].Vertex1;
                        angleC = lines[a].Vertex1;
                    }
                    else continue;

                    double angle = Vertex.GetAngle(angleA, angleMid, angleC);
                    if (angle <= maxAngle)
                    {
                        lines[b] = new Line(angleA, angleC);
                        lines.RemoveAt(a);
                        changed = true;
                        break;
                    }
                }
                if (!changed) break;
            }

            /*
            bool loop = true;
            while (loop)
            {
                bool changed = false;
                for (int a = lines.Count - 1; a >= 0; a--)
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

                        if (a != b)
                        {
                            Vertex angleA;
                            Vertex angleMid;
                            Vertex angleC;
                            if (pointsA[0] == pointsB[0])
                            {
                                angleMid = lines[a].Vertex1;
                                angleA = lines[a].Vertex2;
                                angleC = lines[b].Vertex2;
                            }
                            else if (pointsA[0] == pointsB[1])
                            {
                                angleMid = lines[a].Vertex1;
                                angleA = lines[a].Vertex2;
                                angleC = lines[b].Vertex1;
                            }
                            else if (pointsA[1] == pointsB[0])
                            {
                                angleMid = lines[a].Vertex2;
                                angleA = lines[a].Vertex1;
                                angleC = lines[b].Vertex2;
                            }
                            else if (pointsA[1] == pointsB[1])
                            {
                                angleMid = lines[a].Vertex2;
                                angleA = lines[a].Vertex1;
                                angleC = lines[b].Vertex1;
                            }
                            else continue;

                            double angle = Vertex.GetAngle(angleA, angleMid, angleC);
                            if (angle <= maxAngle)
                            {
                                lines[b] = new Line(angleA, angleC);
                                lines.RemoveAt(a);
                                changed = true;
                                break;
                            }
                        }
                    }
                }
                loop = changed;
            }
            */
        }
    }
}