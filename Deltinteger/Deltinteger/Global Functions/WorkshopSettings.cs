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
            Action = (actionSet, methodCall) => Element.Part("Workshop Setting Hero",
                Element.CustomString((string)methodCall.AdditionalParameterData[0]),
                Element.CustomString((string)methodCall.AdditionalParameterData[1]),
                new AnonymousWorkshopValue(((ConstHeroValueResolver)methodCall.AdditionalParameterData[2]).Hero, true),
                Element.Num((double)methodCall.AdditionalParameterData[3])
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
            Action = (actionSet, methodCall) => Element.Part("Workshop Setting Combo",
                Element.CustomString((string)methodCall.AdditionalParameterData[0]),
                Element.CustomString((string)methodCall.AdditionalParameterData[1]),
                Element.Num((double)methodCall.AdditionalParameterData[2]),
                Element.CreateArray(((List<string>)methodCall.AdditionalParameterData[3]).Select(a => Element.CustomString(a)).ToArray()),
                Element.Num((double)methodCall.AdditionalParameterData[4])
            )
        };

        class ConstStringArrayParameter : CodeParameter
        {
            public ConstStringArrayParameter(string name, string documentation) : base(name, documentation) { }

            public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
            {
                var values = new List<string>();
                ConstantExpressionResolver.Resolve(value, expr =>
                {
                    // If the resulting expression is a CreateArray,
                    if (expr is CreateArrayAction array)
                    {
                        var error = new ConstStringElementErrorHandler(parseInfo.Script.Diagnostics, valueRange);

                        // Iterate through each element in the array and get the string value.
                        foreach (var value in array.Values)
                            ConstantExpressionResolver.Resolve(value, expr =>
                            {
                                // Make sure the value is a string.
                                if (value is StringAction stringAction)
                                    values.Add(stringAction.Value);
                                // Otherwise, add an error.
                                else
                                    error.AddError();
                            });
                    }
                    // Otherwise, add an error.
                    else if (valueRange != null)
                        parseInfo.Script.Diagnostics.Error("Expected a string array", valueRange);
                });
                return values;
            }

            class ConstStringElementErrorHandler
            {
                private readonly FileDiagnostics _diagnostics;
                private readonly DocRange _errorRange;
                private bool _addedError;

                public ConstStringElementErrorHandler(FileDiagnostics diagnostics, DocRange errorRange)
                {
                    _diagnostics = diagnostics;
                    _errorRange = errorRange;
                }

                public void AddError()
                {
                    if (_addedError) return;
                    _addedError = true;
                    _diagnostics.Error("One or more values in the string array is not a constant string expression", _errorRange);
                }
            }
        }
    }
}