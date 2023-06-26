namespace Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;

public record struct WorkshopVariablePosition(WorkshopVariable WorkshopVariable, Element[] Index, Element? Target);