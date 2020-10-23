using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public interface IStatement
    {
        void Translate(ActionSet actionSet);

        void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment)
        {
            diagnostics.Error("This statement cannot be documented.", range);
        }
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
        public IStatement Block { get; }
        public DocRange ErrorRange { get; }
        public bool WillRun { get; }

        public PathInfo(IStatement block, DocRange errorRange, bool willRun)
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
        private readonly Scope ReturningFromScope;

        public ReturnAction(ParseInfo parseInfo, Scope scope, Return returnContext)
        {
            ErrorRange = returnContext.Range;
            if (returnContext.Expression != null) ReturningValue = parseInfo.GetExpression(scope, returnContext.Expression);
            ReturningFromScope = scope;
        }

        public void Translate(ActionSet actionSet)
        {
            if (ReturningValue != null)
                actionSet.ReturnHandler.ReturnValue(ReturningValue.Parse(actionSet));
            actionSet.ReturnHandler.Return(ReturningFromScope, actionSet);
        }
    }

    public class DeleteAction : IStatement
    {
        private IExpression DeleteValue { get; }

        public DeleteAction(ParseInfo parseInfo, Scope scope, Delete deleteContext)
        {
            DeleteValue = parseInfo.GetExpression(scope, deleteContext.Deleting);

            if (DeleteValue.Type() == null)
                parseInfo.Script.Diagnostics.Error("Expression has no type.", deleteContext.Deleting.Range);
            
            else if (!DeleteValue.Type().CanBeDeleted)
                parseInfo.Script.Diagnostics.Error($"Type '{DeleteValue.Type().Name}' cannot be deleted.", deleteContext.Deleting.Range);
        }

        public void Translate(ActionSet actionSet)
        {
            // Object reference to delete.
            Element delete = (Element)DeleteValue.Parse(actionSet);

            // Class data.
            var classData = actionSet.Translate.DeltinScript.GetComponent<ClassData>();

            // Remove the variable from the list of classes.
            actionSet.AddAction(classData.ClassIndexes.SetVariable(
                value: new V_Number(0),
                index: delete
            ));

            // Delete the object.
            DeleteValue.Type().Delete(actionSet, delete);
        }
    }
}