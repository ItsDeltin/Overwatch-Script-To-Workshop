using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using Deltin.Deltinteger;
using Deltin.Deltinteger.Models.Import;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Models
{
    abstract class ModelCreator : CustomMethodBase
    {
        protected const bool GET_EFFECT_IDS_BY_DEFAULT = true;

        protected Element[] RenderModel(Model model, Element visibleTo, Element location, Element scale, IndexedVar store)
        {
            List<Element> actions = new List<Element>();
            for (int i = 0; i < model.Lines.Length; i++)
            {
                actions.Add(CreateLine(model.Lines[i], visibleTo, location, scale));
                if (store != null)
                    actions.AddRange(
                        store.SetVariable(Element.Part<V_Append>(store.GetVariable(), new V_LastCreatedEntity()))
                    );

                if (actions.Count % 25 == 0)
                    actions.Add(A_Wait.MinimumWait);
            }
            return actions.ToArray();
        }

        protected Element CreateLine(Line line, Element visibleTo, Element location, Element scale)
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
                EnumData.GetEnumValue(Elements.Color.LimeGreen),
                EnumData.GetEnumValue(EffectRev.VisibleToPositionAndRadius)
            );
        }
    }

    [CustomMethod("ShowWireframe", CustomMethodType.MultiAction_Value)]
    [VarRefParameter("Model")]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [Parameter("Scale", Elements.ValueType.Number, null)]
    [ConstantParameter("Get Effect IDs", typeof(bool), GET_EFFECT_IDS_BY_DEFAULT)]
    class ShowModel : ModelCreator
    {
        override protected MethodResult Get()
        {
            if (((VarRef)Parameters[0]).Var is ModelVar == false)
                throw new SyntaxErrorException("Variable must reference a model.", ParameterLocations[0]);
            
            ModelVar modelVar = (ModelVar)((VarRef)Parameters[0]).Var;
            Element visibleTo           = (Element)Parameters[1];
            Element location            = (Element)Parameters[2];
            Element scale               = (Element)Parameters[3];
            bool getIds   = (bool)((ConstantObject)Parameters[4]).Value;

            List<Element> actions = new List<Element>();

            IndexedVar effects = null;
            if (getIds)
            {
                effects = TranslateContext.VarCollection.AssignVar(Scope, "Model Effects", TranslateContext.IsGlobal, null);
                actions.AddRange(effects.SetVariable(new V_EmptyArray()));
            }

            actions.AddRange(RenderModel(modelVar.Model, visibleTo, location, scale, effects));

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
                "If true, the method will return the effect IDs used to create the model. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }

    [CustomMethod("CreateText", CustomMethodType.MultiAction_Value)]
    [ConstantParameter("Text", typeof(string))]
    [ConstantParameter("Font", typeof(string))]
    [ConstantParameter("Quality", typeof(double))]
    [ConstantParameter("Angle", typeof(double))]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [ConstantParameter("Scale", typeof(double), 1.0)]
    [ConstantParameter("Get Effect IDs", typeof(bool), GET_EFFECT_IDS_BY_DEFAULT)]
    class CreateTextModel : ModelCreator
    {
        override protected MethodResult Get()
        {
            string text    = (string)((ConstantObject)Parameters[0]).Value;
            string font    = (string)((ConstantObject)Parameters[1]).Value;
            double quality = (double)((ConstantObject)Parameters[2]).Value;
            double angle   = (double)((ConstantObject)Parameters[3]).Value + 22.2;
            Element visibleTo              = (Element)Parameters[4];
            Element location               = (Element)Parameters[5];
            //Element scale                  = (Element)Parameters[6];
            double scale   = (double)((ConstantObject)Parameters[6]).Value;
            bool getIds    = (bool)  ((ConstantObject)Parameters[7]).Value;

            quality = Math.Max(10 - quality, 0.1);

            if (!FontFamily.Families.Any(fam => fam.Name.ToLower() == font.ToLower()))
                throw new SyntaxErrorException("The '" + font + "' font does not exist.", ParameterLocations[1]);

            Model model = Model.ImportString(text, new FontFamily(font), quality, angle, scale);

            List<Element> actions = new List<Element>();

            IndexedVar effects = null;
            if (getIds)
            {
                effects = TranslateContext.VarCollection.AssignVar(Scope, "Model Effects", TranslateContext.IsGlobal, null);
                actions.AddRange(effects.SetVariable(new V_EmptyArray()));
            }
                
            actions.AddRange(RenderModel(model, visibleTo, location, null, effects));
            
            return new MethodResult(actions.ToArray(), effects?.GetVariable());
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Creates in-world text using any custom text.",
                // Parameters
                "The text to display. This is a string constant.",
                "The name of the font to use. This is a string constant.",
                "The quality of the font. The value must be between 0-10. Higher numbers creates more effects. This is a number constant.",
                "The angle of the text. This is a number constant.",
                "Who the text is visible to.",
                "The location to display the text.",
                "The scale of the text.",
                "If true, the method will return the effect IDs used to create the text. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }
}