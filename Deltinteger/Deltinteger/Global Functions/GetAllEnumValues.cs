using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Parse.Types;

namespace Deltin.Deltinteger.GlobalFunctions;

partial class GlobalFunctions
{
    public static FuncMethod GetAllEnumValues(DeltinScript deltinScript)
    {
        var typeValidator = ICustomTypeArgValidator.New((inputType, errorToken) =>
        {
            // 'ValueGroupType' refers to built-in workshop data types,
            // 'EnumType' refers to user-defined enums.
            if (inputType is not (ValueGroupType or EnumType))
            {
                errorToken.Error("Type argument must be an enumerator");
            }
            // Ensure that the enum has no inner values.
            else if (inputType is EnumType enumType && enumType.EnumKind is not EnumKind.NoInnerValues)
            {
                errorToken.Error("Enumerator type must not have inner values");
            }
            // Enum that is a constant type.
            else if (inputType.IsConstant())
            {
                errorToken.Error("Type argument cannot be constant");
            }
        });

        var enumTypeArgument = new AnonymousType("T", new(typeValidator));

        var methodInfo = new MethodInfo(new[] { enumTypeArgument });
        enumTypeArgument.Context = methodInfo.Tracker;

        return new FuncMethodBuilder()
        {
            Name = "GetAllEnumValues",
            Documentation = "Extracts all the values in an enum into a workshop array. Type argument T must be a workshop or user-declared enumerator.",
            MethodInfo = methodInfo,
            ReturnType = new ArrayType(deltinScript.Types, enumTypeArgument),
            Action = (actionSet, methodCall) =>
            {
                var inputType = methodCall.TypeArgs.Links[enumTypeArgument];
                var allValues = inputType.ReturningScope().Variables.Select(v => v.ToWorkshop(actionSet)).ToArray();

                return Element.CreateArray(allValues);
            }
        };
    }
}