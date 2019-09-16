using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Assets.Models;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Assets.Images
{
    abstract class ImageCreator : CustomMethodBase
    {
        protected Element CreatePixel(EffectPixel pixel, int width, int height, Element visibleTo, Element location, Element scale, IWorkshopTree reevaluation, Element rotation)
        {
            //offsets pixels so the image is centered around a center point
            Vertex vertexPos = new Vertex(
                -(pixel.PositionX - (double)width / 2),
                -(pixel.PositionY - (double)height / 2),
            0);

            Element radius = Element.Part<V_Multiply>(scale, new V_Number(0.5));
            Element position;

            if (rotation.ConstantSupported<Vertex>())
            {
                vertexPos = vertexPos.Rotate((Vertex)rotation.GetConstant());
                position = vertexPos.ToVector();
            }
            else
            {
                var posX = new V_Number(vertexPos.X);
                var posY = new V_Number(vertexPos.Y);
                var posZ = new V_Number(vertexPos.Z);

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

                position = Element.Part<V_Vector>(
                    Element.Part<V_Add>(Element.Part<V_Add>(
                        Element.Part<V_Multiply>(Axx, posX),
                        Element.Part<V_Multiply>(Axy, posY)),
                        Element.Part<V_Multiply>(Axz, posZ)),
                    Element.Part<V_Add>(Element.Part<V_Add>(
                        Element.Part<V_Multiply>(Ayx, posX),
                        Element.Part<V_Multiply>(Ayy, posY)),
                        Element.Part<V_Multiply>(Ayz, posZ)),
                    Element.Part<V_Add>(
                        Element.Part<V_Multiply>(Azx, posX),
                        posZ)
                );
            }

            if (scale.ConstantSupported<double>())
            {
                radius = new V_Number((double)scale.GetConstant() * 0.5);

                if (position.ConstantSupported<Vertex>())
                    position = ((Vertex)position.GetConstant()).Scale((double)scale.GetConstant()).ToVector();
                else
                    position = Element.Part<V_Multiply>(position, scale);
            }
            else
            {
                position = Element.Part<V_Multiply>(position, scale);
            }

            return Element.Part<A_CreateEffect>(
                visibleTo,
                EnumData.GetEnumValue(Effect.Sphere),
                EnumData.GetEnumValue(pixel.Color),
                Element.Part<V_Add>(
                    position,
                    location
                ),
                radius,
                reevaluation
            );
        }

        protected Element[] CreateImage(EffectImage image, Element visibleTo, Element location, Element scale, IWorkshopTree reevaluation, Element rotation, IndexedVar store)
        {
            List<Element> elements = new List<Element>();
            for (int x = 0; x < image.Pixels.GetLength(0); x++)
            {
                for (int y = 0; y < image.Pixels.GetLength(1); y++)
                {
                    elements.Add(CreatePixel(image.Pixels[x, y], image.Pixels.GetLength(0), image.Pixels.GetLength(1), visibleTo, location, scale, reevaluation, rotation));
                    if (store != null)
                        elements.AddRange(store.SetVariable(Element.Part<V_Append>(store.GetVariable(), new V_LastCreatedEntity())));
                }
            }

            int waitEvery = rotation.ConstantSupported<Vertex>() ? 24 : 12;

            List<Element> temp = new List<Element>();
            for (int i = 0; i < elements.Count; i++)
            {
                temp.Add(elements[i]);
                if ((i + 1) % waitEvery == 0)
                    temp.Add(A_Wait.MinimumWait);
            }
            elements = temp;

            return elements.ToArray();
        }
    }

    [CustomMethod("ShowImage", CustomMethodType.MultiAction_Value)]
    [VarRefParameter("Image")]
    [Parameter("Visible To", Elements.ValueType.Player, null)]
    [Parameter("Location", Elements.ValueType.Vector, null)]
    [Parameter("Scale", Elements.ValueType.Number, null)]
    [Parameter("Rotation", Elements.ValueType.Vector, null)]
    [EnumParameter("Reevaluation", typeof(EffectRev))]
    [ConstantParameter("Get Effect IDs", typeof(bool), ModelCreator.GET_EFFECT_IDS_BY_DEFAULT)]
    class ShowImage : ImageCreator
    {
        protected override MethodResult Get()
        {
            ImageVar imageVar = (ImageVar)((VarRef)Parameters[0]).Var;
            Element visibleTo = (Element)Parameters[1];
            Element location = (Element)Parameters[2];
            Element scale = (Element)Parameters[3];
            Element rotation = (Element)Parameters[4];
            EnumMember effectRev = (EnumMember)Parameters[5];
            bool getIds = (bool)((ConstantObject)Parameters[6]).Value;

            List<Element> actions = new List<Element>();

            IndexedVar effects = null;
            if (getIds)
            {
                effects = TranslateContext.VarCollection.AssignVar(Scope, "Image Effects", TranslateContext.IsGlobal, null);
                actions.AddRange(effects.SetVariable(new V_EmptyArray()));
            }

            actions.AddRange(CreateImage(imageVar.Image, visibleTo, location, scale, effectRev, rotation, effects));

            return new MethodResult(actions.ToArray(), effects?.GetVariable());
        }

        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki(
                "Displays an image using effects.",
                "The image to display.",
                "Who the image will be visible to.",
                "The location of the image.",
                "The size of the image. Recommended value: 0.1",
                "The rotation of the model as a directional vector. If it is a vector constant, the rotation will be pre-calulated and will consume less server load (much less).",
                "Specifies which of this methods' inputs will be continuously reevaluated, the model will keep asking for and using new values from reevaluated inputs.",
                "If true, the method will return the effect IDs used to create the model. Use DestroyEffectArray() to destroy the effect. This is a boolean constant."
            );
        }
    }
}
