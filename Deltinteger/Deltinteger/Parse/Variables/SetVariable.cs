using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class SetVariableAction : IStatement
    {
        private VariableResolve VariableResolve { get; }
        private string Operation { get; }
        private IExpression Value { get; }

        public SetVariableAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.VarsetContext varsetContext)
        {
            IExpression variableExpression = DeltinScript.GetExpression(parseInfo, scope, varsetContext.var);

            // Get the variable being set.
            VariableResolve = new VariableResolve(new VariableResolveOptions(), variableExpression, DocRange.GetRange(varsetContext), parseInfo.Script.Diagnostics);
            
            // Get the operation.
            if (varsetContext.statement_operation() != null)
            {
                Operation = varsetContext.statement_operation().GetText();

                // If there is no value, syntax error.
                if (varsetContext.val == null)
                    parseInfo.Script.Diagnostics.Error("Expected an expression.", DocRange.GetRange(varsetContext).end.ToRange());
                
                // Parse the value.
                else
                    Value = DeltinScript.GetExpression(parseInfo, scope, varsetContext.val);
            }
            else if (varsetContext.INCREMENT() != null) Operation = "++";
            else if (varsetContext.DECREMENT() != null) Operation = "--";
        }

        public void Translate(ActionSet actionSet)
        {
            VariableElements elements = VariableResolve.ParseElements(actionSet);

            Element value = null;
            if (Value != null) value = (Element)Value.Parse(actionSet);

            Elements.Operation? modifyOperation = null;
            switch (Operation)
            {
                case "=": break;
                case "^=": modifyOperation = Elements.Operation.RaiseToPower; break;
                case "*=": modifyOperation = Elements.Operation.Multiply;     break;
                case "/=": modifyOperation = Elements.Operation.Divide;       break;
                case "%=": modifyOperation = Elements.Operation.Modulo;       break;
                case "+=": modifyOperation = Elements.Operation.Add;          break;
                case "-=": modifyOperation = Elements.Operation.Subtract;     break;
                case "++": value = 1; modifyOperation = Elements.Operation.Add;      break;
                case "--": value = 1; modifyOperation = Elements.Operation.Subtract; break;
                default: throw new Exception($"Unknown operation {Operation}.");
            }

            if (modifyOperation == null)
                actionSet.AddAction(elements.IndexReference.SetVariable(value, elements.Target, elements.Index));
            else
                actionSet.AddAction(elements.IndexReference.ModifyVariable((Elements.Operation)modifyOperation, value, elements.Target, elements.Index));

        }
    }
}