using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    class WhileAction : IStatement, IBlockContainer
    {
        private IExpression Condition { get; }
        private BlockAction Block { get; }
        private PathInfo Path { get; }

        public WhileAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.WhileContext whileContext)
        {
            if (whileContext.expr() == null)
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(whileContext.LEFT_PAREN()));
            else
                Condition = DeltinScript.GetExpression(parseInfo, scope, whileContext.expr());
            
            Block = new BlockAction(parseInfo, scope, whileContext.block());
            Path = new PathInfo(Block, DocRange.GetRange(whileContext.WHILE()), false);
        }

        public void Translate(ActionSet actionSet)
        {
            int actionCountPreCondition = actionSet.ActionCount;

            Element condition = (Element)Condition.Parse(actionSet);
            bool actionsAdded = actionSet.ActionCount > actionCountPreCondition;

            if (!actionsAdded)
            {
                // Create a normal while loop.
                actionSet.AddAction(Element.Part<A_While>(condition));
                
                // Translate the block.
                Block.Translate(actionSet);

                // Cap the block.
                actionSet.AddAction(new A_End());
            }
            else
            {
                // The while condition requires actions to get the value.
                actionSet.ActionList.Insert(actionCountPreCondition, new ALAction(Element.Part<A_While>(new V_True())));

                SkipStartMarker whileEndSkip = new SkipStartMarker(actionSet, condition);
                actionSet.AddAction(whileEndSkip);

                // Translate the block.
                Block.Translate(actionSet);

                // Cap the block.
                actionSet.AddAction(new A_End());

                // Skip to the end when the condition is false.
                SkipEndMarker whileEnd = new SkipEndMarker();
                whileEndSkip.SetEndMarker(whileEnd);
                actionSet.AddAction(whileEnd);
            }
        }

        public PathInfo[] GetPaths()
        {
            return new PathInfo[] { Path };
        }
    }

    class ForAction : IStatement, IBlockContainer
    {
        private Var DefinedVariable { get; }
        private SetVariableAction InitialVarSet { get; }

        private IExpression Condition { get; }
        private SetVariableAction SetVariableAction { get; }
        private BlockAction Block { get; }
        private PathInfo Path { get; }

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
                Condition = DeltinScript.GetExpression(parseInfo, varScope, forContext.expr());
            
            if (forContext.endingVarset != null)
                SetVariableAction = new SetVariableAction(parseInfo, varScope, forContext.endingVarset);

            // Get the block.
            if (forContext.block() != null)
            {
                Block = new BlockAction(parseInfo, varScope, forContext.block());
                // Get the path info.
                Path = new PathInfo(Block, DocRange.GetRange(forContext.FOR()), false);
            }
            else
                parseInfo.Script.Diagnostics.Error("Expected a block.", DocRange.GetRange(forContext.RIGHT_PAREN()));
            }

        public void Translate(ActionSet actionSet)
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

            Element condition = (Element)Condition.Parse(actionSet) ?? new V_True();
            actionSet.AddAction(Element.Part<A_While>(condition));

            Block.Translate(actionSet);

            if (SetVariableAction != null)
                SetVariableAction.Translate(actionSet);
            
            actionSet.AddAction(new A_End());
        }

        public PathInfo[] GetPaths()
        {
            return new PathInfo[] { Path };
        }
    }

    class AutoForAction : IStatement, IBlockContainer
    {
        private VariableResolve VariableResolve { get; }
        private IExpression InitialResolveValue { get; }
        private Var DefinedVariable { get; }

        //private ExpressionOrWorkshopValue Start { get; }
        private IExpression Stop { get; }
        private ExpressionOrWorkshopValue Step { get; }
        private BlockAction Block { get; }
        private PathInfo PathInfo { get; }

        public AutoForAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.For_autoContext autoForContext)
        {
            // Get the auto-for variable. (Required)
            if (autoForContext.forVariable != null)
            {
                IExpression variable = DeltinScript.GetExpression(parseInfo, scope, autoForContext.forVariable);

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
                        InitialResolveValue = DeltinScript.GetExpression(parseInfo, scope, autoForContext.start);
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
                Stop = DeltinScript.GetExpression(parseInfo, scope, autoForContext.stop);
            
            // Get the auto-for step. (Not Required)
            if (autoForContext.step == null)
                Step = new ExpressionOrWorkshopValue(new V_Number(1));
            else
                Step = new ExpressionOrWorkshopValue(DeltinScript.GetExpression(parseInfo, scope, autoForContext.step));
            
            // Get the block.
            if (autoForContext.block() == null)
                parseInfo.Script.Diagnostics.Error("Missing block.", DocRange.GetRange(autoForContext.RIGHT_PAREN()));
            else
            {
                Block = new BlockAction(parseInfo, scope, autoForContext.block());
                PathInfo = new PathInfo(Block, DocRange.GetRange(autoForContext.block()), false);
            }
        }

        public void Translate(ActionSet actionSet)
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

            //Element start = (Element)Start.Parse(actionSet);
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
            Block.Translate(actionSet);

            // Cap the for.
            actionSet.AddAction(new A_End());
        }

        public PathInfo[] GetPaths() => new PathInfo[] { PathInfo };
    }

    class ForeachAction : IStatement
    {
        private Var ForeachVar { get; }
        private IExpression Array { get; }
        private BlockAction Block { get; }
        private PathInfo Path { get; }

        public ForeachAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.ForeachContext foreachContext)
        {
            Scope varScope = scope.Child();

            ForeachVar = new ForeachVariable(varScope, new ForeachContextHandler(parseInfo, foreachContext));

            // Get the array that will be iterated on. Syntax error if it is missing.
            if (foreachContext.expr() != null)
                Array = DeltinScript.GetExpression(parseInfo, scope, foreachContext.expr());
            else
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(foreachContext.IN()));

            // Get the foreach block. Syntax error if it is missing.
            if (foreachContext.block() != null)
            {
                Block = new BlockAction(parseInfo, varScope, foreachContext.block());
                // Get the path info.
                Path = new PathInfo(Block, DocRange.GetRange(foreachContext.FOREACH()), false);
            }
            else
                parseInfo.Script.Diagnostics.Error("Expected block.", DocRange.GetRange(foreachContext.RIGHT_PAREN()));
        }

        public void Translate(ActionSet actionSet)
        {
            ForeachBuilder foreachBuilder = new ForeachBuilder(actionSet, Array.Parse(actionSet));
            actionSet.IndexAssigner.Add(ForeachVar, foreachBuilder.IndexValue);
            Block.Translate(actionSet);
            foreachBuilder.Finish();
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

            public VarBuilderAttribute[] GetAttributes()
            {
                return new VarBuilderAttribute[0];
            }

            public DeltinScriptParser.Code_typeContext GetCodeType()
            {
                return _foreachContext.code_type();
            }

            public Location GetDefineLocation()
            {
                return new Location(ParseInfo.Script.Uri, GetNameRange());
            }

            public string GetName()
            {
                return _foreachContext.name?.Text;
            }

            public DocRange GetNameRange()
            {
                if (_foreachContext.name != null) return DocRange.GetRange(_foreachContext.name);
                return DocRange.GetRange(_foreachContext);
            }
        }
    }
}