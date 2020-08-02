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
using Deltin.Deltinteger.CustomMethods;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Models
{
    public class AssetClass : CodeType
    {
        private Scope StaticScope { get; } = new Scope("class Asset");

        public AssetClass() : base("Asset")
        {
            Description = "Contains functions for displaying assets in the world.";
            StaticScope.AddMethod(CustomMethodData.GetCustomMethod<ShowModel>(), null, null);
            StaticScope.AddMethod(CustomMethodData.GetCustomMethod<CreateTextWithFont>(), null, null);
            StaticScope.AddMethod(CustomMethodData.GetCustomMethod<CreateTextFancy>(), null, null);
            StaticScope.AddMethod(CustomMethodData.GetCustomMethod<CreateText>(), null, null);
        }

        public override Scope ReturningScope() => StaticScope;

        public override CompletionItem GetCompletion() => new CompletionItem() {
            Label = "Asset",
            Kind = CompletionItemKind.Class
        };
    }

    abstract class ModelCreator : CustomMethodBase
    {
        protected const bool GET_EFFECT_IDS_BY_DEFAULT = true;

        protected IWorkshopTree RenderModel(ActionSet actionSet, Model model, Element visibleTo, Element location, Element rotation, Element scale, IWorkshopTree reevaluation, bool getEffectIDs)
        {
            IndexReference effects = null;
            if (getEffectIDs)
            {
                effects = actionSet.VarCollection.Assign("_modelEffects", actionSet.IsGlobal, true);
                actionSet.AddAction(effects.SetVariable(Element.EmptyArray()));
            }

            for (int i = 0; i < model.Lines.Length; i++)
            {
                CreateLine(actionSet, model.Lines[i], visibleTo, location, rotation, scale, reevaluation);

                // Get the last created effect and append it to the store array.
                if (effects != null)
                    actionSet.AddAction(effects.ModifyVariable(Operation.AppendToArray, new V_LastCreatedEntity()));

                // Add a wait every 12 effects to prevent high server load.
                if (i % 10 == 0)
                    actionSet.AddAction(A_Wait.MinimumWait);
            }

            return effects?.GetVariable();
        }

        protected void CreateLine(ActionSet actionSet, Line line, Element visibleTo, Element location, Element rotation, Element scale, IWorkshopTree reevaluation)
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
                var pos1X = vertex1.X;
                var pos1Y = vertex1.Y;
                var pos1Z = vertex1.Z;
                var pos2X = vertex2.X;
                var pos2Y = vertex2.Y;
                var pos2Z = vertex2.Z;

                var yaw = Element.Part<V_HorizontalAngleFromDirection>(rotation);
                var pitch = Element.Part<V_VerticalAngleFromDirection>(rotation);

                var cosa = Element.Part<V_CosineFromDegrees>(pitch);
                var sina = Element.Part<V_SineFromDegrees>(pitch);

                var cosb = Element.Part<V_CosineFromDegrees>(yaw);
                var sinb = Element.Part<V_SineFromDegrees>(yaw);

                var Axx = cosa * cosb;
                var Axy = 0 - sina;
                var Axz = cosa * sinb;

                var Ayx = sina * cosb;
                var Ayy = cosa;
                var Ayz = sina * sinb;

                var Azx = -sinb;
                
                pos1 = Element.Part<V_Vector>(
                    Axx * pos1X +
                    Axy * pos1Y +
                    Axz * pos1Z,
                    Ayx * pos1X +
                    Ayy * pos1Y +
                    Ayz * pos1Z,
                    Azx * pos1X +
                    pos1Z
                );

                pos2 = Element.Part<V_Vector>(
                    Axx * pos2X +
                    Axy * pos2Y +
                    Axz * pos2Z,
                    Ayx * pos2X +
                    Ayy * pos2Y +
                    Ayz * pos2Z,
                    Azx * pos2X +
                    pos2Z
                );
            }
            else
            {
                pos1 = vertex1.ToVector();
                pos2 = vertex2.ToVector();
            }

            if (scale != null && !scaleSet)
            {
                pos1 = pos1 * scale;
                pos2 = pos2 * scale;
            }

            actionSet.AddAction(Element.Part<A_CreateBeamEffect>(
                visibleTo,
                EnumData.GetEnumValue(BeamType.GrappleBeam),
                location + pos1,
                location + pos2,
                EnumData.GetEnumValue(Elements.Color.Red),
                reevaluation
            ));
        }

        protected IWorkshopTree RenderText(
            ActionSet actionSet,
            string text, FontFamily font, double quality,
            Element visibleTo, Element location, Element rotation, Element scale, IWorkshopTree effectRev, bool getIds, double angleRound)
        {
            quality = Math.Max(10 - quality, 0.1);

            Model model = Model.ImportString(text, font, quality, angleRound);

            IndexReference effects = null;
            if (getIds)
            {
                effects = actionSet.VarCollection.Assign("_modelEffects", actionSet.IsGlobal, true);
                actionSet.AddAction(effects.SetVariable(new V_EmptyArray()));
            }
            
            RenderModel(actionSet, model, visibleTo, location, rotation, scale, effectRev, getIds);
            
            return effects?.GetVariable();
        }

        public static FontFamily GetFontFamily(ScriptFile script, DocRange range, string name)
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
                        script.Diagnostics.Error($"Failed to load the font {name} at '{filepath}'.", range);
                        return null;
                    }
                    family = LoadedFonts.Families.FirstOrDefault(fam => fam.Name.ToLower() == name.ToLower());
                    if (family == null)
                    {
                        script.Diagnostics.Error($"Failed to load the font {name} at '{filepath}'.", range);
                        return null;
                    }
                }

                return family;
            }
            
            if (!FontFamily.Families.Any(fam => fam.Name.ToLower() == name.ToLower()))
            {
                script.Diagnostics.Error($"The font {name} does not exist.", range);
                return null;
            }   
            return new FontFamily(name);
        }

        private static PrivateFontCollection LoadedFonts = null;
    }

    [CustomMethod("ShowWireframe", "Create a wireframe of a variable containing a 3D model.", CustomMethodType.MultiAction_Value, false)]
    class ShowModel : ModelCreator
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new ModelParameter("model", "File path of the model to use. Must be a `.obj` file."),
            new CodeParameter("visibleTo", "The array of players that the model will be visible to."),
            new CodeParameter("location", "The location that the model will be shown."),
            new CodeParameter("rotation", "The rotation of the model."),
            new CodeParameter("scale", "The scale of the model."),
            new CodeParameter("reevaluation", "Specifies which of this methods' inputs will be continuously reevaluated, the model will keep asking for and using new values from reevaluated inputs.", ValueGroupType.GetEnumType<EffectRev>()),
            new ConstBoolParameter("getEffectIDs", "If true, the method will return the effect IDs used to create the model. Use `DestroyEffectArray()` to destroy the effect. This is a boolean constant.", false)
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            Model model           = (Model)additionalParameterData[0];
            Element visibleTo           = (Element)parameterValues[1];
            Element location            = (Element)parameterValues[2];
            Element rotation            = (Element)parameterValues[3];
            Element scale               = (Element)parameterValues[4];
            EnumMember effectRev     = (EnumMember)parameterValues[5];
            bool getIds            = (bool)additionalParameterData[6];

            return RenderModel(actionSet, model, visibleTo, location, rotation, scale, effectRev, getIds);
        }
    }

    class ModelParameter : FileParameter
    {
        public ModelParameter(string parameterName, string description) : base(parameterName, description, ".obj") {}
    
        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
        {
            string filepath = base.Validate(parseInfo, value, valueRange) as string;
            if (filepath == null) return null;

            Model newModel;
            try
            {
                newModel = Model.ImportObj(File.ReadAllText(filepath));
            }
            catch (Exception)
            {
                parseInfo.Script.Diagnostics.Error("Failed to load the model.", valueRange);
                return null;
            }

            return newModel;
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData) => null;
    }

    [CustomMethod("CreateTextFont", "Creates in-world text using any custom text.", CustomMethodType.MultiAction_Value, false)]
    class CreateTextWithFont : ModelCreator
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new ConstStringParameter("text", "The text to display. This is a string constant."),
            new FontParameter("font", "The name of the font to use. This is a string constant."),
            new CodeParameter("visibleTo", "The array of players that the text will be visible to."),
            new CodeParameter("location", "The location to display the text."),
            new CodeParameter("rotation", "The rotation of the text."),
            new CodeParameter("scale", "The scale of the text."),
            new CodeParameter("reevaluation", "Specifies which of this methods inputs will be continuously reevaluated, the text will keep asking for and using new values from reevaluated inputs.", ValueGroupType.GetEnumType<EffectRev>()),
            new ConstBoolParameter("getEffectIDs", "If true, the method will return the effect IDs used to create the text. Use `DestroyEffectArray()` to destroy the effect. This is a boolean constant.", false)
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            string text = (string)additionalParameterData[0];
            FontFamily font = (FontFamily)additionalParameterData[1];
            Element visibleTo = (Element)parameterValues[2];
            Element location = (Element)parameterValues[3];
            Element rotation = (Element)parameterValues[4];
            Element scale = (Element)parameterValues[5];
            EnumMember effectRev = (EnumMember)parameterValues[6];
            bool getIds = (bool)additionalParameterData[7];

            return RenderText(actionSet, text, font, 9, visibleTo, location, rotation, scale, effectRev, getIds, 20);
        }
    }

    class FontParameter : ConstStringParameter
    {
        public FontParameter(string name, string documentation) : base(name, documentation) {}

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
        {
            string font = base.Validate(parseInfo, value, valueRange) as string;
            if (font == null) return null;
            return ModelCreator.GetFontFamily(parseInfo.Script, valueRange, font);
        }
    }

    [CustomMethod("CreateTextFancy", "Creates in-world text using any custom text. Uses the BigNoodleTooOblique font, Overwatch's main font.", CustomMethodType.MultiAction_Value, false)]
    class CreateTextFancy : ModelCreator
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new ConstStringParameter("text", "The text to display. This is a string constant."),
            new CodeParameter("visibleTo", "The array of players that the text will be visible to."),
            new CodeParameter("location", "The location to display the text."),
            new CodeParameter("rotation", "The rotation of the text."),
            new CodeParameter("scale", "The scale of the text."),
            new CodeParameter("reevaluation", "Specifies which of this methods inputs will be continuously reevaluated, the text will keep asking for and using new values from reevaluated inputs.", ValueGroupType.GetEnumType<EffectRev>()),
            new ConstBoolParameter("getEffectIDs", "If true, the method will return the effect IDs used to create the text. Use `DestroyEffectArray()` to destroy the effect. This is a boolean constant.", false)
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            string text = (string)additionalParameterData[0];
            Element visibleTo = (Element)parameterValues[1];
            Element location = (Element)parameterValues[2];
            Element rotation = (Element)parameterValues[3];
            Element scale = (Element)parameterValues[4];
            EnumMember effectRev = (EnumMember)parameterValues[5];
            bool getIds = (bool)additionalParameterData[6];

            return RenderText(actionSet, text, GetFontFamily(null, null, "BigNoodleTooOblique"), 9, visibleTo, location, rotation, scale, effectRev, getIds, 0);
        }
    }

    [CustomMethod("CreateText", "The text to display. This is a string constant.", CustomMethodType.MultiAction_Value, false)]
    class CreateText : ModelCreator
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new EconomicTextParameter("text", "The text to display. This is a string constant."),
            new CodeParameter("visibleTo", "The array of players that the text will be visible to."),
            new CodeParameter("location", "The location to display the text."),
            new CodeParameter("rotation", "The rotation of the text."),
            new CodeParameter("scale", "The scale of the text."),
            new CodeParameter("reevaluation", "Specifies which of this methods inputs will be continuously reevaluated, the text will keep asking for and using new values from reevaluated inputs.", ValueGroupType.GetEnumType<EffectRev>()),
            new ConstBoolParameter("getEffectIDs", "If true, the method will return the effect IDs used to create the text. Use `DestroyEffectArray()` to destroy the effect. This is a boolean constant.", false)
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            Model text = (Model)additionalParameterData[0];
            Element visibleTo = (Element)parameterValues[1];
            Element location = (Element)parameterValues[2];
            Element rotation = (Element)parameterValues[3];
            Element scale = (Element)parameterValues[4];
            EnumMember effectRev = (EnumMember)parameterValues[5];
            bool getIds = (bool)additionalParameterData[6];

            return RenderModel(actionSet, text, visibleTo, location, rotation, scale, effectRev, getIds);
        }
    }

    class EconomicTextParameter : ConstStringParameter
    {
        public EconomicTextParameter(string name, string documentation) : base(name, documentation) {}

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
        {
            string text = base.Validate(parseInfo, value, valueRange) as string;
            if (text == null) return null;

            var lines = Letter.Create(text, false, parseInfo.Script, valueRange);
            if (lines == null) return null;
            return new Model(lines);
        }
    }

    class VertexParameter : CodeParameter
    {
        public VertexParameter(string name, string documentation) : base(name, documentation) {}

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
        {
            if (value is NullAction) return new Vertex();
            else if (value is NumberAction) return new Vertex(((NumberAction)value).Value, 0);
            else if (value is CallMethodAction && GetVertex((CallMethodAction)value, out Vertex vertex)) return vertex;

            parseInfo.Script.Diagnostics.Error("Expected a vector constant, number constant, or null.", valueRange);
            return null;
        }

        private static bool GetVertex(CallMethodAction callMethod, out Vertex vertex)
        {
            vertex = null;

            if (callMethod.CallingMethod == ElementList.GetElement<V_Vector>())
            {
                double x = 0, y = 0, z = 0;

                if (callMethod.ParameterValues[0] != null)
                {
                    var num = callMethod.ParameterValues[0] as NumberAction;
                    if (num == null) return false;
                    x = num.Value;
                }
                if (callMethod.ParameterValues[1] != null)
                {
                    var num = callMethod.ParameterValues[1] as NumberAction;
                    if (num == null) return false;
                    y = num.Value;
                }
                if (callMethod.ParameterValues[2] != null)
                {
                    var num = callMethod.ParameterValues[2] as NumberAction;
                    if (num == null) return false;
                    z = num.Value;
                }

                vertex = new Vertex(x, y, z);
                return true;
            }
            return false;
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData) => null;
    }
}