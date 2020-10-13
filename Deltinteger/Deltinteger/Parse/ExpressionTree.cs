using System;
using System.Collections.Generic;
using System.Linq;
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
                    ParseInfo = i == 0 ? parseInfo : parseInfo.SetSourceExpression(ExprContextTree[i - 1]),
                    Getter = scope,
                    Scope = i == 0 ? scope : ExprContextTree[i - 1].GetScope() ?? new Scope(),
                    Parent = i == 0 ? null : ExprContextTree[i - 1],
                    UsedAsExpression = usedAsValue || i < ExprContextTree.Length - 1,
                    IsLast = i == ExprContextTree.Length - 1
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
                        current = Element.ValueInArray(current, workshopIndex);
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
        public bool IsLast;

        public void Error(string message, DocRange range) => ParseInfo.Script.Diagnostics.Error(message, range);
    }

    /// <summary>The base interface for any element in an expression tree.</summary>
    public interface ITreeContextPart
    {
        void Setup(TreeContextParseInfo tcParseInfo);
        void OnResolve(Action<IExpression> resolved) => resolved.Invoke(GetExpression());
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

    /// <summary>Variables or types in the expression tree.</summary>
    class VariableOrTypePart : ITreeContextPart
    {
        private readonly DeltinScriptParser.VariableContext _variable;
        private readonly DocRange _range;
        private readonly string _name;
        private TreeContextParseInfo _tcParseInfo;
        private List<IPotentialPathOption> _potentialPaths;
        private IPotentialPathOption _chosenPath;

        public VariableOrTypePart(DeltinScriptParser.VariableContext variable) {
            _variable = variable;
            _range = DocRange.GetRange(_variable.PART());
            _name = variable.PART().GetText();
        }

        public void Setup(TreeContextParseInfo tcParseInfo)
        {
            bool canBeType = _variable.array() == null && tcParseInfo.Parent == null;
            _tcParseInfo = tcParseInfo;
            _potentialPaths = GetPotentialPaths(tcParseInfo, canBeType);

            // If there are any paths.
            if (_potentialPaths.Count > 0)
            {
                _chosenPath = _potentialPaths[0];
                // This is the last expression in the tree, which means RetrievedScopeable will not be called. At this point, nothing can be done about ambiguities.
                // If ParseInfo implements something like ExpectingCodeType, that can be used to further narrow down the chosen path.
                if (tcParseInfo.IsLast) {
                    CheckAmbiguitiesAndAccept();
                }
            }
            else // There are no paths.
            {
                // May resolve to type or variable.
                if (canBeType)
                    tcParseInfo.ParseInfo.Script.Diagnostics.Error($"No variable or type by the name of '{_name}' exists in the current scope.", _range);
                // May resolve to only variable.
                else
                    tcParseInfo.ParseInfo.Script.Diagnostics.Error($"No variable by the name of '{_name}' exists in the {tcParseInfo.Scope.ErrorName}.", _range);
            }
        }

        private List<IPotentialPathOption> GetPotentialPaths(TreeContextParseInfo tcParseInfo, bool canBeType)
        {
            List<IPotentialPathOption> potentialPaths = new List<IPotentialPathOption>();

            // Get the potential variable.
            if (tcParseInfo.Scope.IsVariable(_variable.PART().GetText()))
            {
                IVariable[] variables = tcParseInfo.Scope.GetAllVariables(_variable.PART().GetText());
                foreach (var variable in variables)
                {
                    // Variable handler.
                    var apply = new PotentialVariableApply(tcParseInfo.ParseInfo);

                    // Check accessor.
                    if (!tcParseInfo.Getter.AccessorMatches(tcParseInfo.Scope, variable.AccessLevel))
                        apply.Error(string.Format("'{0}' is inaccessable due to its access level.", _name), _range);

                    // Get the wrapped expression.
                    IExpression expression = apply.Apply(variable, tcParseInfo.ParseInfo.ExpressionIndexArray(tcParseInfo.Getter, _variable.array()), _range);

                    // Add the potential path.
                    potentialPaths.Add(new VariableOption(tcParseInfo.Parent, apply, expression, variable, tcParseInfo.ParseInfo, _range));
                }
            }
            
            // Get the potential type.
            // Currently, OSTW does not support nested types, so make sure there is no parent.
            if (canBeType)
            {
                CodeType type = tcParseInfo.ParseInfo.TranslateInfo.Types.GetCodeType(_name);
                // If the type exists, add it to potentialPaths.
                if (type != null)
                    potentialPaths.Add(new TypeOption(type, tcParseInfo.ParseInfo, _range));
            }
            
            return potentialPaths;
        }

        public void RetrievedScopeable(IScopeable scopeable)
        {            
            // Narrow down the potential paths.
            for (int i = _potentialPaths.Count - 1; i >= 0; i--)
                if (!_potentialPaths[i].GetScope().ScopeContains(scopeable, _tcParseInfo.Getter))
                    _potentialPaths.RemoveAt(i);
            
            // Done
            CheckAmbiguitiesAndAccept();
        }

        public Scope GetScope()
        {
            // Get the name of the scope batch.
            var batchNameGroup = _potentialPaths.Select(pp => pp.GetScope().ErrorName).Distinct(); // Gets all scope names in an enumerable with no duplicates.
            string name = "current scope"; // The default scope name.

            // Set the scope name.
            if (batchNameGroup.Count() == 1) name = batchNameGroup.First();
            else name = "'" + string.Join(", ", batchNameGroup) + "'";

            // Create the scope.
            Scope scopeBatch = new Scope(name);

            // Add all potential path's scopes to the scope batch.
            foreach (var path in _potentialPaths)
                scopeBatch.CopyAll(path.GetScope(), _tcParseInfo.Getter);

            // Finished.
            return scopeBatch;
        }
        public IExpression GetExpression() => _chosenPath?.GetExpression();
        public DocRange GetRange() => DocRange.GetRange(_variable);

        void CheckAmbiguitiesAndAccept()
        {
            CheckAmbiguities();
            _chosenPath = _potentialPaths[0];
            _chosenPath.Accept();
            CallResolvers();
        }

        void CheckAmbiguities()
        {
            for (int i = 0; i < _potentialPaths.Count; i++)
            for (int a = 0; a < _potentialPaths.Count; a++)
            if (a != i && _potentialPaths[i].IsAmbiguousTo(_potentialPaths[a]))
            {
                _tcParseInfo.Error("'" + _name + "' is ambiguous.", _range);
                return;
            }
        }

        private readonly List<Action<IExpression>> _onResolve = new List<Action<IExpression>>();
        public void OnResolve(Action<IExpression> resolved) => _onResolve.Add(resolved);
        void CallResolvers()
        {
            IExpression result = GetExpression();
            foreach (var onResolve in _onResolve) onResolve.Invoke(result);
        }

        interface IPotentialPathOption {
            Scope GetScope();
            IExpression GetExpression();
            void Accept();
            bool IsAmbiguousTo(IPotentialPathOption other);
        }
        class TypeOption : IPotentialPathOption
        {
            private readonly CodeType _type;
            private readonly ParseInfo _parseInfo;
            private readonly DocRange _callRange;
            
            public TypeOption(CodeType type, ParseInfo parseInfo, DocRange callRange) {
                _type = type;
                _parseInfo = parseInfo;
                _callRange = callRange;
            }

            public Scope GetScope() => _type.ReturningScope();
            public IExpression GetExpression() => _type;
            public void Accept()
            {
                _type.Call(_parseInfo, _callRange);
            }

            public bool IsAmbiguousTo(IPotentialPathOption other) => other is TypeOption;
        }
        class VariableOption : IPotentialPathOption
        {
            private readonly ITreeContextPart _parent;
            private readonly PotentialVariableApply _apply;
            private readonly IExpression _expression;
            private readonly IVariable _variable;
            private readonly ParseInfo _parseInfo;
            private readonly DocRange _callRange;

            public VariableOption(ITreeContextPart parent, PotentialVariableApply apply, IExpression expression, IVariable variable, ParseInfo parseInfo, DocRange callRange) {
                _parent = parent;
                _apply = apply;
                _expression = expression;
                _variable = variable;
                _parseInfo = parseInfo;
                _callRange = callRange;
            }

            public Scope GetScope() => _expression.Type()?.GetObjectScope() ?? _parseInfo.TranslateInfo.PlayerVariableScope;
            public IExpression GetExpression() => _expression;
            public void Accept()
            {
                // Call.
                if (_variable is ICallable callable) callable.Call(_parseInfo, _callRange);

                // Restricted value type check.
                if (_parent != null && _variable is IIndexReferencer referencer && RestrictedCall.EventPlayerDefaultCall(referencer, _parent.GetExpression(), _parseInfo))
                    _parseInfo.RestrictedCallHandler.RestrictedCall(new RestrictedCall(RestrictedCallType.EventPlayer, _parseInfo.GetLocation(_callRange), RestrictedCall.Message_EventPlayerDefault(referencer.Name)));

                // Add diagnostics.
                _parseInfo.Script.Diagnostics.AddDiagnostics(_apply.Errors.ToArray());

                // Notify parent about which element was retrived with it's scope.
                _parent?.RetrievedScopeable(_variable);
            }

            public bool IsAmbiguousTo(IPotentialPathOption other)
            {
                if (other is VariableOption variableOption)
                {
                    if (_variable is IAmbiguityCheck check && variableOption._variable is IAmbiguityCheck otherCheck)
                        return check.CanBeAmbiguous() || otherCheck.CanBeAmbiguous();
                    else
                        return true;
                }
                return false;
            }
        }
    
        class PotentialVariableApply : VariableApply
        {
            public List<Diagnostic> Errors { get; } = new List<Diagnostic>();

            public PotentialVariableApply(ParseInfo parseInfo) : base(parseInfo) {}

            protected override void Call(ICallable callable, DocRange range) {}
            protected override void EventPlayerRestrictedCall(RestrictedCall restrictedCall) {}
            public override void Error(string message, DocRange range) => Errors.Add(new Diagnostic(message, range, Diagnostic.Error));
        }
    }

    /// <summary>The result from converting an expression tree to the workshop.</summary>
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

    public interface IAmbiguityCheck
    {
        bool CanBeAmbiguous();
    }
}