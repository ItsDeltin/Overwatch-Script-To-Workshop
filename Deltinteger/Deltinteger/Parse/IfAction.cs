using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class IfAction : IStatement, IBlockContainer
    {
        public IExpression Expression { get; }
        public IStatement Block { get; }
        public ElseIfAction[] ElseIfs { get; }
        public IStatement ElseBlock { get; }
        private PathInfo[] Paths { get; }
        private string Comment;

        public IfAction(ParseInfo parseInfo, Scope scope, If ifContext)
        {
            // Get the if condition.
            Expression = parseInfo.GetExpression(scope, ifContext.Expression);
            
            // Contains the path info of all blocks in the if/else-if/else list.
            var paths = new List<PathInfo>();

            // Get the if's block.
            Block = parseInfo.GetStatement(scope, ifContext.Statement);
            
            // Add the if block path info.
            paths.Add(new PathInfo(Block, DocRange.GetRange(ifContext.IF()), false));

            // Get the else-ifs.
            ElseIfs = new ElseIfAction[ifContext.ElseIfs.Count];
            for (int i = 0; i < ElseIfs.Length; i++)
            {
                ElseIfs[i] = new ElseIfAction(parseInfo, scope, ifContext.ElseIfs[i]);
                paths.Add(new PathInfo(Block, DocRange.GetRange(ifContext.else_if(i).ELSE(), ifContext.else_if(i).IF()), false));
            }

            // If there is an else statement, get the else block.
            if (ifContext.Else != null)
            {
                ElseBlock = parseInfo.GetStatement(scope, ifContext.Else.Statement);
                
                // Add the else path info.
                paths.Add(new PathInfo(ElseBlock, DocRange.GetRange(ifContext.@else().ELSE()), true));
            }
            Paths = paths.ToArray();
        }

        public PathInfo[] GetPaths() => Paths;

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment)
        {
            Comment = comment;
        }

        public void TranslateSkip(ActionSet actionSet)
        {
            // Create the skip for the start of the if statement.
            SkipStartMarker ifStart = new SkipStartMarker(actionSet, Expression.Parse(actionSet));
            actionSet.AddAction(ifStart);

            // Translate the if block.
            Block.Translate(actionSet);
            
            // 'block caps' are skips that are added to the end of the if block and each else-if block.
            // The skips skip to the end of the entire if/else-if/else.
            List<SkipStartMarker> blockCaps = new List<SkipStartMarker>();

            // Add the if cap if there is an else block or there is an else-if block. 
            if (ElseBlock != null || ElseIfs.Length > 0)
            {
                SkipStartMarker ifCap = new SkipStartMarker(actionSet);
                actionSet.AddAction(ifCap);
                blockCaps.Add(ifCap);
            }

            // Marks the end of the if statement. If the if-condition is false, `ifStart` will skip to here.
            SkipEndMarker ifEnd = new SkipEndMarker();
            actionSet.AddAction(ifEnd);

            // Set the if-skip's count.
            ifStart.SetEndMarker(ifEnd);

            // Get the else-ifs.
            for (int i = 0; i < ElseIfs.Length; i++)
            {
                // This will equal true if this is at the last else-if and there is no else.
                bool isLast = i == ElseIfs.Length - 1 && ElseBlock == null;

                // Create the skip for the start of the else-if.
                SkipStartMarker elseIfStart = new SkipStartMarker(actionSet, ElseIfs[i].Expression.Parse(actionSet));
                actionSet.AddAction(elseIfStart);

                // Translate the else-if block.
                ElseIfs[i].Block.Translate(actionSet);

                // If this is not the last block in the entire if/else-if/else list, add the 'block cap'.
                if (!isLast)
                {
                    SkipStartMarker elseIfCap = new SkipStartMarker(actionSet);
                    actionSet.AddAction(elseIfCap);
                    blockCaps.Add(elseIfCap);
                }

                // Marks the end of the else-if statement. If the condition is false, `elseIfStart` will skip to here.
                SkipEndMarker elseIfEnd = new SkipEndMarker();
                actionSet.AddAction(elseIfEnd);
                elseIfStart.SetEndMarker(elseIfEnd);
            }

            // If there is an else block, translate it.
            if (ElseBlock != null) ElseBlock.Translate(actionSet);

            // contextCap marks the end of the entire if/else-if/list.
            SkipEndMarker contextCap = new SkipEndMarker();
            actionSet.AddAction(contextCap);

            // Set all the block caps so they skip to the end of the list.
            foreach (var blockCap in blockCaps)
                blockCap.SetEndMarker(contextCap);
        }

        public void Translate(ActionSet actionSet)
        {
            // Add the if action.
            A_If newIf = Element.Part<A_If>(Expression.Parse(actionSet));
            newIf.Comment = Comment;
            actionSet.AddAction(newIf);

            // Translate the if block.
            Block.Translate(actionSet.Indent());

            // Add the else-ifs.
            for (int i = 0; i < ElseIfs.Length; i++)
            {
                // Add the else-if action.
                actionSet.AddAction(Element.Part<A_ElseIf>(ElseIfs[i].Expression.Parse(actionSet)));

                // Translate the else-if block.
                ElseIfs[i].Block.Translate(actionSet.Indent());
            }

            // If there is an else block, translate it.
            if (ElseBlock != null)
            {
                actionSet.AddAction(new A_Else());
                ElseBlock.Translate(actionSet.Indent());
            }

            // Add the end of the if.
            actionSet.AddAction(new A_End());
        }
    }

    public class ElseIfAction
    {
        public IExpression Expression { get; }
        public IStatement Block { get; }

        public ElseIfAction(ParseInfo parseInfo, Scope scope, ElseIf elseIfContext)
        {
            // Get the else-if's expression.
            Expression = parseInfo.GetExpression(scope, elseIfContext.Expression);
            
            // Get the else-if's block.
            Block = parseInfo.GetStatement(scope, elseIfContext.Statement);
        }
    }
}