using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OverwatchParser.Parse;

namespace OverwatchParser.Elements
{
    public class CustomMethods
    {
        static Element[] AngleBetween3Vectors(InternalVars internalVars, Element targetPlayer, Var target, Element vector1, Element vector2, Element vector3)
        {
            bool isGlobal = targetPlayer == null;

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

            return new Element[]
            {
                // Save A
                a.SetVariable(vector1, targetPlayer),
                // Save B
                b.SetVariable(vector2, targetPlayer),
                // save C
                c.SetVariable(vector3, targetPlayer),

                // get ab
                // ab[3] = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
                abVec.SetVariable(
                    Element.Part<V_Vector>
                    (
                        Element.Part<V_Subtract>(Element.Part<V_XComponentOf>(b.GetVariable()), Element.Part<V_XComponentOf>(a.GetVariable())),
                        Element.Part<V_Subtract>(Element.Part<V_YComponentOf>(b.GetVariable()), Element.Part<V_YComponentOf>(a.GetVariable())),
                        Element.Part<V_Subtract>(Element.Part<V_ZComponentOf>(b.GetVariable()), Element.Part<V_ZComponentOf>(a.GetVariable()))
                    ), targetPlayer),

                // get bc
                // bc[3] = { c[0] - b[0], c[1] - b[1], c[2] - b[2] };
                bcVec.SetVariable(
                    Element.Part<V_Vector>
                    (
                        Element.Part<V_Subtract>(Element.Part<V_XComponentOf>(c.GetVariable()), Element.Part<V_XComponentOf>(b.GetVariable())),
                        Element.Part<V_Subtract>(Element.Part<V_YComponentOf>(c.GetVariable()), Element.Part<V_YComponentOf>(b.GetVariable())),
                        Element.Part<V_Subtract>(Element.Part<V_ZComponentOf>(c.GetVariable()), Element.Part<V_ZComponentOf>(b.GetVariable()))
                    ), targetPlayer),

                // get abVec
                // abVec = sqrt(ab[0] * ab[0] + ab[1] * ab[1] + ab[2] * ab[2]);
                abVec.SetVariable(
                    Element.Part<V_DistanceBetween>
                    (
                        abVec.GetVariable(),
                        zeroVec
                    ), targetPlayer),

                // get bcVec
                // bcVec = sqrt(bc[0] * bc[0] + bc[1] * bc[1] + bc[2] * bc[2]);
                abVec.SetVariable(
                    Element.Part<V_DistanceBetween>
                    (
                        bcVec.GetVariable(),
                        zeroVec
                    ), targetPlayer),

                // get abNorm
                // abNorm[3] = {ab[0] / abVec, ab[1] / abVec, ab[2] / abVec};
                abNorm.SetVariable(
                    Element.Part<V_Vector>
                    (
                        Element.Part<V_Divide>(Element.Part<V_XComponentOf>(ab.GetVariable()), abVec.GetVariable()),
                        Element.Part<V_Divide>(Element.Part<V_YComponentOf>(ab.GetVariable()), abVec.GetVariable()),
                        Element.Part<V_Divide>(Element.Part<V_ZComponentOf>(ab.GetVariable()), abVec.GetVariable())
                    ), targetPlayer),

                // get bcNorm
                // bcNorm[3] = {bc[0] / bcVec, bc[1] / bcVec, bc[2] / bcVec};
                bcNorm.SetVariable(
                    Element.Part<V_Vector>
                    (
                        Element.Part<V_Divide>(Element.Part<V_XComponentOf>(bc.GetVariable()), bcVec.GetVariable()),
                        Element.Part<V_Divide>(Element.Part<V_YComponentOf>(bc.GetVariable()), bcVec.GetVariable()),
                        Element.Part<V_Divide>(Element.Part<V_ZComponentOf>(bc.GetVariable()), bcVec.GetVariable())
                    ), targetPlayer),

                // get res
                // res = abNorm[0] * bcNorm[0] + abNorm[1] * bcNorm[1] + abNorm[2] * bcNorm[2];
                target.SetVariable(
                    Element.Part<V_Add>
                    (
                        Element.Part<V_Add>
                        (
                            Element.Part<V_Multiply>(Element.Part<V_XComponentOf>(abNorm.GetVariable()), Element.Part<V_XComponentOf>(bcNorm.GetVariable())),
                            Element.Part<V_Multiply>(Element.Part<V_YComponentOf>(abNorm.GetVariable()), Element.Part<V_YComponentOf>(bcNorm.GetVariable()))
                        ),
                        Element.Part<V_Multiply>(Element.Part<V_ZComponentOf>(abNorm.GetVariable()), Element.Part<V_ZComponentOf>(bcNorm.GetVariable()))
                    )
                    , targetPlayer)
            };
        }
    }
}
