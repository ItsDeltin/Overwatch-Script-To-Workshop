using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.Elements
{
    public class CustomMethods
    {
        public static readonly MethodInfo[] CustomMethodList = typeof(CustomMethods)
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(t => t.GetCustomAttribute<CustomMethod>() != null)
                    .ToArray();

        public static MethodInfo GetCustomMethod(string name)
        {
            for (int i = 0; i < CustomMethodList.Length; i++)
                if (CustomMethodList[i].Name == name)
                    return CustomMethodList[i];
            return null;
        }

        [CustomMethod("AngleOfVectors", CustomMethodType.MultiAction_Value)]
        [Parameter("Vector1", ValueType.VectorAndPlayer, null)]
        [Parameter("Vector2", ValueType.VectorAndPlayer, null)]
        [Parameter("Vector3", ValueType.VectorAndPlayer, null)]
        static MethodResult AngleOfVectors(bool isGlobal, VarCollection varCollection, object[] parameters)
        {
            var eventPlayer = new V_EventPlayer();

            Var a      = varCollection.AssignVar("AngleOfVectors: a",      isGlobal);
            Var b      = varCollection.AssignVar("AngleOfVectors: b",      isGlobal);
            Var c      = varCollection.AssignVar("AngleOfVectors: c",      isGlobal);
            Var ab     = varCollection.AssignVar("AngleOfVectors: ab",     isGlobal);
            Var bc     = varCollection.AssignVar("AngleOfVectors: bc",     isGlobal);
            Var abVec  = varCollection.AssignVar("AngleOfVectors: abVec",  isGlobal);
            Var bcVec  = varCollection.AssignVar("AngleOfVectors: bcVec",  isGlobal);
            Var abNorm = varCollection.AssignVar("AngleOfVectors: abNorm", isGlobal);
            Var bcNorm = varCollection.AssignVar("AngleOfVectors: bcNorm", isGlobal);

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
                        ), eventPlayer),
                },
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
                ),
                CustomMethodType.MultiAction_Value
            );
        }

        [CustomMethod("AngleOfVectorsCom", CustomMethodType.Value)]
        [Parameter("Vector1", ValueType.VectorAndPlayer, null)]
        [Parameter("Vector2", ValueType.VectorAndPlayer, null)]
        [Parameter("Vector3", ValueType.VectorAndPlayer, null)]
        static MethodResult AngleOfVectorsCom(bool isGlobal, VarCollection varCollection, object[] parameters)
        {
            Element zeroVec = Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(0));
            Element a = (Element)parameters[0];
            Element b = (Element)parameters[1];
            Element c = (Element)parameters[2];

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

            return new MethodResult(null, res, CustomMethodType.MultiAction_Value);
        }

        [CustomMethod("GetMapID", CustomMethodType.Value)]
        static MethodResult GetMapID(bool isGlobal, VarCollection varCollection, object[] parameters)
        {
            /*
             All credit to https://us.forums.blizzard.com/en/overwatch/t/workshop-resource-get-the-current-map-name-updated-1-action/
             Based off code: 5VAQA
            */

            int mapcount = 0;
            for (int i = 0; i < Constants.MapChecks.Length; i++)
                mapcount += Constants.MapChecks[i].Length;

            V_Append prev = null;
            V_Append current = null;
            for (int s = 0; s < Constants.MapChecks.Length; s++)
                for (int i = 0; i < Constants.MapChecks[s].Length; i++)
                {
                    current = new V_Append()
                    {
                        ParameterValues = new IWorkshopTree[2]
                    };

                    if (prev != null)
                        current.ParameterValues[0] = prev;
                    else
                        current.ParameterValues[0] = new V_EmptyArray();

                    // Set the map ID
                    current.ParameterValues[1] = new V_Number(Constants.MapChecks[s][i]);
                    prev = current;
                }

            return new MethodResult(null,
                Element.Part<V_IndexOfArrayValue>(current,
                Element.Part<V_RoundToInteger>(Element.Part<V_Divide>(
                    Element.Part<V_RoundToInteger>(
                        Element.Part<V_Multiply>(
                            Element.Part<V_DistanceBetween>(
                                Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(0)),
                                Element.Part<V_NearestWalkablePosition>(Element.Part<V_Vector>(new V_Number(100), new V_Number(100), new V_Number(100)))
                            ),
                            new V_Number(100)
                        ),
                        EnumData.GetEnumValue(Rounding.Down)
                    ),
                    new V_Number(4)
                ),
                EnumData.GetEnumValue(Rounding.Down))
            ), CustomMethodType.Value);
        }

        [CustomMethod("RandomValuesInArray", CustomMethodType.Value)]
        [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
        [Parameter("Count", ValueType.Number, typeof(V_Number))]
        static MethodResult RandomValuesInArray(bool isGlobal, VarCollection varCollection, object[] parameters)
        {
            Element array = parameters[0] as Element;
            Element count = parameters[1] as Element;
            return new MethodResult(null, Element.Part<V_ArraySlice>(Element.Part<V_RandomizedArray>(array), count), CustomMethodType.Value);
        }
 
        [CustomMethod("GetMap", CustomMethodType.MultiAction_Value)]
        static MethodResult GetMap(bool isGlobal, VarCollection varCollection, object[] parameters)
        {
            Var temp = varCollection.AssignVar("GetMap: temp", isGlobal);
            
            Element[] actions = new Element[]
            {
                temp.SetVariable(Element.Part<V_RoundToInteger>(
                    Element.Part<V_Add>(
                        Element.Part<V_DistanceBetween>(
                            Element.Part<V_NearestWalkablePosition>(
                                Element.Part<V_Vector>(new V_Number(-500.000), new V_Number(0), new V_Number(0))
                            ),
                            Element.Part<V_NearestWalkablePosition>(Element.Part<V_Vector>(new V_Number(500), new V_Number(0), new V_Number(0)))
                        ), 
                        Element.Part<V_DistanceBetween>(
                            Element.Part<V_NearestWalkablePosition>(Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(-500.000))),
                            Element.Part<V_NearestWalkablePosition>(Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(500)))
                        )
                    ),
                    EnumData.GetEnumValue(Rounding.Down)
                )),

                temp.SetVariable(Element.Part<V_IndexOfArrayValue>( 
                    Element.Part<V_Append>(
                        Element.Part<V_Append>(
                            Element.Part<V_Append>(
                                Element.Part<V_Append>(
                                    Element.Part<V_Append>(
                                        Element.Part<V_Append>(
                                            Element.Part<V_Append>(
                                                Element.Part<V_Append>(
                                                    Element.Part<V_Append>(
                                                        Element.Part<V_Append>(
                                                            Element.Part<V_Append>(
                                                                Element.Part<V_Append>(
                                                                    Element.Part<V_Append>(
                                                                        Element.Part<V_Append>(
                                                                            Element.Part<V_Append>(
                                                                                Element.Part<V_Append>(
                                                                                    Element.Part<V_Append>(
                                                                                        Element.Part<V_Append>(
                                                                                            Element.Part<V_Append>(
                                                                                                Element.Part<V_Append>(
                                                                                                    Element.Part<V_Append>(
                                                                                                        Element.Part<V_Append>(
                                                                                                            Element.Part<V_Append>(
                                                                                                                Element.Part<V_Append>(
                                                                                                                    Element.Part<V_Append>(
                                                                                                                        Element.Part<V_Append>(
                                                                                                                            Element.Part<V_Append>(
                                                                                                                                Element.Part<V_Append>(
                                                                                                                                    Element.Part<V_Append>(
                                                                                                                                        Element.Part<V_Append>(
                                                                                                                                            Element.Part<V_Append>(
                                                                                                                                                Element.Part<V_Append>(
                                                                                                                                                    Element.Part<V_Append>(
                                                                                                                                                        Element.Part<V_Append>(
                                                                                                                                                            Element.Part<V_Append>(
                                                                                                                                                                Element.Part<V_Append>(
                                                                                                                                                                    Element.Part<V_Append>(
                                                                                                                                                                        Element.Part<V_Append>(
                                                                                                                                                                            Element.Part<V_Append>(
                                                                                                                                                                                Element.Part<V_Append>(
                                                                                                                                                                                    Element.Part<V_Append>(
                                                                                                                                                                                        Element.Part<V_Append>(Element.Part<V_EmptyArray>(), new V_Number(153)),
                                                                                                                                                                                    new V_Number(468)), 
                                                                                                                                                                                new V_Number(1196)), 
                                                                                                                                                                            new V_Number(135)), 
                                                                                                                                                                        new V_Number(139)), 
                                                                                                                                                                    new V_Number(477)), 
                                                                                                                                                                new V_Number(184)), 
                                                                                                                                                                Element.Part<V_FirstOf>(
                                                                                                                                                                    Element.Part<V_FilteredArray>(
                                                                                                                                                                        Element.Part<V_Append>(
                                                                                                                                                                            Element.Part<V_Append>(Element.Part<V_EmptyArray>(), new V_Number(343)), 
                                                                                                                                                                        new V_Number(347)), 
                                                                                                                                                                        Element.Part<V_Compare>(
                                                                                                                                                                            Element.Part<V_ArrayElement>(),
                                                                                                                                                                            EnumData.GetEnumValue(Operators.Equal),
                                                                                                                                                                            temp.GetVariable()
                                                                                                                                                                        )
                                                                                                                                                                    )
                                                                                                                                                                )
                                                                                                                                                            ), 
                                                                                                                                                            new V_Number(366)
                                                                                                                                                        ),
                                                                                                                                                        Element.Part<V_FirstOf>(
                                                                                                                                                            Element.Part<V_FilteredArray>(
                                                                                                                                                                Element.Part<V_Append>(
                                                                                                                                                                    Element.Part<V_Append>(Element.Part<V_EmptyArray>(), new V_Number(433)),
                                                                                                                                                                    new V_Number(436)
                                                                                                                                                                ), 
                                                                                                                                                                Element.Part<V_Compare>(
                                                                                                                                                                    Element.Part<V_ArrayElement>(), 
                                                                                                                                                                    EnumData.GetEnumValue(Operators.Equal), 
                                                                                                                                                                    temp.GetVariable()
                                                                                                                                                                )
                                                                                                                                                            )
                                                                                                                                                        )
                                                                                                                                                    ), 
                                                                                                                                                new V_Number(403)), 
                                                                                                                                                Element.Part<V_FirstOf>(
                                                                                                                                                    Element.Part<V_FilteredArray>(
                                                                                                                                                        Element.Part<V_Append>(
                                                                                                                                                            Element.Part<V_Append>(Element.Part<V_EmptyArray>(), new V_Number(382)),
                                                                                                                                                            new V_Number(384)
                                                                                                                                                        ), 
                                                                                                                                                        Element.Part<V_Compare>(
                                                                                                                                                            Element.Part<V_ArrayElement>(), 
                                                                                                                                                            EnumData.GetEnumValue(Operators.Equal), 
                                                                                                                                                            temp.GetVariable()
                                                                                                                                                        )
                                                                                                                                                    )
                                                                                                                                                )
                                                                                                                                            ), 
                                                                                                                                        new V_Number(993)), 
                                                                                                                                    new V_Number(386)), 
                                                                                                                                    Element.Part<V_FirstOf>(
                                                                                                                                        Element.Part<V_FilteredArray>(
                                                                                                                                            Element.Part<V_Append>(
                                                                                                                                                Element.Part<V_Append>(Element.Part<V_EmptyArray>(), new V_Number(331)), 
                                                                                                                                                new V_Number(348)
                                                                                                                                            ), 
                                                                                                                                            Element.Part<V_Compare>(
                                                                                                                                                Element.Part<V_ArrayElement>(), 
                                                                                                                                                EnumData.GetEnumValue(Operators.Equal), 
                                                                                                                                                temp.GetVariable()
                                                                                                                                            )
                                                                                                                                        )
                                                                                                                                    )
                                                                                                                                ), 
                                                                                                                                new V_Number(659)), 
                                                                                                                            new V_Number(145)), 
                                                                                                                        new V_Number(569)), 
                                                                                                                    new V_Number(384)), 
                                                                                                                new V_Number(1150)), 
                                                                                                            new V_Number(371)), 
                                                                                                        new V_Number(179)),
                                                                                                    new V_Number(497)), 
                                                                                                new V_Number(374)), 
                                                                                            new V_Number(312)), 
                                                                                        new V_Number(324)), 
                                                                                    new V_Number(434)), 
                                                                                new V_Number(297)), 
                                                                            new V_Number(276)), 
                                                                        new V_Number(330)), 
                                                                    new V_Number(376)), 
                                                                new V_Number(347)), 
                                                            new V_Number(480)), 
                                                        new V_Number(310)), 
                                                    new V_Number(342)), 
                                                new V_Number(360)), 
                                            new V_Number(364)), 
                                        new V_Number(372)), 
                                    new V_Number(370)), 
                                new V_Number(450)), 
                            new V_Number(356)), 
                        new V_Number(305)),
                    temp.GetVariable())
                )
            };

            return new MethodResult(actions, temp.GetVariable(), CustomMethodType.MultiAction_Value);
        }

        public static string GetName(MethodInfo methodInfo)
        {
            return $"{methodInfo.Name}({string.Join(", ", methodInfo.GetCustomAttributes<Parameter>().Select(v => $"{(v.ParameterType == ParameterType.Value ? v.ValueType.ToString() : v.EnumType.Name)}: {v.Name}"))})";
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
