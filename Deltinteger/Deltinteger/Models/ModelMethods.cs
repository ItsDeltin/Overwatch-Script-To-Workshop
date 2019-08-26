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
        protected Element[] RenderModel(Model model, Element visibleTo, Element location, Element scale, TranslateRule translateContext, ScopeGroup scope)
        {
            IndexedVar effects = null;
            if (translateContext != null)
                effects = translateContext.VarCollection.AssignVar(scope, "Model Effects", translateContext.IsGlobal, null);

            List<Element> actions = new List<Element>();
            for (int i = 0; i < model.Lines.Length; i++)
            {
                actions.Add(CreateLine(model.Lines[i], visibleTo, location, scale));
                if (effects != null)
                    actions.AddRange(
                        effects.SetVariable(Element.Part<V_Append>(effects.GetVariable(), new V_LastCreatedEntity()))
                    );
            }
            return actions.ToArray();
        }

        protected Element CreateLine(Element vec1, Element vec2, Element visibleTo, Element location, Element scale)
        {
            return Element.Part<A_CreateBeamEffect>(
                visibleTo,
                EnumData.GetEnumValue(BeamType.GrappleBeam),
                Element.Part<V_Add>(location, Element.Part<V_Multiply>(vec1, scale)),
                Element.Part<V_Add>(location, Element.Part<V_Multiply>(vec2, scale)),
                EnumData.GetEnumValue(Elements.Color.LimeGreen),
                EnumData.GetEnumValue(EffectRev.VisibleToPositionAndRadius)
            );
        }

        protected Element CreateLine(Line line, Element visibleTo, Element location, Element scale)
        {
            return CreateLine(line.Vertex1.ToVector(), line.Vertex2.ToVector(), visibleTo, location, scale);
        }

        protected Element CreateLine(Line line, Element visibleTo, Element location)
        {
            return CreateLine(line, visibleTo, location, new V_Number(1));
        }
    }

    [CustomMethod("ShowWireframe", CustomMethodType.Action)]
    [VarRefParameter("Model")]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [Parameter("Scale", Elements.ValueType.Number, null)]
    class ShowModel : ModelCreator
    {
        override protected MethodResult Get()
        {
            if (((VarRef)Parameters[0]).Var is ModelVar == false)
                throw new SyntaxErrorException("", null);
            
            ModelVar modelVar = (ModelVar)((VarRef)Parameters[0]).Var;
            Element visibleTo = (Element)Parameters[1];
            Element location = (Element)Parameters[2];
            Element scale = (Element)Parameters[3];

            List<Element> actions = new List<Element>();
            foreach (Line line in modelVar.Model.Lines)
                actions.Add(CreateLine(line, visibleTo, location, scale));

            return new MethodResult(actions.ToArray(), null);
        }

        override public CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Create a wireframe of a variable containing a 3D model.",
                // Parameters
                "The variable containing the model constant.",
                "Who the model is visible to.",
                "The location of the model.",
                "The scale of the model."
            );
        }
    }

    [CustomMethod("CreateText", CustomMethodType.Action)]
    [ConstantParameter("Text", typeof(string))]
    [ConstantParameter("Font", typeof(string))]
    [ConstantParameter("Quality", typeof(double))]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [ConstantParameter("Angle", typeof(double))]
    class CreateTextModel : ModelCreator
    {
        override protected MethodResult Get()
        {
            string text = (string)((ConstantObject)Parameters[0]).Value;
            string font = (string)((ConstantObject)Parameters[1]).Value;
            double quality = (double)((ConstantObject)Parameters[2]).Value;
            double angle = (double)((ConstantObject)Parameters[5]).Value + 22.2;

            quality = Math.Max(10 - quality, 0.1);

            if (!FontFamily.Families.Any(fam => fam.Name.ToLower() == font.ToLower()))
                throw new SyntaxErrorException("The '" + font + "' font does not exist.", ParameterLocations[1]);

            Model model = Model.ImportString(text, new FontFamily(font), quality, angle);

            Element visibleTo = (Element)Parameters[3];
            Element location = (Element)Parameters[4];

            List<Element> actions = new List<Element>();
            actions.AddRange(RenderModel(model, visibleTo, location, new V_Number(1), null, null));
            
            return new MethodResult(actions.ToArray(), null);
        }

        override public CustomMethodWiki Wiki()
        {
            return null;
        }
    }
}