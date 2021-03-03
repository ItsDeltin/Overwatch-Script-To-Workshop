using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse.Overload;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class CreateObjectAction : IExpression
    {
        private CodeType CreatingObjectOf { get; }
        private OverloadChooser OverloadChooser { get; }
        private Constructor Constructor { get; }
        private IExpression[] ConstructorValues { get; }

        public CreateObjectAction(ParseInfo parseInfo, Scope scope, NewExpression context)
        {
            if (!context.Type.Valid)
            {
                CreatingObjectOf = parseInfo.Types.Any();
                return;
            }

            // Get the type. Syntax error if there is no type name.
            CreatingObjectOf = TypeFromContext.GetCodeTypeFromContext(parseInfo, scope, context.Type);
            
            DocRange nameRange = context.Type.Range;

            // Get the constructor to use.
            OverloadChooser = new OverloadChooser(
                CreatingObjectOf.Constructors.Select(c => new ConstructorOverload(c)).ToArray(),
                parseInfo,
                CreatingObjectOf.ReturningScope(),
                scope,
                nameRange,
                context.Range,
                context.Range,
                new OverloadError("type " + CreatingObjectOf.Name)
            );
            OverloadChooser.Apply(context.Parameters, false, null);

            Constructor = (Constructor)OverloadChooser.Overload;
            ConstructorValues = OverloadChooser.Values ?? new IExpression[0];

            if (Constructor != null)
            {
                parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(Constructor, new Location(parseInfo.Script.Uri, nameRange));
                Constructor.Call(parseInfo, nameRange);
                parseInfo.Script.AddHover(context.Range, Constructor.GetLabel(parseInfo.TranslateInfo, LabelInfo.Hover));
                
                // Default restricted parameter values.
                OverloadChooser.Match.CheckOptionalsRestrictedCalls(parseInfo, nameRange);

                // Bridge other restricted values.
                if (Constructor is IApplyBlock applyBlock)
                    foreach (RestrictedCallType type in applyBlock.CallInfo.GetRestrictedCallTypes())
                        parseInfo.RestrictedCallHandler.RestrictedCall(new RestrictedCall(
                            type,
                            parseInfo.GetLocation(nameRange),
                            RestrictedCall.Message_FunctionCallsRestricted(context.Type.GenericToken.Text, type),
                            true
                            // todo Constructor.RestrictedValuesAreFatal
                        ));
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