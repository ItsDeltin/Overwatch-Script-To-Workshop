namespace Deltin.Deltinteger.GlobalFunctions;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using System.Linq;

partial class GlobalFunctions
{
    static FuncMethod PrintFileToWorkshopLog(DeltinScript deltinScript) => new FuncMethodBuilder()
    {
        Name = "PrintFileToWorkshopLog",
        Parameters = new CodeParameter[] {
            new TextFileParameter("file", "The file that will be printed via Log To Inspector.", deltinScript.Types),
            new ConstExpressionArrayParameter("format", "Any formats in the file will be replaced with the values in this array.", deltinScript.Types.AnyArray())
        },
        Documentation = new MarkupBuilder().Add("Takes the input text file and prints it into a series of ").Code("Log To Inspector").Add(" actions.")
            .NewLine().Add("This is used by the [Pathmap Editor](https://github.com/ItsDeltin/Pathmap-editor) to export the core pathfinding code via ").Code("Log To Inspector").Add(".")
            .NewLine().Add("Any format literals such as ").Code("{0}").Add(", ").Code("{1}").Add(", ect. not in strings will be replaced with the values provided in the format parameter."),
        Action = (actionSet, methodCall) =>
        {
            // Get content and formats from parameters.
            var content = (string)methodCall.AdditionalParameterData[0];
            var formats = (ConstExpressionArrayParameter.ConstWorkshopArray)methodCall.ParameterValues[1];
            // Extract content.
            var split = WorkshopStringUtility.ChunkSplit(content, "/*", "*/", new[] { '\'', '"' });

            foreach (var log in split)
            {
                var hasFormatPrevention = log.Any(l => l.HasFormatPrevention);
                var workshopString = StringElement.Join(log.Select(stub => Element.CustomString(stub.Value, stub.Parameters.Select(p =>
                    p < formats.Elements.Length ? formats.Elements[p] : Element.Null()).ToArray())).ToArray());

                // Text has a literal format, do a replace trick so that the workshop doesn't mess it up.
                if (hasFormatPrevention)
                    workshopString = Element.Part("String Replace",
                        workshopString,
                        new StringElement(WorkshopStringUtility.PREVENT_FORMAT_CHARACTER.ToString()),
                        new StringElement("{"));

                actionSet.AddAction(Elements.Element.LogToInspector(workshopString));
            }

            return null;
        }
    };
}