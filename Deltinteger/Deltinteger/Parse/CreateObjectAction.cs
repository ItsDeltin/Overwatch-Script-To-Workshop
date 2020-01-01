using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class CreateObjectAction : IExpression
    {
        private CodeType CreatingObjectOf { get; }
        private OverloadChooser OverloadChooser { get; }
        private Constructor Constructor { get; }
        private IExpression[] ConstructorValues { get; }

        public CreateObjectAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Create_objectContext context)
        {
            // Get the type. Syntax error if there is no type name.
            if (context.type == null)
                parseInfo.Script.Diagnostics.Error("Expected a type name.", DocRange.GetRange(context.NEW()));
            else
                CreatingObjectOf = parseInfo.TranslateInfo.GetCodeType(context.type.Text, parseInfo.Script.Diagnostics, DocRange.GetRange(context.type));
            
            if (CreatingObjectOf != null)
            {
                DocRange nameRange = DocRange.GetRange(context.type);

                // Get the constructor to use.
                OverloadChooser = new OverloadChooser(
                    CreatingObjectOf.Constructors, parseInfo, scope, nameRange, DocRange.GetRange(context), new OverloadError("type " + CreatingObjectOf.Name)
                );

                if (context.call_parameters() != null)
                    OverloadChooser.SetContext(context.call_parameters());
                else
                    OverloadChooser.SetContext();

                Constructor = (Constructor)OverloadChooser.Overload;
                ConstructorValues = OverloadChooser.Values ?? new IExpression[0];

                if (Constructor != null)
                {
                    Constructor.Call(parseInfo.Script, DocRange.GetRange(context.type));
                    parseInfo.Script.AddHover(DocRange.GetRange(context), Constructor.GetLabel(true));

                    if (Constructor is DefinedConstructor)
                        parseInfo.CurrentCallInfo?.Call((DefinedConstructor)Constructor, nameRange);
                }
            }
        }

        public CodeType Type() => CreatingObjectOf;
        public Scope ReturningScope() => CreatingObjectOf.GetObjectScope();

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            IWorkshopTree[] parameterValues = new IWorkshopTree[ConstructorValues.Length];
            for (int i = 0; i < parameterValues.Length; i++)
                parameterValues[i] = ConstructorValues[i].Parse(actionSet);

            return CreatingObjectOf.New(actionSet, Constructor, parameterValues, OverloadChooser.AdditionalParameterData);
        }
    }
}