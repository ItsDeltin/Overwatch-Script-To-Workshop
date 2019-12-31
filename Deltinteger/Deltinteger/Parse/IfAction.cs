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
            if (ifContext.expr() == null)
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(ifContext.LEFT_PAREN()));
            else
                Expression = DeltinScript.GetExpression(parseInfo, scope, ifContext.expr());
            
            var paths = new List<PathInfo>();

            if (ifContext.block() == null)
                parseInfo.Script.Diagnostics.Error("Expected block.", DocRange.GetRange(ifContext.IF()));
            else
                Block = new BlockAction(parseInfo, scope, ifContext.block());
            paths.Add(new PathInfo(Block, DocRange.GetRange(ifContext.IF()), false));

            if (ifContext.else_if() != null)
            {
                ElseIfs = new ElseIf[ifContext.else_if().Length];
                for (int i = 0; i < ElseIfs.Length; i++)
                {
                    ElseIfs[i] = new ElseIf(parseInfo, scope, ifContext.else_if(i));
                    paths.Add(new PathInfo(Block, DocRange.GetRange(ifContext.else_if(i).ELSE(), ifContext.else_if(i).IF()), false));
                }
            }
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
            SkipStartMarker ifStart = new SkipStartMarker(actionSet, Expression.Parse(actionSet));
            actionSet.AddAction(ifStart);

            Block.Translate(actionSet);
            
            List<SkipStartMarker> blockCaps = new List<SkipStartMarker>();

            if (ElseBlock != null || ElseIfs.Length > 0)
            {
                SkipStartMarker ifCap = new SkipStartMarker(actionSet);
                actionSet.AddAction(ifCap);
                blockCaps.Add(ifCap);
            }

            SkipEndMarker ifEnd = new SkipEndMarker();
            actionSet.AddAction(ifEnd);
            ifStart.SkipCount = ifStart.GetSkipCount(ifEnd);

            // Get the else-ifs.
            for (int i = 0; i < ElseIfs.Length; i++)
            {
                bool isLast = i == ElseIfs.Length - 1 && ElseBlock == null;

                SkipStartMarker elseIfStart = new SkipStartMarker(actionSet, ElseIfs[i].Expression.Parse(actionSet));
                actionSet.AddAction(elseIfStart);

                ElseIfs[i].Block.Translate(actionSet);

                if (!isLast)
                {
                    SkipStartMarker elseIfCap = new SkipStartMarker(actionSet);
                    actionSet.AddAction(elseIfCap);
                    blockCaps.Add(elseIfCap);
                }

                SkipEndMarker elseIfEnd = new SkipEndMarker();
                actionSet.AddAction(elseIfEnd);
                elseIfStart.SkipCount = elseIfStart.GetSkipCount(elseIfEnd);
            }

            if (ElseBlock != null) ElseBlock.Translate(actionSet);

            SkipEndMarker contextCap = new SkipEndMarker();
            actionSet.AddAction(contextCap);
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
            if (elseIfContext.expr() == null)
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(elseIfContext.LEFT_PAREN()));
            else
                Expression = DeltinScript.GetExpression(parseInfo, scope, elseIfContext.expr());
            
            if (elseIfContext.block() == null)
                parseInfo.Script.Diagnostics.Error("Expected block.", DocRange.GetRange(elseIfContext.ELSE(), elseIfContext.IF()));
            else
                Block = new BlockAction(parseInfo, scope, elseIfContext.block());
        }
    }
}