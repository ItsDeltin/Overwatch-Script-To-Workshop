using System;
using System.IO;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("HudTextFormat", "Displays an image on the HUD.", CustomMethodType.Action)]
    class HudTextFormat : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new HudFileParameter("imageFile", "The image to render."),
            new CodeParameter("visibleTo", "Who the image is visible to."),
            new CodeParameter("sortOrder", "The sort order of the rendered hud."),
            new CodeParameter("reevaluation", "Which inputs will be reevaluated.", WorkshopEnumType.GetEnumType<StringRev>()),
            new CodeParameter("spectators", "Determines if spectators can see the rendered hud.", WorkshopEnumType.GetEnumType<Spectators>(), new ExpressionOrWorkshopValue(EnumData.GetEnumValue(Spectators.DefaultVisibility)))
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            HudFile hudInfo = (HudFile)additionalParameterData[0];
            Element visibleTo = (Element)parameterValues[1];
            Element sortOrder = (Element)parameterValues[2];
            IWorkshopTree reevaluation = parameterValues[3];
            IWorkshopTree spectators = parameterValues[4];

            V_Null nul = new V_Null();
            IWorkshopTree white = EnumData.GetEnumValue(Color.White);

            for (double i = 0; i < hudInfo.Lines.Length; i++)
            {
                actionSet.AddAction(Element.Part<A_CreateHudText>(
                    /* visible to      */ visibleTo,
                    /* header          */ nul,
                    /* subheader       */ nul,
                    /* text            */ new V_CustomString(hudInfo.Lines[(int)i]),
                    /* location        */ EnumData.GetEnumValue(HudLocation.Top),
                    /* sort order      */ sortOrder + (i / 100),
                    /* header color    */ white,
                    /* subheader color */ white,
                    /* text color      */ white,
                    /* reevaluation    */ reevaluation,
                    /* spectators      */ spectators
                ));
            }

            actionSet.AddAction(Element.Part<A_CreateHudText>(
                /* visible to      */ visibleTo,
                /* header          */ nul,
                /* subheader       */ nul,
                /* text            */ new V_CustomString("　"),
                /* location        */ EnumData.GetEnumValue(HudLocation.Right),
                /* sort order      */ sortOrder + 0.01,
                /* header color    */ white,
                /* subheader color */ white,
                /* text color      */ white,
                /* reevaluation    */ reevaluation,
                /* spectators      */ spectators
            ));
            for (double i = 0; i < hudInfo.Lines.Length; i++)
            {
                int numberOfSpaces = 51 - (int)Math.Round((double)hudInfo.Lines[(int)i].Length / 2.0);
                const int splitEvery = 40;

                List<V_CustomString> strings = new List<V_CustomString>();

                for (int f = numberOfSpaces; f > 0; f -= splitEvery)
                    strings.Add(new V_CustomString(new string('　', Math.Min(splitEvery, f))));
                strings.Insert(0, new V_CustomString(hudInfo.Lines[(int)i]));

                actionSet.AddAction(Element.Part<A_CreateHudText>(
                    /* visible to      */ visibleTo,
                    /* header          */ nul,
                    /* subheader       */ nul,
                    /* text            */ V_CustomString.Join(strings.ToArray()),
                    /* location        */ EnumData.GetEnumValue(HudLocation.Right),
                    /* sort order      */ sortOrder + ((i + 1) / 100),
                    /* header color    */ white,
                    /* subheader color */ white,
                    /* text color      */ white,
                    /* reevaluation    */ reevaluation,
                    /* spectators      */ spectators
                ));
            }

            return null;
        }
    }

    class HudFileParameter : FileParameter
    {
        public HudFileParameter(string parameterName, string description) : base(null, parameterName, description) {}

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            string filePath = base.Validate(script, value, valueRange) as string;
            if (filePath == null) return null;

            try
            {
                return new HudFile(File.ReadAllLines(filePath));
            }
            catch (Exception)
            {
                script.Diagnostics.Error("Failed to load the file.", valueRange);
                return null;
            }
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, bool asElement) => null;
    }

    class HudFile
    {
        public string[] Lines { get; }

        public HudFile(string[] lines)
        {
            Lines = lines;
        }
    }
}