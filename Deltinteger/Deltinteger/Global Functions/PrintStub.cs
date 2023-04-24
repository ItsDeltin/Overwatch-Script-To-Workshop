namespace Deltin.Deltinteger.GlobalFunctions;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using System.Linq;

partial class GlobalFunctions
{
    static FuncMethod PrintStub(DeltinScript deltinScript) => new FuncMethodBuilder()
    {
        Name = "PrintStub",
        Parameters = new CodeParameter[] {
            new TextFileParameter("file", "", deltinScript.Types),
            new ConstExpressionArrayParameter("format", "", deltinScript.Types.Any())
        },
        Documentation = new MarkupBuilder().Add("Takes the input text file and prints it into a series of ").Code("Log To Inspector").Add(" actions."),
        Action = (actionSet, methodCall) =>
        {
            // Get content and formats from parameters.
            var content = (string)methodCall.AdditionalParameterData[0];
            var formats = (ConstExpressionArrayParameter.ConstWorkshopArray)methodCall.ParameterValues[1];
            // Extract content.
            var split = WorkshopStringUtility.ChunkSplit(content, "/*", "*/", new[] { '\'', '"' });

            foreach (var log in split)
                actionSet.AddAction(Elements.Element.LogToInspector(
                    StringElement.Join(log.Select(l => new StringElement(l.Value, false, l.Parameters.Select(p =>
                    {
                        return p < formats.Elements.Length ? formats.Elements[p] : Element.Null();
                    }).ToArray())).ToArray())
                ));

            return null;
        }
    };
}