namespace Deltin.Deltinteger.Assets.Models
{
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
}