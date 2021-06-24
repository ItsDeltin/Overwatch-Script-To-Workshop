using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse.Overload;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class CreateObjectAction : IExpression, IStatement
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
            
            DocRange nameRange = context.Type.GenericToken.Range;

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

            if (Constructor != null)
            {
                Constructor.Call(parseInfo, nameRange);
                parseInfo.Script.AddHover(context.Range, Constructor.GetLabel(parseInfo.TranslateInfo, LabelInfo.Hover));
                
                // Default restricted parameter values.
                OverloadChooser.Match.CheckOptionalsRestrictedCalls(parseInfo, nameRange);

                // Bridge other restricted values.
                if (Constructor.CallInfo != null)
                    RestrictedCall.BridgeMethodCall(parseInfo, Constructor.CallInfo, nameRange, context.Type.GenericToken.Text, Constructor.RestrictedValuesAreFatal);
            }
        }

        public CodeType Type() => CreatingObjectOf;
        public Scope ReturningScope() => CreatingObjectOf?.GetObjectScope();

        public IWorkshopTree Parse(ActionSet actionSet) =>
            CreatingObjectOf.New(actionSet, Constructor, OverloadChooser.ParameterResults.Select(p => p.ToWorkshop(actionSet)).ToArray());
        
        public void Translate(ActionSet actionSet) => Parse(actionSet);
    }
}