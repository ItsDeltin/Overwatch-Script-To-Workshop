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

                if (parseInfo.ReturnType != null &&
                    // Make sure that the return type matches.
                    SemanticsHelper.ExpectValueType(parseInfo, ReturningValue, parseInfo.ReturnType, returnContext.Expression.Range) &&
                    // There is a return tracker with a return handler already added.
                    parseInfo.ReturnTracker != null && parseInfo.ReturnTracker.Returns.Count > 0 &&
                    // The return type is constant.
                    parseInfo.ReturnType.IsConstant())
                    // Multiple returns not allowed.
                    parseInfo.Script.Diagnostics.Error("Cannot have more than one return statement if the function's return type is constant", ErrorRange);

                // returning value in void method
                else if (parseInfo.ReturnType == null)
                    parseInfo.Script.Diagnostics.Error("Return type is void, no value can be returned", ErrorRange);
            }
            // No return value provided, and one was expected.
            else if (parseInfo.ReturnType != null)
                parseInfo.Script.Diagnostics.Error("Must return a value of type '" + parseInfo.ReturnType.GetName() + "'", returnContext.Token.Range);

            parseInfo.ReturnTracker?.Add(this);
        }

        public void Translate(ActionSet actionSet)
        {
            if (ReturningValue != null)
                actionSet.ReturnHandler.ReturnValue(ReturningValue.Parse(actionSet));
            actionSet.ReturnHandler.Return(actionSet);
        }
    }

    public class DeleteAction : IStatement
    {
        readonly IExpression _deleteValue;
        string _comment;
        readonly string _errorMessage;

        public DeleteAction(ParseInfo parseInfo, Scope scope, Delete deleteContext)
        {
            _deleteValue = parseInfo.GetExpression(scope, deleteContext.Deleting);
            _errorMessage = ToWorkshopHelper.LogScriptLocation(parseInfo.Script, deleteContext.DeleteToken.Range);

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

            // Ensure the value is not zero.
            var builder = IfBuilder.If(actionSet, classData.IsReferenceValid(delete), () =>
            {
                // Increment generation
                if (classData.UseClassGenerations)
                    classData.ClassGenerations.Modify(actionSet, Operation.Add, Element.Num(1), null, [classData.GetPointer(delete)]);

                // Remove the variable from the list of classes.
                classData.ClassIndexes.Set(actionSet, value: 0, index: classData.GetPointer(delete));

                // Delete the object.
                _deleteValue.Type().Delete(actionSet, delete);
            });
            if (actionSet.DeltinScript.Settings.LogDeleteReferenceZero)
            {
                builder.Else(() =>
                {
                    actionSet.Log("[Error] Attempted to delete invalid reference" + _errorMessage);
                });
            }
            builder.Ok();
        }

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment) => _comment = comment;
    }
}