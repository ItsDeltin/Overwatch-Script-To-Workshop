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

        // The names of the WorkshopVariable in LoadNodes, LoadSegments, and LoadAttributes must equal the variable names in Modules/PathfindEditor.del. The ID doesn't matter.
        private static readonly WorkshopVariable LoadNodes    = new WorkshopVariable(true, 3, "preloadNodes");
        private static readonly WorkshopVariable LoadSegments = new WorkshopVariable(true, 4, "preloadSegments");
        private static readonly WorkshopVariable LoadAttributes = new WorkshopVariable(true, 5, "preloadAttributes");

        public static void FromPathmapFile(string file)
        {
            DeltinScript deltinScript = Generate(file, Pathmap.ImportFromFile(file), OutputLanguage.enUS);

            string code = deltinScript.WorkshopCode;

            if (code != null)
            {
                Program.WorkshopCodeResult(code);
            }
            else
            {
                Log.Write(LogLevel.Normal, new ColorMod("Build Failed.", ConsoleColor.Red));
                deltinScript.Diagnostics.PrintDiagnostics(Log);
            }
        }
        public static DeltinScript Generate(string fileName, Pathmap map, OutputLanguage language)
        {
            string baseEditorFile = Extras.CombinePathWithDotNotation(null, "!PathfindEditor.del");

            return new DeltinScript(new TranslateSettings(baseEditorFile) {
                AdditionalRules = (varCollection) => {
                    // Set the initial nodes.
                    Rule initialNodes = new Rule("Initial Nodes");
                    initialNodes.Actions = ArrayBuilder<Element>.Build(
                        // File name HUD.
                        Element.Hud(text: new StringElement(fileName), sortOrder: 1, textColor: "Orange", location: "Right"),

                        // Set nodes, segments, and attributes.
                        WorkshopArrayBuilder.SetVariable(null, map.NodesAsWorkshopData(), null, LoadNodes, false),
                        WorkshopArrayBuilder.SetVariable(null, map.SegmentsAsWorkshopData(), null, LoadSegments, false),
                        WorkshopArrayBuilder.SetVariable(null, map.AttributesAsWorkshopData(), null, LoadAttributes, false)
                    );

                    return new Rule[] { initialNodes };
                },
                OptimizeOutput = false,
                OutputLanguage = language
            });
        }
    }
}