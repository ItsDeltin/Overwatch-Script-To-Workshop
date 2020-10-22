using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("LinePlaneIntersection", "Gets the point where a line intersects with an infinite plane.", CustomMethodType.Value, typeof(VectorType))]
    class LinePlaneIntersection : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("linePos", "A point on the line."),
            new CodeParameter("lineDirection", "The directional vector of the line."),
            new CodeParameter("planePos", "A position on the plane."),
            new CodeParameter("planeNormal", "The normal of the plane.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {            
            Element linePos = (Element)parameterValues[0];
            Element lineDirection = (Element)parameterValues[1];
            Element planePos = (Element)parameterValues[2];
            Element planeNormal = (Element)parameterValues[3];

            return linePos + Element.Normalize(lineDirection) * ((Element.DotProduct(planeNormal, planePos) - Element.DotProduct(planeNormal, linePos)) / Element.DotProduct(planeNormal, Element.Normalize(lineDirection)));
        }
    }

    [CustomMethod("DoesLineIntersectSphere", "Determines if a point intersects with a sphere.", CustomMethodType.Value, typeof(BooleanType))]
    class LineSphereIntersection : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("linePos", "The starting point of the line"),
            new CodeParameter("lineDirection", "The direction of the line."),
            new CodeParameter("spherePos", "The position of the sphere."),
            new CodeParameter("sphereRadius", "The radius of a sphere.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            Element linePos = (Element)parameterValues[0];
            Element lineDirection = (Element)parameterValues[1];
            Element spherePos = (Element)parameterValues[2];
            Element sphereRadius = (Element)parameterValues[3];

            Element distanceToSphere = Element.DistanceBetween(linePos, spherePos);
            Element checkPos = linePos + Element.Normalize(lineDirection) * distanceToSphere;

            return Element.DistanceBetween(checkPos, spherePos) < sphereRadius;
        }
    }

    [CustomMethod("SphereHitboxRaycast", "Whether the given player is looking directly at a sphere with collision.", CustomMethodType.Value, typeof(BooleanType))]
    class SphereHitboxRaycastPlayer : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("player", "The player."),
            new CodeParameter("spherePosition", "The position of the sphere."),
            new CodeParameter("sphereRadius", "The radius of the sphere.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            Element player = (Element)parameterValues[0];
            Element position = (Element)parameterValues[1];
            Element radius = (Element)parameterValues[2];
            Element eyePos = Element.EyePosition(player);
            Element range = Element.DistanceBetween(eyePos, position);
            Element direction = Element.FacingDirectionOf(player);
            Element raycast = Element.RaycastPosition(
                eyePos,
                eyePos + direction * range,
                Element.Part("All Players")
            );
            Element distance = Element.DistanceBetween(position, raycast);
            Element compare = distance <= radius;
            return compare;
        }
    }
}
