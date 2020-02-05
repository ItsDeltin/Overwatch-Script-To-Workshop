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
            // Add the if action.
            actionSet.AddAction(Element.Part<A_If>(Expression.Parse(actionSet)));

            // Translate the if block.
            Block.Translate(actionSet.Indent());

            // Add the else-ifs.
            for (int i = 0; i < ElseIfs.Length; i++)
            {
                // Add the else-if action.
                actionSet.AddAction(Element.Part<A_ElseIf>(ElseIfs[i].Expression.Parse(actionSet)));

                // Translate the else-if block.
                ElseIfs[i].Block.Translate(actionSet.Indent());

                // Do not add the end action for the last else-if if there is an else block.
                if (i != ElseIfs.Length - 1 || ElseBlock == null)
                    // End the else-if.
                    actionSet.AddAction(new A_End());
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