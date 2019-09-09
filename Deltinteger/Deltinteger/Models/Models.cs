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

        public Model(Line[] lines)
        {
            List<Line> addLines = new List<Line>(lines);

            if (VERTICAL_LINE_FIX)
                foreach (Line line in addLines)
                    if (line.Vertex1.X == line.Vertex2.X &&
                        line.Vertex1.Z == line.Vertex2.Z)
                    {
                        line.Vertex2.X += 0.0001;
                    }

            Lines = addLines.ToArray();
        }

        public static Model ImportObj(string obj)
        {
            ObjModel result = ObjModel.Import(obj);
            return new Model(result.GetLines());
        }

        public static Model ImportString(string text, FontFamily family, double quality, Vertex angle, double scale, double angleRound)
        {
            StringModel stringModel = new StringModel(text, family, quality, angle, scale, angleRound);
            return new Model(stringModel.GetLines());
        }
    }

    interface IModelLoader
    {
        Line[] GetLines();
    }
}