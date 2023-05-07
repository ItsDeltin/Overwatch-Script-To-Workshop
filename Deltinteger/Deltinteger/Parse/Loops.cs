using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    public abstract class LoopAction : IStatement
    {
        /// <summary>The path info of the loop block.</summary>
        protected PathInfo Path;
        /// <summary>The meta comment preceeding the loop.</summary>
        protected string Comment { get; private set; }

        // If statements nested in while loops will cause the workshop's Continue action
        // to restart right before the if statement instead of at the end of the loop.
        const bool ContinueWorkaround = true;

        public abstract void Translate(ActionSet actionSet);

        public PathInfo[] GetPaths() => new PathInfo[] { Path };

        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment) => Comment = comment;
    }

    class WhileAction : LoopAction
    {
        private IExpression Condition { get; }
        private IStatement Block { get; }

        public WhileAction(ParseInfo parseInfo, Scope scope, While whileContext)
        {
            Condition = parseInfo.GetExpression(scope, whileContext.Condition);

            TypeComparison.ExpectNonConstant(parseInfo, whileContext.Condition.Range, Condition.Type());

            Block = parseInfo.SetLoopAllowed(true).GetStatement(scope, whileContext.Statement);
            Path = new PathInfo(Block, whileContext.Range, false);
        }

        public override void Translate(ActionSet actionSet)
        {
            int actionCountPreCondition = actionSet.ActionCount;

            Element condition = (Element)Condition.Parse(actionSet);
            bool actionsAdded = actionSet.ActionCount > actionCountPreCondition;

            if (!actionsAdded)
            {
                // Create a normal while loop.
                actionSet.AddAction(Element.While(condition).AddComment(Comment));

                // Translate the block.
                var loopHelper = new LoopFlowHelper(actionSet);
                Block.Translate(actionSet.SetLoop(loopHelper));

                // Resolve continues.
                loopHelper.ContinueToHere();

                // Cap the block.
                actionSet.AddAction(Element.End());

                // Resolve breaks.
                loopHelper.BreakToHere();
            }
            else
            {
                // The while condition requires actions to get the value.
                actionSet.ActionList.Insert(actionCountPreCondition, new ALAction(Element.While(Element.True()).AddComment(Comment)));

                SkipStartMarker whileEndSkip = new SkipStartMarker(actionSet, condition);
                actionSet.AddAction(whileEndSkip);

                // Translate the block.
                var loopHelper = new LoopFlowHelper(actionSet);
                Block.Translate(actionSet.SetLoop(loopHelper));

                // Resolve continues.
                loopHelper.ContinueToHere();

                // Cap the block.
                actionSet.AddAction(Element.End());

                // Skip to the end when the condition is false.
                SkipEndMarker whileEnd = new SkipEndMarker();
                whileEndSkip.SetEndMarker(whileEnd);
                actionSet.AddAction(whileEnd);

                // Resolve breaks.
                loopHelper.BreakToHere();
            }
        }
    }

    class ForAction : LoopAction
    {
        readonly bool IsAutoFor;
        readonly IStatement Block;
        readonly IExpression Condition;
        readonly IVariable DefinedVariable;

        // For
        readonly SetVariableAction Initializer;
        readonly IStatement Iterator;

        // Auto-for
        readonly VariableResolve VariableResolve;
        readonly IExpression InitialResolveValue;
        readonly IExpression Step;

        public ForAction(ParseInfo parseInfo, Scope scope, For forContext)
        {
            Scope varScope = scope.Child();

            IsAutoFor = forContext.Iterator is ExpressionStatement;

            // Get the initializer.
            if (!IsAutoFor)
            {
                // Normal for loop initializer.
                if (forContext.Initializer != null)
                {
                    // Declaration for initializer.
                    if (forContext.Initializer is VariableDeclaration declaration)
                        DefinedVariable = new ScopedVariable(false, varScope, new DefineContextHandler(parseInfo, declaration)).GetVar();
                    // Variable assignment for initializer
                    else if (forContext.Initializer is Assignment assignment)
                        Initializer = new SetVariableAction(parseInfo, varScope, assignment);

                    // TODO: Throw error on incorrect initializer type.
                }
            }
            else
            {
                // Auto-for initializer.
                // Missing initializer.
                if (forContext.Initializer == null)
                {
                    // Error if there is no initializer.
                    if (forContext.InitializerSemicolon)
                        parseInfo.Script.Diagnostics.Error("Auto-for loops require an initializer.", forContext.InitializerSemicolon.Range);
                }
                // Declaration
                else if (forContext.Initializer is VariableDeclaration declaration)
                {
                    DefinedVariable = new ScopedVariable(false, varScope, new DefineContextHandler(parseInfo, declaration)).GetVar();
                }
                // Assignment
                else if (forContext.Initializer is Assignment assignment)
                {
                    // Get the variable being set.
                    VariableResolve = new VariableResolve(parseInfo, new VariableResolveOptions()
                    {
                        // The for cannot be indexed and should be on the rule-level.
                        CanBeIndexed = false,
                        FullVariable = true
                    }, parseInfo.GetExpression(varScope, assignment.VariableExpression), assignment.VariableExpression.Range);

                    InitialResolveValue = parseInfo.GetExpression(scope, assignment.Value);
                }
                // Variable
                else if (forContext.Initializer is ExpressionStatement exprStatement && exprStatement.Expression is Identifier identifier)
                {
                    // The variable is defined but no start value was given. In this case, just start at 0.
                    // Get the variable.
                    VariableResolve = new VariableResolve(parseInfo, new VariableResolveOptions()
                    {
                        // The for cannot be indexed and should be on the rule-level.
                        CanBeIndexed = false,
                        FullVariable = true
                    }, parseInfo.GetExpression(varScope, identifier), identifier.Range);
                }
                // Incorrect initializer.
                else
                {
                    // TODO: throw error on incorrect expression type.
                }
            }

            // Get the condition.
            if (forContext.Condition != null)
            {
                Condition = parseInfo.GetExpression(varScope, forContext.Condition);
                TypeComparison.ExpectNonConstant(parseInfo, forContext.Condition.Range, Condition.Type());
            }

            // Get the iterator.
            if (forContext.Iterator != null)
            {
                // Get the auto-for
                if (IsAutoFor)
                {
                    Step = parseInfo.GetExpression(varScope, ((ExpressionStatement)forContext.Iterator).Expression);
                }
                // Get the for assignment.
                else
                {
                    Iterator = parseInfo.GetStatement(varScope, forContext.Iterator);
                }
            }

            // Get the block.
            Block = parseInfo.SetLoopAllowed(true).GetStatement(varScope, forContext.Block);
            // Get the path info.
            Path = new PathInfo(Block, forContext.Range, false);
        }

        public override void Translate(ActionSet actionSet)
        {
            if (IsAutoFor)
                TranslateAutoFor(actionSet);
            else
                TranslateFor(actionSet);
        }

        void TranslateFor(ActionSet actionSet)
        {
            actionSet = actionSet.SetNextComment(Comment);

            if (DefinedVariable != null)
            {
                // Add the defined variable to the index assigner.
                var gettable = DefinedVariable.GetInstance(null, actionSet.ThisTypeLinker).GetAssigner(new(actionSet)).GetValue(actionSet);
                actionSet.IndexAssigner.Add(DefinedVariable, gettable);
            }
            else if (Initializer != null)
                Initializer.Translate(actionSet);

            // Get the condition.
            Element condition;
            if (Condition != null) condition = (Element)Condition.Parse(actionSet); // User-define condition
            else condition = Element.True(); // No condition, just use true.
            actionSet.AddAction(Element.While(condition));

            // Only use workshop continues if there is no iterator statement.
            var loopHelper = new LoopFlowHelper(actionSet, Iterator == null);
            Block.Translate(actionSet.SetLoop(loopHelper));

            // Resolve continues.
            loopHelper.ContinueToHere();

            if (Iterator != null)
                Iterator.Translate(actionSet);

            actionSet.AddAction(Element.End());

            // Resolve breaks.
            loopHelper.BreakToHere();
        }

        void TranslateAutoFor(ActionSet actionSet)
        {
            Element target;
            Element start;

            IndexReference indexReference;

            // Existing variable being used in for.
            if (VariableResolve != null)
            {
                VariableElements elements = VariableResolve.ParseElements(actionSet);
                indexReference = (IndexReference)elements.IndexReference;

                target = elements.Target;
                start = (Element)InitialResolveValue?.Parse(actionSet) ?? Element.Num(0);
            }
            // New variable being use in for.
            else
            {
                // Get the gettable assigner for the for variable.
                var assignerResult = DefinedVariable.GetDefaultInstance(null).GetAssigner(new(actionSet)).GetResult(new GettableAssignerValueInfo(actionSet)
                {
                    SetInitialValue = SetInitialValue.DoNotSet
                });

                // Link the value to the variable.
                actionSet.IndexAssigner.Add(DefinedVariable, assignerResult.Gettable);

                // Set variable, target, and stop.
                indexReference = (IndexReference)actionSet.IndexAssigner.Get(DefinedVariable); // Extract the workshop variable.
                target = Element.EventPlayer(); // Set target to Event Player since declaring variables has no target.
                start = (Element)assignerResult.InitialValue ?? Element.Num(0); // Set start to InitialValue or 0 if null.
            }

            Element stop = (Element)Condition.Parse(actionSet);
            Element step = (Element)Step.Parse(actionSet);

            // We can only auto for if there are no indices.
            bool canAutoFor = indexReference.Index.Length == 0;

            if (canAutoFor)
            {
                var variable = indexReference.WorkshopVariable;

                // Global
                if (variable.IsGlobal)
                    actionSet.AddAction(Element.ForGlobalVariable(variable, start, stop, step)
                        .AddComment(Comment));
                // Player
                else
                    actionSet.AddAction(Element.ForPlayerVariable(target, variable, start, stop, step)
                        .AddComment(Comment));
            }
            else
            {
                // Init
                indexReference.Set(actionSet, Element.Num(0), target);
                // Loop
                actionSet.AddAction(Element.While(Element.Compare(
                    indexReference.Get(target), Operator.LessThan, stop
                )));
            }

            // Translate the block.
            var loopHelper = new LoopFlowHelper(actionSet, canAutoFor);
            Block.Translate(actionSet.SetLoop(loopHelper));

            // Resolve continues.
            loopHelper.ContinueToHere();

            if (!canAutoFor)
                indexReference.Modify(actionSet, Operation.Add, Element.Num(1), target, new Element[0]);

            // Cap the for.
            actionSet.AddAction(Element.End());

            // Resolve breaks.
            loopHelper.BreakToHere();
        }
    }

    class ForeachAction : LoopAction
    {
        readonly IVariable foreachVar;
        readonly IExpression array;
        readonly IStatement block;
        readonly bool isExtended;

        public ForeachAction(ParseInfo parseInfo, Scope scope, Foreach foreachContext)
        {
            Scope varScope = scope.Child();

            foreachVar = new ForeachVariable(varScope, new ForeachContextHandler(parseInfo, foreachContext)).GetVar();

            // Get the array that will be iterated on.
            array = parseInfo.GetExpression(scope, foreachContext.Expression);

            // Strict when struct
            if (array.Type().Attributes.IsStruct)
            {
                // Get the declared variable's type.
                var variableType = foreachVar.GetDefaultInstance(null).CodeType.GetCodeType(parseInfo.TranslateInfo);

                // Make sure the struct is an array.
                if (array.Type() is not ArrayType arrayType)
                    parseInfo.Script.Diagnostics.Error("Struct must be an array", foreachContext.Expression.Range);

                // Make sure the type matches the array's type.
                else if (!variableType.Is(arrayType.ArrayOfType))
                    parseInfo.Script.Diagnostics.Error("Variable type must match the array's type", foreachContext.Identifier.Range);
            }

            // Get the foreach block.
            block = parseInfo.SetLoopAllowed(true).GetStatement(varScope, foreachContext.Statement);
            // Get the path info.
            Path = new PathInfo(block, foreachContext.Range, false);

            isExtended = foreachContext.Extended;
        }

        public override void Translate(ActionSet actionSet)
        {
            actionSet = actionSet.SetNextComment(Comment);

            ForeachBuilder foreachBuilder = new ForeachBuilder(foreachVar.Name, actionSet, array.Parse(actionSet), actionSet.IsRecursive, isExtended);

            // Add the foreach value to the assigner.
            actionSet.IndexAssigner.Add(foreachVar, foreachBuilder.IndexValue);

            // Translate the block.
            var loopHelper = new LoopFlowHelper(actionSet, false);
            block.Translate(actionSet.SetLoop(loopHelper));

            // Resolve continues.
            loopHelper.ContinueToHere();

            // Finish the foreach.
            foreachBuilder.Finish();

            // Resolve breaks.
            loopHelper.BreakToHere();
        }

        class ForeachContextHandler : IVarContextHandler
        {
            public ParseInfo ParseInfo { get; }
            private readonly Foreach _foreachContext;

            public ForeachContextHandler(ParseInfo parseInfo, Foreach foreachContext)
            {
                ParseInfo = parseInfo;
                _foreachContext = foreachContext;
            }

            public void GetComponents(VariableComponentCollection componentCollection) { }
            public IParseType GetCodeType() => _foreachContext.Type;
            public Location GetDefineLocation() => _foreachContext.Identifier ? new Location(ParseInfo.Script.Uri, GetNameRange()) : null;
            public string GetName() => _foreachContext.Identifier?.Text;

            public DocRange GetNameRange() => _foreachContext.Identifier?.Range;
            public DocRange GetTypeRange() => _foreachContext.Type?.Range;
        }
    }
}