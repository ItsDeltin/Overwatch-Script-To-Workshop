using System.Collections.Generic;
using System.Drawing;

namespace Deltin.Deltinteger.Assets
{
    public class Model
    {
        const bool VERTICAL_LINE_FIX = true;

        public Line[] Lines { get; }

        public Model(Line[] lines)
        {
            List<Line> addLines = new List<Line>(lines);

            if (VERTICAL_LINE_FIX)
                foreach (Line line in addLines)
                    if (line.Vertex1.X == line.Vertex2.X &&
                        line.Vertex1.Z == line.Vertex2.Z)
                    {
                        // todo: this won't compile because Vertex2 is a property.
                        // This code is currently unused so I will not bother fixing it right now.
                        // line.Vertex2.X += 0.0001;
                    }

            Lines = addLines.ToArray();
        }
    }
}