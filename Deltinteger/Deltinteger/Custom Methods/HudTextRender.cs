using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("HudTextRender", "Displays an image on the HUD.", CustomMethodType.Action)]
    class HudTextRender : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new HudFileParameter("imageFile", "The image to render."),
            new CodeParameter("visibleTo", "Who the image is visible to."),
            new CodeParameter("foregroundColor", "The foreground color.", WorkshopEnumType.GetEnumType<Elements.Color>()),
            new CodeParameter("backgroundColor", "The background color.", WorkshopEnumType.GetEnumType<Elements.Color>()),
            new CodeParameter("reevaluation", "Which inputs will be reevaluated.", WorkshopEnumType.GetEnumType<StringRev>()),
            new CodeParameter("spectators", "Determines if spectators can see the rendered hud.", WorkshopEnumType.GetEnumType<Spectators>(), new ExpressionOrWorkshopValue(EnumData.GetEnumValue(Spectators.DefaultVisibility))),
            new ConstBoolParameter("getTextIDs", "If true, the method will return the text IDs used to create the image. Use `DestroyHudArray()` to destroy the hud. This is a boolean constant.", false)
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            HudFile hudInfo = (HudFile)additionalParameterData[0];
            Element visibleTo = (Element)parameterValues[1];
            IWorkshopTree foregroundColor = parameterValues[2];
            IWorkshopTree backgroundColor = parameterValues[3];
            IWorkshopTree reevaluation = parameterValues[4];
            IWorkshopTree spectators = parameterValues[5];
            bool getIDs = (bool)additionalParameterData[6];

            IndexReference ids = null;
            if (getIDs)
            {
                ids = actionSet.VarCollection.Assign("_hudTextRender_IDs", actionSet.IsGlobal, true);
                actionSet.AddAction(ids.SetVariable(new V_EmptyArray()));
            }

            hudInfo.Print(actionSet, ids, false, visibleTo, reevaluation, spectators, backgroundColor);
            CreateHud(actionSet, HudLocation.Left, "_", -2, visibleTo, reevaluation, spectators);
            hudInfo.Print(actionSet, ids, true, visibleTo, reevaluation, spectators, foregroundColor);
            return ids?.GetVariable();
        }

        static V_Null nul = new V_Null();
        static IWorkshopTree white = EnumData.GetEnumValue(Elements.Color.White);

        public static void CreateHud(ActionSet actionSet, HudLocation location, string text, double sortOrder, IWorkshopTree visibleTo, IWorkshopTree reevaluation, IWorkshopTree spectators)
        {
            actionSet.AddAction(Element.Part<A_CreateHudText>(
                /* visible to      */ visibleTo,
                /* header          */ nul,
                /* subheader       */ nul,
                /* text            */ new V_CustomString(text),
                /* location        */ EnumData.GetEnumValue(location),
                /* sort order      */ new V_Number(sortOrder),
                /* header color    */ white,
                /* subheader color */ white,
                /* text color      */ white,
                /* reevaluation    */ reevaluation,
                /* spectators      */ spectators
            ));
        }
    }

    class HudFileParameter : FileParameter
    {
        public HudFileParameter(string parameterName, string description) : base(parameterName, description, ".bmp", ".jpg", ".png", ".tiff", ".gif", ".exif") {}

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            string filePath = base.Validate(script, value, valueRange) as string;
            if (filePath == null) return null;

            try
            {
                using (Bitmap bmp = new Bitmap(filePath))
                    return new HudFile(bmp);
            }
            catch (Exception ex)
            {
                script.Diagnostics.Error("Failed to load the file: " + ex.Message, valueRange);
                return null;
            }
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, bool asElement) => null;
    }

    class HudFile
    {
        public HudLine[] Lines { get; }

        public HudFile(Bitmap bmp)
        {
            Lines = new HudLine[bmp.Height];
            for (int y = 0; y < bmp.Height; y++)
            {
                HudLine line = new HudLine(bmp.Width, y);
                for (int x = 0; x < bmp.Width; x++)
                {
                    var pixelColor = bmp.GetPixel(x,y);
                    line.Filled[x] = pixelColor.R + pixelColor.G + pixelColor.B < 255.0 * 2.5;
                }
                Lines[y] = line;
            }
        }

        public void Print(ActionSet actionSet, IndexReference ids, bool bg, IWorkshopTree visibleTo, IWorkshopTree reevaluation, IWorkshopTree spectators, IWorkshopTree color)
        {
            foreach (HudLine line in Lines)
                line.Print(actionSet, ids, bg, visibleTo, reevaluation, spectators, color);
        }
    }

    class HudLine
    {
        public readonly bool[] Filled;
        public readonly int Line;

        public HudLine(int width, int line)
        {
            Filled = new bool[width];
            Line = line;
        }

        static V_Null nul = new V_Null();
        static IWorkshopTree white = EnumData.GetEnumValue(Elements.Color.White);

        public void Print(ActionSet actionSet, IndexReference ids, bool right, IWorkshopTree visibleTo, IWorkshopTree reevaluation, IWorkshopTree spectators, IWorkshopTree color)
        {
            List<V_CustomString> strings = new List<V_CustomString>();

            int numberOfSpaces = 51 - (int)Math.Round((double)Filled.Length / 2.0);
            const int splitEvery = 40;

            for (int f = numberOfSpaces; f > 0; f -= splitEvery)
                strings.Add(new V_CustomString(new string('　', Math.Min(splitEvery, f))));
                
            string str = "";
            foreach (bool filled in Filled) str += filled ? '▒' : '　';

            IWorkshopTree location = EnumData.GetEnumValue(HudLocation.Left);
            Element subheader = nul;

            if (right)
            {
                strings.Insert(0, V_CustomString.SplitLength(str, 40));
                location = EnumData.GetEnumValue(HudLocation.Right);
                if (Line == 0)
                    subheader = new V_CustomString("`");
            }
            else
            {
                strings.Add(V_CustomString.SplitLength(str, 40));
            }
            
            actionSet.AddAction(Element.Part<A_CreateHudText>(
                /* visible to      */ visibleTo,
                /* header          */ nul,
                /* subheader       */ subheader,
                /* text            */ V_CustomString.Join(strings.ToArray()),
                /* location        */ location,
                /* sort order      */ -1 + new V_Number(Line + 1) / 100,
                /* header color    */ white,
                /* subheader color */ white,
                /* text color      */ color,
                /* reevaluation    */ reevaluation,
                /* spectators      */ spectators
            ));

            if (ids != null)
                ids.ModifyVariable(Operation.AppendToArray, new V_LastTextID());
        }
    }
}