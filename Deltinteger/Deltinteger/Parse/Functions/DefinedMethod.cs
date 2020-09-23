using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.FunctionBuilder;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedMethod : DefinedFunction
    {
        /// <summary>The context of the function.</summary>
        public FunctionContext Context { get; }

        // Attributes
        /// <summary>Determines if the function is a subroutine.</summary>
        public bool IsSubroutine { get; private set; }
        /// <summary>The name of the subroutine. Will be null if IsSubroutine is false.</summary>
        public string SubroutineName { get; private set; }

        // Block data
        /// <summary>The block of the function.</summary>
        public BlockAction Block { get; private set; }

        /// <summary>If there is only one return statement, return the reference to
        /// the return expression instead of assigning it to a variable to reduce the number of actions.</summary>
        public bool MultiplePaths { get; private set; }

        /// <summary>If there is only one return statement, this will be the statement being returned.</summary>
        public IExpression SingleReturnValue { get; private set; }

        public DefinedMethod virtualSubroutineAssigned { get; set; }
        public SubroutineInfo subroutineInfo { get; private set; }
        public readonly bool _subroutineDefaultGlobal;

        public DefinedMethod(ParseInfo parseInfo, Scope objectScope, Scope staticScope, FunctionContext context, CodeType containingType)
            : base(parseInfo, context.Identifier.Text, new Location(parseInfo.Script.Uri, context.Identifier.Range))
        {
            this.Context = context;

            Attributes.ContainingType = containingType;

            DocRange nameRange = context.Identifier.Range;

            // Get the attributes.
            MethodAttributeAppender attributeResult = new MethodAttributeAppender(Attributes);
            MethodAttributesGetter attributeGetter = new MethodAttributesGetter(context, attributeResult);
            attributeGetter.GetAttributes(parseInfo.Script.Diagnostics);

            // Copy attribute results
            Static = attributeResult.Static;
            IsSubroutine = attributeResult.IsSubroutine;
            SubroutineName = attributeResult.SubroutineName;
            AccessLevel = attributeResult.AccessLevel;

            // Setup scope.
            SetupScope(Static ? staticScope : objectScope);
            methodScope.MethodContainer = true;

            // Get the type.
            if (!context.Type.IsVoid)
            {
                DoesReturnValue = true;
                ReturnType = CodeType.GetCodeTypeFromContext(parseInfo, context.Type);
            }

            // Setup the parameters and parse the block.
            if (!IsSubroutine)
                SetupParameters(context.Parameters, false);
            else
            {
                _subroutineDefaultGlobal = context.PlayerVar == null;
                Attributes.Parallelable = true;
                parseInfo.TranslateInfo.AddSubroutine(this);

                // Subroutines should not have parameters.
                SetupParameters(context.Parameters, true);
            }

            // Override attribute.
            if (Attributes.Override)
            {
                IMethod overriding = objectScope.GetMethodOverload(this);

                // No method with the name and parameters found.
                if (overriding == null) parseInfo.Script.Diagnostics.Error("Could not find a method to override.", nameRange);
                else if (!overriding.Attributes.IsOverrideable) parseInfo.Script.Diagnostics.Error("The specified method is not marked as virtual.", nameRange);
                else overriding.Attributes.AddOverride(this);

                if (overriding != null && overriding.DefinedAt != null)
                {
                    // Make the override keyword go to the base method.
                    parseInfo.Script.AddDefinitionLink(
                        attributeGetter.ObtainedAttributes.First(at => at.Type == MethodAttributeType.Override).Range,
                        overriding.DefinedAt
                    );

                    if (!Attributes.Recursive)
                        Attributes.Recursive = overriding.Attributes.Recursive;
                }
            }

            if (Attributes.IsOverrideable && AccessLevel == AccessLevel.Private)
                parseInfo.Script.Diagnostics.Error("A method marked as virtual or abstract must have the protection level 'public' or 'protected'.", nameRange);

            // Add to the scope. Check for conflicts if the method is not overriding.
            containingScope.AddMethod(this, parseInfo.Script.Diagnostics, nameRange, !Attributes.Override);

            // Add the hover info.
            parseInfo.Script.AddHover(nameRange, GetLabel(true));

            if (Attributes.IsOverrideable)
                parseInfo.Script.AddCodeLensRange(new ImplementsCodeLensRange(this, parseInfo.Script, CodeLensSourceType.Function, nameRange));

            parseInfo.TranslateInfo.ApplyBlock(this);
        }

        // Sets up the method's block.
        public override void SetupBlock()
        {
            Block = new BlockAction(parseInfo.SetCallInfo(CallInfo), methodScope.Child(), Context.Block);

            // Validate returns.
            BlockTreeScan validation = new BlockTreeScan(DoesReturnValue, parseInfo, this);
            validation.ValidateReturns();
            MultiplePaths = validation.MultiplePaths;

            // If there is only one return statement, set SingleReturnValue.
            if (validation.Returns.Length == 1) SingleReturnValue = validation.Returns[0].ReturningValue;

            // If the return type is a constant type...
            if (ReturnType != null && ReturnType.IsConstant())
                // ... iterate through each return statement ...
                foreach (ReturnAction returnAction in validation.Returns)
                    // ... If the current return statement returns a value and that value does not implement the return type ...
                    if (returnAction.ReturningValue != null && (returnAction.ReturningValue.Type() == null || !returnAction.ReturningValue.Type().Implements(ReturnType)))
                        // ... then add a syntax error.
                        parseInfo.Script.Diagnostics.Error("Must return a value of type '" + ReturnType.GetName() + "'.", returnAction.ErrorRange);
            
            WasApplied = true;
            foreach (var listener in listeners) listener.Applied();
        }

        // Parses the method.
        override public IWorkshopTree Parse(ActionSet actionSet, MethodCall methodCall)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());
            var controller = new FunctionBuildController(actionSet, methodCall, new DefaultGroupDeterminer(Attributes.AllOverrideOptions().Append(this).Select(op => new DefinedFunctionHandler((DefinedMethod)op)).ToArray()));
            return controller.Call();
        }

        // Sets up single-instance methods for methods with the 'rule' attribute.
        public void SetupSubroutine()
        {
            if (subroutineInfo != null || !IsSubroutine) return;

            var builder = new SubroutineBuilder(parseInfo.TranslateInfo, new DefinedSubroutineContext(parseInfo, this));
            builder.SetupSubroutine();
            subroutineInfo = builder.SubroutineInfo;
        }

        public void AssignParameters(ActionSet actionSet, IWorkshopTree[] parameterValues, bool recursive)
        {
            for (int i = 0; i < ParameterVars.Length; i++)
            {
                IGettable indexResult = actionSet.IndexAssigner.Add(actionSet.VarCollection, ParameterVars[i], actionSet.IsGlobal, parameterValues?[i], recursive);

                if (indexResult is IndexReference indexReference && parameterValues?[i] != null)
                    actionSet.AddAction(indexReference.SetVariable((Element)parameterValues[i]));

                foreach (Var virtualParameterOption in VirtualVarGroup(i))
                    actionSet.IndexAssigner.Add(virtualParameterOption, indexResult);
            }
        }

        // Assigns parameters to the index assigner. TODO: Move to OverloadChooser.
        public static void AssignParameters(ActionSet actionSet, Var[] parameterVars, IWorkshopTree[] parameterValues, bool recursive = false)
        {
            for (int i = 0; i < parameterVars.Length; i++)
            {
                actionSet.IndexAssigner.Add(actionSet.VarCollection, parameterVars[i], actionSet.IsGlobal, parameterValues?[i], recursive);

                if (actionSet.IndexAssigner[parameterVars[i]] is IndexReference && parameterValues?[i] != null)
                    actionSet.AddAction(
                        ((IndexReference)actionSet.IndexAssigner[parameterVars[i]]).SetVariable((Element)parameterValues[i])
                    );
            }
        }
    }
}