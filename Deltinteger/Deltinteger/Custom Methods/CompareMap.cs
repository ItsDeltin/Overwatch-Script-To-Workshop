using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.CustomMethods
{
    [CustomMethod("CompareMap", "Compares the current map to a map value. Map variants are considered as well.", CustomMethodType.Value, typeof(BooleanType))]
    class CompareCurrentMap : CustomMethodBase
    {
        public override CodeParameter[] Parameters() => new CodeParameter[] {
            new MapParameter()
        };

        public override IWorkshopTree Get(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            var enumData = (ElementEnumMember)parameterValues[0];
            string map = enumData.Name;
            MapLink mapLink = MapLink.GetMapLink(map);

            if (mapLink == null)
                return Element.Compare(Element.Part("Current Map"), Operator.Equal, enumData.ToElement());
            else
                return Element.Contains(mapLink.GetArray(), Element.Part("Current Map"));
        }
    }

    class MapParameter : CodeParameter
    {
        public MapParameter() : base("map", "The map to compare.") {}

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData)
        {
            return base.Parse(actionSet, expression, additionalParameterData);
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
        {
            var variableCallAction = ExpressionTree.ResultingExpression(value) as EnumValuePair;

            if (variableCallAction == null || variableCallAction.Member.Enum.Name != "Map")
            {
                parseInfo.Script.Diagnostics.Error("Expected a map value.", valueRange);
                return null;
            }

            return variableCallAction.Member.Enum.Name;
        }
    }

    class MapLink
    {
        public string[] Maps { get; }

        public MapLink(params string[] maps)
        {
            Maps = maps;
        }

        public Element GetArray() => Element.CreateArray(
            // Convert the maps to EnumMembers encased in V_MapVar.
            Maps.Select(m => ElementRoot.Instance.GetEnumValueFromWorkshop("Map", m))
            .ToArray()
        );

        public static MapLink GetMapLink(string map) => MapLinks.FirstOrDefault(m => m.Maps.Contains(map));

        public static readonly MapLink[] MapLinks = new MapLink[] {
            new MapLink("Black Forest", "Black Forest (Winter)"),
            new MapLink("Blizzard World", "Blizzard World (Winter)"),
            new MapLink("Busan", "Busan Downtown (Lunar New Year)", "Busan Sanctuary (Lunar New Year)", "Busan Stadium"),
            new MapLink("Château Guillard", "Château Guillard (Halloween)"),
            new MapLink("Ecopoint: Antarctica", "Ecopoint: Antarctica (Winter)"),
            new MapLink("Eichenwalde", "Eichenwalde (Halloween)"),
            new MapLink("Hanamura", "Hanamura (Winter)"),
            new MapLink("Hollywood", "Hollywood (Halloween)"),
            new MapLink("Ilios", "Ilios Lighthouse", "Ilios Ruins", "Ilions Well"),
            new MapLink("Lijiang Control Center", "Lijiang Control Center (Lunar New Year)", "Lijiang Garden", "Lijiang Garden (Lunar New Year)", "Lijiang Night Market", "Lijiang Night Market (Lunar New Year)", "Lijiang Tower", "Lijiang Tower (Lunar New Year)"),
            new MapLink("Nepal", "Nepal Sanctum", "Nepal Shrine", "Nepal Village"),
            new MapLink("Oasis", "Oasis City Center", "Oasis Gardens", "Oasis University")
        };
    }
}
