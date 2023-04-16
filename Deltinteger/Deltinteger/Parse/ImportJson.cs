namespace Deltin.Deltinteger.Parse;
using JsonSyntax = Deltin.Deltinteger.Compiler.SyntaxTree.ImportJsonSyntax;
using System;
using System.IO;

class ImportJson : IExpression
{
    public ImportJson(JsonSyntax syntax)
    {
        // Load the file
        if (!syntax.File)
            return;

        // try
        // {
        //     File.ReadAllText(syntax.File);
        // }
        // catch (Exception ex)
        // {
        //     if (ex.InnerException)
        // }
    }

    public IWorkshopTree Parse(ActionSet actionSet)
    {
        throw new System.NotImplementedException();
    }

    public CodeType Type()
    {
        throw new System.NotImplementedException();
    }
}