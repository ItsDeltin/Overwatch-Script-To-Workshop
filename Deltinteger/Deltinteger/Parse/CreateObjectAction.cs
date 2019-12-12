using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class CreateObjectAction : IExpression
    {
        private CodeType CreatingObjectOf { get; }

        public CreateObjectAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Create_objectContext context)
        {
            // Get the type. Syntax error if there is no type name.
            if (context.type == null)
                script.Diagnostics.Error("Expected a type name.", DocRange.GetRange(context.NEW()));
            else
                CreatingObjectOf = translateInfo.GetCodeType(context.type.Text, script.Diagnostics, DocRange.GetRange(context.type));
        }

        public CodeType Type()
        {
            return CreatingObjectOf;
        }

        public Scope ReturningScope()
        {
            return CreatingObjectOf.GetObjectScope();
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            // TODO: Create object
            throw new NotImplementedException();
        }
    }
}