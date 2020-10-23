using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedMacro : DefinedFunction
    {
        public IExpression Expression { get; private set; }
        private readonly MacroFunctionContext _context;

        public DefinedMacro(ParseInfo parseInfo, Scope objectScope, Scope staticScope, MacroFunctionContext context, CodeType returnType)
            : base(parseInfo, context.Identifier.Text, new LanguageServer.Location(parseInfo.Script.Uri, context.Identifier.Range))
        {
            _context = context;
            DocRange nameRange = context.Identifier.Range;
            Attributes.ContainingType = (Static ? staticScope: objectScope).This;
            
            // Get the attributes.
            MethodAttributeAppender attributeResult = new MethodAttributeAppender(Attributes);
            FunctionAttributesGetter attributeInfo = new MacroAttributesGetter(context, attributeResult);
            attributeInfo.GetAttributes(parseInfo.Script.Diagnostics);

            // Copy attribute results
            Static = attributeResult.Static;
            AccessLevel = attributeResult.AccessLevel;
            
            SetupScope(Static ? staticScope : objectScope);
            CodeType = returnType;
            DoesReturnValue = true;

            SetupParameters(context.Parameters, false);

            if (Attributes.Override)
            {
                IMethod overriding = objectScope.GetMethodOverload(this);

                // No method with the name and parameters found.
                if (overriding == null) parseInfo.Script.Diagnostics.Error("Could not find a macro to override.", nameRange);
                else if (!overriding.Attributes.IsOverrideable) parseInfo.Script.Diagnostics.Error("The specified method is not marked as virtual.", nameRange);
                else overriding.Attributes.AddOverride(this);

                if (overriding != null && overriding.DefinedAt != null)
                {
                    // Make the override keyword go to the base method.
                    parseInfo.Script.AddDefinitionLink(
                        attributeInfo.ObtainedAttributes.First(at => at.Type == MethodAttributeType.Override).Range,
                        overriding.DefinedAt
                    );
                }
            }

            containingScope.AddMethod(this, parseInfo.Script.Diagnostics, DefinedAt.range, !Attributes.Override);

            if (Attributes.IsOverrideable && AccessLevel == AccessLevel.Private)
                parseInfo.Script.Diagnostics.Error("A method marked as virtual or abstract must have the protection level 'public' or 'protected'.", nameRange);

            if (Attributes.IsOverrideable)
                parseInfo.Script.AddCodeLensRange(new ImplementsCodeLensRange(this, parseInfo.Script, CodeLensSourceType.Function, nameRange));

        }

        public override void SetupParameters()
        {
            parseInfo.Script.AddHover(_context.Identifier.Range, GetLabel(true));
        }

        override public void SetupBlock()
        {
            Expression = parseInfo.SetCallInfo(CallInfo).GetExpression(methodScope, _context.Expression);
            WasApplied = true;
            foreach (var listener in listeners) listener.Applied();
        }

        override public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            // Assign the parameters.
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            return AbstractMacroBuilder.Call(actionSet, this, methodCall);
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