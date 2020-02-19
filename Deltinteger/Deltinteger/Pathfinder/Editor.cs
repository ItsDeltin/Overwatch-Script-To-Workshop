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

            string baseEditorFile = Extras.CombinePathWithDotNotation(null, "!PathfindEditor.del");
            Diagnostics diagnostics = new Diagnostics();

            DeltinScript deltinScript = new DeltinScript(new TranslateSettings(diagnostics, baseEditorFile) {
                AdditionalRules = (varCollection) => {
                    // Set the initial nodes.
                    Rule initialNodes = new Rule("Initial Nodes");
                    initialNodes.Actions = ArrayBuilder<Element>.Build(
                        WorkshopArrayBuilder.SetVariable(null, map.NodesAsWorkshopData(), null, LoadNodes, false),
                        WorkshopArrayBuilder.SetVariable(null, map.SegmentsAsWorkshopData(), null, LoadSegments, false)
                    );

                    return new Rule[] { initialNodes };
                }
            });

            string code = deltinScript.WorkshopCode;

            if (code != null)
            {
                Program.WorkshopCodeResult(code);
            }
            else
            {
                Log.Write(LogLevel.Normal, new ColorMod("Build Failed.", ConsoleColor.Red));
                diagnostics.PrintDiagnostics(Log);
            }
        }
    }
}