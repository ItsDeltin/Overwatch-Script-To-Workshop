#nullable enable
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Model;

namespace Deltin.Deltinteger.Emulator;

static class EmulateHelper
{
    public static Result<string, string> ExtractVariableName(IWorkshopTree workshopTree)
    {
        return ((workshopTree as WorkshopVariable)?.Name).OkOr($"Failed to convert '{workshopTree}' into a variable name");
    }

    public static Result<string, string> ExtractSubroutineName(IWorkshopTree workshopTree)
    {
        return ((workshopTree as Subroutine)?.Name).OkOr($"Failed to convert '{workshopTree}' into a subroutine name");
    }

    public static Result<Operation, string> ExtractOperation(IWorkshopTree workshopTree)
    {
        return ((workshopTree as OperationElement)?.Operation).OkOr("Failed to convert workshop item to operation");
    }

    public static Result<Operator, string> ExtractOperator(IWorkshopTree workshopTree)
    {
        return ((workshopTree as OperatorElement)?.Operator).OkOr("Failed to convert workshop item to operator");
    }
}