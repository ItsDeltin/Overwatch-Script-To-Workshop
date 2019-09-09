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

        public int Vertex1Reference { get; }
        public int Vertex2Reference { get; }

        public Line(Vertex vertex1, Vertex vertex2)
        {
            Vertex1 = vertex1;
            Vertex2 = vertex2;
        }

        public Line(Vertex vertex1, Vertex vertex2, int vertex1Reference, int vertex2Reference)
        {
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Vertex1Reference = vertex1Reference;
            Vertex2Reference = vertex2Reference;
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
        }
    }
}