using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedMethod : DefinedFunction
    {
        private DeltinScriptParser.Define_methodContext context;

        public CodeType ContainingType { get; }

        // Attributes
        public bool RuleContained { get; private set; }
        public bool IsRecursive { get; private set; }

        // Block data
        private BlockAction block;
        private bool doesReturnValue;
        /// <summary>
        /// If there is only one return statement, return the reference to
        /// the return expression instead of assigning it to a variable to reduce the number of actions.
        /// </summary>
        private bool multiplePaths;
        private SingleInstanceInfo singleInstanceInfo;

        public DefinedMethod(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Define_methodContext context, CodeType containingType)
            : base(parseInfo, scope, context.name.Text, new Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)))
        {
            this.context = context;
            ContainingType = containingType;

            // Get the attributes.
            GetAttributes(context.method_attribute());

            // Get the type.
            ReturnType = CodeType.GetCodeTypeFromContext(parseInfo, context.code_type());

            // Get the access level.
            AccessLevel = context.accessor().GetAccessLevel();

            // Setup the parameters and parse the block.
            if (!RuleContained)
                SetupParameters(context.setParameters(), false);
            else
            {
                SetupParameters(context.setParameters(), true);
                parseInfo.TranslateInfo.AddSingleInstanceMethod(this);
            }

            if (context.block() == null)
                parseInfo.Script.Diagnostics.Error("Expected block.", DocRange.GetRange(context.name));

            scope.AddMethod(this, parseInfo.Script.Diagnostics, DocRange.GetRange(context.name));

            // Add the hover info.
            parseInfo.Script.AddHover(DocRange.GetRange(context.name), GetLabel(true));

            parseInfo.TranslateInfo.ApplyBlock(this);
        }

        // Gets the method attributes ('rule', 'recursive').
        private void GetAttributes(DeltinScriptParser.Method_attributeContext[] attributeContexts)
        {
            if (attributeContexts == null)
            {
                IsRecursive = false;
                RuleContained = false;
                return;
            }

            DocRange recursiveAttributeRange = null;
            DocRange ruleAttributeRange = null;

            foreach (var attribute in attributeContexts)
            {
                // Recursive attribute.
                if (attribute.RECURSIVE() != null)
                {
                    if (IsRecursive) parseInfo.Script.Diagnostics.Error("'recursive' attribute was already set.", DocRange.GetRange(attribute.RECURSIVE()));
                    IsRecursive = true;
                    recursiveAttributeRange = DocRange.GetRange(attribute.RECURSIVE());
                }
                // Rule attribute.
                else if (attribute.RULE_WORD() != null)
                {
                    if (RuleContained) parseInfo.Script.Diagnostics.Error("'rule' attribute was already set.", DocRange.GetRange(attribute.RULE_WORD()));
                    RuleContained = true;
                    ruleAttributeRange = DocRange.GetRange(attribute.RULE_WORD());
                }
                // Unimplemented attribute option
                else throw new NotImplementedException();
            }

            if (RuleContained && IsRecursive)
                parseInfo.Script.Diagnostics.Error("Functions with the 'rule' attribute cannot be recursive.", recursiveAttributeRange);
        }

        public override void SetupParameters()
        {
            // TODO: Check if parameters need to be added here.
        }
        // Sets up the method's block.
        public override void SetupBlock()
        {
            if (context.block() != null)
            {
                block = new BlockAction(parseInfo.SetCallInfo(CallInfo), methodScope, context.block());
                ValidateReturns(parseInfo.Script, context);
            }
            foreach (var listener in listeners) listener.Applied();
        }

        // Makes sure each return statement returns a value if the method returns a value and that each path returns a value.
        private void ValidateReturns(ScriptFile script, DeltinScriptParser.Define_methodContext context)
        {
            ReturnAction[] returns = GetReturns();
            if (returns.Any(ret => ret.ReturningValue != null))
            {
                doesReturnValue = true;

                // If there is only one return statement, return the reference to
                // the return statement to reduce the number of actions.
                multiplePaths = returns.Length > 1;

                // Syntax error if there are any paths that don't return a value.
                CheckPath(script, new PathInfo(block, DocRange.GetRange(context.name), true));

                // If one return statement returns a value, the rest must as well.
                foreach (var ret in returns)
                    if (ret.ReturningValue == null)
                        script.Diagnostics.Error("Must return a value.", ret.ErrorRange);
            }
        }

        // Gets all return statements in a method.
        private ReturnAction[] GetReturns()
        {
            List<ReturnAction> returns = new List<ReturnAction>();
            getReturns(returns, block);
            return returns.ToArray();

            void getReturns(List<ReturnAction> actions, BlockAction block)
            {
                // Loop through each statement in the block.
                foreach (var statement in block.Statements)
                    // If the current statement is a return statement, add it to the list.
                    if (statement is ReturnAction) actions.Add((ReturnAction)statement);

                    // If the current statement contains sub-blocks, get the return statements in those blocks recursively.
                    else if (statement is IBlockContainer)
                        foreach (var path in ((IBlockContainer)statement).GetPaths())
                            getReturns(actions, path.Block);
            }
        }

        // Makes sure each path returns a value.
        private static void CheckPath(ScriptFile script, PathInfo path)
        {
            bool blockReturns = false;
            // Check the statements backwards.
            for (int i = path.Block.Statements.Length - 1; i >= 0; i--)
            {
                if (path.Block.Statements[i] is ReturnAction)
                {
                    blockReturns = true;
                    break;
                }
                
                if (path.Block.Statements[i] is IBlockContainer)
                {
                    // If any of the paths in the block container has WillRun set to true,
                    // set blockReturns to true. The responsibility of checking if this
                    // block will run is given to the block container.
                    if (((IBlockContainer)path.Block.Statements[i]).GetPaths().Any(containerPath => containerPath.WillRun))
                        blockReturns = true;

                    CheckContainer(script, (IBlockContainer)path.Block.Statements[i]);
                }
            }
            if (!blockReturns)
                script.Diagnostics.Error("Path does not return a value.", path.ErrorRange);
        }
        private static void CheckContainer(ScriptFile script, IBlockContainer container)
        {
            foreach (var path in container.GetPaths()) CheckPath(script, path);
        }

        // Checks if the method returns a value.
        override public bool DoesReturnValue() => doesReturnValue;

        // Parses the method.
        override public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            actionSet = actionSet
                .New(actionSet.IndexAssigner.CreateContained());
            
            if (RuleContained) return ParseSingleInstance(actionSet, parameterValues);
            if (IsRecursive) return ParseRecursive(actionSet, parameterValues);
            
            ReturnHandler returnHandler = new ReturnHandler(actionSet, Name, multiplePaths);
            actionSet = actionSet.New(returnHandler);
            
            AssignParameters(actionSet, ParameterVars, parameterValues);
            block.Translate(actionSet);

            returnHandler.ApplyReturnSkips();
            return returnHandler.GetReturnedValue();
        }

        // Parses the method recursively.
        private IWorkshopTree ParseRecursive(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            // Check the method stack to see if this method was already called.
            RecursiveMethodStack lastCall = actionSet.Translate.MethodStack.FirstOrDefault(ms => ms.Function == this) as RecursiveMethodStack;

            // If not, set up the stack and call the method.
            if (lastCall == null)
            {
                // Assign the parameters.
                AssignParameters(actionSet, ParameterVars, parameterValues, true);

                // Get the return handler if a value is returned.
                ReturnHandler returnHandler = new ReturnHandler(actionSet, Name, true);

                // Set up the condinue skip array.
                IndexReference continueSkipArray = actionSet.VarCollection.Assign("recursiveContinueArray", actionSet.IsGlobal, false);

                SkipEndMarker methodStart = new SkipEndMarker();
                actionSet.AddAction(methodStart);

                // Add the method to the stack.
                var stack = new RecursiveMethodStack(this, returnHandler, continueSkipArray, methodStart);
                actionSet.Translate.MethodStack.Add(stack);

                // Parse the method block.
                block.Translate(actionSet.New(returnHandler));

                // Apply the returns.
                if (returnHandler != null)
                    returnHandler.ApplyReturnSkips();
                
                // Pop the recursive parameters
                // TODO: Make this work with all sub scoped variables somehow
                for (int i = 0; i < ParameterVars.Length; i++)
                {
                    var pop = (actionSet.IndexAssigner[ParameterVars[i]] as RecursiveIndexReference)?.Pop();
                    if (pop != null) actionSet.AddAction(pop);
                }

                // Setup the continue skip
                actionSet.ContinueSkip.Setup(actionSet);
                actionSet.ContinueSkip.SetSkipCount(actionSet, Element.Part<V_LastOf>(continueSkipArray.GetVariable()));

                // Remove the last recursive continue skip.
                actionSet.AddAction(continueSkipArray.SetVariable(
                    // Pop
                    Element.Part<V_ArraySlice>(
                        continueSkipArray.GetVariable(), 
                        new V_Number(0),
                        Element.Part<V_CountOf>(continueSkipArray.GetVariable()) - 1
                    )
                ));

                // Loop if there are any values in the continue skip array.
                actionSet.AddAction(Element.Part<A_LoopIf>(
                    Element.Part<V_CountOf>(continueSkipArray.GetVariable()) > 0
                ));

                // Reset the continue skip.
                actionSet.ContinueSkip.ResetSkipCount(actionSet);
                actionSet.AddAction(continueSkipArray.SetVariable(0));

                // Remove the method from the stack.
                actionSet.Translate.MethodStack.Remove(stack);

                return returnHandler.GetReturnedValue();
            }
            // If it is, push the parameters to the stack.
            else
            {
                for (int i = 0; i < ParameterVars.Length; i++)
                {
                    var varReference = actionSet.IndexAssigner[ParameterVars[i]];
                    if (varReference is RecursiveIndexReference)
                    {
                        actionSet.AddAction(((RecursiveIndexReference)varReference).Push(
                            (Element)parameterValues[i]
                        ));
                    }
                }

                // Add to the continue skip array.
                V_Number skipLength = new V_Number();
                actionSet.AddAction(lastCall.ContinueSkipArray.SetVariable(
                    Element.Part<V_Append>(lastCall.ContinueSkipArray.GetVariable(), skipLength)
                ));

                actionSet.ContinueSkip.Setup(actionSet);
                actionSet.ContinueSkip.SetSkipCount(actionSet, lastCall.MethodStart);
                actionSet.AddAction(new A_Loop());

                SkipEndMarker continueAt = new SkipEndMarker();
                actionSet.AddAction(continueAt);
                skipLength.Value = actionSet.ContinueSkip.GetSkipCount(continueAt).Value;

                return lastCall.ReturnHandler.GetReturnedValue();
            }
        }
    
        // Sets up single-instance methods for methods with the 'rule' attribute.
        public void SetupSingleInstance()
        {
            if (!RuleContained) throw new Exception(Name + " does not have the rule attribute.");

            // Single instance methods are contained in their own rule.
            // Unlike normal methods, the block is only translated once making it effective for big, complicated methods.
            // There is a 0.016 second delay when calling single instance methods.
            // Callers of the method store the data of their call into arrays.

            VarCollection varCollection = parseInfo.TranslateInfo.VarCollection;

            // Each value in the `parameterStacks` array stores an array containing the parameter values for method callers.
            IndexReference[] parameterStacks = new IndexReference[ParameterVars.Length];

            // `currentCall` is the current call index.
            IndexReference currentCall = varCollection.Assign("_" + Name + "_currentCall", true, true);
            parseInfo.TranslateInfo.InitialGlobal.ActionSet.AddAction(currentCall.SetVariable(-1));

            // Setup the parameters.
            for (int i = 0; i < ParameterVars.Length; i++)
            {
                IndexReference variableStack = varCollection.Assign(ParameterVars[i].Name, true, true);
                IndexReference variableReference = variableStack.CreateChild((Element)currentCall.GetVariable());
                parseInfo.TranslateInfo.DefaultIndexAssigner.Add(ParameterVars[i], variableReference);
                parameterStacks[i] = variableStack;
            }

            // Each caller gets a number.
            // When a callers' index is true, that means the number is taken.
            // When it goes from true to false, the rule-method completed the call.
            IndexReference callers = varCollection.Assign("_" + Name + "_calls", true, false);

            // Set the 1000th value as null.
            parseInfo.TranslateInfo.InitialGlobal.ActionSet.AddAction(callers.SetVariable(new V_Null(), null, Constants.MAX_ARRAY_LENGTH));

            TranslateRule instanceRule = new TranslateRule(parseInfo.TranslateInfo, Name);
            // Run the rule if there are any callers.
            instanceRule.Conditions.Add(
                Element.Part<V_ArrayContains>(callers.GetVariable(), new V_True())
            );

            // Setup the return handler.
            ReturnHandler returnHandler = new ReturnHandler(instanceRule.ActionSet, Name, multiplePaths);
            ActionSet actionSet = instanceRule.ActionSet.New(returnHandler);

            // Stores the current object the method is being called for.
            IndexReference currentObject = null;
            if (ContainingType != null)
            {
                // If there is a type, assign the current object variable.
                currentObject = varCollection.Assign("_" + Name + "_currentObject", true, true);

                // Add the object variables to the assigner.
                ContainingType.AddObjectVariablesToAssigner(Element.Part<V_ValueInArray>(currentObject.GetVariable(), currentCall.GetVariable()), actionSet.IndexAssigner);
            }

            // Get the next caller.
            actionSet.AddAction(currentCall.SetVariable(
                Element.Part<V_IndexOfArrayValue>(callers.GetVariable(), new V_True())
            ));

            // AssignParameters(actionSet, ParameterVars, null, false);
            // TODO: Assign intial values
            block.Translate(actionSet);

            returnHandler.ApplyReturnSkips();

            actionSet.AddAction(callers.SetVariable(new V_False(), null, (Element)currentCall.GetVariable()));
            actionSet.AddAction(A_Wait.MinimumWait);
            actionSet.AddAction(new A_LoopIfConditionIsTrue());

            parseInfo.TranslateInfo.WorkshopRules.Add(instanceRule.GetRule());
            singleInstanceInfo = new SingleInstanceInfo(parameterStacks, callers, returnHandler.GetReturnedValue(), currentObject);
        }

        // Calls single-instance methods.
        private IWorkshopTree ParseSingleInstance(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            // `callID` stores the index that this call will store data in.
            IndexReference callID = actionSet.VarCollection.Assign("_" + Name + "_callID", true, true);
            actionSet.AddAction(callID.SetVariable(
                Element.Part<V_IndexOfArrayValue>(singleInstanceInfo.Callers.GetVariable(), new V_False())
            ));
            actionSet.AddAction(singleInstanceInfo.Callers.SetVariable(new V_True(), null, (Element)callID.GetVariable()));

            for (int i = 0; i < ParameterVars.Length; i++)
            {
                // Push the parameters to the parameter list.
                actionSet.AddAction(singleInstanceInfo.ParameterStacks[i].SetVariable(
                    value: (Element)parameterValues[i],
                    index: (Element)callID.GetVariable()
                ));
            }

            // Set the object to modify.
            if (ContainingType != null)
            {
                actionSet.AddAction(singleInstanceInfo.CurrentObject.SetVariable(
                    value: (Element)actionSet.CurrentObject,
                    index: (Element)callID.GetVariable()
                ));
            }

            // Wait for the method to return the value.
            SpinWhileBuilder.Build(actionSet, Element.Part<V_ValueInArray>(
                singleInstanceInfo.Callers.GetVariable(),
                callID.GetVariable()
            ));

            return singleInstanceInfo.ReturningValue;
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

    public class SingleInstanceInfo
    {
        /// <summary>
        /// Stores data about each parameter input array.
        /// </summary>
        public IndexReference[] ParameterStacks { get; }
        /// <summary>
        /// A boolean array that indicates which call indexes were taken. 
        /// </summary>
        public IndexReference Callers { get; }
        /// <summary>
        /// A reference to the value that is returned. This may need to be changed to an array for each caller.
        /// </summary>
        public IWorkshopTree ReturningValue { get; }
        /// <summary>
        /// A reference to the current object to run the method for. Should be null for rule-level methods and should not be null for class-level methods.
        /// </summary>
        public IndexReference CurrentObject { get; }
        
        public SingleInstanceInfo(IndexReference[] parameterStacks, IndexReference callers, IWorkshopTree returningValue, IndexReference currentObject)
        {
            ParameterStacks = parameterStacks;
            Callers = callers;
            ReturningValue = returningValue;
            CurrentObject = currentObject;
        }
    }
}