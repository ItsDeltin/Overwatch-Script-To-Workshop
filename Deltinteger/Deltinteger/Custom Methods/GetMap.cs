using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.WorkshopWiki;

namespace Deltin.Deltinteger.Elements
{
    [CustomMethod("GetMap", CustomMethodType.MultiAction_Value)]
    class GetMap : CustomMethodBase
    {
        protected override MethodResult Get()
        {
            IndexedVar temp = TranslateContext.VarCollection.AssignVar(Scope, "GetMap: temp", TranslateContext.IsGlobal, null);

            Element[] actions = ArrayBuilder<Element>.Build
            (
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
            );

            return new MethodResult(actions, temp.GetVariable());
        }
    
        public override CustomMethodWiki Wiki()
        {
            return new CustomMethodWiki("Gets the current map. The result can be compared with the Map enum.");
        }
    }
}