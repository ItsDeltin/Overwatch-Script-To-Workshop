using System;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("AngleOfVectors", CustomMethodType.MultiAction_Value)]
    [Parameter("Vector1", ValueType.VectorAndPlayer, null)]
    [Parameter("Vector2", ValueType.VectorAndPlayer, null)]
    [Parameter("Vector3", ValueType.VectorAndPlayer, null)]
    class AngleOfVectors : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            var eventPlayer = new V_EventPlayer();

            IndexedVar a      = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: a"     , TranslateContext.IsGlobal);
            IndexedVar b      = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: b"     , TranslateContext.IsGlobal);
            IndexedVar c      = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: c"     , TranslateContext.IsGlobal);
            IndexedVar ab     = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: ab"    , TranslateContext.IsGlobal);
            IndexedVar bc     = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: bc"    , TranslateContext.IsGlobal);
            IndexedVar abVec  = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: abVec" , TranslateContext.IsGlobal);
            IndexedVar bcVec  = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: bcVec" , TranslateContext.IsGlobal);
            IndexedVar abNorm = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: abNorm", TranslateContext.IsGlobal);
            IndexedVar bcNorm = TranslateContext.VarCollection.AssignVar(Scope, "AngleOfVectors: bcNorm", TranslateContext.IsGlobal);

            Element zeroVec = Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(0));

            Element[] actions = ArrayBuilder<Element>.Build
            (
                // Save A
                a.SetVariable((Element)Parameters[0], eventPlayer),
                // Save B
                b.SetVariable((Element)Parameters[1], eventPlayer),
                // save C
                c.SetVariable((Element)Parameters[2], eventPlayer),

                // get ab
                // ab[3] = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
                ab.SetVariable(
                    Element.Part<V_Vector>
                    (
                        Element.Part<V_Subtract>(Element.Part<V_XOf>(b.GetVariable()), Element.Part<V_XOf>(a.GetVariable())),
                        Element.Part<V_Subtract>(Element.Part<V_YOf>(b.GetVariable()), Element.Part<V_YOf>(a.GetVariable())),
                        Element.Part<V_Subtract>(Element.Part<V_ZOf>(b.GetVariable()), Element.Part<V_ZOf>(a.GetVariable()))
                    ), eventPlayer),

                // get bc
                // bc[3] = { c[0] - b[0], c[1] - b[1], c[2] - b[2] };
                bc.SetVariable(
                    Element.Part<V_Vector>
                    (
                        Element.Part<V_Subtract>(Element.Part<V_XOf>(c.GetVariable()), Element.Part<V_XOf>(b.GetVariable())),
                        Element.Part<V_Subtract>(Element.Part<V_YOf>(c.GetVariable()), Element.Part<V_YOf>(b.GetVariable())),
                        Element.Part<V_Subtract>(Element.Part<V_ZOf>(c.GetVariable()), Element.Part<V_ZOf>(b.GetVariable()))
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
                        Element.Part<V_Divide>(Element.Part<V_XOf>(ab.GetVariable()), abVec.GetVariable()),
                        Element.Part<V_Divide>(Element.Part<V_YOf>(ab.GetVariable()), abVec.GetVariable()),
                        Element.Part<V_Divide>(Element.Part<V_ZOf>(ab.GetVariable()), abVec.GetVariable())
                    ), eventPlayer),

                // get bcNorm
                // bcNorm[3] = {bc[0] / bcVec, bc[1] / bcVec, bc[2] / bcVec};
                bcNorm.SetVariable(
                    Element.Part<V_Vector>
                    (
                        Element.Part<V_Divide>(Element.Part<V_XOf>(bc.GetVariable()), bcVec.GetVariable()),
                        Element.Part<V_Divide>(Element.Part<V_YOf>(bc.GetVariable()), bcVec.GetVariable()),
                        Element.Part<V_Divide>(Element.Part<V_ZOf>(bc.GetVariable()), bcVec.GetVariable())
                    ), eventPlayer)
            );

            Element result = Element.Part<V_Divide>
            (
                Element.Part<V_Multiply>
                (
                    Element.Part<V_ArccosineInRadians>
                    (
                        // get res
                        // res = abNorm[0] * bcNorm[0] + abNorm[1] * bcNorm[1] + abNorm[2] * bcNorm[2];
                        //target.SetVariable(
                        Element.Part<V_Add>
                        (
                            Element.Part<V_Add>
                            (
                                Element.Part<V_Multiply>(Element.Part<V_XOf>(abNorm.GetVariable()), Element.Part<V_XOf>(bcNorm.GetVariable())),
                                Element.Part<V_Multiply>(Element.Part<V_YOf>(abNorm.GetVariable()), Element.Part<V_YOf>(bcNorm.GetVariable()))
                            ),
                            Element.Part<V_Multiply>(Element.Part<V_ZOf>(abNorm.GetVariable()), Element.Part<V_ZOf>(bcNorm.GetVariable()))
                        )
                    ),
                    new V_Number(180)
                ),
                new V_Number(Math.PI)
            );

            return new MethodResult(actions, result);
        }
    
        public override WikiMethod Wiki()
        {
            return new WikiMethod("AngleOfVectors", "Gets the angle of 3 vectors in 3d space.", null);
        }
    }
}