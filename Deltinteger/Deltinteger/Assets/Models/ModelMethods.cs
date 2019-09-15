using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using Deltin.Deltinteger;
using Deltin.Deltinteger.Assets.Models.Import;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Assets.Models
{
    abstract class ModelCreator : CustomMethodBase
    {
        protected const bool GET_EFFECT_IDS_BY_DEFAULT = true;

        protected virtual int FontParameter { get; } = -1;

        protected Element[] RenderModel(Model model, Element visibleTo, Element location, Element scale, IWorkshopTree reevaluation, IndexedVar store, Element rotation)
        {
            List<Element> actions = new List<Element>();
            int waitEvery = rotation.ConstantSupported<Vertex>() ? 25 : 10;
            for (int i = 0; i < model.Lines.Length; i++)
            {
                actions.Add(CreateLine(model.Lines[i], visibleTo, location, scale, reevaluation, rotation));

                // Get the last created effect and append it to the store array.
                if (store != null)
                    actions.AddRange(
                        store.SetVariable(Element.Part<V_Append>(store.GetVariable(), new V_LastCreatedEntity()))
                    );

                // Add a wait every 12 effects to prevent high server load.
                if (i % waitEvery == 0)
                    actions.Add(A_Wait.MinimumWait);
            }
            return actions.ToArray();
        }

        protected Element CreateLine(Line line, Element visibleTo, Element location, Element scale, IWorkshopTree reevaluation, Element rotation)
        {
            Vertex vertex1 = line.Vertex1;
            Vertex vertex2 = line.Vertex2;
            Element pos1;
            Element pos2;

            bool scaleSet = false;
            bool rotationSet = false;

            if (scale != null)
            {
                double? constantScale = null;

                // Double constant scale
                if (scale.ConstantSupported<double>())
                    constantScale = (double)scale.GetConstant();
                
                // Null constant rotation
                else if (scale is V_Null)
                    constantScale = 1;

                if (constantScale == 1)
                    scaleSet = true;
                
                if (!scaleSet && constantScale != null)
                {
                    vertex1 = vertex1.Scale((double)constantScale);
                    vertex2 = vertex2.Scale((double)constantScale);
                    scaleSet = true;
                }
            }

            if (rotation != null)
            {
                Vertex rotationConstant = null;

                // Vector constant rotation
                if (rotation.ConstantSupported<Vertex>())
                    rotationConstant = (Vertex)rotation.GetConstant();
                
                // Double constant rotation
                else if (rotation.ConstantSupported<double>())
                    rotationConstant = new Vertex(0, (double)rotation.GetConstant(), 0);
                
                // Null constant rotation
                else if (rotation is V_Null)
                    rotationConstant = new Vertex(0, 0, 0);
                
                if (rotationConstant != null && rotationConstant.EqualTo(new Vertex(0, 0, 0)))
                    rotationSet = true;
                
                if (rotationConstant != null && !rotationSet)
                {
                    vertex1 = vertex1.Rotate(rotationConstant);
                    vertex2 = vertex2.Rotate(rotationConstant);
                    rotationSet = true;
                }
            }

            if (rotation != null && !rotationSet)
            {
                var pos1X = new V_Number(vertex1.X);
                var pos1Y = new V_Number(vertex1.Y);
                var pos1Z = new V_Number(vertex1.Z);
                var pos2X = new V_Number(vertex2.X);
                var pos2Y = new V_Number(vertex2.Y);
                var pos2Z = new V_Number(vertex2.Z);

                var yaw = Element.Part<V_HorizontalAngleFromDirection>(rotation);
                var pitch = Element.Part<V_VerticalAngleFromDirection>(rotation);

                var cosa = Element.Part<V_CosineFromDegrees>(pitch);
                var sina = Element.Part<V_SineFromDegrees>(pitch);

                var cosb = Element.Part<V_CosineFromDegrees>(yaw);
                var sinb = Element.Part<V_SineFromDegrees>(yaw);

                var Axx = Element.Part<V_Multiply>(cosa, cosb);
                var Axy = Element.Part<V_Subtract>(new V_Number(0), sina);
                var Axz = Element.Part<V_Multiply>(cosa, sinb);

                var Ayx = Element.Part<V_Multiply>(sina, cosb);
                var Ayy = cosa;
                var Ayz = Element.Part<V_Multiply>(sina, sinb);

                var Azx = Element.Part<V_Multiply>(new V_Number(-1), sinb);

                pos1 = Element.Part<V_Vector>(
                    Element.Part<V_Add>(Element.Part<V_Add>(
                        Element.Part<V_Multiply>(Axx, pos1X),
                        Element.Part<V_Multiply>(Axy, pos1Y)),
                        Element.Part<V_Multiply>(Axz, pos1Z)),
                    Element.Part<V_Add>(Element.Part<V_Add>(
                        Element.Part<V_Multiply>(Ayx, pos1X),
                        Element.Part<V_Multiply>(Ayy, pos1Y)),
                        Element.Part<V_Multiply>(Ayz, pos1Z)),
                    Element.Part<V_Add>(
                        Element.Part<V_Multiply>(Azx, pos1X),
                        pos1Z)
                );

                pos2 = Element.Part<V_Vector>(
                    Element.Part<V_Add>(Element.Part<V_Add>(
                        Element.Part<V_Multiply>(Axx, pos2X),
                        Element.Part<V_Multiply>(Axy, pos2Y)),
                        Element.Part<V_Multiply>(Axz, pos2Z)),
                    Element.Part<V_Add>(Element.Part<V_Add>(
                        Element.Part<V_Multiply>(Ayx, pos2X),
                        Element.Part<V_Multiply>(Ayy, pos2Y)),
                        Element.Part<V_Multiply>(Ayz, pos2Z)),
                    Element.Part<V_Add>(
                        Element.Part<V_Multiply>(Azx, pos2X),
                        pos2Z)
                );
            }
            else
            {
                pos1 = vertex1.ToVector();
                pos2 = vertex2.ToVector();
            }

            if (scale != null && !scaleSet)
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

        protected MethodResult RenderText(string text, string font, double quality, Element visibleTo, Element location, Element scale, IWorkshopTree effectRev, bool getIds, double angleRound, Element rotation)
        {
            quality = Math.Max(10 - quality, 0.1);

            Model model;
            using (FontFamily family = GetFontFamily(font, FontParameter == -1 ? MethodLocation : ParameterLocations[FontParameter]))
                model = Model.ImportString(text, family, quality, angleRound);

            List<Element> actions = new List<Element>();

            IndexedVar effects = null;
            if (getIds)
            {
                effects = TranslateContext.VarCollection.AssignVar(Scope, "Model Effects", TranslateContext.IsGlobal, null);
                actions.AddRange(effects.SetVariable(new V_EmptyArray()));
            }
            
            actions.AddRange(RenderModel(model, visibleTo, location, scale, effectRev, effects, rotation));
            
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
    [Parameter("Rotation", Elements.ValueType.Vector, null)]
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
            Element rotation            = (Element)Parameters[3];
            Element scale               = (Element)Parameters[4];
            EnumMember effectRev     = (EnumMember)Parameters[5];
            bool getIds   = (bool)((ConstantObject)Parameters[6]).Value;

            List<Element> actions = new List<Element>();

            IndexedVar effects = null;
            if (getIds)
            {
                effects = TranslateContext.VarCollection.AssignVar(Scope, "Model Effects", TranslateContext.IsGlobal, null);
                actions.AddRange(effects.SetVariable(new V_EmptyArray()));
            }

            actions.AddRange(RenderModel(modelVar.Model, visibleTo, location, scale, effectRev, effects, rotation));

            return new MethodResult(actions.ToArray(), effects?.GetVariable());
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Create a wireframe of a variable containing a 3D model. Using a non-constant rotation will use excessive server load. To reduce server load set rotation as a variable which that can be modified.",
                // Parameters
                "The variable containing the model constant.",
                "Who the model is visible to.",
                "The location of the model.",
                "The rotation of the model as a directional vector. If it is a vector constant, the rotation will be pre-calulated and will consume less server load (much less).",
                "The scale of the model.",
                "Specifies which of this methods' inputs will be continuously reevaluated, the model will keep asking for and using new values from reevaluated inputs.",
                "If true, the method will return the effect IDs used to create the model. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }

    [CustomMethod("CreateTextFont", CustomMethodType.MultiAction_Value)]
    [ConstantParameter("Text", typeof(string))]
    [ConstantParameter("Font", typeof(string))]
    [ConstantParameter("Quality", typeof(double))]
    [ConstantParameter("Line Angle Merge", typeof(double))]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [Parameter("Rotation", Elements.ValueType.Vector, null)]
    [Parameter("Scale", Elements.ValueType.Number, null)]
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
            Element visibleTo              = (Element)Parameters[5];
            Element location               = (Element)Parameters[6];
            Element rotation               = (Element)Parameters[7];
            Element scale                  = (Element)Parameters[8];
            EnumMember effectRev        = (EnumMember)Parameters[9];
            bool getIds    = (bool)  ((ConstantObject)Parameters[10]).Value;

            return RenderText(text, font, quality, visibleTo, location, scale, effectRev, getIds, merge, rotation);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Creates in-world text using any custom text. Using a non-constant rotation will use excessive server load. To reduce server load set rotation as a variable which that can be modified.",
                // Parameters
                "The text to display. This is a string constant.",
                "The name of the font to use. This is a string constant.",
                "The quality of the font. The value must be between 0-10. Higher numbers creates more effects. This is a number constant.",
                "Merge lines if their angles are under this amount.",
                "Who the text is visible to.",
                "The location to display the text.",
                "The rotation of the model as a directional vector. If it is a vector constant, the rotation will be pre-calulated and will consume less server load (much less).",
                "The scale of the text.",
                "Specifies which of this methods inputs will be continuously reevaluated, the text will keep asking for and using new values from reevaluated inputs.",
                "If true, the method will return the effect IDs used to create the text. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }

    [CustomMethod("CreateTextFancy", CustomMethodType.MultiAction_Value)]
    [ConstantParameter("Text", typeof(string))]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [Parameter("Rotation", Elements.ValueType.Vector, null)]
    [Parameter("Scale", Elements.ValueType.Number, null)]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    [ConstantParameter("Get Effect IDs", typeof(bool), GET_EFFECT_IDS_BY_DEFAULT)]
    class CreateTextFancy : ModelCreator
    {
        override protected MethodResult Get()
        {
            string text    = (string)((ConstantObject)Parameters[0]).Value;
            Element visibleTo              = (Element)Parameters[1];
            Element location               = (Element)Parameters[2];
            Element rotation               = (Element)Parameters[3];
            Element scale                  = (Element)Parameters[4];
            EnumMember effectRev        = (EnumMember)Parameters[5];
            bool getIds    = (bool)  ((ConstantObject)Parameters[6]).Value;

            return RenderText(text, "BigNoodleTooOblique", 9, visibleTo, location, scale, effectRev, getIds, 0, rotation);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Creates in-world text using any custom text. Uses the BigNoodleTooOblique font, Overwatch's main font. Using a non-constant rotation will use excessive server load. To reduce server load set rotation as a variable which that can be modified.",
                // Parameters
                "The text to display. This is a string constant.",
                "Who the text is visible to.",
                "The location to display the text.",
                "The rotation of the model as a directional vector. If it is a vector constant, the rotation will be pre-calulated and will consume less server load (much less).",
                "The scale of the text.",
                "Specifies which of this methods inputs will be continuously reevaluated, the text will keep asking for and using new values from reevaluated inputs.",
                "If true, the method will return the effect IDs used to create the text. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }

    [CustomMethod("CreateText", CustomMethodType.MultiAction_Value)]
    [ConstantParameter("Text", typeof(string))]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [Parameter("Rotation", Elements.ValueType.Vector, null)]
    [Parameter("Scale", Elements.ValueType.Number, null)]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    [ConstantParameter("Get Effect IDs", typeof(bool), GET_EFFECT_IDS_BY_DEFAULT)]
    class CreateText : ModelCreator
    {
        override protected MethodResult Get()
        {
            string text    = (string)((ConstantObject)Parameters[0]).Value;
            Element visibleTo              = (Element)Parameters[1];
            Element location               = (Element)Parameters[2];
            Element rotation               = (Element)Parameters[3];
            Element scale                  = (Element)Parameters[4];
            EnumMember effectRev        = (EnumMember)Parameters[5];
            bool getIds      = (bool)((ConstantObject)Parameters[6]).Value;

            Model model = new Model(Letter.Create(text, false, ParameterLocations[0]));

            List<Element> actions = new List<Element>();

            IndexedVar effects = null;
            if (getIds)
            {
                effects = TranslateContext.VarCollection.AssignVar(Scope, "Model Effects", TranslateContext.IsGlobal, null);
                actions.AddRange(effects.SetVariable(new V_EmptyArray()));
            }

            actions.AddRange(RenderModel(model, visibleTo, location, scale, effectRev, effects, rotation));

            return new MethodResult(actions.ToArray(), effects?.GetVariable());
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Creates in-world text using any custom text. Uses a less amount of effects.",
                // Parameters
                "The text to display. This is a string constant.",
                "Who the text is visible to.",
                "The location to display the text.",
                "The rotation of the model as a directional vector. If it is a vector constant, the rotation will be pre-calulated and will consume less server load (much less).",
                "The scale of the text.",
                "Specifies which of this methods inputs will be continuously reevaluated, the text will keep asking for and using new values from reevaluated inputs.",
                "If true, the method will return the effect IDs used to create the text. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }
}