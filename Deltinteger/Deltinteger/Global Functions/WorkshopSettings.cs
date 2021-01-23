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
                new ConstStringParameter("category", "The name of the category in which this setting can be found."),
                new ConstStringParameter("name", "The name of this setting."),
                new ConstHeroParameter("default", "The default value for this setting."),
                new ConstNumberParameter("sortOrder", "The sort order of this setting relative to other settings in the same category. Settings with a higher sort order will come after settings with a lower sort order.")
            },
            DoesReturnValue = true,
            Action = (actionSet, methodCall) => Element.Part<V_WorkshopSettingHero>(
                new V_CustomString((string)methodCall.AdditionalParameterData[0]),
                new V_CustomString((string)methodCall.AdditionalParameterData[1]),
                EnumData.GetEnumValue(((ConstHeroValueResolver)methodCall.AdditionalParameterData[2]).Hero),
                new V_Number((double)methodCall.AdditionalParameterData[3])
            )
        };

        public static FuncMethod WorkshopSettingCombo(DeltinScript deltinScript) => new FuncMethodBuilder()
        {
            Name = "WorkshopSettingCombo",
            Documentation = "Proves the value (a choice of strings) of a new option setting that will appear in the Workshop Seettings card as a combo box. This value returns the index of the selected choice.",
            Parameters = new CodeParameter[] {
                new ConstStringParameter("category", "The name of the category in which this setting can be found."),
                new ConstStringParameter("name", "The name of this setting."),
                new ConstNumberParameter("default", "The default value for this setting."),
                new ConstStringArrayParameter("options", "The options for this setting."),
                new ConstNumberParameter("sortOrder", "The sort order of this setting relative to other settings in the same category. Settings with a higher sort order will come after settings with a lower sort order.")
            },
            DoesReturnValue = true,
            Action = (actionSet, methodCall) => Element.Part<V_WorkshopSettingCombo>(
                new V_CustomString((string)methodCall.AdditionalParameterData[0]),
                new V_CustomString((string)methodCall.AdditionalParameterData[1]),
                new V_Number((double)methodCall.AdditionalParameterData[2]),
                Element.CreateArray(((List<string>)methodCall.AdditionalParameterData[3]).Select(a => new V_CustomString(a)).ToArray()),
                new V_Number((double)methodCall.AdditionalParameterData[4])
            )
        };
    }
}