using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public interface IStatement
    {
        void Translate(ActionSet actionSet);
    }

    /// <summary>
    /// Anything inherting this interface should also inherit IStatement, but not doing so won't cause any problems.
    /// </summary>
    public interface IBlockContainer
    {
        PathInfo[] GetPaths();
    }

    public class PathInfo
    {
        public BlockAction Block { get; }
        public DocRange ErrorRange { get; }
        public bool WillRun { get; }

        public PathInfo(BlockAction block, DocRange errorRange, bool willRun)
        {
            Block = block;
            ErrorRange = errorRange;
            WillRun = willRun;
        }
    }

    public class ReturnAction : IStatement
    {
        public IExpression ReturningValue { get; }
        public DocRange ErrorRange { get; }

        public ReturnAction(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.ReturnContext returnContext)
        {
            ErrorRange = DocRange.GetRange(returnContext.RETURN());
            if (returnContext.expr() != null) ReturningValue = DeltinScript.GetExpression(script, translateInfo, scope, returnContext.expr());
        }

        public void Translate(ActionSet actionSet)
        {
            if (ReturningValue != null)
                actionSet.ReturnHandler.ReturnValue(ReturningValue.Parse(actionSet));
            actionSet.ReturnHandler.Return();
        }
    }
}