using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class IfAction : IStatement, IBlockContainer
    {
        public IExpression Expression { get; }
        public BlockAction Block { get; }
        public ElseIf[] ElseIfs { get; }
        public BlockAction ElseBlock { get; }
        private PathInfo[] Paths { get; }

        public IfAction(ParseInfo parseInfo, Scope scope, DeltinScriptParser.IfContext ifContext)
        {
            // Get the if condition.
            if (ifContext.expr() != null)
                Expression = DeltinScript.GetExpression(parseInfo, scope, ifContext.expr());
            else
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(ifContext.LEFT_PAREN()));
            
            // Contains the path info of all blocks in the if/else-if/else list.
            var paths = new List<PathInfo>();

            // Get the if's block.
            if (ifContext.block() != null)
                Block = new BlockAction(parseInfo, scope, ifContext.block());
            else
                parseInfo.Script.Diagnostics.Error("Expected block.", DocRange.GetRange(ifContext.IF()));
            
            // Add the if block path info.
            paths.Add(new PathInfo(Block, DocRange.GetRange(ifContext.IF()), false));

            // Get the else-ifs.
            if (ifContext.else_if() != null)
            {
                ElseIfs = new ElseIf[ifContext.else_if().Length];
                for (int i = 0; i < ElseIfs.Length; i++)
                {
                    ElseIfs[i] = new ElseIf(parseInfo, scope, ifContext.else_if(i));
                    paths.Add(new PathInfo(Block, DocRange.GetRange(ifContext.else_if(i).ELSE(), ifContext.else_if(i).IF()), false));
                }
            }
            // If there is none, set `ElseIfs` to an empty array since it should not be null.
            else ElseIfs = new ElseIf[0];

            // If there is an else statement, get the else block.
            if (ifContext.@else() != null)
            {
                if (ifContext.block() == null)
                    parseInfo.Script.Diagnostics.Error("Expected block.", DocRange.GetRange(ifContext.@else().block()));
                else
                    ElseBlock = new BlockAction(parseInfo, scope, ifContext.@else().block());
                
                // Add the else path info.
                paths.Add(new PathInfo(ElseBlock, DocRange.GetRange(ifContext.@else().ELSE()), true));
            }
            Paths = paths.ToArray();
        }

        public PathInfo[] GetPaths() => Paths;

        public void Translate(ActionSet actionSet)
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
            ifStart.SkipCount = ifStart.GetSkipCount(ifEnd);

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
                elseIfStart.SkipCount = elseIfStart.GetSkipCount(elseIfEnd);
            }

            // If there is an else block, translate it.
            if (ElseBlock != null) ElseBlock.Translate(actionSet);

            // contextCap marks the end of the entire if/else-if/list.
            SkipEndMarker contextCap = new SkipEndMarker();
            actionSet.AddAction(contextCap);

            // Set all the block caps so they skip to the end of the list.
            foreach (var blockCap in blockCaps)
                blockCap.SkipCount = blockCap.GetSkipCount(contextCap);
        }
    }

    public class ElseIf
    {
        public IExpression Expression { get; }
        public BlockAction Block { get; }

        public ElseIf(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Else_ifContext elseIfContext)
        {
            // Get the else-if's expression.
            if (elseIfContext.expr() != null)
                Expression = DeltinScript.GetExpression(parseInfo, scope, elseIfContext.expr());
            else
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(elseIfContext.LEFT_PAREN()));
            
            // Get the else-if's block.
            if (elseIfContext.block() != null)
                Block = new BlockAction(parseInfo, scope, elseIfContext.block());
            else
                parseInfo.Script.Diagnostics.Error("Expected block.", DocRange.GetRange(elseIfContext.ELSE(), elseIfContext.IF()));
        }
    }
}