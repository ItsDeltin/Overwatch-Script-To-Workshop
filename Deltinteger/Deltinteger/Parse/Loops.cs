using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public abstract class LoopAction : IStatement, IBlockContainer, IContinueContainer, IBreakContainer
    {
        /// <summary>The path info of the loop block.</summary>
        protected PathInfo Path;

        /// <summary>Determines if the continue action is used directly.</summary>
        protected bool RawContinue = true;
        /// <summary>Determines if the break action is used directly.</summary>
        protected readonly bool RawBreak = true; // Remove the readonly if this needs to be changed.

        /// <summary>Stores skips that continue the loop.</summary>
        private readonly List<SkipStartMarker> Continue = new List<SkipStartMarker>();

        /// <summary>Stores skips that break the loop.</summary>
        private readonly List<SkipStartMarker> Break = new List<SkipStartMarker>();

        public abstract void Translate(ActionSet actionSet);

        public PathInfo[] GetPaths() => new PathInfo[] { Path };

        public void AddContinue(ActionSet actionSet)
        {
            if (RawContinue)
                actionSet.AddAction(new A_Continue());
            else
            {
                SkipStartMarker continuer = new SkipStartMarker(actionSet);
                actionSet.AddAction(continuer);
                Continue.Add(continuer);
            }
        }

        public void AddBreak(ActionSet actionSet)
        {
            if (RawBreak)
                actionSet.AddAction(new A_Break());
            else
            {
                SkipStartMarker breaker = new SkipStartMarker(actionSet);
                actionSet.AddAction(breaker);
                Break.Add(breaker);
            }
        }

        protected void ResolveContinues(ActionSet actionSet)
        {
            Resolve(actionSet, Continue);
        }
        protected void ResolveBreaks(ActionSet actionSet)
        {
            Resolve(actionSet, Break);
        }
        private void Resolve(ActionSet actionSet, List<SkipStartMarker> skips)
        {
            // Create the end marker that marks the spot right before the End action (if continuing) or right after the End action (if breaking).
            SkipEndMarker endMarker = new SkipEndMarker();

            // Add the end marker to the action set.
            actionSet.AddAction(endMarker);

            // Assign the end marker to the continue/break skips.
            foreach (SkipStartMarker startMarker in skips)
                startMarker.SetEndMarker(endMarker);
        }
    }

    class WhileAction : LoopAction
    {
        private IExpression Condition { get; }
        private BlockAction Block { get; }

        public WhileAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.WhileContext whileContext)
        {
            RawContinue = true;

            if (whileContext.expr() == null)
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(whileContext.LEFT_PAREN()));
            else
                Condition = parseInfo.GetExpression(scope, whileContext.expr());
            
            Block = new BlockAction(parseInfo.SetLoop(this), scope, whileContext.block());
            Path = new PathInfo(Block, DocRange.GetRange(whileContext.WHILE()), false);
        }

        public override void Translate(ActionSet actionSet)
        {
            int actionCountPreCondition = actionSet.ActionCount;

            Element condition = (Element)Condition.Parse(actionSet);
            bool actionsAdded = actionSet.ActionCount > actionCountPreCondition;

            if (!actionsAdded)
            {
                // Create a normal while loop.
                actionSet.AddAction(Element.Part<A_While>(condition));
                
                // Translate the block.
                Block.Translate(actionSet.Indent());

                // Resolve continues.
                ResolveContinues(actionSet);

                // Cap the block.
                actionSet.AddAction(new A_End());

                // Resolve breaks.
                ResolveBreaks(actionSet);
            }
            else
            {
                // The while condition requires actions to get the value.
                actionSet.ActionList.Insert(actionCountPreCondition, new ALAction(Element.Part<A_While>(new V_True())));

                SkipStartMarker whileEndSkip = new SkipStartMarker(actionSet, condition);
                actionSet.Indent().AddAction(whileEndSkip);

                // Translate the block.
                Block.Translate(actionSet.Indent());

                // Resolve continues.
                ResolveContinues(actionSet);

                // Cap the block.
                actionSet.AddAction(new A_End());

                // Skip to the end when the condition is false.
                SkipEndMarker whileEnd = new SkipEndMarker();
                whileEndSkip.SetEndMarker(whileEnd);
                actionSet.AddAction(whileEnd);

                // Resolve breaks.
                ResolveBreaks(actionSet);
            }
        }
    }

    class ForAction : LoopAction
    {
        private Var DefinedVariable { get; }
        private SetVariableAction InitialVarSet { get; }

        private IExpression Condition { get; }
        private SetVariableAction SetVariableAction { get; }
        private BlockAction Block { get; }

        public ForAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.ForContext forContext)
        {
            Scope varScope = scope.Child();

            if (forContext.define() != null)
            {
                DefinedVariable = new ScopedVariable(varScope, new DefineContextHandler(parseInfo, forContext.define()));
            }
            else if (forContext.initialVarset != null)
                InitialVarSet = new SetVariableAction(parseInfo, varScope, forContext.initialVarset);

            if (forContext.expr() != null)
                Condition = parseInfo.GetExpression(varScope, forContext.expr());
            
            if (forContext.endingVarset != null)
            {
                SetVariableAction = new SetVariableAction(parseInfo, varScope, forContext.endingVarset);
                RawContinue = false;
            }
            else RawContinue = true;

            // Get the block.
            if (forContext.block() != null)
            {
                Block = new BlockAction(parseInfo.SetLoop(this), varScope, forContext.block());
                // Get the path info.
                Path = new PathInfo(Block, DocRange.GetRange(forContext.FOR()), false);
            }
            else
                parseInfo.Script.Diagnostics.Error("Expected a block.", DocRange.GetRange(forContext.RIGHT_PAREN()));
            }

        public override void Translate(ActionSet actionSet)
        {
            if (DefinedVariable != null)
            {
                // Add the defined variable to the index assigner.
                actionSet.IndexAssigner.Add(actionSet.VarCollection, DefinedVariable, actionSet.IsGlobal, null);

                // Set the initial variable.
                if (actionSet.IndexAssigner[DefinedVariable] is IndexReference && DefinedVariable.InitialValue != null)
                    actionSet.AddAction(((IndexReference)actionSet.IndexAssigner[DefinedVariable]).SetVariable(
                        (Element)DefinedVariable.InitialValue.Parse(actionSet)
                    ));
            }
            else if (InitialVarSet != null)
                InitialVarSet.Translate(actionSet);

            // Get the condition.
            Element condition;
            if (Condition != null) condition = (Element)Condition.Parse(actionSet); // User-define condition
            else condition = new V_True(); // No condition, just use true.
            actionSet.AddAction(Element.Part<A_While>(condition));

            Block.Translate(actionSet.Indent());

            // Resolve continues.
            ResolveContinues(actionSet);

            if (SetVariableAction != null)
                SetVariableAction.Translate(actionSet.Indent());
                        
            actionSet.AddAction(new A_End());

            // Resolve breaks.
            ResolveBreaks(actionSet);
        }
    }

    class AutoForAction : LoopAction
    {
        private VariableResolve VariableResolve { get; }
        private IExpression InitialResolveValue { get; }
        private Var DefinedVariable { get; }

        private IExpression Stop { get; }
        private ExpressionOrWorkshopValue Step { get; }
        private BlockAction Block { get; }

        public AutoForAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.For_autoContext autoForContext)
        {
            RawContinue = true;

            // Get the auto-for variable. (Required)
            if (autoForContext.forVariable != null)
            {
                IExpression variable = parseInfo.GetExpression(scope, autoForContext.forVariable);

                // Get the variable being set.
                VariableResolve = new VariableResolve(new VariableResolveOptions() {
                    // The for cannot be indexed and should be on the rule-level.
                    CanBeIndexed = false,
                    FullVariable = true
                }, variable, DocRange.GetRange(autoForContext.forVariable), parseInfo.Script.Diagnostics);

                if (autoForContext.EQUALS() != null)
                {
                    if (autoForContext.start == null)
                        parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(autoForContext.EQUALS()));
                    else
                        InitialResolveValue = parseInfo.GetExpression(scope, autoForContext.start);
                }
            }
            // Get the defined variable.
            else if (autoForContext.forDefine != null)
            {
                DefinedVariable = new AutoForVariable(scope, new DefineContextHandler(parseInfo, autoForContext.forDefine));
            }
            else
                parseInfo.Script.Diagnostics.Error("Expected define or variable.", DocRange.GetRange(autoForContext.FOR()));
            
            // Get the auto-for end. (Required)
            if (autoForContext.stop == null)
                parseInfo.Script.Diagnostics.Error("Expected end expression.", DocRange.GetRange(autoForContext.startSep));
            else
                Stop = parseInfo.GetExpression(scope, autoForContext.stop);
            
            // Get the auto-for step. (Not Required)
            if (autoForContext.step == null)
                Step = new ExpressionOrWorkshopValue(new V_Number(1));
            else
                Step = new ExpressionOrWorkshopValue(parseInfo.GetExpression(scope, autoForContext.step));
            
            // Get the block.
            if (autoForContext.block() == null)
                parseInfo.Script.Diagnostics.Error("Missing block.", DocRange.GetRange(autoForContext.RIGHT_PAREN()));
            else
            {
                Block = new BlockAction(parseInfo.SetLoop(this), scope, autoForContext.block());
                Path = new PathInfo(Block, DocRange.GetRange(autoForContext.block()), false);
            }
        }

        public override void Translate(ActionSet actionSet)
        {
            WorkshopVariable variable;
            Element target;
            Element start;

            // Existing variable being used in for.
            if (VariableResolve != null)
            {
                VariableElements elements = VariableResolve.ParseElements(actionSet);
                variable = elements.IndexReference.WorkshopVariable;
                target = elements.Target;
                start = (Element)InitialResolveValue?.Parse(actionSet) ?? new V_Number(0);
            }
            // New variable being use in for.
            else
            {
                actionSet.IndexAssigner.Add(actionSet.VarCollection, DefinedVariable, actionSet.IsGlobal, null);
                variable = ((IndexReference)actionSet.IndexAssigner[DefinedVariable]).WorkshopVariable;
                target = new V_EventPlayer();
                start = (Element)DefinedVariable.InitialValue?.Parse(actionSet) ?? new V_Number(0);
            }

            Element stop = (Element)Stop.Parse(actionSet);
            Element step = (Element)Step.Parse(actionSet);

            // Global
            if (variable.IsGlobal)
                actionSet.AddAction(Element.Part<A_ForGlobalVariable>(
                    variable,
                    start, stop, step
                ));
            // Player
            else
                actionSet.AddAction(Element.Part<A_ForPlayerVariable>(
                    target,
                    variable,
                    start, stop, step
                ));
            
            // Translate the block.
            Block.Translate(actionSet.Indent());

            // Resolve continues.
            ResolveContinues(actionSet);

            // Cap the for.
            actionSet.AddAction(new A_End());

            // Resolve breaks.
            ResolveBreaks(actionSet);
        }
    }

    class ForeachAction : LoopAction
    {
        private Var ForeachVar { get; }
        private IExpression Array { get; }
        private BlockAction Block { get; }

        public ForeachAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.ForeachContext foreachContext)
        {
            RawContinue = false;

            Scope varScope = scope.Child();

            ForeachVar = new ForeachVariable(varScope, new ForeachContextHandler(parseInfo, foreachContext));

            // Get the array that will be iterated on. Syntax error if it is missing.
            if (foreachContext.expr() != null)
                Array = parseInfo.GetExpression(scope, foreachContext.expr());
            else
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(foreachContext.IN()));

            // Get the foreach block. Syntax error if it is missing.
            if (foreachContext.block() != null)
            {
                Block = new BlockAction(parseInfo.SetLoop(this), varScope, foreachContext.block());
                // Get the path info.
                Path = new PathInfo(Block, DocRange.GetRange(foreachContext.FOREACH()), false);
            }
            else
                parseInfo.Script.Diagnostics.Error("Expected block.", DocRange.GetRange(foreachContext.RIGHT_PAREN()));
        }

        public override void Translate(ActionSet actionSet)
        {
            ForeachBuilder foreachBuilder = new ForeachBuilder(actionSet, Array.Parse(actionSet), actionSet.IsRecursive);

            // Add the foreach value to the assigner.
            actionSet.IndexAssigner.Add(ForeachVar, foreachBuilder.IndexValue);

            // Translate the block.
            Block.Translate(actionSet.Indent());

            // Resolve continues.
            ResolveContinues(actionSet);

            // Finish the foreach.
            foreachBuilder.Finish();

            // Resolve breaks.
            ResolveBreaks(actionSet);
        }

        class ForeachContextHandler : IVarContextHandler
        {
            public ParseInfo ParseInfo { get; }
            private readonly DeltinScriptParser.ForeachContext _foreachContext;

            public ForeachContextHandler(ParseInfo parseInfo, DeltinScriptParser.ForeachContext foreachContext)
            {
                ParseInfo = parseInfo;
                _foreachContext = foreachContext;
            }

            public VarBuilderAttribute[] GetAttributes() => new VarBuilderAttribute[0];
            public DeltinScriptParser.Code_typeContext GetCodeType() => _foreachContext.code_type();
            public Location GetDefineLocation() => new Location(ParseInfo.Script.Uri, GetNameRange());
            public string GetName() => _foreachContext.name?.Text;

            public DocRange GetNameRange()
            {
                if (_foreachContext.name != null) return DocRange.GetRange(_foreachContext.name);
                return DocRange.GetRange(_foreachContext);
            }
            public DocRange GetTypeRange() => DocRange.GetRange(_foreachContext.code_type());
        }
    }
}