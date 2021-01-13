using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class ExpressionTree : IExpression, IStatement
    {
        public IExpression[] Tree { get; }
        public IExpression Result { get; }
        public bool Completed { get; } = true;
        public ITreeContextPart[] ExprContextTree { get; }

        private Token _trailingSeperator = null;
        private readonly ParseInfo _parseInfo;

        public ExpressionTree(ParseInfo parseInfo, Scope scope, BinaryOperatorExpression exprContext, bool usedAsValue)
        {
            _parseInfo = parseInfo;
            ExprContextTree = Flatten(parseInfo.Script, exprContext);

            // Setup
            var usageResolvers = new UsageResolver[ExprContextTree.Length];
            for (int i = 0; i < ExprContextTree.Length; i++)
            {
                usageResolvers[i] = new UsageResolver();

                ParseInfo partInfo = parseInfo.SetUsageResolver(usageResolvers[i], i == 0 ? null : usageResolvers[i - 1]);
                // If this is not the first expression, clear tail data and set the source expression.
                if (i != 0) partInfo = partInfo.ClearTail().SetSourceExpression(ExprContextTree[i - 1]);
                // If this is not the last expression, clear head data.
                if (i != ExprContextTree.Length - 1) partInfo = partInfo.ClearHead();

                ExprContextTree[i].Setup(new TreeContextParseInfo()
                {
                    ParseInfo = partInfo,
                    Getter = scope,
                    Scope = i == 0 ? scope : ExprContextTree[i - 1].GetScope() ?? new Scope(),
                    Parent = i == 0 ? null : ExprContextTree[i - 1],
                    UsedAsExpression = usedAsValue || i < ExprContextTree.Length - 1,
                    IsLast = i == ExprContextTree.Length - 1
                });
            }

            for (int i = 0; i < usageResolvers.Length; i++)
                usageResolvers[i].ResolveUnknownIfNotResolved();

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

        private ITreeContextPart[] Flatten(ScriptFile script, BinaryOperatorExpression exprContext)
        {
            var exprList = new List<ITreeContextPart>();
            Flatten(script, exprContext, exprList);
            return exprList.ToArray();

            // Recursive flatten function.
            void Flatten(ScriptFile script, BinaryOperatorExpression exprContext, List<ITreeContextPart> exprList)
            {
                // If the expression is a Tree, recursively flatten.
                if (exprContext.Left is BinaryOperatorExpression op && op.IsDotExpression())
                    Flatten(script, op, exprList);
                // Otherwise, add the expression to the list.
                else
                {
                    // Get the function.
                    if (exprContext.Left is FunctionExpression method)
                        exprList.Add(new FunctionPart(method));
                    // Get the variable.
                    else if (exprContext.Left is Identifier variable)
                        exprList.Add(new VariableOrTypePart(variable));
                    // Get the expression.
                    else exprList.Add(new ExpressionPart(exprContext.Left));
                }

                // Get the expression to the right of the dot.

                // If the expression is a Tree, recursively flatten.
                if (exprContext.Right is BinaryOperatorExpression rop && rop.IsDotExpression())
                    Flatten(script, rop, exprList);
                // Otherwise, add the expression to the list.
                else
                {
                    // Get the method.
                    if (exprContext.Right is FunctionExpression rightMethod)
                        exprList.Add(new FunctionPart(rightMethod));
                    // Get the variable.
                    else if (exprContext.Right is Identifier rightVariable && rightVariable.Token)
                        exprList.Add(new VariableOrTypePart(rightVariable));
                    // Missing function or variable, set the _trailingSeperator.
                    else
                        _trailingSeperator = exprContext.Operator.Token;
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
                        else if (_trailingSeperator != null && !script.IsTokenLast(_trailingSeperator))
                        {
                            range = new DocRange(
                                _trailingSeperator.Range.End,
                                script.NextToken(_trailingSeperator).Range.Start
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

        public CodeType Type() => Result?.Type() ?? _parseInfo.TranslateInfo.Types.Unknown();

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
            IGettable currentObjectReference = null;
            IWorkshopTree target = null; // The resulting player.
            IWorkshopTree previousTarget = null;
            IWorkshopTree result = null; // The resulting value.
            VarIndexAssigner currentAssigner = actionSet.IndexAssigner;
            IWorkshopTree currentObject = null;
            Element[] resultIndex = new Element[0];
            List<IWorkshopTree> resultingSources = new List<IWorkshopTree>();

            for (int i = 0; i < Tree.Length; i++)
            {
                bool isLast = i == Tree.Length - 1;
                IWorkshopTree current = null;
                if (Tree[i] is CallVariableAction callVariableAction)
                {
                    // Get the reference.
                    var reference = currentAssigner.Get(callVariableAction.Calling.Provider);
                    current = reference.GetVariable((Element)target);

                    // Get the index.
                    resultIndex = new Element[callVariableAction.Index.Length];
                    for (int ai = 0; ai < callVariableAction.Index.Length; ai++)
                    {
                        var workshopIndex = callVariableAction.Index[ai].Parse(actionSet);
                        resultIndex[ai] = (Element)workshopIndex;
                        current = Element.ValueInArray(current, workshopIndex);
                    }

                    // Set the resulting variable.
                    resultingVariable = reference;
                    currentObjectReference = reference;
                }
                else
                {
                    var newCurrent = Tree[i].Parse(actionSet.New(currentAssigner).New(currentObject).New(currentObjectReference, previousTarget as Element));
                    if (newCurrent != null)
                    {
                        current = newCurrent;
                        resultIndex = new Element[0];
                    }
                }

                currentObject = current;
                if (Tree[i].Type() != null)
                {
                    var type = Tree[i].Type();
                    currentAssigner = actionSet.IndexAssigner.CreateContained();
                    type.AddObjectVariablesToAssigner(currentObject, currentAssigner);
                }

                // If this isn't the last in the tree, set it as the target.
                if (!isLast)
                {
                    previousTarget = target;
                    target = current;
                }

                result = current;
                resultingSources.Add(result);
            }

            if (result == null && expectingValue) throw new Exception("Expression tree result is null");
            return new ExpressionTreeParseResult(result, resultIndex, target, resultingVariable);
        }

        public bool IsStatement() => _trailingSeperator || (Result?.IsStatement() ?? true);
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
        void RetrievedScopeable(IScopeable scopeable) { }
        IExpression GetExpression();
        DocRange GetRange();
    }

    /// <summary>Expressions in the tree.</summary>
    class ExpressionPart : ITreeContextPart
    {
        private readonly IParseExpression _expressionContext;
        private IExpression _expression;

        public ExpressionPart(IParseExpression expression)
        {
            _expressionContext = expression;
        }

        public void Setup(TreeContextParseInfo tcParseInfo)
        {
            _expression = tcParseInfo.ParseInfo.GetExpression(tcParseInfo.Scope, _expressionContext);
        }

        public Scope GetScope() => _expression.ReturningScope();
        public IExpression GetExpression() => _expression;
        public DocRange GetRange() => _expressionContext.Range;
    }

    /// <summary>Functions in the expression tree.</summary>
    class FunctionPart : ITreeContextPart
    {
        private readonly FunctionExpression _methodContext;
        private CallMethodAction _methodCall;

        public FunctionPart(FunctionExpression method)
        {
            _methodContext = method;
        }

        public void Setup(TreeContextParseInfo tcParseInfo)
        {
            _methodCall = new CallMethodAction(tcParseInfo.ParseInfo, tcParseInfo.Scope, _methodContext, tcParseInfo.UsedAsExpression, tcParseInfo.Getter);
            tcParseInfo.Parent?.RetrievedScopeable(_methodCall.CallingMethod);
        }

        public Scope GetScope() => _methodCall.ReturningScope();
        public IExpression GetExpression() => _methodCall;
        public DocRange GetRange() => _methodContext.Target.Range;
    }

    /// <summary>Variables or types in the expression tree.</summary>
    class VariableOrTypePart : ITreeContextPart
    {
        private readonly Identifier _variable;
        private readonly DocRange _range;
        private readonly string _name;
        private bool _canBeType;
        private TreeContextParseInfo _tcParseInfo;
        private List<IPotentialPathOption> _potentialPaths;
        private IPotentialPathOption _chosenPath;

        public VariableOrTypePart(Identifier variable)
        {
            _variable = variable;
            _range = _variable.Token.Range;
            _name = variable.Token.Text;
        }

        public void Setup(TreeContextParseInfo tcParseInfo)
        {
            _canBeType = (_variable.Index == null || _variable.Index.Count == 0) && tcParseInfo.Parent == null;
            _tcParseInfo = tcParseInfo;
            _potentialPaths = GetPotentialPaths(tcParseInfo);

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
                NoPathsError();
            }
        }

        void NoPathsError()
        {
            // May resolve to type or variable.
            if (_canBeType)
                _tcParseInfo.ParseInfo.Script.Diagnostics.Error($"No variable or type by the name of '{_name}' exists in the current scope.", _range);
            // May resolve to only variable.
            else
                _tcParseInfo.ParseInfo.Script.Diagnostics.Error($"No variable by the name of '{_name}' exists in the {_tcParseInfo.Scope.ErrorName}.", _range);
        }

        private List<IPotentialPathOption> GetPotentialPaths(TreeContextParseInfo tcParseInfo)
        {
            List<IPotentialPathOption> potentialPaths = new List<IPotentialPathOption>();

            // Get the potential variable.
            if (tcParseInfo.Scope.IsVariable(_name))
            {
                IVariableInstance[] variables = tcParseInfo.Scope.GetAllVariables(_name);
                foreach (var variable in variables)
                {
                    // Variable handler.
                    var apply = new VariableApply(tcParseInfo.ParseInfo, tcParseInfo.Getter, variable, _variable);

                    // Check accessor.
                    bool accessorMatches = tcParseInfo.Getter.AccessorMatches(tcParseInfo.Scope, variable.AccessLevel);

                    // Add the potential path.
                    potentialPaths.Add(new VariableOption(tcParseInfo.Parent, apply, tcParseInfo.ParseInfo, accessorMatches));
                }
            }

            // Get the potential type.
            // Currently, OSTW does not support nested types, so make sure there is no parent.
            if (_canBeType)
            {
                var typeErrorHandler = new DefaultTypeContextError(tcParseInfo.ParseInfo, _variable, false);
                var type = TypeFromContext.GetCodeTypeFromContext(typeErrorHandler, tcParseInfo.ParseInfo, tcParseInfo.Scope, _variable);
                // If the type exists, add it to potentialPaths.
                if (typeErrorHandler.Exists)
                    potentialPaths.Add(new TypeOption(typeErrorHandler, type, tcParseInfo.ParseInfo, _range));
            }
            
            return potentialPaths;
        }

        public void RetrievedScopeable(IScopeable scopeable)
        {
            if (scopeable == null) return;

            // Narrow down the potential paths.
            for (int i = _potentialPaths.Count - 1; i >= 0; i--)
                if (!_potentialPaths[i].GetScope().ScopeContains(scopeable))
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
            Scope scopeBatch = new Scope(name) { TagPlayerVariables = true };

            // Add all potential path's scopes to the scope batch.
            foreach (var path in _potentialPaths)
                scopeBatch.CopyAll(path.GetScope());

            // Finished.
            return scopeBatch;
        }
        public IExpression GetExpression() => _chosenPath?.GetExpression();
        public DocRange GetRange() => _range;

        void CheckAmbiguitiesAndAccept()
        {
            CheckAmbiguities();
            
            if (_potentialPaths.Count == 0)
            {
                NoPathsError();
            }
            else
            {
                _chosenPath = _potentialPaths[0];
                _chosenPath.Accept();
                CallResolvers();
            }
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

        interface IPotentialPathOption
        {
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
            private readonly ITypeContextError _errorHandler;
            
            public TypeOption(ITypeContextError typeErrorHandler, CodeType type, ParseInfo parseInfo, DocRange callRange) {
                _type = type;
                _parseInfo = parseInfo;
                _callRange = callRange;
                _errorHandler = typeErrorHandler;
            }

            public Scope GetScope() => _type.ReturningScope();
            public IExpression GetExpression() => _type;
            public bool IsAmbiguousTo(IPotentialPathOption other) => other is TypeOption;
            public void Accept()
            {
                _type.Call(_parseInfo, _callRange);
                _errorHandler.ApplyErrors();
            }
        }
        class VariableOption : IPotentialPathOption
        {
            private readonly ITreeContextPart _parent;
            private readonly VariableApply _apply;
            private readonly IVariableInstance _variable;
            private readonly ParseInfo _parseInfo;
            private readonly DocRange _callRange;
            private readonly bool _accessorMatches;
            private readonly IExpression _expression;

            public VariableOption(ITreeContextPart parent, VariableApply apply, ParseInfo parseInfo, bool accessorMatches) {
                _parent = parent;
                _apply = apply;
                _variable = apply.Variable;
                _parseInfo = parseInfo;
                _callRange = apply.CallRange;
                _accessorMatches = accessorMatches;
                _expression = apply.VariableCall;
            }

            public Scope GetScope() => _expression.Type()?.GetObjectScope() ?? _parseInfo.TranslateInfo.PlayerVariableScope;
            public IExpression GetExpression() => _expression;
            public void Accept()
            {
                _apply.Accept();

                // Check accessor.
                if (!_accessorMatches)
                    _parseInfo.Script.Diagnostics.Error(string.Format("'{0}' is inaccessable due to its access level.", _variable.Name), _callRange);

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

    public class UsageResolver
    {
        public bool WasResolved { get; private set; }
        private readonly List<Action<UsageType>> _onResolve = new List<Action<UsageType>>();

        public void ResolveUnknownIfNotResolved() => Resolve(UsageType.Unknown);

        public void OnResolve(Action<UsageType> action) => _onResolve.Add(action);

        public void Resolve(UsageType usageType)
        {
            if (WasResolved) return;
            WasResolved = true;
            foreach (var action in _onResolve)
                action(usageType);
        }
    }

    public enum UsageType
    {
        Unknown,
        StringFormat
    }
}