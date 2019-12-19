using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.WorkshopWiki;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;
using StringOrMarkupContent = OmniSharp.Extensions.LanguageServer.Protocol.Models.StringOrMarkupContent;

namespace Deltin.Deltinteger.Parse
{
    public abstract class DefinedFunction : IMethod, ICallable
    {
        public string Name { get; }
        public CodeType ReturnType { get; protected set; }
        public CodeParameter[] Parameters { get; private set; }
        public AccessLevel AccessLevel { get; protected set; }
        public Location DefinedAt { get; }
        public bool WholeContext { get; } = true;
        public StringOrMarkupContent Documentation { get; } = null;

        private DeltinScript translateInfo { get; }
        protected Scope methodScope { get; }
        protected Var[] ParameterVars { get; private set; }
        
        public DefinedFunction(DeltinScript translateInfo, Scope scope, string name, Location definedAt)
        {
            Name = name;
            DefinedAt = definedAt;
            this.translateInfo = translateInfo;
            methodScope = scope.Child();
            translateInfo.AddSymbolLink(this, definedAt);
        }

        protected static CodeType GetCodeType(ScriptFile script, DeltinScript translateInfo, string name, DocRange range)
        {
            if (name == null)
                return null;
            else
                return translateInfo.GetCodeType(name, script.Diagnostics, range);
        }

        protected void SetupParameters(ScriptFile script, DeltinScriptParser.SetParametersContext context)
        {
            var parameterInfo = CodeParameter.GetParameters(script, translateInfo, methodScope, context);
            Parameters = parameterInfo.Parameters;
            ParameterVars = parameterInfo.Variables;
        }

        public void Call(ScriptFile script, DocRange callRange)
        {
            script.AddDefinitionLink(callRange, DefinedAt);
            translateInfo.AddSymbolLink(this, new Location(script.Uri, callRange));
        }

        public string GetLabel(bool markdown) => HoverHandler.GetLabel(Name, Parameters, markdown, null);

        public abstract IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues);

        public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = Name,
                Kind = CompletionItemKind.Method
            };
        }
    }

    public class DefinedMethod : DefinedFunction
    {
        public bool IsRecursive { get; }
        private BlockAction block { get; }
        private bool doesReturnValue { get; set; }
        /// <summary>
        /// If there is only one return statement, return the reference to
        /// the return expression instead of assigning it to a variable to reduce the number of actions.
        /// </summary>
        /// <value></value>
        private bool multiplePaths { get; set; }

        public DefinedMethod(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Define_methodContext context)
            : base(translateInfo, scope, context.name.Text, new Location(script.Uri, DocRange.GetRange(context.name)))
        {
            // Check if recursion is enabled.
            IsRecursive = context.RECURSIVE() != null;

            // Get the type.
            if (context.type != null)
                ReturnType = GetCodeType(script, translateInfo, context.type.Text, DocRange.GetRange(context.type));

            // Get the access level.
            AccessLevel = context.accessor().GetAccessLevel();

            // Setup the parameters and parse the block.
            SetupParameters(script, context.setParameters());
            block = new BlockAction(script, translateInfo, methodScope, context.block());

            // Add the hover info.
            script.AddHover(DocRange.GetRange(context.name), GetLabel(true));

            ValidateReturns(script, context);
        }

        private void ValidateReturns(ScriptFile script, DeltinScriptParser.Define_methodContext context)
        {
            ReturnAction[] returns = GetReturns();
            if (returns.Any(ret => ret.ReturningValue != null))
            {
                doesReturnValue = true;

                // If there is only one return statement, return the reference to
                // the return statement to reduce the number of actions.
                multiplePaths = returns.Length > 1;

                // Syntax error if there are any paths that don't return a value.
                CheckPath(script, new PathInfo(block, DocRange.GetRange(context.name), true));

                // If one return statement returns a value, the rest must as well.
                foreach (var ret in returns)
                    if (ret.ReturningValue == null)
                        script.Diagnostics.Error("Must return a value.", ret.ErrorRange);
            }
        }

        private static bool HasReturnStatement(BlockAction block)
        {
            foreach (var statement in block.Statements)
                if (statement is ReturnAction) return true;
                else if (statement is IBlockContainer)
                    foreach (var path in ((IBlockContainer)statement).GetPaths())
                        if (HasReturnStatement(path.Block))
                            return true;
            return false;
        }

        private ReturnAction[] GetReturns()
        {
            List<ReturnAction> returns = new List<ReturnAction>();
            getReturns(returns, block);
            return returns.ToArray();

            void getReturns(List<ReturnAction> actions, BlockAction block)
            {
                foreach (var statement in block.Statements)
                if (statement is ReturnAction) actions.Add((ReturnAction)statement);
                else if (statement is IBlockContainer)
                    foreach (var path in ((IBlockContainer)statement).GetPaths())
                        getReturns(actions, path.Block);
            }
        }

        private static void CheckPath(ScriptFile script, PathInfo path)
        {
            bool blockReturns = false;
            // Check the statements backwards.
            for (int i = path.Block.Statements.Length - 1; i >= 0; i--)
            {
                if (path.Block.Statements[i] is ReturnAction)
                {
                    blockReturns = true;
                    break;
                }
                
                if (path.Block.Statements[i] is IBlockContainer)
                {
                    // If any of the paths in the block container has WillRun set to true,
                    // set blockReturns to true. The responsibility of checking if this
                    // block will run is given to the block container.
                    if (((IBlockContainer)path.Block.Statements[i]).GetPaths().Any(containerPath => containerPath.WillRun))
                        blockReturns = true;

                    CheckContainer(script, (IBlockContainer)path.Block.Statements[i]);
                }
            }
            if (!blockReturns)
                script.Diagnostics.Error("Path does not return a value.", path.ErrorRange);
        }
        private static void CheckContainer(ScriptFile script, IBlockContainer container)
        {
            foreach (var path in container.GetPaths()) CheckPath(script, path);
        }

        override public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            actionSet = actionSet
                .New(actionSet.IndexAssigner.CreateContained());
            
            ReturnHandler returnHandler = null;
            if (doesReturnValue)
            {
                returnHandler = new ReturnHandler(actionSet, Name, multiplePaths);
                actionSet = actionSet.New(returnHandler);
            }
            
            AssignParameters(actionSet, ParameterVars, parameterValues);
            block.Translate(actionSet);

            returnHandler.ApplyReturnSkips();

            if (returnHandler == null) return null;
            else return returnHandler.GetReturnedValue();
        }

        public static void AssignParameters(ActionSet actionSet, Var[] parameterVars, IWorkshopTree[] parameterValues)
        {
            for (int i = 0; i < parameterVars.Length; i++)
            {
                actionSet.IndexAssigner.Add(actionSet.VarCollection, parameterVars[i], actionSet.IsGlobal, parameterValues[i]);

                // todo: improve this
                if (actionSet.IndexAssigner[parameterVars[i]] is IndexReference)
                    actionSet.AddAction(
                        ((IndexReference)actionSet.IndexAssigner[parameterVars[i]]).SetVariable((Element)parameterValues[i])
                    );
            }
        }
    }

    public class ReturnHandler
    {
        private readonly ActionSet ActionSet;
        private readonly bool MultiplePaths;

        // If `MultiplePaths` is true, use `ReturnStore`. Else use `ReturningValue`.
        private readonly IndexReference ReturnStore;
        private IWorkshopTree ReturningValue;

        private bool ValueWasReturned;

        private readonly List<SkipStartMarker> skips = new List<SkipStartMarker>();

        public ReturnHandler(ActionSet actionSet, string methodName, bool multiplePaths)
        {
            ActionSet = actionSet;
            MultiplePaths = multiplePaths;

            if (multiplePaths)
                ReturnStore = actionSet.VarCollection.Assign("_" + methodName + "ReturnValue", actionSet.IsGlobal, true);
        }

        public void ReturnValue(IWorkshopTree value)
        {
            if (!MultiplePaths && ValueWasReturned)
                throw new Exception("_multiplePaths is set as false and 2 expressions were returned.");
            ValueWasReturned = true;

            // Multiple return paths.
            if (MultiplePaths)
                ActionSet.AddAction(ReturnStore.SetVariable((Element)value));
            // One return path.
            else
                ReturningValue = value;
        }

        public void Return()
        {
            SkipStartMarker returnSkipStart = new SkipStartMarker(ActionSet);
            ActionSet.AddAction(returnSkipStart);

            // 0 skip workaround.
            ActionSet.AddAction(new A_Abort() { Disabled = true });

            skips.Add(returnSkipStart);
        }

        public void ApplyReturnSkips()
        {
            if (!MultiplePaths) return;

            SkipEndMarker methodEndMarker = new SkipEndMarker();
            ActionSet.AddAction(methodEndMarker);

            foreach (var returnSkip in skips)
                returnSkip.SkipCount = returnSkip.GetSkipCount(methodEndMarker);
        }

        public IWorkshopTree GetReturnedValue()
        {
            if (MultiplePaths)
                return ReturnStore.GetVariable();
            else
                return ReturningValue;
        }
    }

    public class DefinedMacro : DefinedFunction
    {
        public IExpression Expression { get; private set; }

        public DefinedMacro(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Define_macroContext context)
            : base(translateInfo, scope, context.name.Text, new Location(script.Uri, DocRange.GetRange(context)))
        {
            AccessLevel = context.accessor().GetAccessLevel();
            SetupParameters(script, context.setParameters());

            if (context.expr() == null)
                script.Diagnostics.Error("Expected expression.", DocRange.GetRange(context.TERNARY_ELSE()));
            else
            {
                Expression = DeltinScript.GetExpression(script, translateInfo, methodScope, context.expr());
                if (Expression != null)
                    ReturnType = Expression.Type();
            }

            script.AddHover(DocRange.GetRange(context.name), GetLabel(true));
        }

        override public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            // TODO: fix this
            return Expression.Parse(actionSet);
            // throw new NotImplementedException();
        }
    }
}