using System;
using System.Collections.Generic;
using System.IO;
using Deltin.Deltinteger.Csv;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    class PathMap
    {
        private static readonly Variable IsBuildingMarker = Variable.D;

        public static void Import(string file)
        {
            CsvFrame[] frames = CsvFrame.ParseSet(File.ReadAllLines(file));

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i].VariableValues[IsBuildingMarker] is CsvBoolean && ((CsvBoolean)frames[i].VariableValues[IsBuildingMarker]).Value)
                {
                    
                }
            }
        }
    }
}