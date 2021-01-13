using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod LinePlaneIntersection(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "LinePlaneIntersection",
            Documentation = "Gets the point where a line intersects with an infinite plane.",
            Parameters = new[] {
                new CodeParameter("linePos", "A point on the line.", deltinScript.Types.Vector()),
                new CodeParameter("lineDirection", "The directional vector of the line.", deltinScript.Types.Vector()),
                new CodeParameter("planePos", "A position on the plane.", deltinScript.Types.Vector()),
                new CodeParameter("planeNormal", "The normal of the plane.", deltinScript.Types.Vector())
            },
            ReturnType = deltinScript.Types.Vector(),
            Action = (actionSet, methodCall) => {
                Element linePos = methodCall.Get(0), lineDirection = methodCall.Get(1), planePos = methodCall.Get(2), planeNormal = methodCall.Get(3);
                return linePos + Element.Normalize(lineDirection) * ((Element.DotProduct(planeNormal, planePos) - Element.DotProduct(planeNormal, linePos)) / Element.DotProduct(planeNormal, Element.Normalize(lineDirection)));
            }
        };

        public static FuncMethod DoesLineIntersectSphere(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "DoesLineIntersectSphere",
            Documentation = "Determines if a point intersects with a sphere.",
            Parameters = new[] {
                new CodeParameter("linePos", "The starting point of the line", deltinScript.Types.Vector()),
                new CodeParameter("lineDirection", "The direction of the line.", deltinScript.Types.Vector()),
                new CodeParameter("spherePos", "The position of the sphere.", deltinScript.Types.Vector()),
                new CodeParameter("sphereRadius", "The radius of a sphere.", deltinScript.Types.Number())
            },
            ReturnType = deltinScript.Types.Boolean(),
            Action = (actionSet, methodCall) => {
                Element linePos = methodCall.Get(0), lineDirection = methodCall.Get(1), spherePos = methodCall.Get(2), sphereRadius = methodCall.Get(3);

                Element distanceToSphere = Element.DistanceBetween(linePos, spherePos);
                Element checkPos = linePos + Element.Normalize(lineDirection) * distanceToSphere;

                return Element.DistanceBetween(checkPos, spherePos) < sphereRadius;
            }
        };

        public static FuncMethod SphereHitboxRaycast(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "SphereHitboxRaycast",
            Documentation = "Whether the given player is looking directly at a sphere with collision.",
            Parameters = new CodeParameter[] {
                new CodeParameter("player", "The player to do the raycast with.", deltinScript.Types.Player()),
                new CodeParameter("spherePosition", "The position of the sphere.", deltinScript.Types.Vector()),
                new CodeParameter("sphereRadius", "The radius of the sphere.", deltinScript.Types.Number())
            },
            ReturnType = deltinScript.Types.Boolean(),
            Action = (actionSet, methodCall) => {
                Element player = methodCall.Get(0), position = methodCall.Get(1), radius = methodCall.Get(2),
                    eyePos = Element.EyePosition(player),
                    range = Element.DistanceBetween(eyePos, position),
                    direction = Element.FacingDirectionOf(player),
                    raycast = Element.RaycastPosition(
                        eyePos,
                        eyePos + direction * range,
                        Element.Part("All Players")
                    ),
                    distance = Element.DistanceBetween(position, raycast),
                    compare = distance <= radius;
                return compare;
            }
        };
    }
}