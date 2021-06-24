using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod CompareMap(DeltinScript deltinScript) => new FuncMethodBuilder() {
            Name = "CompareMap",
            Documentation = "Compares the current map to a map value. Map variants are considered as well.",
            ReturnType = deltinScript.Types.Boolean(), 
            Parameters = new[] { new MapParameter(deltinScript.Types) },
            Action = (actionSet, methodCall) => {
                var enumData = (ElementEnumMember)methodCall.ParameterValues[0];
                string map = enumData.Name;
                MapLink mapLink = MapLink.GetMapLink(map);

                if (mapLink == null)
                    return Element.Compare(Element.Part("Current Map"), Operator.Equal, enumData.ToElement());
                else
                    return Element.Contains(mapLink.GetArray(), Element.Part("Current Map"));
            }
        };

        class MapParameter : CodeParameter
        {
            public MapParameter(ITypeSupplier typeSupplier) : base("map", "The map to compare.", typeSupplier.Map()) { }

            public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData)
            {
                return base.Parse(actionSet, expression, additionalParameterData);
            }

            public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
            {
                ConstantExpressionResolver.Resolve(value, value => {
                    // Parameter value is map.
                    if (!(value is EnumValuePair enumValue && enumValue.Member.Enum.Name == "Map"))
                        parseInfo.Script.Diagnostics.Error("Expected a map value.", valueRange);
                });

                return null;
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
}