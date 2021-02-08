using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.GlobalFunctions
{
    partial class GlobalFunctions
    {
        public static FuncMethod WorkshopSettingHero(DeltinScript deltinScript) => new FuncMethodBuilder()
        {
            Name = "WorkshopSettingHero",
            Documentation = "Provides the value of a new hero setting that will appear in the Workshop Settings card as a combo box.",
            Parameters = new CodeParameter[] {
                new ConstStringParameter("category", "The name of the category in which this setting can be found.", deltinScript.Types),
                new ConstStringParameter("name", "The name of this setting.", deltinScript.Types),
                new ConstHeroParameter("default", "The default value for this setting.", deltinScript.Types),
                new ConstNumberParameter("sortOrder", "The sort order of this setting relative to other settings in the same category. Settings with a higher sort order will come after settings with a lower sort order.", deltinScript.Types)
            },
            Action = (actionSet, methodCall) => Element.Part("Workshop Setting Hero",
                Element.CustomString((string)methodCall.AdditionalParameterData[0]),
                Element.CustomString((string)methodCall.AdditionalParameterData[1]),
                new AnonymousWorkshopValue(((ConstHeroValueResolver)methodCall.AdditionalParameterData[2]).Hero.I18nIdentifier(), true),
                Element.Num((double)methodCall.AdditionalParameterData[3])
            )
        };

        public static FuncMethod WorkshopSettingCombo(DeltinScript deltinScript) => new FuncMethodBuilder()
        {
            Name = "WorkshopSettingCombo",
            Documentation = "Proves the value (a choice of strings) of a new option setting that will appear in the Workshop Seettings card as a combo box. This value returns the index of the selected choice.",
            Parameters = new CodeParameter[] {
                new ConstStringParameter("category", "The name of the category in which this setting can be found.", deltinScript.Types),
                new ConstStringParameter("name", "The name of this setting.", deltinScript.Types),
                new ConstNumberParameter("default", "The default value for this setting.", deltinScript.Types),
                new ConstStringArrayParameter("options", "The options for this setting.", deltinScript.Types),
                new ConstNumberParameter("sortOrder", "The sort order of this setting relative to other settings in the same category. Settings with a higher sort order will come after settings with a lower sort order.", deltinScript.Types)
            },
            Action = (actionSet, methodCall) => Element.Part("Workshop Setting Combo",
                Element.CustomString((string)methodCall.AdditionalParameterData[0]),
                Element.CustomString((string)methodCall.AdditionalParameterData[1]),
                Element.Num((double)methodCall.AdditionalParameterData[2]),
                Element.CreateArray(((List<string>)methodCall.AdditionalParameterData[3]).Select(a => Element.CustomString(a)).ToArray()),
                Element.Num((double)methodCall.AdditionalParameterData[4])
            )
        };
    }
}