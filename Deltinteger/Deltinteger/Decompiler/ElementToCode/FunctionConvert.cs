using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Decompiler.TextToElement;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Decompiler.ElementToCode
{
    public static class WorkshopFunctionDecompileHook
    {
        private static readonly string[] TerminatorFunctions = new string[] { "End", "Else If", "Else" };

        public static readonly Dictionary<string, Action<DecompileRule, FunctionExpression>> Convert = new Dictionary<string, Action<DecompileRule, FunctionExpression>>() {
            {"Empty Array", (decompiler, function) => decompiler.Append("[]")},
            {"Null", (decompiler, function) => decompiler.Append("null")},
            {"True", (decompiler, function) => decompiler.Append("true")},
            {"False", (decompiler, function) => decompiler.Append("false")},
            {"Array", (decompiler, function) => {
                decompiler.Append("[");
                for (int i = 0; i < function.Values.Length; i++)
                {
                    function.Values[i].Decompile(decompiler);
                    if (i < function.Values.Length - 1) decompiler.Append(", ");
                }
                decompiler.Append("]");
            }},

            {"Map", (decompiler, function) => function.Values[0].Decompile(decompiler)},
            {"Game Mode", (decompiler, function) => function.Values[0].Decompile(decompiler)},
            // {"Button", (decompiler, function) => function.Values[0].Decompile(decompiler)},
            {"Hero", (decompiler, function) => function.Values[0].Decompile(decompiler)},

            {"Modify Global Variable", (decompiler, function) => {
                decompiler.Append("ModifyVariable(");
                // Variable
                function.Values[0].Decompile(decompiler);
                decompiler.Append(", ");
                function.Values[1].Decompile(decompiler);
                decompiler.Append(", ");
                function.Values[2].Decompile(decompiler);
                decompiler.Append(")");
                // Finished
                decompiler.EndAction();
            }},
            {"Modify Player Variable", (decompiler, function) => {
                decompiler.Append("ModifyVariable(");
                // Variable
                function.Values[0].WritePlayerSeperator(decompiler);
                function.Values[1].Decompile(decompiler);
                decompiler.Append(", ");
                function.Values[2].Decompile(decompiler);
                decompiler.Append(", ");
                function.Values[3].Decompile(decompiler);
                decompiler.Append(")");
                // Finished
                decompiler.EndAction();
            }},
            {"Modify Global Variable At Index", (decompiler, function) => {
                decompiler.Append("ModifyVariable(");
                // Variable
                function.Values[0].Decompile(decompiler);
                decompiler.Append("[");
                function.Values[1].Decompile(decompiler);
                decompiler.Append("]");
                decompiler.Append(", ");
                function.Values[2].Decompile(decompiler);
                decompiler.Append(", ");
                function.Values[3].Decompile(decompiler);
                decompiler.Append(")");
                // Finished
                decompiler.EndAction();
            }},
            {"Modify Player Variable At Index", (decompiler, function) => {
                decompiler.Append("ModifyVariable(");
                // Variable
                function.Values[0].WritePlayerSeperator(decompiler);
                function.Values[1].Decompile(decompiler);
                decompiler.Append("[");
                function.Values[2].Decompile(decompiler);
                decompiler.Append("]");
                decompiler.Append(", ");
                function.Values[3].Decompile(decompiler);
                decompiler.Append(", ");
                function.Values[4].Decompile(decompiler);
                decompiler.Append(")");
                // Finished
                decompiler.EndAction();
            }},

            {"Chase Global Variable Over Time", (decompiler, function) => {
                decompiler.Append("ChaseVariableOverTime(");
                // Variable
                function.Values[0].Decompile(decompiler);
                decompiler.Append(", ");
                // Destination
                function.Values[1].Decompile(decompiler);
                decompiler.Append(", ");
                // Duration
                function.Values[2].Decompile(decompiler);
                decompiler.Append(", ");
                // Reevaluation
                function.Values[3].Decompile(decompiler);
                // Finished
                decompiler.Append(")");
                decompiler.EndAction();
            }},
            {"Chase Player Variable Over Time", (decompiler, function) => {
                decompiler.Append("ChaseVariableOverTime(");
                // Player
                function.Values[0].WritePlayerSeperator(decompiler);
                // Variable
                function.Values[1].Decompile(decompiler);
                decompiler.Append(", ");
                // Destination
                function.Values[2].Decompile(decompiler);
                decompiler.Append(", ");
                // Duration
                function.Values[3].Decompile(decompiler);
                decompiler.Append(", ");
                // Reevaluation
                function.Values[4].Decompile(decompiler);
                // Finished
                decompiler.Append(")");
                decompiler.EndAction();
            }},
            {"Chase Global Variable At Rate", (decompiler, function) => {
                decompiler.Append("ChaseVariableAtRate(");
                // Variable
                function.Values[0].Decompile(decompiler);
                decompiler.Append(", ");
                // Destination
                function.Values[1].Decompile(decompiler);
                decompiler.Append(", ");
                // Duration
                function.Values[2].Decompile(decompiler);
                decompiler.Append(", ");
                // Reevaluation
                function.Values[3].Decompile(decompiler);
                // Finished
                decompiler.Append(")");
                decompiler.EndAction();
            }},
            {"Chase Player Variable At Rate", (decompiler, function) => {
                decompiler.Append("ChaseVariableAtRate(");
                // Player
                function.Values[0].WritePlayerSeperator(decompiler);
                // Variable
                function.Values[1].Decompile(decompiler);
                decompiler.Append(", ");
                // Destination
                function.Values[2].Decompile(decompiler);
                decompiler.Append(", ");
                // Duration
                function.Values[3].Decompile(decompiler);
                decompiler.Append(", ");
                // Reevaluation
                function.Values[4].Decompile(decompiler);
                // Finished
                decompiler.Append(")");
                decompiler.EndAction();
            }},
            {"Stop Chasing Global Variable", (decompiler, function) => {
                decompiler.Append("StopChasingVariable(");
                function.Values[0].Decompile(decompiler); // Decompile the variable name.
                decompiler.Append(")");
                decompiler.EndAction();
            }},
            {"Stop Chasing Player Variable", (decompiler, function) => {
                decompiler.Append("StopChasingVariable(");
                function.Values[0].WritePlayerSeperator(decompiler); // Decompile the player.
                function.Values[1].Decompile(decompiler); // Decompile the variable name.
                decompiler.Append(")");
                decompiler.EndAction();
            }},

            {"If", (decompiler, function) => {
                decompiler.Append("if (");
                function.Values[0].Decompile(decompiler);
                decompiler.Append(")");
                decompiler.AddBlock();
                decompiler.Advance();

                bool acceptingElseIfs = true;
                bool finished = false;
                while (!decompiler.IsFinished)
                {
                    if (decompiler.Current is FunctionExpression childFunc)
                    {
                        if (acceptingElseIfs && childFunc.Function.Name == "Else If")
                        {
                            Cap(decompiler);
                            decompiler.Append("else if (");
                            childFunc.Values[0].Decompile(decompiler);
                            decompiler.Append(")");
                            decompiler.AddBlock();
                            decompiler.Advance();
                        }
                        else if (childFunc.Function.Name == "Else")
                        {
                            Cap(decompiler);
                            acceptingElseIfs = false;
                            decompiler.Append("else");
                            decompiler.AddBlock();
                            decompiler.Advance();
                        }
                        else if (childFunc.Function.Name == "End")
                        {
                            finished = true;
                            Cap(decompiler);
                            decompiler.Advance();
                            break;
                        }
                        else
                            decompiler.DecompileCurrentAction();
                    }
                    else
                        decompiler.DecompileCurrentAction();
                }

                if (!finished)
                    Cap(decompiler);
            }},
            {"While", (decompiler, function) => {
                decompiler.Append("while (");
                function.Values[0].Decompile(decompiler);
                decompiler.Append(")");
                bool withBlock = !IsSingleStatementBlock(decompiler);
                decompiler.AddBlock(withBlock);
                decompiler.Advance();

                bool finished = false;
                while (!decompiler.IsFinished)
                {
                    if (decompiler.Current is FunctionExpression childFunc && childFunc.Function.Name == "End")
                    {
                        finished = true;
                        Cap(decompiler);
                        decompiler.Advance();
                        break;
                    }
                    else
                        decompiler.DecompileCurrentAction();
                }

                if (!finished)
                    Cap(decompiler, withBlock);
            }},
            {"For Global Variable", (decompiler, function) => {
                decompiler.Append("for (");
                function.Values[0].Decompile(decompiler);
                decompiler.Append(" = ");
                function.Values[1].Decompile(decompiler);
                decompiler.Append("; ");
                function.Values[2].Decompile(decompiler);
                decompiler.Append("; ");
                function.Values[3].Decompile(decompiler);
                decompiler.Append(")");
                bool withBlock = StartBlock(decompiler);

                new ActionGroupIterator(decompiler).On("End", endFunc => {
                    Cap(decompiler);
                    decompiler.Advance();
                    return true;
                }).OnInterupt(() => Cap(decompiler, withBlock)).Get();
            }},
            {"For Player Variable", (decompiler, function) => {
                decompiler.Append("for (");
                function.Values[0].WritePlayerSeperator(decompiler);
                function.Values[1].Decompile(decompiler);
                decompiler.Append(" = ");
                function.Values[2].Decompile(decompiler);
                decompiler.Append("; ");
                function.Values[3].Decompile(decompiler);
                decompiler.Append("; ");
                function.Values[4].Decompile(decompiler);
                decompiler.Append(")");
                bool withBlock = StartBlock(decompiler);
                
                new ActionGroupIterator(decompiler).On("End", endFunc => {
                    Cap(decompiler);
                    decompiler.Advance();
                    return true;
                }).OnInterupt(() => Cap(decompiler, withBlock)).Get();
            }},
            {"Wait", (decompiler, function) => {
                // Convert the Wait to a MinWait if the wait duration is less than or equal to the minimum.
                if ((function.Values[0] is NumberExpression number && number.Value <= Constants.MINIMUM_WAIT) || (function.Values[0] is FunctionExpression durationFunc && durationFunc.Function.Name == "False"))
                {
                    decompiler.Append("MinWait(");

                    // Add wait behavior if it is not the default.
                    if (function.Values[1] is ConstantEnumeratorExpression enumerator && enumerator.Member != ElementRoot.Instance.GetEnumValue("WaitBehavior", "IgnoreCondition"))
                        enumerator.Decompile(decompiler);
                    
                    // End function
                    decompiler.Append(")");

                    // Finished
                    decompiler.EndAction();
                }
                else
                {
                    // Default
                    function.Default(decompiler, true);
                }
            }},
            
            {"Break", (decompiler, function) => {
                decompiler.Append("break");
                decompiler.EndAction();
            }},
            {"Continue", (decompiler, function) => {
                decompiler.Append("continue");
                decompiler.EndAction();
            }},
            
            // * Legacy *
            {"Set Global Variable", (decompiler, function) => {
                function.Values[0].Decompile(decompiler);
                decompiler.Append(" = ");
                function.Values[1].Decompile(decompiler);

                // Finished
                decompiler.EndAction();
            }},
            {"Set Player Variable", (decompiler, function) => {
                function.Values[0].WritePlayerSeperator(decompiler);
                function.Values[1].Decompile(decompiler);
                decompiler.Append(" = ");
                function.Values[2].Decompile(decompiler);

                // Finished
                decompiler.EndAction();
            }},
            {"Set Global Variable At Index", (decompiler, function) => {
                function.Values[0].Decompile(decompiler);
                decompiler.Append("[");
                function.Values[1].Decompile(decompiler);
                decompiler.Append("]");
                decompiler.Append(" = ");
                function.Values[2].Decompile(decompiler);

                // Finished
                decompiler.EndAction();
            }},
            {"Set Player Variable At Index", (decompiler, function) => {
                function.Values[0].WritePlayerSeperator(decompiler);
                function.Values[1].Decompile(decompiler);
                decompiler.Append("[");
                function.Values[2].Decompile(decompiler);
                decompiler.Append("]");
                decompiler.Append(" = ");
                function.Values[3].Decompile(decompiler);

                // Finished
                decompiler.EndAction();
            }},
            {"Global Variable", (decompiler, function) => {
                function.Values[0].Decompile(decompiler);
            }},
            {"Player Variable", (decompiler, function) => {
                function.Values[0].WritePlayerSeperator(decompiler);
                function.Values[1].Decompile(decompiler);
            }}
        };

        private static bool StartBlock(DecompileRule decompiler)
        {
            bool withBlock = !IsSingleStatementBlock(decompiler);
            decompiler.AddBlock(withBlock);
            decompiler.Advance();
            return withBlock;
        }

        private static void Cap(DecompileRule decompiler, bool endBlock = true)
        {
            decompiler.Outdent();
            if (endBlock)
            {
                decompiler.Append("}");
                decompiler.NewLine();
            }
        }

        private static bool IsSingleStatementBlock(DecompileRule decompiler)
            => decompiler.CurrentAction + 2 < decompiler.ActionList.Length
            && decompiler.ActionList[decompiler.CurrentAction] is FunctionExpression func
            && Array.Exists(TerminatorFunctions, element => element == func.Function.Name);
    }

    class ActionGroupIterator
    {
        private readonly DecompileRule _decompiler;
        private readonly Dictionary<string, Func<FunctionExpression, bool>> _onFunction = new Dictionary<string, Func<FunctionExpression, bool>>();
        private Action _onInterupt;

        public ActionGroupIterator(DecompileRule decompiler)
        {
            _decompiler = decompiler;
        }

        public ActionGroupIterator On(string func, Func<FunctionExpression, bool> action)
        {
            _onFunction.Add(func, action);
            return this;
        }

        public ActionGroupIterator OnInterupt(Action action)
        {
            _onInterupt = action;
            return this;
        }

        public void Get()
        {
            bool finished = false;
            while (!_decompiler.IsFinished)
            {
                // If the current action is a function and there is an action registered for it, invoke it.
                if (_decompiler.Current is FunctionExpression func && _onFunction.TryGetValue(func.Function.Name, out var action))
                {
                    if (action.Invoke(func))
                    {
                        // If the invocation returns true, end the action group.
                        finished = true;
                        break;
                    }
                }
                // Otherwise, decompile the current action.
                else
                    _decompiler.DecompileCurrentAction();
            }
            // If the end of the action set was reached and the group was not completed, invoke _onInterupt.
            if (!finished && _onInterupt != null) _onInterupt.Invoke();
        }
    }
}