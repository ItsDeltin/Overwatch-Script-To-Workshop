using System;
using Deltin.Deltinteger.Compiler;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Deltin.Deltinteger.Parse
{
    public class MissingElementAction : IExpression, IStatement
    {
        private readonly CodeType _type;

        public MissingElementAction(DeltinScript deltinScript)
        {
            _type = deltinScript.Types.Any();
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => _type;
        public bool IsStatement() => true;
        public bool ExpressionErrorHandled() => true;
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
        public CodeType CodeType { get; }

        public MissingVariable(DeltinScript deltinScript, string name)
        {
            Name = name;
            CodeType = deltinScript.Types.Any();
        }

        public Scope ReturningScope() => null;
        public CodeType Type() => CodeType;
        public CompletionItem GetCompletion() => throw new NotImplementedException();
        public IWorkshopTree Parse(ActionSet actionSet) => throw new NotImplementedException();
    }
}