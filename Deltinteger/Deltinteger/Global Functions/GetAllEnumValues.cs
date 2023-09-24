using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;
using Deltin.Deltinteger.Parse;

namespace Deltin.Deltinteger.GlobalFunctions;

partial class GlobalFunctions
{
    public static FuncMethod GetAllEnumValues(DeltinScript deltinScript)
    {
        var typeValidator = ICustomTypeArgValidator.New((inputType, errorToken) =>
        {
            if (inputType is not ValueGroupType && inputType is not DefinedEnum)
            {
                errorToken.Error("Type argument must be an enumerator");
            }
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