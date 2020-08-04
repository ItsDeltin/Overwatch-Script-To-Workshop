using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Decompiler.TextToElement;

namespace Deltin.Deltinteger.Decompiler.ElementToCode
{
    public static class WorkshopFunctionDecompileHook
    {
        public static readonly Dictionary<string, Action<DecompileRule, FunctionExpression>> Convert = new Dictionary<string, Action<DecompileRule, FunctionExpression>>() {
            {"Empty Array", (decompiler, function) => decompiler.Append("[]")},
            {"Null", (decompiler, function) => decompiler.Append("null")},
            {"True", (decompiler, function) => decompiler.Append("true")},
            {"False", (decompiler, function) => decompiler.Append("false")},
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
                End(decompiler);
            }},
            {"Modify Player Variable", (decompiler, function) => {
                decompiler.Append("ModifyVariable(");
                // Variable
                function.Values[0].Decompile(decompiler);
                decompiler.Append(".");
                function.Values[1].Decompile(decompiler);
                decompiler.Append(", ");
                function.Values[2].Decompile(decompiler);
                decompiler.Append(", ");
                function.Values[3].Decompile(decompiler);
                decompiler.Append(")");
                // Finished
                End(decompiler);
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
                End(decompiler);
            }},
            {"Modify Player Variable At Index", (decompiler, function) => {
                decompiler.Append("ModifyVariable(");
                // Variable
                function.Values[0].Decompile(decompiler);
                decompiler.Append(".");
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
                End(decompiler);
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
                        if (acceptingElseIfs && childFunc.Function.WorkshopName == "Else If")
                        {
                            Cap(decompiler);
                            decompiler.Append("else if (");
                            childFunc.Values[0].Decompile(decompiler);
                            decompiler.Append(")");
                            decompiler.AddBlock();
                            decompiler.Advance();
                        }
                        else if (childFunc.Function.WorkshopName == "Else")
                        {
                            Cap(decompiler);
                            acceptingElseIfs = false;
                            decompiler.Append("else");
                            decompiler.AddBlock();
                            decompiler.Advance();
                        }
                        else if (childFunc.Function.WorkshopName == "End")
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
            }}
        };

        private static void Cap(DecompileRule decompiler)
        {
            decompiler.Outdent();
            decompiler.Append("}");
            decompiler.NewLine();
        }

        public static void End(DecompileRule decompiler)
        {
            decompiler.Append(";");
            decompiler.NewLine();
        }
    }
}