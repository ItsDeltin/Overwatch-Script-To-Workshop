using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("AngleOfVectors", "Gets the angle of 3 vectors in 3d space.", CustomMethodType.MultiAction_Value)]
    class AngleOfVectors : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new CodeParameter("vector1", "The first vector."),
            new CodeParameter("vector2", "The second vector."),
            new CodeParameter("vector3", "The third vector.")
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            var a = actionSet.VarCollection.Assign("_angleOfVectors_a", actionSet.IsGlobal, true);
            var b = actionSet.VarCollection.Assign("_angleOfVectors_b", actionSet.IsGlobal, true);
            var c = actionSet.VarCollection.Assign("_angleOfVectors_c", actionSet.IsGlobal, true);
            var ab = actionSet.VarCollection.Assign("_angleOfVectors_ab", actionSet.IsGlobal, true);
            var bc = actionSet.VarCollection.Assign("_angleOfVectors_bc", actionSet.IsGlobal, true);
            var abVec = actionSet.VarCollection.Assign("_angleOfVectors_abVec", actionSet.IsGlobal, true);
            var bcVec = actionSet.VarCollection.Assign("_angleOfVectors_bcVec", actionSet.IsGlobal, true);
            var abNorm = actionSet.VarCollection.Assign("_angleOfVectors_abNorm", actionSet.IsGlobal, true);
            var bcNorm = actionSet.VarCollection.Assign("_angleOfVectors_bcNorm", actionSet.IsGlobal, true);

            Element zeroVec = Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(0));

            actionSet.AddAction(ArrayBuilder<Element>.Build(
                // Save A
                a.SetVariable((Element)parameterValues[0]),
                // Save B
                b.SetVariable((Element)parameterValues[1]),
                // save C
                c.SetVariable((Element)parameterValues[2]),

                // get ab
                // ab[3] = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
                ab.SetVariable(Element.Part<V_Vector>(
                    Element.Part<V_XOf>(b.GetVariable()) - Element.Part<V_XOf>(a.GetVariable()),
                    Element.Part<V_YOf>(b.GetVariable()) - Element.Part<V_YOf>(a.GetVariable()),
                    Element.Part<V_ZOf>(b.GetVariable()) - Element.Part<V_ZOf>(a.GetVariable())
                )),

                // get bc
                // bc[3] = { c[0] - b[0], c[1] - b[1], c[2] - b[2] };
                bc.SetVariable(Element.Part<V_Vector>(
                    Element.Part<V_XOf>(c.GetVariable()) - Element.Part<V_XOf>(b.GetVariable()),
                    Element.Part<V_YOf>(c.GetVariable()) - Element.Part<V_YOf>(b.GetVariable()),
                    Element.Part<V_ZOf>(c.GetVariable()) - Element.Part<V_ZOf>(b.GetVariable())
                )),

                // get abVec
                // abVec = sqrt(ab[0] * ab[0] + ab[1] * ab[1] + ab[2] * ab[2]);
                abVec.SetVariable(Element.Part<V_DistanceBetween>(
                    ab.GetVariable(),
                    zeroVec
                )),

                // get bcVec
                // bcVec = sqrt(bc[0] * bc[0] + bc[1] * bc[1] + bc[2] * bc[2]);
                bcVec.SetVariable(Element.Part<V_DistanceBetween>(
                    bc.GetVariable(),
                    zeroVec
                )),

                // get abNorm
                // abNorm[3] = {ab[0] / abVec, ab[1] / abVec, ab[2] / abVec};
                abNorm.SetVariable(Element.Part<V_Vector>(
                    Element.Part<V_XOf>(ab.GetVariable()) / (Element)abVec.GetVariable(),
                    Element.Part<V_YOf>(ab.GetVariable()) / (Element)abVec.GetVariable(),
                    Element.Part<V_ZOf>(ab.GetVariable()) / (Element)abVec.GetVariable()
                )),

                // get bcNorm
                // bcNorm[3] = {bc[0] / bcVec, bc[1] / bcVec, bc[2] / bcVec};
                bcNorm.SetVariable(Element.Part<V_Vector>(
                    Element.Part<V_XOf>(bc.GetVariable()) / (Element)bcVec.GetVariable(),
                    Element.Part<V_YOf>(bc.GetVariable()) / (Element)bcVec.GetVariable(),
                    Element.Part<V_ZOf>(bc.GetVariable()) / (Element)bcVec.GetVariable()
                ))
            ));

            Element result = (Element.Part<V_ArccosineInRadians>(
                // get res
                // res = abNorm[0] * bcNorm[0] + abNorm[1] * bcNorm[1] + abNorm[2] * bcNorm[2];
                //target.SetVariable(
                Element.Part<V_XOf>(abNorm.GetVariable()) * Element.Part<V_XOf>(bcNorm.GetVariable()) +
                Element.Part<V_YOf>(abNorm.GetVariable()) * Element.Part<V_YOf>(bcNorm.GetVariable()) +
                Element.Part<V_ZOf>(abNorm.GetVariable()) * Element.Part<V_ZOf>(bcNorm.GetVariable())
            ) * 180) / Math.PI;

            return result;
        }
    }
}