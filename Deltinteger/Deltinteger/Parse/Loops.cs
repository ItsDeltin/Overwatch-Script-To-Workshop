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

        public WhileAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.WhileContext whileContext)
        {
            if (whileContext.expr() == null)
                script.Diagnostics.Error("Expected expression.", DocRange.GetRange(whileContext.LEFT_PAREN()));
            else
                Condition = DeltinScript.GetExpression(script, translateInfo, scope, whileContext.expr());
            
            Block = new BlockAction(script, translateInfo, scope, whileContext.block());
            Path = new PathInfo(Block, DocRange.GetRange(whileContext.WHILE()), false);
        }

        public void Translate(ActionSet actionSet)
        {
            WhileBuilder whileBuilder = new WhileBuilder(actionSet, Condition.Parse(actionSet));
            whileBuilder.Setup();
            Block.Translate(actionSet);
            whileBuilder.Finish();
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

        public ForAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.ForContext forContext)
        {
            Scope varScope = scope.Child();

            if (forContext.define() != null)
            {
                DefinedVariable = Var.CreateVarFromContext(VariableDefineType.Scoped, script, translateInfo, forContext.define());
                DefinedVariable.Finalize(varScope);
            }
            else if (forContext.initialVarset != null)
                InitialVarSet = new SetVariableAction(script, translateInfo, varScope, forContext.initialVarset);

            if (forContext.expr() != null)
                Condition = DeltinScript.GetExpression(script, translateInfo, varScope, forContext.expr());
            
            if (forContext.endingVarset != null)
                SetVariableAction = new SetVariableAction(script, translateInfo, varScope, forContext.endingVarset);

            // Get the block.
            if (forContext.block() != null)
            {
                Block = new BlockAction(script, translateInfo, varScope, forContext.block());
                // Get the path info.
                Path = new PathInfo(Block, DocRange.GetRange(forContext.FOR()), false);
            }
            else
                script.Diagnostics.Error("Expected a block.", DocRange.GetRange(forContext.RIGHT_PAREN()));
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
            
            WhileBuilder whileBuilder = new WhileBuilder(actionSet, Condition?.Parse(actionSet));
            whileBuilder.Setup();

            Block.Translate(actionSet);

            if (SetVariableAction != null)
                SetVariableAction.Translate(actionSet);
            
            whileBuilder.Finish();
        }

        public PathInfo[] GetPaths()
        {
            return new PathInfo[] { Path };
        }
    }

    class ForeachAction : IStatement
    {
        private Var ForeachVar { get; }
        private IExpression Array { get; }
        private BlockAction Block { get; }
        private PathInfo Path { get; }

        public ForeachAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.ForeachContext foreachContext)
        {
            Scope varScope = scope.Child();

            ForeachVar = new Var(foreachContext.name.Text, new Location(script.Uri, DocRange.GetRange(foreachContext.name)), script, translateInfo);
            ForeachVar.VariableType = VariableType.ElementReference;
            ForeachVar.CodeType = CodeType.GetCodeTypeFromContext(translateInfo, script, foreachContext.code_type());
            ForeachVar.Finalize(varScope);

            // Get the array that will be iterated on. Syntax error if it is missing.
            if (foreachContext.expr() != null)
                Array = DeltinScript.GetExpression(script, translateInfo, scope, foreachContext.expr());
            else
                script.Diagnostics.Error("Expected expression.", DocRange.GetRange(foreachContext.IN()));

            // Get the foreach block. Syntax error if it is missing.
            if (foreachContext.block() != null)
            {
                Block = new BlockAction(script, translateInfo, varScope, foreachContext.block());
                // Get the path info.
                Path = new PathInfo(Block, DocRange.GetRange(foreachContext.FOREACH()), false);
            }
            else
                script.Diagnostics.Error("Expected block.", DocRange.GetRange(foreachContext.RIGHT_PAREN()));
        }

        public void Translate(ActionSet actionSet)
        {
            ForeachBuilder foreachBuilder = new ForeachBuilder(actionSet, Array.Parse(actionSet));
            actionSet.IndexAssigner.Add(ForeachVar, foreachBuilder.IndexValue);
            foreachBuilder.Setup();
            Block.Translate(actionSet);
            foreachBuilder.Finish();
        }
    }
}