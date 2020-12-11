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

using System;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Animation
{
    public static class AnimationOperations
    {
        public static (Element axis, Element w) MultiplyQuaternion(Element axis1, Element angle1, Element axis2, Element angle2)
        {
            return (
                axis1*angle2 + axis2*angle1 + Element.Part<V_CrossProduct>(axis1, axis2),
                angle1*angle2 - Element.Part<V_DotProduct>(axis1, axis2)
            );
        }

        /// <summary>Creates a quaternion from an euler vector. X is yaw, Y is pitch, and Z is roll.</summary>
        public static Quaternion QuaternionFromEuler(ActionSet actionSet, Element euler)
        {
            Element yaw = Element.Part<V_ZOf>(euler),
                    pitch = Element.Part<V_YOf>(euler),
                    roll = Element.Part<V_XOf>(euler);
            Element cy = actionSet.AssignAndSave("cy", Element.Part<V_CosineFromDegrees>(yaw * 0.5)).Get();
            Element sy = actionSet.AssignAndSave("sy", Element.Part<V_SineFromDegrees>(yaw * 0.5)).Get();
            Element cp = actionSet.AssignAndSave("cp", Element.Part<V_CosineFromDegrees>(pitch * 0.5)).Get();
            Element sp = actionSet.AssignAndSave("sp", Element.Part<V_SineFromDegrees>(pitch * 0.5)).Get();
            Element cr = actionSet.AssignAndSave("cr", Element.Part<V_CosineFromDegrees>(roll * 0.5)).Get();
            Element sr = actionSet.AssignAndSave("sr", Element.Part<V_SineFromDegrees>(roll * 0.5)).Get();

            Element xyz = actionSet.AssignAndSave("xyz", new V_Vector(
                sr * cp * cy - cr * sp * sy,
                cr * sp * cy + sr * cp * sy,
                cr * cp * sy - sr * sp * cy
            )).Get();
            Element w = actionSet.AssignAndSave("w", cr * cp * cy + sr * sp * sy).Get();
            return new Quaternion(xyz, w);
        }

        public static Element CreateColumnGrouped3x3MatrixFromQuaternion(Element xyz, Element w)
        {
            var array = Create3x3MatrixRawArrayFromQuaternion(xyz, w);
            return Element.CreateArray(
                Element.Part<V_Vector>(array[0], array[3], array[6]),
                Element.Part<V_Vector>(array[1], array[4], array[7]),
                Element.Part<V_Vector>(array[2], array[5], array[8])
            );
        }

        public static Element Create3x3MatrixArrayFromQuaternion(Element xyz, Element w) => Element.CreateArray(Create3x3MatrixRawArrayFromQuaternion(xyz, w));

        public static Element[] Create3x3MatrixRawArrayFromQuaternion(Element xyz, Element w)
        {
            Element x = Element.Part<V_XOf>(xyz), y = Element.Part<V_YOf>(xyz), z = Element.Part<V_ZOf>(xyz),
                ww = w*w, xx = x*x, yy = y*y, zz = z*z,
                m_sqrt2 = (Element)Math.Sqrt(2),
                q0 = m_sqrt2 * w,
                q1 = m_sqrt2 * x,
                q2 = m_sqrt2 * y,
                q3 = m_sqrt2 * z,
                qda = q0 * q1,
                qdb = q0 * q2,
                qdc = q0 * q3,
                qaa = q1 * q1,
                qab = q1 * q2,
                qac = q1 * q3,
                qbb = q2 * q2,
                qbc = q2 * q3,
                qcc = q3 * q3;

            return new Element[] {
                (1.0 - qbb - qcc),  // [0][0] -> [0] 8
                (-qdc + qab),      // [1][0] -> [1] 5
                (qdb + qac),       // [2][0] -> [2] 2

                (qdc + qab),       // [0][1] -> [3] 7
                (1.0 - qaa - qcc), // [1][1] -> [4] 4
                (-qda + qbc),      // [2][1] -> [5] 1

                (-qdb + qac),      // [0][2] -> [6] 6
                (qda + qbc),       // [1][2] -> [7] 3
                (1.0 - qaa - qbb)  // [2][2] -> [8] 0
            };
        }

        public static Element Multiply3x3MatrixAndVectorToVector(Element ma, Element v) {
            var m = new Element[] { ma[0][0], ma[0][1], ma[0][2], ma[1][0], ma[1][1], ma[1][2], ma[2][0], ma[2][1], ma[2][2] };
            Element t0 = Element.Part<V_XOf>(v), t1 = Element.Part<V_YOf>(v), t2 = Element.Part<V_ZOf>(v);
            return new V_Vector(
                m[0] * t0 + m[1] * t1 + m[2] * t2,
                m[3] * t0 + m[4] * t1 + m[5] * t2,
                m[6] * t0 + m[7] * t1 + m[8] * t2
            );
        }

        public static Element Create3x3MatrixFromEulerRadiansVector(Element eulerVector)
            => Create3x3MatrixFromEulerRadians(Element.Part<V_XOf>(eulerVector), Element.Part<V_YOf>(eulerVector), Element.Part<V_ZOf>(eulerVector));

        public static Element Create3x3MatrixFromEulerRadians(Element attitude, Element bank, Element heading)
        {
            Func<Element, Element> sin = e => Element.Part<V_SineFromRadians>(e), cos = e => Element.Part<V_CosineFromRadians>(e);
            
            Element sa = sin(attitude),
                ca = cos(attitude),
                sb = sin(bank),
                cb = cos(bank),
                sh = sin(heading),
                ch = cos(heading);

            return Element.CreateArray(
                ch*ca,  -ch*sa*cb+sh*sb, ch*sa*sb+sh*cb,
                sa,     ca*cb,           -ca*sb,
                -sh*ca, sh*sa*cb+sh*sb,  -sh*sa*sb+ch*cb
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

        public static void NormalizeQuaternion(ActionSet actionSet, IndexReference axis, IndexReference angle)
        {
            Element w = angle.Get(), x = Element.Part<V_XOf>(axis.Get()), y = Element.Part<V_YOf>(axis.Get()), z = Element.Part<V_ZOf>(axis.Get());
            var magnitude = actionSet.SaveValue("Normalize Quaternion -> Magnitude", Element.Part<V_SquareRoot>(
                (w*w) +
                (x*x) +
                (y*y) +
                (z*z)
            ), false);
            actionSet.AddAction(axis.ModifyVariable(Operation.Divide, magnitude));
            actionSet.AddAction(angle.ModifyVariable(Operation.Divide, magnitude));
        }

        public static Element RotatePointDir(ActionSet actionSet, Element p, Element a, Element w)
        {
            var rot = MultiplyQuaternion(a, w, p, 0);
            var half_a = actionSet.AssignAndSave("half_a", rot.Item1).Get();
            var half_w = actionSet.AssignAndSave("half_w", rot.Item2).Get();
            var a_inv = actionSet.AssignAndSave("a_inv", a * -1).Get();
            var result = MultiplyQuaternion(half_a, half_w, a_inv, w);
            return result.Item1;
        }

        public static Element RotatePoint(ActionSet actionSet, Element p, Element a, Element w)
        {
            var matrix = actionSet.AssignAndSave("matrix", Create3x3MatrixArrayFromQuaternion(a, w)).Get();
            return Multiply3x3MatrixAndVectorToVector(matrix, p);
        }
        
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

        public static Quaternion Slerp(ActionSet actionSet, Element axis0, Element angle0, Element axis1, Element angle1, Element t)
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
            actionSet.AddAction(Element.Part<A_SmallMessage>(new V_AllPlayers(), new V_CustomString("dot < 0"))); // Invert the dot.
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

            actionSet.AddAction(axisResult.SetVariable((s0.Get() * axis0) + (s1.Get() * axis1)));
            actionSet.AddAction(angleResult.SetVariable((s0.Get() * angle0) + (s1.Get() * angle1)));

            // End the if/else.
            actionSet.AddAction(Element.Part<A_End>());

            return new Quaternion(axisResult.Get(), angleResult.Get());
        }

        public static Quaternion Slerp2(ActionSet actionSet, Element axis0, Element angle0, Element axis1, Element angle1, Element t)
        {
            var axisResult = actionSet.VarCollection.Assign("axisResult", actionSet.IsGlobal, false);
            var angleResult = actionSet.VarCollection.Assign("angleResult", actionSet.IsGlobal, false);

            var cosHalfTheta = actionSet.AssignAndSave("cosHalfTheta", QuaternionDotProduct(axis0, angle0, axis1, angle1)).Get();

            actionSet.AddAction(Element.Part<A_If>(new V_Compare(Element.Part<V_AbsoluteValue>(cosHalfTheta), Operators.GreaterThanOrEqual, new V_Number(1))));
            actionSet.AddAction(axisResult.SetVariable(axis0));
            actionSet.AddAction(angleResult.SetVariable(angle0));
            actionSet.AddAction(new A_Else());
            
            var halfTheta = actionSet.AssignAndSave("halfTheta", Element.Part<V_ArccosineInRadians>(cosHalfTheta)).Get();
            var sinHalfTheta = actionSet.AssignAndSave("sinHalfTheta", Element.Part<V_SquareRoot>(1 - cosHalfTheta*cosHalfTheta)).Get();

            actionSet.AddAction(Element.Part<A_If>(new V_Compare(Element.Part<V_AbsoluteValue>(sinHalfTheta), Operators.LessThan, new V_Number(0.001))));
            actionSet.AddAction(axisResult.SetVariable(axis0 * 0.5 + axis1 * 0.5));
            actionSet.AddAction(angleResult.SetVariable(angle0 * 0.5 + angle1 * 0.5));
            actionSet.AddAction(new A_Else());

            var ratioA = actionSet.AssignAndSave("ratioA", Element.Part<V_SineFromRadians>((1 - t) * halfTheta) / sinHalfTheta).Get();
            var ratioB = actionSet.AssignAndSave("ratioB", Element.Part<V_SineFromRadians>(t * halfTheta) / sinHalfTheta).Get();

            actionSet.AddAction(axisResult.SetVariable(axis0 * ratioA + axis1 * ratioB));
            actionSet.AddAction(angleResult.SetVariable(angle0 * ratioA + angle1 * ratioB));

            actionSet.AddAction(new A_End());
            actionSet.AddAction(new A_End());

            return new Quaternion(axisResult.Get(), angleResult.Get());
        }
    
        /// <summary>Multiplies two 3x3 matrices.</summary>
        /// <param name="lefthandMatrix">The lefthand matrix that will be multiplied. The numbers in this matrix should be row-grouped like so:
        /// ```
        /// m1 = [(3, 12, 4), // v1 / row1
        /// |     (5, 6,  8), // v2 / row2
        /// |     (1, 0,  2)] // v3 / row3
        /// ```</param>
        /// <param name="righthandMatrix">The righthand matrix that will be multipled. Unlike 'lefthandMatrix', the numbers in this
        /// matrix should be column grouped like so:
        /// ```
        /// //   v1   v2   v3
        /// m2= [(7,  (3, (8,
        /// |     11,  9,  5
        /// |     6),  8), 4)]
        /// ```
        /// To convert a row-grouped matrix to a column-grouped matrix, use the `ConvertRowGroupedMatrixToColumnGroupedMatrix` function.</param>
        /// <returns>The two matrices multiplied to an array-grouped matrix.</returns>
        public static Element VectorNotatedMultiplyMatrix3x3AndMatrix3x3(Element lefthandMatrix, Element righthandMatrix)
        {
            Element m1 = lefthandMatrix, m2 = righthandMatrix;
            return Element.CreateArray(MultiplyMatrixRow(m1, m2, 0), MultiplyMatrixRow(m1, m2, 1), MultiplyMatrixRow(m1, m2, 2));
        }
        private static Element MultiplyMatrixRow(Element m1, Element m2, Element row) =>
            Element.Part<V_MappedArray>(Element.Part<V_MappedArray>(m2, m1[row] * new V_ArrayElement()), Element.Part<V_XOf>(new V_ArrayElement()) + Element.Part<V_YOf>(new V_ArrayElement()) + Element.Part<V_ZOf>(new V_ArrayElement()));

        public static Element ConvertArrayGroupedMatrixToColumnGroupedMatrix(Element matrix) => Element.Part<V_MappedArray>(matrix, Element.Part<V_Vector>(
            matrix[0][new V_CurrentArrayIndex()], matrix[1][new V_CurrentArrayIndex()], matrix[2][new V_CurrentArrayIndex()] 
        ));
        public static Element ConvertArrayGroupedMatrixToRowGroupedMatrix(Element matrix) => Element.Part<V_MappedArray>(matrix, Element.Part<V_Vector>(
            matrix[new V_CurrentArrayIndex()][0], matrix[new V_CurrentArrayIndex()][1], matrix[new V_CurrentArrayIndex()][2]
        ));

        public static Element CaptureValue(Element value, Func<Element, Element> operation) => Element.Part<V_FirstOf>(Element.Part<V_MappedArray>(
            Element.Part<V_Array>(value),
            operation(new V_ArrayElement())
        ));
    }

    public class Quaternion
    {
        public Element V { get; }
        public Element W { get; }

        public Quaternion(Element axis, Element angle)
        {
            V = axis;
            W = angle;
        }

        public Quaternion Multiply(Quaternion q) => new Quaternion(
            V*q.W + q.V*W + Element.Part<V_CrossProduct>(V, q.V),
            W*q.W - Element.Part<V_DotProduct>(V, q.V)
        );
    }
}