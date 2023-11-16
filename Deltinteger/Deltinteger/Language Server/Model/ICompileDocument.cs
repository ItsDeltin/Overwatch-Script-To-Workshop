namespace Deltin.Deltinteger.LanguageServer;

using System;

interface ICompileDocument
{
    void OnKeypress(Uri activeModel);

    void OnSave(Uri activeModel);
}

class TimedScriptCompiler : ICompileDocument
{
    public void OnKeypress(Uri activeModel)
    {
        throw new NotImplementedException();
    }

    public void OnSave(Uri activeModel)
    {
        throw new NotImplementedException();
    }
}