/*
This file contains useful math operations related to animation for the workshop output.

- Quaternions are arrays with 4 elements, formatted like [w, x, y, z].
- If MultiplyQuaternion is ever used more than once, create a subroutine for it in the output. 
- Rotating a vector with a quaternion:

    variables 'axis', 'angle', 'vector':
    var quaternion = QuaternionFromAxis(axis, angle);
    var rotated = AnimationOperations.RotatePoint(actionSet, vector, quaternion);

- Quaternions and matrices will probably not be used in favour of the faster, smaller 'RotatePointRodrique' functions which take advantage of vector notation.
  Quaternions are still used, but they should be supplied by the blend object rather than created on the fly.
*/

using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Animation
{
    public static class AnimationOperations
    {
        /// <summary>Multiplies 2 Element quaternions.
        /// Element quaternions are an array where [0] is w, [1] is x, [2] is y, and [3] is z.
        /// q1 * q2 is NOT equal to q2 * q1.</summary>
        public static Element MultiplyQuaternion(Element q1, Element q2)
        {
            Element w = 0, x = 1, y = 2, z = 3;
            return Element.CreateArray(
                q1[w]*q2[w] - q1[x]*q2[x] - q1[y]*q2[y] - q1[z]*q2[z], // w
                q1[w]*q2[x] + q1[x]*q2[w] + q1[y]*q2[z] - q1[z]*q2[y], // x
                q1[w]*q2[y] - q1[x]*q2[z] + q1[y]*q2[w] + q1[z]*q2[x], // y
                q1[w]*q2[z] + q1[x]*q2[y] - q1[y]*q2[x] + q1[z]*q2[w] // z
            );
        }

        /// <summary>Converts a vector axis and an angle to a quaternion.</summary>
        public static Element QuaternionFromAxis(Element vectorAxis, Element fAngle) => Element.CreateArray(
            // It is probably a good idea to turn this into a multi-action function and cache the sine of fAngle/2 into a variable.
            Element.Part<V_CosineFromRadians>(fAngle / 2), // w
            Element.Part<V_XOf>(vectorAxis) * Element.Part<V_SineFromRadians>(fAngle / 2), // x
            Element.Part<V_YOf>(vectorAxis) * Element.Part<V_SineFromRadians>(fAngle / 2), // y
            Element.Part<V_ZOf>(vectorAxis) * Element.Part<V_SineFromRadians>(fAngle / 2) // z
        );

        /// <summary>Creates a 3x3 matrix from a quaternion.
        /// This will return an array with 9 elements.
        /// To get the 2x3 value, you must do matrix[5].</summary>
        public static Element Create3x3MatrixFromQuaternion(Element q) 
        {
            // If setting a variable once then getting the variable twice is less expensive then doing a multiplication operation twice,
            // saving xw, xy, xz, yw, yx, yz, etc may be more efficient.
            Element w = q[0], x = q[1], y = q[2], z = q[3];
            // return Element.CreateArray(
            //     1 - 2*y^2 - 2*z^2, // 1x1
            //     2*x*y - 2*w*z, // 1x2
            //     2*x*z + 2*w*y, // 1x3

            //     2*x*y + 2*w*z, // 2x1
            //     1 - 2*x^2-2*z^2, // 2x2
            //     2*y*z+2*w*x, // 2x3

            //     2*x*z - 2*w*y, // 3x1
            //     2*y*z - 2*w*x, // 3x2
            //     1 - 2*x^2-2*y^2 // 3x3
            // );
            // return Element.CreateArray(
            //     1 - 2*y*y - 2*z*z, // 1x1
            //     2*x*y - 2*w*z, // 1x2
            //     2*x*z + 2*w*y, // 1x3

            //     2*x*y + 2*w*z, // 2x1
            //     1 - 2*x*x - 2*z*z, // 2x2
            //     2*y*z + 2*w*x, // 2x3

            //     2*x*z - 2*w*y, // 3x1
            //     2*y*z - 2*w*x, // 3x2
            //     1 - 2*x*x - 2*y*y // 3x3
            // );
            return Element.CreateArray(
                w*w + x*x + y*y + z*z,
                2*x*y - 2*w*z,
                2*x*z + 2*w*y,

                2*x*y + 2*w*z,
                w*w - x*x + y*y - z*z,
                2*y*z + 2*w*x,

                2*x*z - 2*w*y,
                2*y*z - 2*w*x,
                w*w - x*x - y*y + z*z
            );
        }

        /// <summary>Creates a 3x3 matrix from a quaternion.
        /// This will return an array with 3 elements, each of those elements being a vector.
        /// For example, to get the 2x3 value, you must do matrix[1].Z</summary>
        public static Element Create3x3MatrixFromQuaternionVector(Element q) 
        {
            // If setting a variable once then getting the variable twice is less expensive then doing a multiplication operation twice,
            // saving xw, xy, xz, yw, yx, yz, etc may be more efficient.
            Element w = q[0], x = q[1], y = q[2], z = q[3];
            return Element.CreateArray(
                new V_Vector(
                    1 - 2*y^2 - 2*z^2, // 1x1
                    2*x*y - 2*w*z, // 1x2
                    2*x*z + 2*w*y // 1x3
                ),
                new V_Vector(
                    2*x*y + 2*w*z, // 2x1
                    1 - 2*x^2-2*z^2, // 2x2
                    2*y*z+2*w*x // 2x3
                ),
                new V_Vector(
                    2*x*z - 2*w*y, // 3x1
                    2*y*z - 2*w*x, // 3x2
                    1 - 2*x^2-2*y^2 // 3x3
                )
            );
        }

        /// <summary>Multiplies a 3x3 matrix and a 3x1 matrix into a vector.</summary>
        public static Element MultiplyMatrix3x3AndMatrix3x1ToVector(Element m3x3, Element m3x1) => new V_Vector(
            m3x3[0]*m3x1[0] + m3x3[1]*m3x1[1] + m3x3[2]*m3x1[2],
            m3x3[3]*m3x1[0] + m3x3[4]*m3x1[1] + m3x3[5]*m3x1[2],
            m3x3[6]*m3x1[0] + m3x3[7]*m3x1[1] + m3x3[8]*m3x1[2]
        );
        
        /// <summary>Multiplies a 3x3 matrix and a vector.</summary>
        /// <returns>Returns a Vector.</returns>
        public static Element MultiplyMatrix3x3AndVectorToVector(Element m3x3, Element vector)
        {
            Element x = Element.Part<V_XOf>(vector), y = Element.Part<V_YOf>(vector), z = Element.Part<V_ZOf>(vector);
            return new V_Vector(
                m3x3[0]*x + m3x3[1]*y + m3x3[2]*z,
                m3x3[3]*x + m3x3[4]*y + m3x3[5]*z,
                m3x3[6]*x + m3x3[7]*y + m3x3[8]*z
            );
        }

        /// <summary>Multiplies 2 3x3 matrixes into a matrix.</summary>
        /// <param name="actionSet">The actionset of the current rule. Inline multiplication is not practical.</param>
        /// <param name="a">The first 3x3 matrix.</param>
        /// <param name="b">The second 3x3 matrix.</param>
        /// <returns>A new 3x3 matrix created from a*b.</returns>
        public static Element MultiplyMatrix3x3AndMatrix3x3(ActionSet actionSet, Element a, Element b)
        {
            var product = actionSet.VarCollection.Assign("m3x3p", actionSet.IsGlobal, false);

            // Create the 3x3 loop.
            var li = new ForRangeBuilder(actionSet, "m3x3i", 3); li.Init();
            var lj = new ForRangeBuilder(actionSet, "m3x3j", 3); lj.Init();

            // The variable for the current 3x3 sum.
            var sum = actionSet.VarCollection.Assign("m3x3s", actionSet.IsGlobal, false);
            actionSet.AddAction(sum.SetVariable(0)); // Reset the variable.

            var lk = new ForRangeBuilder(actionSet, "m3x3k", 3); lk.Init();

            Element i = li, j = lj, k = lk; // Easy access loop iterator as elements.

            // Append to the sum.
            actionSet.AddAction(sum.ModifyVariable(Operation.Add, a[i*3 + k] * b[k*3 + j]));
            lk.Finish();

            // Set the current matrix value.
            actionSet.AddAction(product.SetVariable(index: i*3 + j, value: sum.Get()));
            
            // Complete the 3x3 loop.
            lj.Finish();
            li.Finish();

            // Done.
            return product.Get();
        }

        /// <summary>Represents a quaternion without any rotation (1, 0, 0, 0).
        /// In reality, this returns an array containing only the number 1. It is assumed that accessing
        /// the other values will return 0 by default.</summary>
        public static Element ZeroQuaternion() => Element.CreateArray(new V_Number(1));

        /// <summary>Normalizes a quaternion.</summary>
        public static Element NormalizeQuaternion(ActionSet actionSet, Element q)
        {
            Element w = q[0], x = q[1], y = q[2], z = q[3];
            var magnitude = actionSet.SaveValue("Normalize Quaternion -> Magnitude", Element.Part<V_SquareRoot>(
                w^2 +
                x^2 +
                y^2 +
                z^2
            ), false);
            return Element.CreateArray(
                w / magnitude,
                x / magnitude,
                y / magnitude,
                z / magnitude
            );
        }

        public static void NormalizeQuaternion(ActionSet actionSet, IndexReference axis, IndexReference angle)
        {
            Element w = angle.Get(), x = Element.Part<V_XOf>(axis.Get()), y = Element.Part<V_YOf>(axis.Get()), z = Element.Part<V_ZOf>(axis.Get());
            var magnitude = actionSet.SaveValue("Normalize Quaternion -> Magnitude", Element.Part<V_SquareRoot>(
                w^2 +
                x^2 +
                y^2 +
                z^2
            ), false);
            actionSet.AddAction(axis.ModifyVariable(Operation.Divide, magnitude));
            actionSet.AddAction(angle.ModifyVariable(Operation.Divide, magnitude));
        }

        public static Element QuaternionFromVector(Element v) => Element.CreateArray(new V_Number(0), Element.Part<V_XOf>(v), Element.Part<V_YOf>(v), Element.Part<V_ZOf>(v));

        /// <summary>Creates a quaternion array from a vector. This is used to multiply existing vectors with quaternions.
        /// Alternatively, use QuaternionFromAxis to create a true quaternion from a vector and angle.</summary>
        /// <returns>[0, x, y, z]</returns>
        public static Element VectorFromQuaternion(Element q) => new V_Vector(q[1], q[2], q[3]);

        /// <summary>Inverts a quaternion.</summary>
        public static Element InvertQuaternion(Element q) => Element.CreateArray(q[0], q[1] * -1, q[2] * -1, q[3] * -1);

        /// <summary>Rotates a vector with a quaternion.</summary>
        public static Element RotatePoint(ActionSet actionSet, Element v, Element q)
        {
            v = actionSet.SaveValue("rotate_p_to_4d", QuaternionFromVector(v), false);
            q = actionSet.SaveValue("q", q, false);
            Element half = actionSet.SaveValue("half", MultiplyQuaternion(q, v), false);
            Element result = actionSet.SaveValue("rotate_result", MultiplyQuaternion(half, actionSet.SaveValue("rotate_inverse", InvertQuaternion(q), false)), false);
            return VectorFromQuaternion(result);
        }

        /// <summary>Rotates a local vector around an axis and angle.
        /// Unlike the alternative overload, this will generate a quaternion from the input axis and angle.
        /// The elements will be stored inside a variable, so storing them beforehand is not required.</summary>
        public static Element RotatePointRodrique(ActionSet actionSet, Element position, Element axis, Element angle)
        {
            Element v = position;
            Element u = actionSet.SaveValue("a", new V_Vector(
                Element.Part<V_XOf>(axis) * Element.Part<V_SineFromRadians>(angle / 2), // x
                Element.Part<V_YOf>(axis) * Element.Part<V_SineFromRadians>(angle / 2), // y
                Element.Part<V_ZOf>(axis) * Element.Part<V_SineFromRadians>(angle / 2) // z
            ));
            // Element s = angle;
            Element s = actionSet.SaveValue("s", Element.Part<V_CosineFromRadians>(angle / 2), false);
            return RotatePointRodrique(v, u, s);
        }

        /// <summary>Rotates a local vector around an axis and angle.</summary>
        /// <param name="v">The vector that will be rotated. This element is accessed 3 times, so store this into a variable if it is complicated.</param>
        /// <param name="u">A vector containing the X, Y, and Z values of the quaternion. This element is accessed 3 times, so store this into a variable if it is complicated.</param>
        /// <param name="s">The W value of the quaternion. This element is accessed only once, so storing it inside a variable is not required.</param>
        /// <returns>The vector rotated around the point.</returns>
        public static Element RotatePointRodrique(Element v, Element u, Element s)
            => v + ((Element.Part<V_CrossProduct>(u, v) * s) + Element.Part<V_CrossProduct>(u, Element.Part<V_CrossProduct>(u, v))) * 2;
        
        /// <summary>Gets the dot product of 2 quaternions defined as a vector axis (X, Y, Z) and an angle (W).</summary>
        public static Element QuaternionDotProduct(Element axis0, Element angle0, Element axis1, Element angle1)
            => Element.Part<V_DotProduct>(axis0, axis1) + angle0 * angle1;
        
        /// <summary>Gets the dot product of 2 quaternions defined as an array [W, X, Y, Z].</summary>
        public static Element QuaternionDotProduct(Element q0, Element q1)
        {
            Element w0 = q0[0], x0 = q0[1], y0 = q0[2], z0 = q0[3],
                    w1 = q1[0], x1 = q1[1], y1 = q1[2], z1 = q1[3];
            return w0 * w1 + x0 * x1 + y0 * y1 + z0 * z1;
        }

        public static SlerpResult Slerp(ActionSet actionSet, Element axis0, Element angle0, Element axis1, Element angle1, Element t)
        {
            var dot = actionSet.VarCollection.Assign("slerp_dot_product", actionSet.IsGlobal, false);

            // Save axis0 to variable.
            var axis0Var = actionSet.AssignAndSave("slerp_axis0", axis0);
            axis0 = axis0Var.Get();

            // Save axis1 to variable.
            var axis1Var = actionSet.AssignAndSave("slerp_axis1", axis1);
            axis1 = axis1Var.Get();

            // Get the dot product of the quaternions.
            actionSet.AddAction(dot.SetVariable(QuaternionDotProduct(axis0, angle0, axis1, angle1)));

            // If the dot product is negative, slerp won't take
            // the shorter path. Note that v1 and -v1 are equivalent when
            // the negation is applied to all four components. Fix by 
            // reversing one quaternion.
            actionSet.AddAction(Element.Part<A_If>(new V_Compare(dot.Get(), Operators.LessThan, new V_Number(0))));
            actionSet.AddAction(axis1Var.ModifyVariable(Operation.Multiply, -1)); // Invert the axis.
            actionSet.AddAction(dot.ModifyVariable(Operation.Multiply, -1)); // Invert the dot.
            actionSet.AddAction(new A_End()); // End the if.

            // Inputs are too close.
            const double DOT_THRESHOLD = 0.9995;
            actionSet.AddAction(Element.Part<A_If>(new V_Compare(dot.Get(), Operators.GreaterThan, new V_Number(DOT_THRESHOLD))));
            
            var axisResult = actionSet.VarCollection.Assign("slerp_axis_result", actionSet.IsGlobal, false);
            var angleResult = actionSet.VarCollection.Assign("slerp_angle_result", actionSet.IsGlobal, false);

            actionSet.AddAction(axisResult.SetVariable(axis0 + t * (axis1 - axis0)));
            actionSet.AddAction(angleResult.SetVariable(angle0 + t * (angle1 - angle0)));
            NormalizeQuaternion(actionSet, axisResult, angleResult);
        
            // Acos is safe
            actionSet.AddAction(Element.Part<A_Else>());

            var theta_0 = actionSet.AssignAndSave("slerp_theta_0", Element.Part<V_ArccosineInRadians>(dot.Get())); // theta_0 = angle between input vectors
            var theta = actionSet.AssignAndSave("slerp_theta", theta_0.Get() * t); // theta = angle between v0 and result
            var sin_theta = actionSet.AssignAndSave("slerp_sin_theta", Element.Part<V_SineFromRadians>(theta.Get()));
            var sin_theta_0 = actionSet.AssignAndSave("slerp_sin_theta_0", Element.Part<V_SineFromRadians>(theta_0.Get()));

            var s0 = actionSet.AssignAndSave("slerp_s0", Element.Part<V_CosineFromRadians>(theta.Get()) - dot.Get() * sin_theta.Get() / sin_theta_0.Get());
            var s1 = actionSet.AssignAndSave("slerp_s1", sin_theta.Get() / sin_theta_0.Get());

            axisResult.SetVariable((s0.Get() * axis0) + (s1.Get() * axis1));
            angleResult.SetVariable((s0.Get() * angle0) + (s1.Get() * angle1));

            // End the if/else.
            actionSet.AddAction(Element.Part<A_End>());

            return new SlerpResult(axisResult.Get(), angleResult.Get());
        }
    }

    public class SlerpResult
    {
        public Element Axis { get; }
        public Element Angle { get; }

        public SlerpResult(Element axis, Element angle)
        {
            Axis = axis;
            Angle = angle;
        }
    }
}