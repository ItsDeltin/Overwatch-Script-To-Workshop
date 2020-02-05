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
        public string SubroutineName { get; private set; }

        // Block data
        private BlockAction block;
        private bool doesReturnValue;
        /// <summary>
        /// If there is only one return statement, return the reference to
        /// the return expression instead of assigning it to a variable to reduce the number of actions.
        /// </summary>
        private bool multiplePaths;

        private SubroutineInfo subroutineInfo;

        public DefinedMethod(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Define_methodContext context, CodeType containingType)
            : base(parseInfo, scope, context.name.Text, new Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)))
        {
            this.context = context;
            ContainingType = containingType;

            // Get the type.
            ReturnType = CodeType.GetCodeTypeFromContext(parseInfo, context.code_type());

            // Get the access level.
            AccessLevel = context.accessor().GetAccessLevel();

            // Get the attributes.
            if (context.STRINGLITERAL() != null)
                SubroutineName = Extras.RemoveQuotes(context.STRINGLITERAL().GetText());

            // Setup the parameters and parse the block.
            if (SubroutineName == null)
                SetupParameters(context.setParameters(), false);
            else
            {
                Asyncable = true;
                parseInfo.TranslateInfo.AddSubroutine(this);

                // Subroutines should not have parameters.
                SetupParameters(context.setParameters(), false);
            }

            if (SubroutineName != null)
            {
                // Syntax error if the method is a subroutine and there are parameters.
                if (Parameters.Length > 0)
                    parseInfo.Script.Diagnostics.Error("Subroutines cannot have parameters.", DocRange.GetRange(context.name));
            
                // Syntax error if the method is a subroutine and there is a return type.
                //if (ReturnType != null)
                  //  parseInfo.Script.Diagnostics.Error("Subroutines cannot return a value.", DocRange.GetRange(context.name));
                
                // Syntax error if the method is a subroutine and the method is defined in a class.
                if (ContainingType != null)
                    parseInfo.Script.Diagnostics.Error("Subroutines cannot be defined in types.", DocRange.GetRange(context.name));
            }
            
            // Syntax error if the block is missing.
            if (context.block() == null)
                parseInfo.Script.Diagnostics.Error("Expected block.", DocRange.GetRange(context.name));

            scope.AddMethod(this, parseInfo.Script.Diagnostics, DocRange.GetRange(context.name));

            // Add the hover info.
            parseInfo.Script.AddHover(DocRange.GetRange(context.name), GetLabel(true));

            parseInfo.TranslateInfo.ApplyBlock(this);
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
        override public IWorkshopTree Parse(ActionSet actionSet, bool parallel, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            actionSet = actionSet
                .New(actionSet.IndexAssigner.CreateContained());
            
            if (SubroutineName != null) return ParseSubroutine(actionSet, parallel, parameterValues);
            
            ReturnHandler returnHandler = new ReturnHandler(actionSet, Name, multiplePaths);
            actionSet = actionSet.New(returnHandler);
            
            AssignParameters(actionSet, ParameterVars, parameterValues);
            block.Translate(actionSet);

            returnHandler.ApplyReturnSkips();
            return returnHandler.GetReturnedValue();
        }

        // Sets up single-instance methods for methods with the 'rule' attribute.
        public void SetupSubroutine()
        {
            if (SubroutineName == null) throw new Exception(Name + " does not have the subroutine attribute.");

            // Setup the subroutine element.
            Subroutine subroutine = parseInfo.TranslateInfo.SubroutineCollection.NewSubroutine(Name);

            // Create the rule.
            TranslateRule subroutineRule = new TranslateRule(parseInfo.TranslateInfo, SubroutineName, subroutine);

            // Setup the return handler.
            ReturnHandler returnHandler = new ReturnHandler(subroutineRule.ActionSet, Name, multiplePaths);
            ActionSet actionSet = subroutineRule.ActionSet.New(returnHandler);

            // Parse the block.
            block.Translate(actionSet);

            // Apply returns.
            returnHandler.ApplyReturnSkips();

            // Add the subroutine.
            parseInfo.TranslateInfo.WorkshopRules.Add(subroutineRule.GetRule());

            subroutineInfo = new SubroutineInfo(subroutine, returnHandler);
        }

        // Calls single-instance methods.
        private IWorkshopTree ParseSubroutine(ActionSet actionSet, bool parallel, IWorkshopTree[] parameterValues)
        {
            if (!parallel)
                actionSet.AddAction(Element.Part<A_CallSubroutine>(subroutineInfo.Subroutine));
            else
                actionSet.AddAction(Element.Part<A_CallSubroutine>(subroutineInfo.Subroutine));
            return subroutineInfo.ReturnHandler.GetReturnedValue();
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

    class SubroutineInfo
    {
        public Subroutine Subroutine { get; }
        public ReturnHandler ReturnHandler { get; }

        public SubroutineInfo(Subroutine routine, ReturnHandler returnHandler)
        {
            Subroutine = routine;
            ReturnHandler = returnHandler;
        }
    }
}