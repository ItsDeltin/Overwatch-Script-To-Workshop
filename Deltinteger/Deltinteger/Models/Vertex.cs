using System;
using System.Xml.Serialization;
using Deltin.Deltinteger.Elements;
using Newtonsoft.Json;

namespace Deltin.Deltinteger.Models
{
    public class Vertex : ICloneable
    {
        [XmlAttribute]
        public double X { get; set; }
        [XmlAttribute]
        public double Y { get; set; }
        [XmlAttribute]
        public double Z { get; set; }
        [XmlIgnore]
        [JsonIgnore]
        public double W { get; set; }

        public Vertex(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        public Vertex(double x, double y, double z) : this(x, y, z, 0) { }
        public Vertex(double x, double y) : this(x, y, 0, 0) { }
        public Vertex() : this(0, 0, 0, 0) { }

        public Element ToVector()
        {
            return Element.Vector(X, Y, Z);
        }

        public Vertex Rotate(double pitch, double roll, double yaw)
        {
            var cosa = Math.Cos(yaw);
            var sina = Math.Sin(yaw);

            var cosb = Math.Cos(pitch);
            var sinb = Math.Sin(pitch);

            var cosc = Math.Cos(roll);
            var sinc = Math.Sin(roll);

            var Axx = cosa * cosb;
            var Axy = cosa * sinb * sinc - sina * cosc;
            var Axz = cosa * sinb * cosc + sina * sinc;

            var Ayx = sina * cosb;
            var Ayy = sina * sinb * sinc + cosa * cosc;
            var Ayz = sina * sinb * cosc - cosa * sinc;

            var Azx = -sinb;
            var Azy = cosb * sinc;
            var Azz = cosb * cosc;

            var newX = Axx * X + Axy * Y + Axz * Z;
            var newY = Ayx * X + Ayy * Y + Ayz * Z;
            var newZ = Azx * X + Azy * Y + Azz * Z;

            return new Vertex(newX, newY, newZ);
        }

        public Vertex Rotate(Vertex xyz)
        {
            return Rotate(xyz.Y, xyz.X, xyz.Z);
        }

        public Vertex Scale(double scale)
        {
            return new Vertex(X * scale, Y * scale, Z * scale);
        }

        public void Offset(double x, double y, double z)
        {
            X += x;
            Y += y;
            Z += z;
        }

        public object Clone()
        {
            return new Vertex(X, Y, Z, W);
        }

        public static double GetAngle(Vertex a, Vertex b, Vertex c)
        {
            if (a == null) throw new ArgumentNullException("a");
            if (b == null) throw new ArgumentNullException("b");
            if (c == null) throw new ArgumentNullException("c");

            double[] ab = { b.X - a.X, b.Y - a.Y, b.Z - a.Z };
            double[] bc = { c.X - b.X, c.Y - b.Y, c.Z - b.Z };

            double abVec = Math.Sqrt(ab[0] * ab[0] + ab[1] * ab[1] + ab[2] * ab[2]);
            double bcVec = Math.Sqrt(bc[0] * bc[0] + bc[1] * bc[1] + bc[2] * bc[2]);

            double[] abNorm = { ab[0] / abVec, ab[1] / abVec, ab[2] / abVec };
            double[] bcNorm = { bc[0] / bcVec, bc[1] / bcVec, bc[2] / bcVec };

            double res = abNorm[0] * bcNorm[0] + abNorm[1] * bcNorm[1] + abNorm[2] * bcNorm[2];

            return Math.Acos(res) * (180.0 / Math.PI);
        }

        public bool EqualTo(Vertex other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public Vertex Normalize()
        {
            double distance = Length;
            return new Vertex(X / distance, Y / distance, Z / distance);
        }

        public double DistanceTo(Vertex point2)
        {
            Vertex offset = VectorTowards(point2);
            return Math.Sqrt(Math.Pow(offset.X, 2) + Math.Pow(offset.Y, 2) + Math.Pow(offset.Z, 2));
        }

        public Vertex DirectionTowards(Vertex point2) =>
            VectorTowards(point2).Normalize();

        public Vertex CrossProduct(Vertex vert2)
        {
            double x = Y * vert2.Z - vert2.Y * Z;
            double y = -(X * vert2.Z - vert2.X * Z);
            double z = X * vert2.Y - vert2.X * Y;
            return new Vertex(x, y, z);
        }

        public Vertex RemoveNaNs() => new Vertex(
            double.IsNaN(X) ? 0 : X,
            double.IsNaN(Y) ? 0 : Y,
            double.IsNaN(Z) ? 0 : Z);

        [JsonIgnore]
        public double Length =>
            DistanceTo(new Vertex());

        public double DotProduct(Vertex vert2) =>
            X * vert2.X + Y * vert2.Y + Z * vert2.Z;

        public Vertex VectorTowards(Vertex vert2) =>
            new Vertex(vert2.X - X, vert2.Y - Y, vert2.Z - Z);
    }
}