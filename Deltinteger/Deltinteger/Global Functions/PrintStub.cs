namespace Deltin.Deltinteger.GlobalFunctions;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Elements;
using System.Linq;

partial class GlobalFunctions
{
    static FuncMethod PrintStub(DeltinScript deltinScript) => new FuncMethodBuilder()
    {
        Name = "PrintStub",
        Parameters = new[] {
            new TextFileParameter("file", "", deltinScript.Types)
        },
        Documentation = new MarkupBuilder().Add("Takes the input text file and prints it into a series of ").Code("Log To Inspector").Add(" actions."),
        Action = (actionSet, methodCall) =>
        {
            var content = (string)methodCall.AdditionalParameterData[0];
            var split = WorkshopStringUtility.ChunkSplit(content, "/*", "*/", new[] { '\'', '"' });

            foreach (var log in split)
                actionSet.AddAction(Elements.Element.LogToInspector(
                    StringElement.Join(log.Select(l => new StringElement(l, false)).ToArray())
                ));

            return null;
        }
    };
}