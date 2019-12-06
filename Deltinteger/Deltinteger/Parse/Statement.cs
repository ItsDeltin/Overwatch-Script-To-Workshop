using System;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public interface IStatement
    {
        void Translate(ActionSet actionSet);
    }
}