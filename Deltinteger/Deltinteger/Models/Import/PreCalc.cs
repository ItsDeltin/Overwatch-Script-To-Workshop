using System;

namespace Deltin.Deltinteger.Models
{
    class PreCalc
    {
        public static double DistanceBetween(Vertex point1, Vertex point2)
        {
            return Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2) + Math.Pow(point2.Z - point1.Z, 2));
        }

        public static Vertex DirectionTowards(Vertex point1, Vertex point2)
        {
            double dist = DistanceBetween(point1, point2);
            return new Vertex(
                (point2.X - point1.X) / dist,
                (point2.Y - point1.Y) / dist,
                (point2.Z - point1.Z) / dist
            );
        }

        public static double HorizontalAngleFromDirection(Vertex direction)
        {
            return Math.Atan(direction.Z / direction.X) - 180;
        }

        public static double VerticalAngleFromDirection(Vertex direction)
        {
            double X = Math.Sqrt(Math.Pow(direction.X, 2) + Math.Pow(direction.Z, 2));
            return Math.Atan(direction.Y / X) - 90;
        }
    }
}
