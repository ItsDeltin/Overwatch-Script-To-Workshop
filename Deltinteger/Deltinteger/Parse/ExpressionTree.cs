using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Antlr4.Runtime.Tree;

namespace Deltin.Deltinteger.Parse
{
    public class ExpressionTree : IExpression, IStatement
    {
        public IExpression[] Tree { get; }
        public IExpression Result { get; }
        public bool Completed { get; } = true;
        public ITreeContextPart[] ExprContextTree { get; }

        private ITerminalNode _trailingSeperator = null;

        public ExpressionTree(ParseInfo parseInfo, Scope scope, DeltinScriptParser.E_expr_treeContext exprContext, bool usedAsValue)
        {
            ExprContextTree = Flatten(parseInfo.Script, exprContext);

            // Setup
            for (int i = 0; i < ExprContextTree.Length; i++)
                ExprContextTree[i].Setup(new TreeContextParseInfo() {
                    ParseInfo = parseInfo,
                    Getter = scope,
                    Scope = i == 0 ? scope : ExprContextTree[i - 1].GetScope() ?? new Scope(),
                    Parent = i == 0 ? null : ExprContextTree[i - 1],
                    UsedAsExpression = usedAsValue || i < ExprContextTree.Length - 1
                });

            // Get expressions
            Tree = new IExpression[ExprContextTree.Length];
            IExpression current = ExprContextTree[0].GetExpression();
            Tree[0] = current;
            if (current != null)
                for (int i = 1; i < ExprContextTree.Length; i++)
                {
                    current = ExprContextTree[i].GetExpression();

                    Tree[i] = current;

                    if (current == null)
                    {
                        Completed = false;
                        break;
                    }
                }
            else Completed = false;
        
            if (Completed)
                Result = Tree[Tree.Length - 1];
            
            // Get the completion items for each expression in the path.
            GetCompletion(parseInfo.Script, scope);
        }

        private ITreeContextPart[] Flatten(ScriptFile script, DeltinScriptParser.E_expr_treeContext exprContext)
        {
            var exprList = new List<ITreeContextPart>();
            Flatten(script, exprContext, exprList);
            return exprList.ToArray();

            // Recursive flatten function.
            void Flatten(ScriptFile script, DeltinScriptParser.E_expr_treeContext exprContext, List<ITreeContextPart> exprList)
            {
                // If the expression is a Tree, recursively flatten.
                if (exprContext.expr() is DeltinScriptParser.E_expr_treeContext)
                    Flatten(script, (DeltinScriptParser.E_expr_treeContext)exprContext.expr(), exprList);
                // Otherwise, add the expression to the list.
                else
                {
                    // Get the function.
                    if (exprContext.expr() is DeltinScriptParser.E_methodContext method)
                        exprList.Add(new FunctionPart(method.method()));
                    // Get the variable.
                    else if (exprContext.expr() is DeltinScriptParser.E_variableContext variable)
                        exprList.Add(new VariableOrTypePart(variable.variable()));
                    // Get the expression.
                    else exprList.Add(new ExpressionPart(exprContext.expr()));
                }

                // Syntax error if there is no method or variable.
                if (exprContext.method() == null && exprContext.variable() == null)
                {
                    script.Diagnostics.Error("Expected expression.", DocRange.GetRange(exprContext.SEPERATOR()));
                    _trailingSeperator = exprContext.SEPERATOR();
                }
                else
                {
                    // Get the method.
                    if (exprContext.method() != null)
                        exprList.Add(new FunctionPart(exprContext.method()));
                    // Get the variable.
                    if (exprContext.variable() != null)
                        exprList.Add(new VariableOrTypePart(exprContext.variable()));
                }
            }
        }

        private void GetCompletion(ScriptFile script, Scope scope)
        {
            for (int i = 0; i < Tree.Length; i++)
            if (Tree[i] != null)
            {
                // Get the treescope. Don't get the completion items if it is null.
                var treeScope = ExprContextTree[i].GetScope();
                if (treeScope != null)
                {
                    DocRange range;
                    if (i < Tree.Length - 1)
                    {
                        range = ExprContextTree[i + 1].GetRange();
                    }
                    // Expression path has a trailing '.'
                    else if (_trailingSeperator != null)
                    {
                        range = new DocRange(
                            DocRange.GetRange(_trailingSeperator).end,
                            DocRange.GetRange(script.NextToken(_trailingSeperator)).start
                        );
                    }
                    else continue;

                    script.AddCompletionRange(new CompletionRange(treeScope, scope, range, CompletionRangeKind.ClearRest));
                }
            }
        }

        public Scope ReturningScope()
        {
            if (Completed)
                return Result.ReturningScope();
            else
                return null;
        }

        public CodeType Type() => Result?.Type();

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return ParseTree(actionSet, true).Result;
        }

        public void Translate(ActionSet actionSet)
        {
            ParseTree(actionSet, false);
        }

        /// <summary>Sets the related output comment. This assumes that the result is both an IExpression and an IStatement.</summary>
        public void OutputComment(FileDiagnostics diagnostics, DocRange range, string comment) => ((IStatement)Result).OutputComment(diagnostics, range, comment);

        public ExpressionTreeParseResult ParseTree(ActionSet actionSet, bool expectingValue)
        {
            IGettable resultingVariable = null; // The resulting variable.
            IWorkshopTree target = null; // The resulting player.
            IWorkshopTree result = null; // The resulting value.
            VarIndexAssigner currentAssigner = actionSet.IndexAssigner;
            IWorkshopTree currentObject = null;
            Element[] resultIndex = new Element[0];

            for (int i = 0; i < Tree.Length; i++)
            {
                bool isLast = i == Tree.Length - 1;
                IWorkshopTree current = null;
                if (Tree[i] is CallVariableAction)
                {
                    var callVariableAction = (CallVariableAction)Tree[i];

                    var reference = currentAssigner[callVariableAction.Calling];
                    current = reference.GetVariable((Element)target);

                    resultIndex = new Element[callVariableAction.Index.Length];
                    for (int ai = 0; ai < callVariableAction.Index.Length; ai++)
                    {
                        var workshopIndex = callVariableAction.Index[ai].Parse(actionSet);
                        resultIndex[ai] = (Element)workshopIndex;
                        current = Element.Part<V_ValueInArray>(current, workshopIndex);
                    }

                    // If this is the last node in the tree, set the resulting variable.
                    if (isLast) resultingVariable = reference;
                }
                else
                {
                    var newCurrent = Tree[i].Parse(actionSet.New(currentAssigner).New(currentObject));
                    if (newCurrent != null)
                    {
                        current = newCurrent;
                        resultIndex = new Element[0];
                    }
                }
                
                if (Tree[i].Type() == null)
                {
                    // If this isn't the last in the tree, set it as the target.
                    if (!isLast)
                        target = current;
                    currentObject = null;
                }
                else
                {
                    var type = Tree[i].Type();

                    currentObject = current;
                    currentAssigner = actionSet.IndexAssigner.CreateContained();
                    type.AddObjectVariablesToAssigner(currentObject, currentAssigner);
                }

                result = current;
            }

            if (result == null && expectingValue) throw new Exception("Expression tree result is null");
            return new ExpressionTreeParseResult(result, resultIndex, target, resultingVariable);
        }
    
        public static IExpression ResultingExpression(IExpression expression)
        {
            if (expression is ExpressionTree expressionTree) return expressionTree.Result;
            return expression;
        }
    }

    /// <summary>Data that gets sent to ITreeContextPart.</summary>
    public class TreeContextParseInfo
    {
        public ParseInfo ParseInfo;
        public Scope Scope;
        public Scope Getter;
        public bool UsedAsExpression;
        public ITreeContextPart Parent;
    }

    /// <summary>The base interface for any element in an expression tree.</summary>
    public interface ITreeContextPart
    {
        void Setup(TreeContextParseInfo tcParseInfo);
        Scope GetScope();
        void RetrievedScopeable(IScopeable scopeable) {}
        IExpression GetExpression();
        DocRange GetRange();
    }

    /// <summary>Expressions in the tree.</summary>
    class ExpressionPart : ITreeContextPart
    {
        private readonly DeltinScriptParser.ExprContext _expressionContext;
        private IExpression _expression;

        public ExpressionPart(DeltinScriptParser.ExprContext expression) {
            _expressionContext = expression;
        }

        public void Setup(TreeContextParseInfo tcParseInfo)
        {
            _expression = tcParseInfo.ParseInfo.GetExpression(tcParseInfo.Scope, _expressionContext);
        }

        public Scope GetScope() => _expression.ReturningScope();
        public IExpression GetExpression() => _expression;
        public DocRange GetRange() => DocRange.GetRange(_expressionContext);
    }

    /// <summary>Functions in the expression tree.</summary>
    class FunctionPart : ITreeContextPart
    {
        private readonly DeltinScriptParser.MethodContext _methodContext;
        private CallMethodAction _methodCall;

        public FunctionPart(DeltinScriptParser.MethodContext method) {
            _methodContext = method;
        }

        public void Setup(TreeContextParseInfo tcParseInfo)
        {
            _methodCall = new CallMethodAction(tcParseInfo.ParseInfo, tcParseInfo.Scope, _methodContext, tcParseInfo.UsedAsExpression, tcParseInfo.Getter);
            tcParseInfo.Parent?.RetrievedScopeable(_methodCall.CallingMethod);
        }

        public Scope GetScope() => _methodCall.ReturningScope();
        public IExpression GetExpression() => _methodCall;
        public DocRange GetRange() => DocRange.GetRange(_methodContext.PART());
    }

    /// <summary>Variables or tyes in the expression tree.</summary>
    class VariableOrTypePart : ITreeContextPart
    {
        private readonly DeltinScriptParser.VariableContext _variable;
        private readonly string _name;
        private readonly bool _canBeType;
        private TreeContextParseInfo _tcParseInfo;
        private IPotentialPathOption[] _potentialPaths;
        private IPotentialPathOption _chosenPath;

        public VariableOrTypePart(DeltinScriptParser.VariableContext variable) {
            _variable = variable;
            _name = variable.PART().GetText();
            _canBeType = variable.array() == null;
        }

        public void Setup(TreeContextParseInfo tcParseInfo)
        {
            _tcParseInfo = tcParseInfo;
            _potentialPaths = GetPotentialPaths(tcParseInfo.ParseInfo, tcParseInfo.Scope, tcParseInfo.Getter);
            // default
            if (_potentialPaths.Length > 0) _chosenPath = _potentialPaths[0];
        }

        private IPotentialPathOption[] GetPotentialPaths(ParseInfo parseInfo, Scope scope, Scope getter)
        {
            List<IPotentialPathOption> potentialPaths = new List<IPotentialPathOption>();

            // Get the potential variable.
            IExpression variable = parseInfo.GetVariable(scope, getter, _variable, false);
            // If the variable exists, add it to potentialPaths.
            if (variable != null)
                potentialPaths.Add(new VariableOption(variable));
            
            // Get the potential type.
            if (_canBeType)
            {
                CodeType type = parseInfo.TranslateInfo.Types.GetCodeType(_name);
                // If the type exists, add it to potentialPaths.
                if (type != null)
                    potentialPaths.Add(new TypeOption(type));
            }
            
            return potentialPaths.ToArray();
        }

        public void RetrievedScopeable(IScopeable scopeable)
        {
            foreach (var option in _potentialPaths)
                if (option.GetScope().ScopeContains(scopeable, _tcParseInfo.Getter))
                {
                    _chosenPath = option;
                    return;
                }
        }

        public Scope GetScope()
        {
            Scope scopeBatch = new Scope();
            foreach (var path in _potentialPaths) {
                Scope addScope = path.GetScope();
                if (addScope != null) scopeBatch.CopyAll(addScope, _tcParseInfo.Getter);
            }
            return scopeBatch;
        }
        public IExpression GetExpression() => _chosenPath?.GetExpression();
        public DocRange GetRange() => DocRange.GetRange(_variable);

        interface IPotentialPathOption {
            Scope GetScope();
            IExpression GetExpression();
        }
        class TypeOption : IPotentialPathOption
        {
            private readonly CodeType _type;
            
            public TypeOption(CodeType type) {
                _type = type;
            }

            public Scope GetScope() => _type.ReturningScope();
            public IExpression GetExpression() => _type;
        }
        class VariableOption : IPotentialPathOption
        {
            private readonly IExpression _variable;

            public VariableOption(IExpression variable) {
                _variable = variable;
            }

            public Scope GetScope() => _variable.Type()?.GetObjectScope();
            public IExpression GetExpression() => _variable;
        }
    }

    public class ExpressionTreeParseResult
    {
        public IWorkshopTree Result { get; }
        public Element[] ResultingIndex { get; }
        public IWorkshopTree Target { get; }
        public IGettable ResultingVariable { get; }

        public ExpressionTreeParseResult(IWorkshopTree result, Element[] index, IWorkshopTree target, IGettable resultingVariable)
        {
            Result = result;
            ResultingIndex = index;
            Target = target;
            ResultingVariable = resultingVariable;
        }
    }
}