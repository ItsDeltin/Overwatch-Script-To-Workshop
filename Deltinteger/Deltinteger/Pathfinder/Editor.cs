using System;
using System.IO;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Pathfinder
{
    class Editor
    {
        private static readonly Log Log = new Log("Editor");

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
                WorkshopArrayBuilder.SetVariable(null, map.NodesAsWorkshopData(), true, null, Variable.J, false),
                WorkshopArrayBuilder.SetVariable(null, map.SegmentsAsWorkshopData(), true, null, Variable.K, false)
            );
            rules.Add(initialNodes);

            if (!result.Diagnostics.ContainsErrors())
            {
                string final = Program.RuleArrayToWorkshop(rules.ToArray(), result.VarCollection);
                Log.Write(LogLevel.Normal, "Press enter to copy code to clipboard, then in Overwatch click \"Paste Rule\".");
                Console.ReadLine();
                Program.SetClipboard(final);
            }
            else
            {
                Log.Write(LogLevel.Normal, new ColorMod("Build Failed.", ConsoleColor.Red));
                result.Diagnostics.PrintDiagnostics(Log);
            }
        }
    }
}