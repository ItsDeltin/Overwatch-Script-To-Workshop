using System;
using Deltin.Deltinteger.Compiler;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class MissingElementAction : IExpression, IStatement
    {
        public static readonly MissingElementAction MissingElement = new MissingElementAction();

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
        public bool IsStatement() => true;
        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment) {}
        public IWorkshopTree Parse(ActionSet actionSet) => throw new NotImplementedException();
        public void Translate(ActionSet actionSet) => throw new NotImplementedException();
    }

    public class MissingVariable : IVariable, IExpression
    {
        public string Name { get; }
        public bool Static => true;
        public bool WholeContext => true;
        public LanguageServer.Location DefinedAt => null;
        public AccessLevel AccessLevel => AccessLevel.Public;

        public MissingVariable(string name)
        {
            Name = name;
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => null;
        public CompletionItem GetCompletion() => throw new NotImplementedException();
        public IWorkshopTree Parse(ActionSet actionSet) => throw new NotImplementedException();
    }
}