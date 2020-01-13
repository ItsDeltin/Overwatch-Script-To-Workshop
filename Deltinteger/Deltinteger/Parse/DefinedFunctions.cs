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
    public abstract class DefinedFunction : IMethod, ICallable, IApplyBlock
    {
        public string Name { get; }
        public CodeType ReturnType { get; protected set; }
        public CodeParameter[] Parameters { get; private set; }
        public AccessLevel AccessLevel { get; protected set; }
        public Location DefinedAt { get; }
        public bool WholeContext { get; } = true;
        public StringOrMarkupContent Documentation { get; } = null;

        protected ParseInfo parseInfo { get; }
        protected Scope methodScope { get; }
        protected Var[] ParameterVars { get; private set; }

        public CallInfo CallInfo { get; }

        public DefinedFunction(ParseInfo parseInfo, Scope scope, string name, Location definedAt)
        {
            Name = name;
            DefinedAt = definedAt;
            this.parseInfo = parseInfo;
            methodScope = scope.Child();
            CallInfo = new CallInfo(this, parseInfo.Script);

            parseInfo.TranslateInfo.AddSymbolLink(this, definedAt);
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.Function, DefinedAt.range));
        }

        public virtual void SetupBlock() {}

        protected static CodeType GetCodeType(ParseInfo parseInfo, string name, DocRange range)
        {
            if (name == null)
                return null;
            else
                return parseInfo.TranslateInfo.GetCodeType(name, parseInfo.Script.Diagnostics, range);
        }

        protected void SetupParameters(DeltinScriptParser.SetParametersContext context)
        {
            var parameterInfo = CodeParameter.GetParameters(parseInfo, methodScope, context);
            Parameters = parameterInfo.Parameters;
            ParameterVars = parameterInfo.Variables;
        }

        public void Call(ScriptFile script, DocRange callRange)
        {
            script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.TranslateInfo.AddSymbolLink(this, new Location(script.Uri, callRange));
        }

        public virtual bool DoesReturnValue() => true;

        public string GetLabel(bool markdown) => HoverHandler.GetLabel(ReturnType, Name, Parameters, markdown, null);

        public abstract IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData);

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
        private DeltinScriptParser.Define_methodContext context;
        public bool IsRecursive { get; }
        private BlockAction block { get; set; }
        private bool doesReturnValue { get; set; }
        /// <summary>
        /// If there is only one return statement, return the reference to
        /// the return expression instead of assigning it to a variable to reduce the number of actions.
        /// </summary>
        /// <value></value>
        private bool multiplePaths { get; set; }

        public DefinedMethod(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Define_methodContext context)
            : base(parseInfo, scope, context.name.Text, new Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)))
        {
            this.context = context;

            // Check if recursion is enabled.
            IsRecursive = context.RECURSIVE() != null;

            // Get the type.
            ReturnType = CodeType.GetCodeTypeFromContext(parseInfo, context.code_type());

            // Get the access level.
            AccessLevel = context.accessor().GetAccessLevel();

            // Setup the parameters and parse the block.
            SetupParameters(context.setParameters());

            if (context.block() == null)
                parseInfo.Script.Diagnostics.Error("Expected block.", DocRange.GetRange(context.name));

            scope.AddMethod(this, parseInfo.Script.Diagnostics, DocRange.GetRange(context.name));

            // Add the hover info.
            parseInfo.Script.AddHover(DocRange.GetRange(context.name), GetLabel(true));
        }

        public override void SetupBlock()
        {
            if (context.block() != null)
            {
                block = new BlockAction(parseInfo.SetCallInfo(CallInfo), methodScope, context.block());
                ValidateReturns(parseInfo.Script, context);
            }
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

        override public bool DoesReturnValue() => doesReturnValue;

        override public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            actionSet = actionSet
                .New(actionSet.IndexAssigner.CreateContained());
            
            if (IsRecursive) return ParseRecursive(actionSet, parameterValues);
            
            ReturnHandler returnHandler = new ReturnHandler(actionSet, Name, multiplePaths);
            actionSet = actionSet.New(returnHandler);
            
            AssignParameters(actionSet, ParameterVars, parameterValues);
            block.Translate(actionSet);

            returnHandler.ApplyReturnSkips();
            return returnHandler.GetReturnedValue();
        }

        private IWorkshopTree ParseRecursive(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            // Check the method stack to see if this method was already called.
            RecursiveMethodStack lastCall = actionSet.Translate.MethodStack.FirstOrDefault(ms => ms.Function == this) as RecursiveMethodStack;

            // If not, set up the stack and call the method.
            if (lastCall == null)
            {
                // Assign the parameters.
                AssignParameters(actionSet, ParameterVars, parameterValues, true);

                // Get the return handler if a value is returned.
                ReturnHandler returnHandler = new ReturnHandler(actionSet, Name, true);

                // Set up the condinue skip array.
                IndexReference continueSkipArray = actionSet.VarCollection.Assign("recursiveContinueArray", actionSet.IsGlobal, false);

                SkipEndMarker methodStart = new SkipEndMarker();
                actionSet.AddAction(methodStart);

                // Add the method to the stack.
                var stack = new RecursiveMethodStack(this, returnHandler, continueSkipArray, methodStart);
                actionSet.Translate.MethodStack.Add(stack);

                // Parse the method block.
                block.Translate(actionSet.New(returnHandler));

                // Apply the returns.
                if (returnHandler != null)
                    returnHandler.ApplyReturnSkips();
                
                // Pop the recursive parameters
                // TODO: Make this work with all sub scoped variables somehow
                for (int i = 0; i < ParameterVars.Length; i++)
                {
                    var pop = (actionSet.IndexAssigner[ParameterVars[i]] as RecursiveIndexReference)?.Pop();
                    if (pop != null) actionSet.AddAction(pop);
                }

                // Setup the continue skip
                actionSet.ContinueSkip.Setup(actionSet);
                actionSet.ContinueSkip.SetSkipCount(actionSet, Element.Part<V_LastOf>(continueSkipArray.GetVariable()));

                // Remove the last recursive continue skip.
                actionSet.AddAction(continueSkipArray.SetVariable(
                    // Pop
                    Element.Part<V_ArraySlice>(
                        continueSkipArray.GetVariable(), 
                        new V_Number(0),
                        Element.Part<V_CountOf>(continueSkipArray.GetVariable()) - 1
                    )
                ));

                // Loop if there are any values in the continue skip array.
                actionSet.AddAction(Element.Part<A_LoopIf>(
                    Element.Part<V_CountOf>(continueSkipArray.GetVariable()) > 0
                ));

                // Reset the continue skip.
                actionSet.ContinueSkip.ResetSkipCount(actionSet);
                actionSet.AddAction(continueSkipArray.SetVariable(0));

                // Remove the method from the stack.
                actionSet.Translate.MethodStack.Remove(stack);

                return returnHandler.GetReturnedValue();
            }
            // If it is, push the parameters to the stack.
            else
            {
                for (int i = 0; i < ParameterVars.Length; i++)
                {
                    var varReference = actionSet.IndexAssigner[ParameterVars[i]];
                    if (varReference is RecursiveIndexReference)
                    {
                        actionSet.AddAction(((RecursiveIndexReference)varReference).Push(
                            (Element)parameterValues[i]
                        ));
                    }
                }

                // Add to the continue skip array.
                V_Number skipLength = new V_Number();
                actionSet.AddAction(lastCall.ContinueSkipArray.SetVariable(
                    Element.Part<V_Append>(lastCall.ContinueSkipArray.GetVariable(), skipLength)
                ));

                actionSet.ContinueSkip.Setup(actionSet);
                actionSet.ContinueSkip.SetSkipCount(actionSet, lastCall.MethodStart);
                actionSet.AddAction(new A_Loop());

                SkipEndMarker continueAt = new SkipEndMarker();
                actionSet.AddAction(continueAt);
                skipLength.Value = actionSet.ContinueSkip.GetSkipCount(continueAt).Value;

                return lastCall.ReturnHandler.GetReturnedValue();
            }
        }

        public static void AssignParameters(ActionSet actionSet, Var[] parameterVars, IWorkshopTree[] parameterValues, bool recursive = false)
        {
            for (int i = 0; i < parameterVars.Length; i++)
            {
                actionSet.IndexAssigner.Add(actionSet.VarCollection, parameterVars[i], actionSet.IsGlobal, parameterValues[i], recursive);

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
        private DeltinScriptParser.ExprContext ExpressionToParse { get; }

        public DefinedMacro(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Define_macroContext context)
            : base(parseInfo, scope, context.name.Text, new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)))
        {
            AccessLevel = context.accessor().GetAccessLevel();
            SetupParameters(context.setParameters());

            if (context.TERNARY_ELSE() == null)
            {
                parseInfo.Script.Diagnostics.Error("Expected :", DocRange.GetRange(context).end.ToRange());
            }
            else
            {
                ExpressionToParse = context.expr();
                if (ExpressionToParse == null)
                    parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(context.TERNARY_ELSE()));
            }

            parseInfo.Script.AddHover(DocRange.GetRange(context.name), GetLabel(true));
        }

        override public void SetupBlock()
        {
            if (ExpressionToParse == null) return;
            Expression = DeltinScript.GetExpression(parseInfo.SetCallInfo(CallInfo), methodScope, ExpressionToParse);
            ReturnType = Expression?.Type();
        }

        override public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues, object[] additionalParameterData)
        {
            // Assign the parameters.
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());
            for (int i = 0; i < ParameterVars.Length; i++)
                actionSet.IndexAssigner.Add(ParameterVars[i], parameterValues[i]);

            // Parse the expression.
            return Expression.Parse(actionSet);
        }
    }

    public class MethodStack
    {
        public IApplyBlock Function { get; }

        public MethodStack(IApplyBlock function)
        {
            Function = function;
        }
    }

    public class RecursiveMethodStack : MethodStack
    {
        public ReturnHandler ReturnHandler { get; }
        public IndexReference ContinueSkipArray { get; }
        public SkipEndMarker MethodStart { get; }

        public RecursiveMethodStack(DefinedMethod method, ReturnHandler returnHandler, IndexReference continueSkipArray, SkipEndMarker methodStart) : base(method)
        {
            ReturnHandler = returnHandler;
            ContinueSkipArray = continueSkipArray;
            MethodStart = methodStart;
        }
    }

    public class MacroVar : IScopeable, IExpression, ICallable, IApplyBlock
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; }
        public LanguageServer.Location DefinedAt { get; }
        public bool WholeContext => true;

        public IExpression Expression { get; private set; }
        public CodeType ReturnType { get; private set; }

        private DeltinScriptParser.ExprContext ExpressionToParse { get; }
        private Scope scope { get; }
        private ParseInfo parseInfo { get; }

        public CallInfo CallInfo { get; }

        public MacroVar(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Define_macroContext macroContext)
        {
            Name = macroContext.name.Text;
            AccessLevel = macroContext.accessor()?.GetAccessLevel() ?? AccessLevel.Private;
            DefinedAt = new Location(parseInfo.Script.Uri, DocRange.GetRange(macroContext.name));
            CallInfo = new CallInfo(this, parseInfo.Script);

            if (macroContext.TERNARY_ELSE() == null)
            {
                parseInfo.Script.Diagnostics.Error("Expected :", DocRange.GetRange(macroContext).end.ToRange());
            }
            else
            {
                ExpressionToParse = macroContext.expr();
                if (ExpressionToParse == null)
                    parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(macroContext.TERNARY_ELSE()));
            }

            this.scope = scope;
            this.parseInfo = parseInfo;
        }

        public void SetupBlock()
        {
            if (ExpressionToParse == null) return;
            Expression = DeltinScript.GetExpression(parseInfo.SetCallInfo(CallInfo), scope, ExpressionToParse);
            ReturnType = Expression?.Type();
        }

        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true) => Expression.Parse(actionSet);

        public Scope ReturningScope() => ReturnType?.GetObjectScope();

        public CodeType Type() => ReturnType;

        public void Call(ScriptFile script, DocRange callRange)
        {
            script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.TranslateInfo.AddSymbolLink(this, new Location(script.Uri, callRange));
        }

        public CompletionItem GetCompletion()
        {
            return new CompletionItem() {
                Label = Name,
                Kind = CompletionItemKind.Property
            };
        }

        public string GetLabel(bool markdown) => Name;
    }
}