using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using Deltin.Deltinteger;
using Deltin.Deltinteger.Models.Import;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Models
{
    abstract class ModelCreator : CustomMethodBase
    {
        protected const bool GET_EFFECT_IDS_BY_DEFAULT = true;

        protected virtual int FontParameter { get; } = -1;

        protected Element[] RenderModel(Model model, Element visibleTo, Element location, Element scale, IWorkshopTree reevaluation, IndexedVar store)
        {
            List<Element> actions = new List<Element>();
            for (int i = 0; i < model.Lines.Length; i++)
            {
                actions.Add(CreateLine(model.Lines[i], visibleTo, location, scale, reevaluation));

                // Get the last created effect and append it to the store array.
                if (store != null)
                    actions.AddRange(
                        store.SetVariable(Element.Part<V_Append>(store.GetVariable(), new V_LastCreatedEntity()))
                    );

                // Add a wait every 25 actions to prevent high server load.
                if (actions.Count % 25 == 0)
                    actions.Add(A_Wait.MinimumWait);
            }
            return actions.ToArray();
        }

        protected Element CreateLine(Line line, Element visibleTo, Element location, Element scale, IWorkshopTree reevaluation)
        {
            Element pos1 = line.Vertex1.ToVector();
            Element pos2 = line.Vertex2.ToVector();

            if (scale != null)
            {
                pos1 = Element.Part<V_Multiply>(pos1, scale);
                pos2 = Element.Part<V_Multiply>(pos2, scale);
            }

            return Element.Part<A_CreateBeamEffect>(
                visibleTo,
                EnumData.GetEnumValue(BeamType.GrappleBeam),
                Element.Part<V_Add>(location, pos1),
                Element.Part<V_Add>(location, pos2),
                EnumData.GetEnumValue(Elements.Color.Red),
                reevaluation
            );
        }

        protected MethodResult RenderText(string text, string font, double quality, double angle, Element visibleTo, Element location, double scale, IWorkshopTree effectRev, bool getIds, double angleRound)
        {
            quality = Math.Max(10 - quality, 0.1);

            Model model;
            using (FontFamily family = GetFontFamily(font, FontParameter == -1 ? MethodLocation : ParameterLocations[FontParameter]))
                model = Model.ImportString(text, family, quality, angle, scale, angleRound);

            List<Element> actions = new List<Element>();

            IndexedVar effects = null;
            if (getIds)
            {
                effects = TranslateContext.VarCollection.AssignVar(Scope, "Model Effects", TranslateContext.IsGlobal, null);
                actions.AddRange(effects.SetVariable(new V_EmptyArray()));
            }
                
            actions.AddRange(RenderModel(model, visibleTo, location, null, effectRev, effects));
            
            return new MethodResult(actions.ToArray(), effects?.GetVariable());
        }

        private static FontFamily GetFontFamily(string name, Location location)
        {
            string filepath = Path.Combine(Program.ExeFolder, "Fonts", name + ".ttf");

            if (File.Exists(filepath))
            {
                if (LoadedFonts == null)
                    LoadedFonts = new PrivateFontCollection();

                FontFamily family = LoadedFonts.Families.FirstOrDefault(fam => fam.Name.ToLower() == name.ToLower());

                if (family == null)
                {
                    try
                    {
                        LoadedFonts.AddFontFile(filepath);
                    }
                    catch (ArgumentException)
                    {
                        throw new SyntaxErrorException($"Failed to load the font {name} at '{filepath}'.", location);
                    }
                    family = LoadedFonts.Families.FirstOrDefault(fam => fam.Name.ToLower() == name.ToLower());
                    if (family == null)
                        throw new SyntaxErrorException($"Failed to load the font {name} at '{filepath}'.", location);
                }

                return family;
            }
            
            if (!FontFamily.Families.Any(fam => fam.Name.ToLower() == name.ToLower()))
                throw new SyntaxErrorException($"The font {name} does not exist.", location);
            
            return new FontFamily(name);
        }

        private static PrivateFontCollection LoadedFonts = null;
    }

    [CustomMethod("ShowWireframe", CustomMethodType.MultiAction_Value)]
    [VarRefParameter("Model")]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [Parameter("Scale", Elements.ValueType.Number, null)]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    [ConstantParameter("Get Effect IDs", typeof(bool), GET_EFFECT_IDS_BY_DEFAULT)]
    class ShowModel : ModelCreator
    {
        override protected MethodResult Get()
        {
            if (((VarRef)Parameters[0]).Var is ModelVar == false)
                throw SyntaxErrorException.InvalidVarRefType(((VarRef)Parameters[0]).Var.Name, VarType.Model, ParameterLocations[0]);
            
            ModelVar modelVar = (ModelVar)((VarRef)Parameters[0]).Var;
            Element visibleTo           = (Element)Parameters[1];
            Element location            = (Element)Parameters[2];
            Element scale               = (Element)Parameters[3];
            EnumMember effectRev     = (EnumMember)Parameters[4];
            bool getIds   = (bool)((ConstantObject)Parameters[5]).Value;

            List<Element> actions = new List<Element>();

            IndexedVar effects = null;
            if (getIds)
            {
                effects = TranslateContext.VarCollection.AssignVar(Scope, "Model Effects", TranslateContext.IsGlobal, null);
                actions.AddRange(effects.SetVariable(new V_EmptyArray()));
            }

            actions.AddRange(RenderModel(modelVar.Model, visibleTo, location, scale, effectRev, effects));

            return new MethodResult(actions.ToArray(), effects?.GetVariable());
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Create a wireframe of a variable containing a 3D model.",
                // Parameters
                "The variable containing the model constant.",
                "Who the model is visible to.",
                "The location of the model.",
                "The scale of the model.",
                "Specifies which of this methods inputs will be continuously reevaluated, the model will keep asking for and using new values from reevaluated inputs.",
                "If true, the method will return the effect IDs used to create the model. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }

    [CustomMethod("CreateTextFont", CustomMethodType.MultiAction_Value)]
    [ConstantParameter("Text", typeof(string))]
    [ConstantParameter("Font", typeof(string))]
    [ConstantParameter("Quality", typeof(double))]
    [ConstantParameter("Line Angle Merge", typeof(double))]
    [ConstantParameter("Angle", typeof(double))]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [ConstantParameter("Scale", typeof(double))]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    [ConstantParameter("Get Effect IDs", typeof(bool), GET_EFFECT_IDS_BY_DEFAULT)]
    class CreateTextWithFont : ModelCreator
    {
        override protected int FontParameter { get; } = 1;

        override protected MethodResult Get()
        {
            string text    = (string)((ConstantObject)Parameters[0]).Value;
            string font    = (string)((ConstantObject)Parameters[1]).Value;
            double quality = (double)((ConstantObject)Parameters[2]).Value;
            double merge   = (double)((ConstantObject)Parameters[3]).Value;
            double angle   = (double)((ConstantObject)Parameters[4]).Value + 22.2; // Add offset to make it even with HorizontalAngleOf().
            Element visibleTo              = (Element)Parameters[5];
            Element location               = (Element)Parameters[6];
            double scale   = (double)((ConstantObject)Parameters[7]).Value;
            EnumMember effectRev        = (EnumMember)Parameters[8];
            bool getIds    = (bool)  ((ConstantObject)Parameters[9]).Value;

            return RenderText(text, font, quality, angle, visibleTo, location, scale, effectRev, getIds, merge);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Creates in-world text using any custom text.",
                // Parameters
                "The text to display. This is a string constant.",
                "The name of the font to use. This is a string constant.",
                "The quality of the font. The value must be between 0-10. Higher numbers creates more effects. This is a number constant.",
                "Merge lines if their angles are under this amount.",
                "The angle of the text. This is a number constant.",
                "Who the text is visible to.",
                "The location to display the text.",
                "The scale of the text.",
                "Specifies which of this methods inputs will be continuously reevaluated, the text will keep asking for and using new values from reevaluated inputs.",
                "If true, the method will return the effect IDs used to create the text. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }

    [CustomMethod("CreateText", CustomMethodType.MultiAction_Value)]
    [ConstantParameter("Text", typeof(string))]
    [ConstantParameter("Angle", typeof(double))]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [ConstantParameter("Scale", typeof(double))]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    [ConstantParameter("Get Effect IDs", typeof(bool), GET_EFFECT_IDS_BY_DEFAULT)]
    class CreateText : ModelCreator
    {
        override protected MethodResult Get()
        {
            string text    = (string)((ConstantObject)Parameters[0]).Value;
            double angle   = (double)((ConstantObject)Parameters[1]).Value + 22.2; // Add offset to make it even with HorizontalAngleOf().
            Element visibleTo              = (Element)Parameters[2];
            Element location               = (Element)Parameters[3];
            double scale   = (double)((ConstantObject)Parameters[4]).Value;
            EnumMember effectRev        = (EnumMember)Parameters[5];
            bool getIds    = (bool)  ((ConstantObject)Parameters[6]).Value;

            return RenderText(text, "BigNoodleTooOblique", 9, angle, visibleTo, location, scale, effectRev, getIds, 0);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Creates in-world text using any custom text. Uses the BigNoodleTooOblique font, Overwatch's main font.",
                // Parameters
                "The text to display. This is a string constant.",
                "The angle of the text. This is a number constant.",
                "Who the text is visible to.",
                "The location to display the text.",
                "The scale of the text.",
                "Specifies which of this methods inputs will be continuously reevaluated, the text will keep asking for and using new values from reevaluated inputs.",
                "If true, the method will return the effect IDs used to create the text. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }

    [CustomMethod("CreateTextMinimal", CustomMethodType.MultiAction_Value)]
    [ConstantParameter("Text", typeof(string))]
    [ConstantParameter("Angle", typeof(double))]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [ConstantParameter("Scale", typeof(double))]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    [ConstantParameter("Get Effect IDs", typeof(bool), GET_EFFECT_IDS_BY_DEFAULT)]
    class CreateTextMinimal : ModelCreator
    {
        override protected MethodResult Get()
        {
            string text    = (string)((ConstantObject)Parameters[0]).Value;
            double angle   = (double)((ConstantObject)Parameters[1]).Value + 22.2; // Add offset to make it even with HorizontalAngleOf().
            Element visibleTo              = (Element)Parameters[2];
            Element location               = (Element)Parameters[3];
            double scale   = (double)((ConstantObject)Parameters[4]).Value;
            EnumMember effectRev        = (EnumMember)Parameters[5];
            bool getIds    = (bool)  ((ConstantObject)Parameters[6]).Value;

            return RenderText(text, "1CamBam_Stick_1", 10, angle, visibleTo, location, scale, effectRev, getIds, 50);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Creates in-world text using any custom text. Uses a less amount of effects.",
                // Parameters
                "The text to display. This is a string constant.",
                "The angle of the text. This is a number constant.",
                "Who the text is visible to.",
                "The location to display the text.",
                "The scale of the text.",
                "Specifies which of this methods inputs will be continuously reevaluated, the text will keep asking for and using new values from reevaluated inputs.",
                "If true, the method will return the effect IDs used to create the text. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }

    [CustomMethod("CreateTextMinimaler", CustomMethodType.MultiAction_Value)]
    [ConstantParameter("Text", typeof(string))]
    [ConstantParameter("Angle", typeof(double))]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [ConstantParameter("Scale", typeof(double))]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    [ConstantParameter("Get Effect IDs", typeof(bool), GET_EFFECT_IDS_BY_DEFAULT)]
    class CreateTextMinimaler : ModelCreator
    {
        override protected MethodResult Get()
        {
            string text    = (string)((ConstantObject)Parameters[0]).Value;
            double angle   = (double)((ConstantObject)Parameters[1]).Value + 22.2; // Add offset to make it even with HorizontalAngleOf().
            Element visibleTo              = (Element)Parameters[2];
            Element location               = (Element)Parameters[3];
            double scale   = (double)((ConstantObject)Parameters[4]).Value;
            EnumMember effectRev        = (EnumMember)Parameters[5];
            bool getIds    = (bool)  ((ConstantObject)Parameters[6]).Value;

            Model model = new Model(Letter.Create(text, false, ParameterLocations[0], scale));

            List<Element> actions = new List<Element>();

            IndexedVar effects = null;
            if (getIds)
            {
                effects = TranslateContext.VarCollection.AssignVar(Scope, "Model Effects", TranslateContext.IsGlobal, null);
                actions.AddRange(effects.SetVariable(new V_EmptyArray()));
            }

            actions.AddRange(RenderModel(model, visibleTo, location, null, effectRev, effects));

            return new MethodResult(actions.ToArray(), effects?.GetVariable());
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Creates in-world text using any custom text. Uses a less amount of effects.",
                // Parameters
                "The text to display. This is a string constant.",
                "The angle of the text. This is a number constant.",
                "Who the text is visible to.",
                "The location to display the text.",
                "The scale of the text.",
                "Specifies which of this methods inputs will be continuously reevaluated, the text will keep asking for and using new values from reevaluated inputs.",
                "If true, the method will return the effect IDs used to create the text. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }
}