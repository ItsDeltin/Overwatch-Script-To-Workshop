using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Parse.Functions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedMacro : DefinedFunction
    {
        public IExpression Expression { get; private set; }
        private DeltinScriptParser.ExprContext ExpressionToParse { get; }
        private DeltinScriptParser.Define_macroContext context { get; }

        private readonly FunctionAttributesGetter attributeInfo;

        public DefinedMacro(ParseInfo parseInfo, Scope objectScope, Scope staticScope, DeltinScriptParser.Define_macroContext context, CodeType returnType, bool addToScope)
            : base(parseInfo, context.name.Text, new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)), addToScope)
        {
            this.context = context;
            DocRange nameRange = DocRange.GetRange(context.name);
            Attributes.ContainingType = (Static ? staticScope: objectScope).This;
            
            // Get the attributes.
            attributeInfo = new MacroAttributesGetter(context, Attributes);
            attributeInfo.GetAttributes(parseInfo.Script.Diagnostics);
            
            SetupScope(Static ? staticScope : objectScope);
            ReturnType = returnType;
            ExpressionToParse = context.expr();
            DoesReturnValue = true;
            if(!addToScope)
            {
                Expression = parseInfo.SetCallInfo(CallInfo).GetExpression(methodScope, ExpressionToParse);
            }

            SetupParameters(context.setParameters(), false);

            if (Attributes.Override)
            {
                IMethod overriding = objectScope.GetMethodOverload(this);

                // No method with the name and parameters found.
                if (overriding == null) parseInfo.Script.Diagnostics.Error("Could not find a macro to override.", nameRange);
                else if (!overriding.Attributes.IsOverrideable) parseInfo.Script.Diagnostics.Error("The specified method is not marked as virtual.", nameRange);
                else overriding.Attributes.AddOverride(this);

                if (overriding != null && overriding.DefinedAt != null && addToScope)
                {
                    // Make the override keyword go to the base method.
                    parseInfo.Script.AddDefinitionLink(
                        attributeInfo.ObtainedAttributes.First(at => at.Type == MethodAttributeType.Override).Range,
                        overriding.DefinedAt
                    );
                }
            }

            if(addToScope)
                containingScope.AddMethod(this, parseInfo.Script.Diagnostics, DefinedAt.range, !Attributes.Override);

            if (Attributes.IsOverrideable && AccessLevel == AccessLevel.Private)
                parseInfo.Script.Diagnostics.Error("A method marked as virtual or abstract must have the protection level 'public' or 'protected'.", nameRange);

            if (Attributes.IsOverrideable)
                parseInfo.Script.AddCodeLensRange(new ImplementsCodeLensRange(this, parseInfo.Script, CodeLensSourceType.Function, nameRange));

        }

        public override void SetupParameters()
        {
            //SetupParameters(context.setParameters(), false);
            parseInfo.Script.AddHover(DocRange.GetRange(context.name), GetLabel(true));
        }

        override public void SetupBlock()
        {
            if (ExpressionToParse != null) Expression = parseInfo.SetCallInfo(CallInfo).GetExpression(methodScope, ExpressionToParse);
            foreach (var listener in listeners) listener.Applied();
        }

        override public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            // Assign the parameters.
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            return MacroBuilder.Call(this, methodCall, actionSet);
        }

        public void AssignParameters(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            for (int i = 0; i < ParameterVars.Length; i++)
            {
                IGettable result = actionSet.IndexAssigner.Add(ParameterVars[i], parameterValues[i]);

                //if (indexResult is IndexReference indexReference && parameterValues?[i] != null)
                    //actionSet.AddAction(indexReference.SetVariable((Element)parameterValues[i]));

                foreach (Var virtualParameterOption in VirtualVarGroup(i))
                    actionSet.IndexAssigner.Add(virtualParameterOption, result);
            }
        }
    }
}