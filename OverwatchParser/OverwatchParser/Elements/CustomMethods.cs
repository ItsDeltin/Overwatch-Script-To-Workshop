using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using OverwatchParser.Parse;

namespace OverwatchParser.Elements
{
    public class CustomMethods
    {
        private static MethodInfo[] CustomMethodList = null;

        public static MethodInfo GetCustomMethod(string name)
        {
            if (CustomMethodList == null)
                CustomMethodList = typeof(CustomMethods)
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(t => t.GetCustomAttribute<CustomMethod>() != null)
                    .ToArray();

            for (int i = 0; i < CustomMethodList.Length; i++)
                if (CustomMethodList[i].Name == name)
                    return CustomMethodList[i];
            return null;
        }

        [CustomMethod("AngleOfVectors", CustomMethodType.MultiAction_Value)]
        static MethodResult AngleOfVectors(InternalVars internalVars, bool isGlobal, object[] parameters)
        {
            var eventPlayer = new V_EventPlayer();

            Var a      = internalVars.AssignVar(isGlobal);
            Var b      = internalVars.AssignVar(isGlobal);
            Var c      = internalVars.AssignVar(isGlobal);
            Var ab     = internalVars.AssignVar(isGlobal);
            Var bc     = internalVars.AssignVar(isGlobal);
            Var abVec  = internalVars.AssignVar(isGlobal);
            Var bcVec  = internalVars.AssignVar(isGlobal);
            Var abNorm = internalVars.AssignVar(isGlobal);
            Var bcNorm = internalVars.AssignVar(isGlobal);

            Element zeroVec = Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(0));

            return new MethodResult
            (
                new Element[]
                {
                    // Save A
                    a.SetVariable((Element)parameters[0], eventPlayer),
                    // Save B
                    b.SetVariable((Element)parameters[1], eventPlayer),
                    // save C
                    c.SetVariable((Element)parameters[2], eventPlayer),

                    // get ab
                    // ab[3] = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
                    ab.SetVariable(
                        Element.Part<V_Vector>
                        (
                            Element.Part<V_Subtract>(Element.Part<V_XComponentOf>(b.GetVariable()), Element.Part<V_XComponentOf>(a.GetVariable())),
                            Element.Part<V_Subtract>(Element.Part<V_YComponentOf>(b.GetVariable()), Element.Part<V_YComponentOf>(a.GetVariable())),
                            Element.Part<V_Subtract>(Element.Part<V_ZComponentOf>(b.GetVariable()), Element.Part<V_ZComponentOf>(a.GetVariable()))
                        ), eventPlayer),

                    // get bc
                    // bc[3] = { c[0] - b[0], c[1] - b[1], c[2] - b[2] };
                    bc.SetVariable(
                        Element.Part<V_Vector>
                        (
                            Element.Part<V_Subtract>(Element.Part<V_XComponentOf>(c.GetVariable()), Element.Part<V_XComponentOf>(b.GetVariable())),
                            Element.Part<V_Subtract>(Element.Part<V_YComponentOf>(c.GetVariable()), Element.Part<V_YComponentOf>(b.GetVariable())),
                            Element.Part<V_Subtract>(Element.Part<V_ZComponentOf>(c.GetVariable()), Element.Part<V_ZComponentOf>(b.GetVariable()))
                        ), eventPlayer),

                    // get abVec
                    // abVec = sqrt(ab[0] * ab[0] + ab[1] * ab[1] + ab[2] * ab[2]);
                    abVec.SetVariable(
                        Element.Part<V_DistanceBetween>
                        (
                            ab.GetVariable(),
                            zeroVec
                        ), eventPlayer),

                    // get bcVec
                    // bcVec = sqrt(bc[0] * bc[0] + bc[1] * bc[1] + bc[2] * bc[2]);
                    bcVec.SetVariable(
                        Element.Part<V_DistanceBetween>
                        (
                            bc.GetVariable(),
                            zeroVec
                        ), eventPlayer),

                    // get abNorm
                    // abNorm[3] = {ab[0] / abVec, ab[1] / abVec, ab[2] / abVec};
                    abNorm.SetVariable(
                        Element.Part<V_Vector>
                        (
                            Element.Part<V_Divide>(Element.Part<V_XComponentOf>(ab.GetVariable()), abVec.GetVariable()),
                            Element.Part<V_Divide>(Element.Part<V_YComponentOf>(ab.GetVariable()), abVec.GetVariable()),
                            Element.Part<V_Divide>(Element.Part<V_ZComponentOf>(ab.GetVariable()), abVec.GetVariable())
                        ), eventPlayer),

                    // get bcNorm
                    // bcNorm[3] = {bc[0] / bcVec, bc[1] / bcVec, bc[2] / bcVec};
                    bcNorm.SetVariable(
                        Element.Part<V_Vector>
                        (
                            Element.Part<V_Divide>(Element.Part<V_XComponentOf>(bc.GetVariable()), bcVec.GetVariable()),
                            Element.Part<V_Divide>(Element.Part<V_YComponentOf>(bc.GetVariable()), bcVec.GetVariable()),
                            Element.Part<V_Divide>(Element.Part<V_ZComponentOf>(bc.GetVariable()), bcVec.GetVariable())
                        ), eventPlayer),
                },
                // get res
                // res = abNorm[0] * bcNorm[0] + abNorm[1] * bcNorm[1] + abNorm[2] * bcNorm[2];
                //target.SetVariable(
                Element.Part<V_Add>
                (
                    Element.Part<V_Add>
                    (
                        Element.Part<V_Multiply>(Element.Part<V_XComponentOf>(abNorm.GetVariable()), Element.Part<V_XComponentOf>(bcNorm.GetVariable())),
                        Element.Part<V_Multiply>(Element.Part<V_YComponentOf>(abNorm.GetVariable()), Element.Part<V_YComponentOf>(bcNorm.GetVariable()))
                    ),
                    Element.Part<V_Multiply>(Element.Part<V_ZComponentOf>(abNorm.GetVariable()), Element.Part<V_ZComponentOf>(bcNorm.GetVariable()))
                ),
                CustomMethodType.MultiAction_Value
            );
        }

        [CustomMethod("AngleOfVectorsCon", CustomMethodType.Value)]
        static MethodResult AngleOfVectorsCon(InternalVars internalVars, bool isGlobal, object[] parameters)
        {
            Element vector1 = (Element)parameters[0];
            Element vector2 = (Element)parameters[1];
            Element vector3 = (Element)parameters[2];

            // Condensed version of AngleBetween3Vectors. Not optimized at all.
            Element zeroVec = Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(0));
            Element ab = Element.Part<V_Vector>
            (
                Element.Part<V_Subtract>(Element.Part<V_XComponentOf>(vector2), Element.Part<V_XComponentOf>(vector1)),
                Element.Part<V_Subtract>(Element.Part<V_YComponentOf>(vector2), Element.Part<V_YComponentOf>(vector1)),
                Element.Part<V_Subtract>(Element.Part<V_ZComponentOf>(vector2), Element.Part<V_ZComponentOf>(vector1))
            );
            Element bc = Element.Part<V_Vector>
            (
                Element.Part<V_Subtract>(Element.Part<V_XComponentOf>(vector3), Element.Part<V_XComponentOf>(vector2)),
                Element.Part<V_Subtract>(Element.Part<V_YComponentOf>(vector3), Element.Part<V_YComponentOf>(vector2)),
                Element.Part<V_Subtract>(Element.Part<V_ZComponentOf>(vector3), Element.Part<V_ZComponentOf>(vector2))
            );
            Element abVec = Element.Part<V_DistanceBetween>
            (
                ab,
                zeroVec
            );
            Element bcVec = Element.Part<V_DistanceBetween>
            (
                bc,
                zeroVec
            );
            Element abNorm = Element.Part<V_Vector>
            (
                Element.Part<V_Divide>(Element.Part<V_XComponentOf>(ab), abVec),
                Element.Part<V_Divide>(Element.Part<V_YComponentOf>(ab), abVec),
                Element.Part<V_Divide>(Element.Part<V_ZComponentOf>(ab), abVec)
            );
            Element bcNorm = Element.Part<V_Vector>
            (
                Element.Part<V_Divide>(Element.Part<V_XComponentOf>(bc), bcVec),
                Element.Part<V_Divide>(Element.Part<V_YComponentOf>(bc), bcVec),
                Element.Part<V_Divide>(Element.Part<V_ZComponentOf>(bc), bcVec)
            );
            Element res = Element.Part<V_Add>
            (
                Element.Part<V_Add>
                (
                    Element.Part<V_Multiply>(Element.Part<V_XComponentOf>(abNorm), Element.Part<V_XComponentOf>(bcNorm)),
                    Element.Part<V_Multiply>(Element.Part<V_YComponentOf>(abNorm), Element.Part<V_YComponentOf>(bcNorm))
                ),
                Element.Part<V_Multiply>(Element.Part<V_ZComponentOf>(abNorm), Element.Part<V_ZComponentOf>(bcNorm))
            );
            return new MethodResult(null, res, CustomMethodType.Value);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CustomMethod : Attribute
    {
        public CustomMethod(string elementName, CustomMethodType methodType)
        {
            MethodName = MethodName;
            MethodType = methodType;
        }

        public string MethodName { get; private set; }
        public CustomMethodType MethodType { get; private set; }
    }

    public class MethodResult
    {
        public MethodResult(Element[] elements, Element result, CustomMethodType methodType)
        {
            Elements = elements;
            Result = result;
            MethodType = methodType;
        }
        public Element[] Elements { get; private set; }
        public Element Result { get; private set; }
        public CustomMethodType MethodType { get; private set; }
    }

    public enum CustomMethodType
    {
        Value,
        MultiAction_Value,
        Action
    }
}
