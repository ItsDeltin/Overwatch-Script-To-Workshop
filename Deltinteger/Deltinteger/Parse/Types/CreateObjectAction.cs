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
                CreatingObjectOf = parseInfo.TranslateInfo.Types.GetCodeType(context.type.Text, parseInfo.Script.Diagnostics, DocRange.GetRange(context.type));
            
            if (CreatingObjectOf != null)
            {
                DocRange nameRange = DocRange.GetRange(context.type);

                // Get the constructor to use.
                OverloadChooser = new OverloadChooser(
                    CreatingObjectOf.Constructors, parseInfo, CreatingObjectOf.ReturningScope(), scope, nameRange, DocRange.GetRange(context), new OverloadError("type " + CreatingObjectOf.Name)
                );
                OverloadChooser.Apply(context.call_parameters());

                Constructor = (Constructor)OverloadChooser.Overload;
                ConstructorValues = OverloadChooser.Values ?? new IExpression[0];

                if (Constructor != null)
                {
                    parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(Constructor, new Location(parseInfo.Script.Uri, nameRange));
                    Constructor.Call(parseInfo, DocRange.GetRange(context.type));
                    parseInfo.Script.AddHover(DocRange.GetRange(context), Constructor.GetLabel(true));
                }
            }
        }

        public CodeType Type() => CreatingObjectOf;
        public Scope ReturningScope() => CreatingObjectOf?.GetObjectScope();

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            IWorkshopTree[] parameterValues = new IWorkshopTree[ConstructorValues.Length];
            for (int i = 0; i < parameterValues.Length; i++)
                parameterValues[i] = ConstructorValues[i].Parse(actionSet);

            return CreatingObjectOf.New(actionSet, Constructor, parameterValues, OverloadChooser.AdditionalParameterData);
        }
    }
}