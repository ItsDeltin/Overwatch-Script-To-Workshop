using System;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{    
    [CustomMethod("AngleOfVectorsCom", CustomMethodType.Value)]
    [Parameter("Vector1", ValueType.VectorAndPlayer, null)]
    [Parameter("Vector2", ValueType.VectorAndPlayer, null)]
    [Parameter("Vector3", ValueType.VectorAndPlayer, null)]
    class AngleOfVectorsCom : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            Element zeroVec = Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(0));
            Element a = (Element)Parameters[0];
            Element b = (Element)Parameters[1];
            Element c = (Element)Parameters[2];

            Element ab = Element.Part<V_Vector>
            (
                Element.Part<V_Subtract>(Element.Part<V_XOf>(b), Element.Part<V_XOf>(a)),
                Element.Part<V_Subtract>(Element.Part<V_YOf>(b), Element.Part<V_YOf>(a)),
                Element.Part<V_Subtract>(Element.Part<V_ZOf>(b), Element.Part<V_ZOf>(a))
            );
            Element bc = Element.Part<V_Vector>
            (
                Element.Part<V_Subtract>(Element.Part<V_XOf>(c), Element.Part<V_XOf>(b)),
                Element.Part<V_Subtract>(Element.Part<V_YOf>(c), Element.Part<V_YOf>(b)),
                Element.Part<V_Subtract>(Element.Part<V_ZOf>(c), Element.Part<V_ZOf>(b))
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
                Element.Part<V_Divide>(Element.Part<V_XOf>(ab), abVec),
                Element.Part<V_Divide>(Element.Part<V_YOf>(ab), abVec),
                Element.Part<V_Divide>(Element.Part<V_ZOf>(ab), abVec)
            );
            Element bcNorm = Element.Part<V_Vector>
            (
                Element.Part<V_Divide>(Element.Part<V_XOf>(bc), bcVec),
                Element.Part<V_Divide>(Element.Part<V_YOf>(bc), bcVec),
                Element.Part<V_Divide>(Element.Part<V_ZOf>(bc), bcVec)
            );
            Element res = Element.Part<V_Add>
            (
                Element.Part<V_Add>
                (
                    Element.Part<V_Multiply>(Element.Part<V_XOf>(abNorm), Element.Part<V_XOf>(bcNorm)),
                    Element.Part<V_Multiply>(Element.Part<V_YOf>(abNorm), Element.Part<V_YOf>(bcNorm))
                ),
                Element.Part<V_Multiply>(Element.Part<V_ZOf>(abNorm), Element.Part<V_ZOf>(bcNorm))
            );
            Element result = Element.Part<V_Divide>
            (
                Element.Part<V_Multiply>
                (
                    Element.Part<V_ArccosineInRadians>(res),
                    new V_Number(180)
                ),
                new V_Number(Math.PI)
            );

            return new MethodResult(null, result);
        }
    
        public override WikiMethod Wiki()
        {
            return new WikiMethod("AngleOfVectorsCom", "Gets the angle of 3 vectors in 3d space.", null);
        }
    }
}