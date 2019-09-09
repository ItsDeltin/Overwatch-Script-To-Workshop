using System;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Models
{
    public class Vertex : ICloneable
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
        public Vertex(double x, double y) : this(x,y,0,0) {}
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
            double[] bc = { c.X - b.X, c.Y - b.Y, c.Z - b.Z  };

            double abVec = Math.Sqrt(ab[0] * ab[0] + ab[1] * ab[1] + ab[2] * ab[2]);
            double bcVec = Math.Sqrt(bc[0] * bc[0] + bc[1] * bc[1] + bc[2] * bc[2]);

            double[] abNorm = {ab[0] / abVec, ab[1] / abVec, ab[2] / abVec};
            double[] bcNorm = {bc[0] / bcVec, bc[1] / bcVec, bc[2] / bcVec};

            double res = abNorm[0] * bcNorm[0] + abNorm[1] * bcNorm[1] + abNorm[2] * bcNorm[2];

            return Math.Acos(res)*180.0/ 3.141592653589793;
        }

        public bool EqualTo(Vertex other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }
    }
}