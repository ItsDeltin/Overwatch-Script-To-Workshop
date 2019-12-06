using System;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Deltin.Deltinteger.Parse
{
    public interface IExpression
    {
        Scope ReturningScope();
        CodeType Type();
        IWorkshopTree Parse(ActionSet actionSet);
    }

    public class ExpressionTree : IExpression
    {
        public IExpression[] Tree { get; }
        public IExpression Result { get; }
        public bool Completed { get; } = true;
        public DeltinScriptParser.ExprContext[] ExprContextTree { get; }

        public ExpressionTree(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.ExprContext exprContext)
        {
            ExprContextTree = exprContext.expr();

            // Syntax error if there is a . but no expression afterwards.
            for (int i = 0; i < exprContext.ChildCount; i++)
                if (IsSeperator(exprContext.GetChild(i)) && (i == exprContext.ChildCount - 1 || exprContext.GetChild(i + 1) is DeltinScriptParser.ExprContext == false))
                    script.Diagnostics.Error("Expected expression.", DocRange.GetRange((ITerminalNode)exprContext.GetChild(i)));

            Tree = new IExpression[ExprContextTree.Length];
            IExpression current = DeltinScript.GetExpression(script, translateInfo, scope, ExprContextTree[0], false);
            Tree[0] = current;
            if (current != null)
                for (int i = 1; i < ExprContextTree.Length; i++)
                {
                    current = DeltinScript.GetExpression(script, translateInfo, current.ReturningScope() ?? new Scope(), ExprContextTree[i], false);

                    // todo: combine CallMethodAction and IMethod, check if current is IScopeable instead. 
                    if (current != null && current is Var == false && current is CallMethodAction == false && current is ScopedEnumMember == false)
                        script.Diagnostics.Error("Expected variable or method.", DocRange.GetRange(ExprContextTree[i]));

                    Tree[i] = current;

                    if (current == null)
                    {
                        Completed = false;
                        break;
                    }
                }
            else Completed = false;
        
            if (Completed)
                Result = Tree[Tree.Length - 1];
            
            // Get the completion items for each expression in the path.
            for (int i = 0; i < Tree.Length; i++)
            if (Tree[i] != null)
            {
                // Get the treescope. Don't get the completion items if it is null.
                var treeScope = Tree[i].ReturningScope();
                if (treeScope != null)
                {
                    Pos start;
                    Pos end;
                    if (i < Tree.Length - 1)
                    {
                        start = DocRange.GetRange(ExprContextTree[i + 1]).start;
                        end = DocRange.GetRange(ExprContextTree[i + 1]).end;
                    }
                    // Expression path has a trailing '.'
                    else if (IsSeperator(exprContext.children.Last()))
                    {
                        var lastAsToken = ((ITerminalNode)exprContext.children.Last()).Symbol;
                        start = DocRange.GetRange(lastAsToken).end;
                        end = DocRange.GetRange(script.Tokens[lastAsToken.TokenIndex + 1]).start;
                    }
                    else continue;

                    DocRange range = new DocRange(start, end);
                    script.AddCompletionRange(new CompletionRange(treeScope, range, true));
                }
            }
        }

        private static bool IsSeperator(IParseTree element)
        {
            return element is TerminalNodeImpl && ((TerminalNodeImpl)element).Symbol.Type == DeltinScriptParser.SEPERATOR;
        }

        public Scope ReturningScope()
        {
            if (Completed)
                return Result.ReturningScope();
            else
                return null;
        }

        public CodeType Type() => Result.Type();

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return Result.Parse(actionSet);
        }
    }

    public class NumberAction : IExpression
    {
        public double Value { get; }

        public NumberAction(ScriptFile script, DeltinScriptParser.NumberContext numberContext)
        {
            Value = double.Parse(numberContext.GetText());
        }

        public Scope ReturningScope()
        {
            return null;
        }

        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return new V_Number(Value);
        }
    }

    public class BoolAction : IExpression
    {
        public bool Value { get; }

        public BoolAction(ScriptFile script, bool value)
        {
            Value = value;
        }

        public Scope ReturningScope()
        {
            return null;
        }

        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            if (Value) return new V_True();
            else return new V_False();
        }
    }

    public class ValueInArrayAction : IExpression
    {
        public IExpression Expression { get; }
        public IExpression Index { get; }
        private DocRange expressionRange { get; }
        private DocRange indexRange { get; }

        public ValueInArrayAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.ExprContext exprContext)
        {
            if (exprContext.expr() == null) throw new Exception("Array can be determined without inital expression.");

            Expression = DeltinScript.GetExpression(script, translateInfo, scope, exprContext.expr(0));
            expressionRange = DocRange.GetRange(exprContext.expr(0));

            if (exprContext.index == null)
                script.Diagnostics.Error("Expected an expression.", DocRange.GetRange(exprContext.INDEX_START()));
            else
            {
                Index = DeltinScript.GetExpression(script, translateInfo, scope, exprContext.index);
                indexRange = DocRange.GetRange(exprContext.index);
            }
        }

        public Scope ReturningScope()
        {
            // TODO: Support class arrays.
            return null;
        }

        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return Element.Part<V_ValueInArray>(Expression.Parse(actionSet.New(expressionRange)), Index.Parse(actionSet.New(indexRange)));
            //return Expression.Parse(actionSet.New(expressionRange))[Index.Parse(actionSet.New(indexRange))];
        }
    }
}