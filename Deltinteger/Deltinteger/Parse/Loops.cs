using System;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    class WhileAction : IStatement
    {
        public IExpression Condition { get; }
        public BlockAction Block { get; }

        public WhileAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.WhileContext whileContext)
        {
            if (whileContext.expr() == null)
                script.Diagnostics.Error("Expected expression.", DocRange.GetRange(whileContext.LEFT_PAREN()));
            else
                Condition = DeltinScript.GetExpression(script, translateInfo, scope, whileContext.expr());
            
            Block = new BlockAction(script, translateInfo, scope, whileContext.block());
        }

        public void Translate(ActionSet actionSet)
        {
            WhileBuilder whileBuilder = new WhileBuilder(actionSet, Condition.Parse(actionSet));
            whileBuilder.Setup();
            Block.Translate(actionSet);
            whileBuilder.Finish();
        }
    }

    class ForAction : IStatement
    {
        private Var DefinedVariable { get; }
        private IExpression Condition { get; }
        private SetVariableAction SetVariableAction { get; }
        private BlockAction Block { get; }

        public ForAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.ForContext forContext)
        {
            Scope varScope = scope.Child();

            if (forContext.define() != null)
            {
                DefinedVariable = Var.CreateVarFromContext(VariableDefineType.Scoped, script, translateInfo, forContext.define());
                DefinedVariable.Finalize(varScope);
            }

            if (forContext.expr() != null)
                Condition = DeltinScript.GetExpression(script, translateInfo, varScope, forContext.expr());
            
            if (forContext.varset() != null)
                SetVariableAction = new SetVariableAction(script, translateInfo, varScope, forContext.varset());

            // Get the block.
            Scope blockScope = varScope.Child();
            Block = new BlockAction(script, translateInfo, blockScope, forContext.block());
        }

        public void Translate(ActionSet actionSet)
        {
            if (DefinedVariable != null)
            {
                // Add the defined variable to the index assigner.
                actionSet.IndexAssigner.Add(actionSet.VarCollection, DefinedVariable, actionSet.IsGlobal, null);

                // Set the initial variable.
                if (actionSet.IndexAssigner[DefinedVariable] is IndexReference)
                    actionSet.AddAction(((IndexReference)actionSet.IndexAssigner[DefinedVariable]).SetVariable(
                        (Element)DefinedVariable.InitialValue.Parse(actionSet)
                    ));
            }
            
            WhileBuilder whileBuilder = new WhileBuilder(actionSet, Condition?.Parse(actionSet));
            whileBuilder.Setup();

            Block.Translate(actionSet);

            if (SetVariableAction != null)
                SetVariableAction.Translate(actionSet);
            
            whileBuilder.Finish();
        }
    }
}