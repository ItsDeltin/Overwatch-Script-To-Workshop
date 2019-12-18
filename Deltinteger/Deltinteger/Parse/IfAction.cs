using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class IfAction : IStatement
    {
        public IExpression Expression { get; }
        public BlockAction Block { get; }
        public ElseIf[] ElseIfs { get; }
        public BlockAction ElseBlock { get; }

        public IfAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.IfContext ifContext)
        {
            if (ifContext.expr() == null)
                script.Diagnostics.Error("Expected expression.", DocRange.GetRange(ifContext.LEFT_PAREN()));
            else
                Expression = DeltinScript.GetExpression(script, translateInfo, scope, ifContext.expr());
            
            Block = new BlockAction(script, translateInfo, scope, ifContext.block());

            if (ifContext.else_if() != null)
            {
                ElseIfs = new ElseIf[ifContext.else_if().Length];
                for (int i = 0; i < ElseIfs.Length; i++)
                    ElseIfs[i] = new ElseIf(script, translateInfo, scope, ifContext.else_if(i));
            }
            else ElseIfs = new ElseIf[0];

            if (ifContext.@else() != null)
                ElseBlock = new BlockAction(script, translateInfo, scope, ifContext.@else().block());
        }

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

        public ElseIf(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Else_ifContext elseIfContext)
        {
            if (elseIfContext.expr() == null)
                script.Diagnostics.Error("Expected expression.", DocRange.GetRange(elseIfContext.LEFT_PAREN()));
            else
                Expression = DeltinScript.GetExpression(script, translateInfo, scope, elseIfContext.expr());
            
            Block = new BlockAction(script, translateInfo, scope, elseIfContext.block());
        }
    }
}