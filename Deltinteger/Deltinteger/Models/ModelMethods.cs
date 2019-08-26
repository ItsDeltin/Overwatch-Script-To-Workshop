using System;
using System.Collections.Generic;
using Deltin.Deltinteger;
using Deltin.Deltinteger.Models.Import;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Models
{
    [CustomMethod("ShowWireframe", CustomMethodType.Action)]
    [VarRefParameter("Model")]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [Parameter("Scale", Elements.ValueType.Number, null)]
    class ShowModel : CustomMethodBase
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
            {
                actions.Add(
                    Element.Part<A_CreateBeamEffect>(
                        visibleTo,
                        EnumData.GetEnumValue(BeamType.GrappleBeam),
                        Element.Part<V_Add>(location, Element.Part<V_Multiply>(line.Vertex1.ToVector(), scale)),
                        Element.Part<V_Add>(location, Element.Part<V_Multiply>(line.Vertex2.ToVector(), scale)),
                        EnumData.GetEnumValue(Color.LimeGreen),
                        EnumData.GetEnumValue(EffectRev.VisibleToPositionAndRadius)
                    )
                );
            }

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
}