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

            Var a = varCollection.AssignVar(isGlobal);
            Var b = varCollection.AssignVar(isGlobal);
            Var c = varCollection.AssignVar(isGlobal);
            Var ab = varCollection.AssignVar(isGlobal);
            Var bc = varCollection.AssignVar(isGlobal);
            Var abVec = varCollection.AssignVar(isGlobal);
            Var bcVec = varCollection.AssignVar(isGlobal);
            Var abNorm = varCollection.AssignVar(isGlobal);
            Var bcNorm = varCollection.AssignVar(isGlobal);

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

        /*
         AngleOfVectors condensed into one action.
         Don't uncomment this monstrocity, please.

        [CustomMethod("AngleOfVectorsCom", CustomMethodType.Value)]
        static MethodResult AngleOfVectorsCom(bool isGlobal, object[] parameters)
        {
            // AngleOfVectors compacted down into one element
            // for the love of god, don't use this
            // please
            // why am I even including this

            Element zeroVec = Element.Part<V_Vector>(new V_Number(0), new V_Number(0), new V_Number(0));
            Element a = (Element)parameters[0];
            Element b = (Element)parameters[1];
            Element c = (Element)parameters[2];

            Element ab = Element.Part<V_Vector>
                (
                    Element.Part<V_Subtract>(Element.Part<V_XComponentOf>(b), Element.Part<V_XComponentOf>(a)),
                    Element.Part<V_Subtract>(Element.Part<V_YComponentOf>(b), Element.Part<V_YComponentOf>(a)),
                    Element.Part<V_Subtract>(Element.Part<V_ZComponentOf>(b), Element.Part<V_ZComponentOf>(a))
                );
            Element bc = Element.Part<V_Vector>
                (
                    Element.Part<V_Subtract>(Element.Part<V_XComponentOf>(c), Element.Part<V_XComponentOf>(b)),
                    Element.Part<V_Subtract>(Element.Part<V_YComponentOf>(c), Element.Part<V_YComponentOf>(b)),
                    Element.Part<V_Subtract>(Element.Part<V_ZComponentOf>(c), Element.Part<V_ZComponentOf>(b))
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

            return new MethodResult(null, res, CustomMethodType.MultiAction_Value);
        }
        */

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
                        ParameterValues = new object[2]
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
                        Rounding.Down
                    ),
                    new V_Number(4)
                ),
                Rounding.Down)
            ), CustomMethodType.Value);
        }

        [CustomMethod("RandomValuesInArray", CustomMethodType.Value)]
        [Parameter("Array", ValueType.Any, typeof(V_AllPlayers))]
        [Parameter("Count", ValueType.Number, typeof(V_Number))]
        static MethodResult RandomValuesInArray(bool isGlobal, object[] parameters)
        {
            Element array = parameters[0] as Element;
            Element count = parameters[1] as Element;
            return new MethodResult(null, Element.Part<V_ArraySlice>(Element.Part<V_RandomizedArray>(array), count), CustomMethodType.Value);
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
