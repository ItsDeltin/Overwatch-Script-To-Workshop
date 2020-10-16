using System;
using System.Collections.Generic;
using Vertex = Deltin.Deltinteger.Models.Vertex;

namespace Deltin.Deltinteger.Elements
{
    public static class OptimizeElements
    {
		static readonly List<String> vecFunctions = new List<String> {"Vector", "Left", "Right", "Up", "Down", "Velocity Of", "Facing Direction" };
        public static readonly Dictionary<string, Func<Element, Element>> Optimizations = new Dictionary<string, Func<Element, Element>>() {
            {"Absolute Value", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Abs(a.Value);
                return element;
            }},
            {"Add", element => OptimizeAddOperation(
                element,
                op       : (a, b) => a + b,
                areEqual : (a, b) => a * 2,
                true
            )},
            {"And", element => {
                Element a = (Element)element.ParameterValues[0];
                Element b = (Element)element.ParameterValues[1];

                if (a.TryGetConstant(out bool ac) && b.TryGetConstant(out bool bc))
                    return ac && bc;

                if (a.EqualTo(b))
                    return a;

                if (a.Function.Name == "Not")
                {
                    if (b.EqualTo(a.ParameterValues[0]))
                        return false;
                }
                else if (b.Function.Name == "Not")
                {
                    if (a.EqualTo(b.ParameterValues[0]))
                        return false;
                }

                return element;
            }},
            {"Angle Between Vectors", element => {
                Element a = (Element)element.ParameterValues[0];
                Element b = (Element)element.ParameterValues[1];

                if (a.TryGetConstant(out Vertex vertexA) && b.TryGetConstant(out Vertex vertexB))
                    return Math.Acos(vertexA.DotProduct(vertexB) / (vertexA.Length * vertexB.Length)) * (180 / Math.PI);

                return element;
            }},
            {"Angle Difference", element => {
                if (element.ParameterValues[0] is NumberElement a && element.ParameterValues[1] is NumberElement b)
                {
                    double diff = Math.Abs(a.Value - b.Value) % 360;
                    if (diff > 180) diff = 360 - diff;
                    return diff;
                }
                return element;
            }},
            {"Arccosine In Degrees", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Acos(a.Value) * (180.0 / Math.PI);
                return element;
            }},
            {"Arccosine In Radians", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Acos(a.Value);
                return element;
            }},
            {"Arcsine In Degrees", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Asin(a.Value) * (180 / Math.PI);
                return element;
            }},
            {"Arcsine In Radians", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Asin(a.Value);
                return element;
            }},
            {"Arctangent In Degrees", element => {
                if (element.ParameterValues[0] is NumberElement a && element.ParameterValues[1] is NumberElement b)
                    return Math.Atan2(a.Value, b.Value) * (180 / Math.PI);
                return element;
            }},
            {"Arctangent In Radians", element => {
                if (element.ParameterValues[0] is NumberElement a && element.ParameterValues[1] is NumberElement b)
                    return Math.Atan2(a.Value, b.Value);
                return element;
            }},
            {"Compare", element => {
                Element left = (Element)element.ParameterValues[0];
                OperatorElement op = (OperatorElement)element.ParameterValues[1];
                Element right = (Element)element.ParameterValues[2];

                if (op.Operator == Operator.Equal)
                {
                    if (left.EqualTo(right))
                        return true;
                    if (left is NumberElement a && right is NumberElement b)
                        return a.Value == b.Value;
                }
                else if (op.Operator == Operator.GreaterThan)
                {
                    if (left.EqualTo(right))
                        return false;
                    if (left is NumberElement a && right is NumberElement b)
                        return a.Value > b.Value;
                }
                else if (op.Operator == Operator.GreaterThanOrEqual)
                {
                    if (left.EqualTo(right))
                        return true;
                    if (left is NumberElement a && right is NumberElement b)
                        return a.Value >= b.Value;
                }
                else if (op.Operator == Operator.LessThan)
                {
                    if (left.EqualTo(right))
                        return false;
                    if (left is NumberElement a && right is NumberElement b)
                        return a.Value < b.Value;
                }
                else if (op.Operator == Operator.LessThanOrEqual)
                {
                    if (left.EqualTo(right))
                        return true;
                    if (left is NumberElement a && right is NumberElement b)
                        return a.Value <= b.Value;
                }
                else if (op.Operator == Operator.NotEqual)
                {
                    if (left is NumberElement a && right is NumberElement b)
                        return a.Value != b.Value;
                    if (left.EqualTo(right))
                        return false;
                }

                return element;
            }},
            {"Cosine From Degrees", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Cos(a.Value * (Math.PI / 180));
                return element;
            }},
            {"Cosine From Radians", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Cos(a.Value);
                return element;
            }},
            {"Cross Product", element => {
                Element a = (Element)element.ParameterValues[0];
                Element b = (Element)element.ParameterValues[1];

                if (a.TryGetConstant(out Vertex vertexA) && b.TryGetConstant(out Vertex vertexB))
                    return vertexA.CrossProduct(vertexB).RemoveNaNs().ToVector();

                return element;
            }},
            {"Direction From Angles", element => {
                if (element.ParameterValues[0] is NumberElement a && element.ParameterValues[1] is NumberElement b)
                {
                    double h = a.Value;
                    double v = b.Value;

                    double x = Math.Sin(h * (Math.PI / 180));
                    double y = -Math.Sin(v * (Math.PI / 180));
                    double z = Math.Cos(h * (Math.PI / 180));

                    if (y == -0)
                        y = 0;

                    return Element.Vector(x, y, z);
                }

                return element;
            }},
            {"Direction Towards", element => {
                Element a = (Element)element.ParameterValues[0];
                Element b = (Element)element.ParameterValues[1];

                if (a.TryGetConstant(out Vertex vertexA) && b.TryGetConstant(out Vertex vertexB))
                    return vertexA.DirectionTowards(vertexB).RemoveNaNs().ToVector();
                return element;
            }},
            {"Distance Between", element => {
                Element a = (Element)element.ParameterValues[0];
                Element b = (Element)element.ParameterValues[1];

                if (a.TryGetConstant(out Vertex vertexA) && b.TryGetConstant(out Vertex vertexB))
                    return vertexA.DistanceTo(vertexB);
                return element;
            }},
            {"Divide", element => OptimizeMultiplyOperation(
                element,
                op      : (a, b) => a / b,
                areEqual: (a, b) => 1,
                false
            )},
            {"Dot Product", element => {
                Element a = (Element)element.ParameterValues[0];
                Element b = (Element)element.ParameterValues[1];

                if (a.TryGetConstant(out Vertex vertexA) && b.TryGetConstant(out Vertex vertexB))
                    return vertexA.DotProduct(vertexB);
                return element;
            }},
            {"Horizontal Angle From Direction", element => {
                Element a = (Element)element.ParameterValues[0];

                if (a.TryGetConstant(out Vertex vert))
                {
                    double gradient = vert.X / vert.Z;
                    if (double.IsNaN(gradient))
                        gradient = 0;
                    double result = Math.Atan(gradient) * (180 / Math.PI);
                    if (result == -0) //thank you c# for -0 being a thing
                        result = 180;
                    return result;
                }
                return element;
            }},
            {"Max", element => {
                Element a = (Element)element.ParameterValues[0];
                Element b = (Element)element.ParameterValues[1];

                if (a is NumberElement aNum && b is NumberElement bNum)
                    return Math.Max(aNum.Value, bNum.Value);
                return element;
            }},
            {"Min", element => {
                Element a = (Element)element.ParameterValues[0];
                Element b = (Element)element.ParameterValues[1];

                if (a is NumberElement aNum && b is NumberElement bNum)
                    return Math.Min(aNum.Value, bNum.Value);
                return element;
            }},
            {"Modulo", element => {
                NumberElement a = element.ParameterValues[0] as NumberElement;
                NumberElement b = element.ParameterValues[1] as NumberElement;

                if (a != null && b != null)
                    return a.Value % b.Value;

                if (a != null)
                {
                    if (a.Value == 0) return 0;
                    if (a.Value == 1) return 1;
                }

                if (b != null && b.Value == 0) return 0;

                if (((Element)element.ParameterValues[0]).EqualTo(element.ParameterValues[1])) return 0;
                
                return element;
            }},
            {"Multiply", element => OptimizeMultiplyOperation(
                element,
                op      : (a, b) => a * b,
                areEqual: (a, b) => Element.Pow(a, 2),
                true
            )},
            {"Normalize", element => {
                Element a = (Element)element.ParameterValues[0];

                if (a.TryGetConstant(out Vertex vertexA))
                    return vertexA.Normalize().ToVector();

                return element;
            }},
            {"Not", element => {
                Element a = (Element)element.ParameterValues[0];

                if (a.Function.Name == "True")
                    return false;

                if (a.Function.Name == "False")
                    return true;

                if (a.Function.Name == "Not")
                    return (Element)a.ParameterValues[0];

                if (a.Function.Name == "Compare")
                {
                    Operator op = ((OperatorElement)a.ParameterValues[1]).Operator;
                    IWorkshopTree left = a.ParameterValues[0];
                    IWorkshopTree right = a.ParameterValues[2];
                    if (op == Operator.Equal)
                        return Element.Compare(left, Operator.NotEqual, right);
                    else if (op == Operator.GreaterThan)
                        return Element.Compare(left, Operator.LessThanOrEqual, right);
                    else if (op == Operator.GreaterThanOrEqual)
                        return Element.Compare(left, Operator.LessThan, right);
                    else if (op == Operator.LessThan)
                        return Element.Compare(left, Operator.GreaterThanOrEqual, right);
                    else if (op == Operator.LessThanOrEqual)
                        return Element.Compare(left, Operator.GreaterThan, right);
                    else if (op == Operator.NotEqual)
                        return Element.Compare(left, Operator.Equal, right);
                }

                return element;
            }},
            {"Or", element => {
                Element a = (Element)element.ParameterValues[0];
                Element b = (Element)element.ParameterValues[1];

                if (a.TryGetConstant(out bool ac) && b.TryGetConstant(out bool ab))
                    return ac || ab;

                // If either condition is already true, return true. This may or may not work due to short-circuiting.
                if (a.Function.Name == "True" || b.Function.Name == "True") return true;
                
                if (a.EqualTo(b)) return a;

                if (a.Function.Name == "Not")
                    if (b.EqualTo(a.ParameterValues[0]))
                        return true;

                if (b.Function.Name == "Not")
                    if (a.EqualTo(b.ParameterValues[0]))
                        return true;

                return element;
            }},
            {"Raise To Power", element => {
                NumberElement a = element.ParameterValues[0] as NumberElement;
                NumberElement b = element.ParameterValues[1] as NumberElement;

                if (a != null && b != null)
                    return Math.Pow(
                        a.Value,
                        b.Value
                    );

                if (a != null)
                {
                    if (a.Value == 0) return 0;
                    if (a.Value == 1) return 1;
                    
                    // ! Workshop Bug: Pow on values less than 0 always equals 0.
                    if (a.Value < 0) return 0;
                }

                if (b != null)
                {
                    if (b.Value == 0) return 1;
                    if (b.Value == 1) return a;
                }
                
                return element;
            }},
            {"Round To Integer", element => {
                if (element.ParameterValues[0] is NumberElement a)
                {
                    var roundingType = (ElementEnumMember)element.ParameterValues[1];
                    double num = a.Value;
                    if (roundingType.Name == "Down") return Math.Floor(num);
                    if (roundingType.Name == "To Nearest") return Math.Round(num);
                    if (roundingType.Name == "Up") return Math.Ceiling(num);
                }
                return element;
            }},
            {"Sine From Degrees", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Sin(a.Value * (Math.PI / 180));
                return element;
            }},
            {"Sine From Radians", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Sin(a.Value);
                return element;
            }},
            {"Square Root", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Sqrt(a.Value);

                return element;
            }},
            {"Subtract", element =>  OptimizeAddOperation(
                element,
                op       : (a, b) => a - b,
                areEqual : (a, b) => vecFunctions.Contains(a.Function.CodeName()) ? Element.Vector(0,0,0) : 0,
                false
            )},
            {"Tangent From Degrees", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Tan(a.Value * (Math.PI / 180));

                return element;
            }},
            {"Tangent From Radians", element => {
                if (element.ParameterValues[0] is NumberElement a)
                    return Math.Tan(a.Value);

                return element;
            }},
            {"Value In Array", element => {
                if (element.ParameterValues[1] is NumberElement num && num.Value == 0)
                    return Element.FirstOf(element.ParameterValues[0]);
                
                return element;
            }},
            {"Vector", element => {
                if (element.ParameterValues[0] is NumberElement xNum &&
                    element.ParameterValues[1] is NumberElement yNum &&
                    element.ParameterValues[2] is NumberElement zNum)
                {
                    double x = xNum.Value, y = yNum.Value, z = zNum.Value;

                    if (x == 0  && y == 1  && z == 0 ) return Element.Part("Up");
                    if (x == 0  && y == -1 && z == 0 ) return Element.Part("Down");
                    if (x == -1 && y == 0  && z == 0 ) return Element.Part("Right");
                    if (x == 1  && y == 0  && z == 0 ) return Element.Part("Left");
                    if (x == 0  && y == 0  && z == 1 ) return Element.Part("Forward");
                    if (x == 0  && y == 0  && z == -1) return Element.Part("Backward");
                }

                // Convert zeros to Empty Array.
                IWorkshopTree oX = element.ParameterValues[0];
                IWorkshopTree oY = element.ParameterValues[1];
                IWorkshopTree oZ = element.ParameterValues[2];
				bool oXIsZero = false;
				bool oYIsZero = false;
				bool oZIsZero = false;
                if (oX is NumberElement oXNum && oXNum.Value == 0){ oX = Element.EmptyArray(); oXIsZero = true;}
                if (oY is NumberElement oYNum && oYNum.Value == 0){ oY = Element.EmptyArray(); oYIsZero = true;}
                if (oZ is NumberElement oZNum && oZNum.Value == 0){ oZ = Element.EmptyArray(); oZIsZero = true;}
				if(oXIsZero && oYIsZero && oZIsZero)
					return Element.Subtract(Element.Part("Left"), Element.Part("Left"));

                if (oX != element.ParameterValues[0] ||
                    oY != element.ParameterValues[1] ||
                    oZ != element.ParameterValues[2]) return Element.Vector(oX, oY, oZ);

                return element;
            }},
            {"Vector Towards", element => {
                Element a = (Element)element.ParameterValues[0];
                Element b = (Element)element.ParameterValues[1];

                if (a.TryGetConstant(out Vertex vertA) && b.TryGetConstant(out Vertex vertB))
                    return vertA.VectorTowards(vertB).ToVector();

                return element;
            }},
            {"Vertical Angle From Direction", element => {
                if (((Element)element.ParameterValues[0]).TryGetConstant(out Vertex vertex))
                {
                    double result = -Math.Asin(vertex.Y) * (180 / Math.PI);
                    if (result == -0)
                        result = 0;
                    return result;
                }

                return element;
            }},
            {"X Component Of", element => {
                Element a = (Element)element.ParameterValues[0];

                if (a.Function.Name == "Vector")
                    return (Element)a.ParameterValues[0];
                
                return element;
            }},
            {"Y Component Of", element => {
                Element a = (Element)element.ParameterValues[0];

                if (a.Function.Name == "Vector")
                    return (Element)a.ParameterValues[1];
                
                return element;
            }},
            {"Z Component Of", element => {
                Element a = (Element)element.ParameterValues[0];

                if (a.Function.Name == "Vector")
                    return (Element)a.ParameterValues[2];
                
                return element;
            }}
        };

        static Element OptimizeAddOperation(
            Element element,
            Func<double, double, double> op,
            Func<Element, Element, Element> areEqual,
            bool returnBIf0
        )
        {
            Element a = (Element)element.ParameterValues[0];
            Element b = (Element)element.ParameterValues[1];

            NumberElement aAsNumber = a as NumberElement;
            NumberElement bAsNumber = b as NumberElement;

            // If a and b are numbers, operate them.
            if (aAsNumber != null && bAsNumber != null)
                return op(aAsNumber.Value, bAsNumber.Value);
            
            // If a is 0, return b.
            if (aAsNumber != null && aAsNumber.Value == 0 && returnBIf0)
                return b;
            
            // If b is 0, return a.
            if (bAsNumber != null && bAsNumber.Value == 0)
                return a;

            if (a.EqualTo(b))
				if(a.Function.CodeName() =="Left")
					return element;
                else return areEqual(a, b);
            
            if (a.TryGetConstant(out Vertex aVertex) && b.TryGetConstant(out Vertex bVertex))
                return Element.Vector(
                    op(aVertex.X, bVertex.X),
                    op(aVertex.Y, bVertex.Y),
                    op(aVertex.Z, bVertex.Z)
                );
            
            return element;
        }

        static Element OptimizeMultiplyOperation(
            Element element,
            Func<double, double, double> op,
            Func<Element, Element, Element> areEqual,
            bool returnBIf1
        )
        {
            Element a = (Element)element.ParameterValues[0];
            Element b = (Element)element.ParameterValues[1];

            NumberElement aAsNumber = a as NumberElement;
            NumberElement bAsNumber = b as NumberElement;

            // Multiply number and number
            if (aAsNumber != null && bAsNumber != null)
                return op(aAsNumber.Value, bAsNumber.Value);

            // Multiply vector and a vector
            if (a.TryGetConstant(out Vertex vertexA) && b.TryGetConstant(out Vertex vertexB))
                return Element.Vector(
                    op(vertexA.X, vertexB.X),
                    op(vertexA.Y, vertexB.Y),
                    op(vertexA.Z, vertexB.Z)
                );

            // Multiply vector and number
            Vertex mVertexB = null;
            if ((a.TryGetConstant(out Vertex mVertexA) && bAsNumber != null) || (aAsNumber != null && b.TryGetConstant(out mVertexB)))
            {
                Vertex vector = bAsNumber != null ? mVertexA : mVertexB;
                NumberElement number = aAsNumber ?? bAsNumber;
                return Element.Vector(
                    op(vector.X, number.Value),
                    op(vector.Y, number.Value),
                    op(vector.Z, number.Value)
                );
            }

            if (aAsNumber != null)
            {
                if (aAsNumber.Value == 1 && returnBIf1) return b;
                if (aAsNumber.Value == 0) return 0;
            }

            if (bAsNumber != null)
            {
                if (bAsNumber.Value == 1) return a;
                if (bAsNumber.Value == 0) return 0;
            }

            if (a.EqualTo(b))
                return areEqual(a, b);
            
            return element;
        }
    }
}
