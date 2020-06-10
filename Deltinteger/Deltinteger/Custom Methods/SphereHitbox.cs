using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("LinePlaneIntersection", "Gets the point where a line intersects with an infinite plane.", CustomMethodType.Value)]
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

            return linePos + Element.Part<V_Normalize>(lineDirection) * ((Element.Part<V_DotProduct>(planeNormal, planePos) - Element.Part<V_DotProduct>(planeNormal, linePos)) / Element.Part<V_DotProduct>(planeNormal, Element.Part<V_Normalize>(lineDirection)));
        }
    }

    [CustomMethod("DoesLineIntersectSphere", "Determines if a point intersects with a sphere.", CustomMethodType.Value)]
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

            Element distanceToSphere = Element.Part<V_DistanceBetween>(linePos, spherePos);
            Element checkPos = linePos + Element.Part<V_Normalize>(lineDirection) * distanceToSphere;

            return Element.Part<V_DistanceBetween>(checkPos, spherePos) < sphereRadius;
        }
    }

    [CustomMethod("SphereHitboxRaycast", "Whether the given player is looking directly at a sphere with collision.", CustomMethodType.Value)]
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
            Element eyePos = Element.Part<V_EyePosition>(player);
            Element range = Element.Part<V_DistanceBetween>(eyePos, position);
            Element direction = Element.Part<V_FacingDirectionOf>(player);
            Element raycast = Element.Part<V_RayCastHitPosition>(
                eyePos,
                eyePos + direction * range,
                new V_AllPlayers(),
                new V_Null(),
                new V_False()
            );
            Element distance = Element.Part<V_DistanceBetween>(position, raycast);
            Element compare = distance <= radius;
            return compare;
        }
    }
}