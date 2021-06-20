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
            ReturningFromScope = scope;

            // Get the expression being returned.
            if (returnContext.Expression != null)
            {
                ReturningValue = parseInfo.SetExpectType(parseInfo.ReturnType).GetExpression(scope, returnContext.Expression);
                
                if (parseInfo.ReturnType != null)
                    SemanticsHelper.ExpectValueType(parseInfo, ReturningValue, parseInfo.ReturnType, returnContext.Expression.Range);
            }
            // No return value provided, and one was expected.
            else if (parseInfo.ReturnType != null)
                parseInfo.Script.Diagnostics.Error("Must return a value of type '" + parseInfo.ReturnType.GetName() + "'", returnContext.Token.Range);
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
        readonly IExpression _deleteValue;
        string _comment;

        public DeleteAction(ParseInfo parseInfo, Scope scope, Delete deleteContext)
        {
            _deleteValue = parseInfo.GetExpression(scope, deleteContext.Deleting);

            if (!_deleteValue.Type().CanBeDeleted)
                parseInfo.Script.Diagnostics.Error($"Type '{_deleteValue.Type().Name}' cannot be deleted", deleteContext.Deleting.Range);
        }

        public void Translate(ActionSet actionSet)
        {
            actionSet = actionSet.SetNextComment(_comment);

            // Object reference to delete.
            Element delete = (Element)_deleteValue.Parse(actionSet);

            // Class data.
            var classData = actionSet.Translate.DeltinScript.GetComponent<ClassData>();

            // Remove the variable from the list of classes.
            classData.ClassIndexes.Set(actionSet, value: 0, index: delete);

            // Delete the object.
            _deleteValue.Type().Delete(actionSet, delete);
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment) => _comment = comment;
    }
}