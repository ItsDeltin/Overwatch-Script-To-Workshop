using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class CreateObjectAction : IExpression
    {
        private CodeType CreatingObjectOf { get; }
        private OverloadChooser OverloadChooser { get; }
        private Constructor Constructor { get; }

        public CreateObjectAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Create_objectContext context)
        {
            // Get the type. Syntax error if there is no type name.
            if (context.type == null)
                script.Diagnostics.Error("Expected a type name.", DocRange.GetRange(context.NEW()));
            else
                CreatingObjectOf = translateInfo.GetCodeType(context.type.Text, script.Diagnostics, DocRange.GetRange(context.type));
            
            if (CreatingObjectOf != null)
            {
                OverloadChooser = new OverloadChooser(
                    CreatingObjectOf.Constructors, script, translateInfo, scope, DocRange.GetRange(context.type), new OverloadError("type " + CreatingObjectOf.Name)
                );

                if (context.call_parameters() != null)
                    OverloadChooser.SetContext(context.call_parameters());
                else
                    OverloadChooser.SetContext();

                Constructor = (Constructor)OverloadChooser.Overload;
            }
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
            return CreatingObjectOf.New(actionSet, Constructor);
        }
    }
}