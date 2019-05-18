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
        static MethodResult AngleOfVectors(bool isGlobal, object[] parameters)
        {
            var eventPlayer = new V_EventPlayer();

            Var a      = Var.AssignVar(isGlobal);
            Var b      = Var.AssignVar(isGlobal);
            Var c      = Var.AssignVar(isGlobal);
            Var ab     = Var.AssignVar(isGlobal);
            Var bc     = Var.AssignVar(isGlobal);
            Var abVec  = Var.AssignVar(isGlobal);
            Var bcVec  = Var.AssignVar(isGlobal);
            Var abNorm = Var.AssignVar(isGlobal);
            Var bcNorm = Var.AssignVar(isGlobal);

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

        [CustomMethod("GetMapID", CustomMethodType.MultiAction_Value)]
        static MethodResult GetMapID(bool isGlobal, object[] parameters)
        {
            /*
             All credit to https://us.forums.blizzard.com/en/overwatch/t/workshop-resource-get-the-current-map-name-updated-1-action/
             Based off code: 5VAQA
            */

            int mapcount = 0;
            for (int i = 0; i < Constants.MapChecks.Length; i++)
                mapcount += Constants.MapChecks[i].Length;

            Var work = Var.AssignVarRange(isGlobal, mapcount);

            List<Element> sets = new List<Element>();

            for (int s = 0; s < Constants.MapChecks.Length; s++)
            {
                V_AppendToArray prev = null;
                V_AppendToArray current = null;

                for (int i = 0; i < Constants.MapChecks[s].Length; i++)
                {
                    current = new V_AppendToArray();
                    current.ParameterValues = new object[2];

                    if (prev != null)
                        current.ParameterValues[0] = prev;
                    else if (s > 0)
                        current.ParameterValues[0] = work.GetVariable();
                    else
                        current.ParameterValues[0] = new V_EmptyArray();

                    // Set the map ID
                    current.ParameterValues[1] = new V_Number(Constants.MapChecks[s][i]);
                    prev = current;
                }

                sets.Add(work.SetVariable(current));
            }

            return new MethodResult(sets.ToArray(),
                Element.Part<V_IndexOfArrayValue>(work.GetVariable(),
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
            ), CustomMethodType.MultiAction_Value);
        }

        [CustomMethod("GetMapIDCom", CustomMethodType.MultiAction_Value)]
        static MethodResult GetMapIDCom(bool isGlobal, object[] parameters)
        {
            /*
             All credit to https://us.forums.blizzard.com/en/overwatch/t/workshop-resource-get-the-current-map-name-updated-1-action/
             Based off code: 5VAQA
            */

            int mapcount = 0;
            for (int i = 0; i < Constants.MapChecks.Length; i++)
                mapcount += Constants.MapChecks[i].Length;

            List<Element> sets = new List<Element>();

            V_AppendToArray prev = null;
            V_AppendToArray current = null;
            for (int s = 0; s < Constants.MapChecks.Length; s++)
                for (int i = 0; i < Constants.MapChecks[s].Length; i++)
                {
                    current = new V_AppendToArray();
                    current.ParameterValues = new object[2];

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
            ), CustomMethodType.MultiAction_Value);
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
