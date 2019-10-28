using System;
using System.IO;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using TextCopy;

namespace Deltin.Deltinteger.Pathfinder
{
    class Editor
    {
        private static readonly Log Log = new Log("Editor");

        // The names of the WorkshopVariable in LoadNodes and LoadSegments must equal the variable 
        // names in Modules/PathfindEditor.del. The ID doesn't matter.
        // line 328: define globalvar preloadNodes [5];
        // line 329: define globalvar preloadSegments [6];
        private static readonly WorkshopVariable LoadNodes    = new WorkshopVariable(true, 5, "preloadNodes");
        private static readonly WorkshopVariable LoadSegments = new WorkshopVariable(true, 6, "preloadSegments");

        public static void FromPathmapFile(string file)
        {
            PathMap map = PathMap.ImportFromXML(file);

            string baseEditorLoc = Extras.CombinePathWithDotNotation(null, "!PathfindEditor.del");
            string baseEditor = File.ReadAllText(baseEditorLoc);
            ParsingData result = ParsingData.GetParser(baseEditorLoc, baseEditor);

            List<Rule> rules = new List<Rule>();
            rules.AddRange(result.Rules);
            
            Rule initialNodes = new Rule("Initial Nodes");
            initialNodes.Actions = ArrayBuilder<Element>.Build(
                WorkshopArrayBuilder.SetVariable(null, map.NodesAsWorkshopData(), true, null, LoadNodes, false),
                WorkshopArrayBuilder.SetVariable(null, map.SegmentsAsWorkshopData(), true, null, LoadSegments, false)
            );
            rules.Add(initialNodes);

            if (!result.Diagnostics.ContainsErrors())
            {
                string final = Program.RuleArrayToWorkshop(rules.ToArray(), result.VarCollection);
                Program.WorkshopCodeResult(final);
            }
            else
            {
                Log.Write(LogLevel.Normal, new ColorMod("Build Failed.", ConsoleColor.Red));
                result.Diagnostics.PrintDiagnostics(Log);
            }
        }
    }
}