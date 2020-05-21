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

        private MethodAttributeHandler[] attributes;

        public DefinedMacro(ParseInfo parseInfo, Scope objectScope, Scope staticScope, DeltinScriptParser.Define_macroContext context, CodeType returnType, bool addToScope)
            : base(parseInfo, context.name.Text, new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)), addToScope)
        {
            this.context = context;
            DocRange nameRange = DocRange.GetRange(context.name);
            Attributes.ContainingType = (Static ? staticScope: objectScope).This;
            GetAttributes();

            
            SetupScope(Static ? staticScope : objectScope);
            ReturnType = returnType;
            ExpressionToParse = context.expr();
            doesReturnValue = true;
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
                        attributes.First(at => at.Type == MethodAttributeType.Override).Range,
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


            // Parse the expression.
            //return Expression.Parse(actionSet);
        }

        public void AssignParameters(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            for (int i = 0; i < ParameterVars.Length; i++)
            {
                IGettable result = actionSet.IndexAssigner.Add(ParameterVars[i], parameterValues[i]);

                //if (indexResult is IndexReference indexReference && parameterValues?[i] != null)
                    //actionSet.AddAction(indexReference.SetVariable((Element)parameterValues[i]));

                foreach (Var virtualParameterOption in VirtualVarGroup(i))
                {
                    actionSet.IndexAssigner.Add(virtualParameterOption, result);
                }

            }
        }

        public Var[] VirtualVarGroup(int i)
        {
            List<Var> parameters = new List<Var>();

            foreach (var macroOverrider in Attributes.AllOverrideOptions())
                parameters.Add(((DefinedMacro)macroOverrider).ParameterVars[i]);

            return parameters.ToArray();
        }


        private void GetAttributes()
        {
            // If the STRINGLITERAL is not null, the method will be stored in a subroutine.
            // Get the name of the rule the method will be stored in.

            // method_attributes will ne null if there are no attributes.
            if (context.method_attributes() == null) return;

            int numberOfAttributes = context.method_attributes().Length;
            attributes = new MethodAttributeHandler[numberOfAttributes];

            // Loop through all attributes.
            for (int i = 0; i < numberOfAttributes; i++)
            {
                var newAttribute = new MethodAttributeHandler(context.method_attributes(i));
                attributes[i] = newAttribute;

                bool wasCopy = false;

                // If the attribute already exists, syntax error.
                for (int c = i - 1; c >= 0; c--)
                    if (attributes[c].Type == newAttribute.Type)
                    {
                        newAttribute.Copy(parseInfo.Script.Diagnostics);
                        wasCopy = true;
                        break;
                    }

                // Additonal syntax errors. Only throw if the attribute is not a copy.
                if (!wasCopy)
                {
                    // Virtual attribute on a static method (static attribute was first.)
                    if (Static && newAttribute.Type == MethodAttributeType.Virtual)
                        parseInfo.Script.Diagnostics.Error("Static macros cannot be virtual.", newAttribute.Range);

                    // Static attribute on a virtual method (virtual attribute was first.)
                    if (Attributes.Virtual && newAttribute.Type == MethodAttributeType.Static)
                        parseInfo.Script.Diagnostics.Error("Virtual macros cannot be static.", newAttribute.Range);
                }

                // Apply the attribute.
                switch (newAttribute.Type)
                {
                    // Apply accessor
                    case MethodAttributeType.Accessor: AccessLevel = newAttribute.AttributeContext.accessor().GetAccessLevel(); break;

                    // Apply static
                    case MethodAttributeType.Static: Static = true; break;

                    // Apply virtual
                    case MethodAttributeType.Virtual: Attributes.Virtual = true; break;

                    // Apply override
                    case MethodAttributeType.Override: Attributes.Override = true; break;

                    // Apply Recursive
                    case MethodAttributeType.Recursive: parseInfo.Script.Diagnostics.Error("Macros cannot be recursive.", newAttribute.Range); break;
                }
            }
        }
    }
}