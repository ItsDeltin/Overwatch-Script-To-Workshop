using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse.FunctionBuilder
{
    /*
    Function order

    -   set or push parameters
    - (recursive object) push this
    - (virtual) virtual table
    -   parse function
    - (recursive object) pop this
    - (recursive) pop parameters

    Subroutine order

    #CALLER
    -   set or push parameters
    - (object) set or push this

    #ACTIONS
    - (virtual) virtual table
    -   parse function
    - (recursive) pop this
    - (recursive) pop parameters
    */
            
    public class FunctionBuildController
    {
        public ActionSet ActionSet { get; private set; }
        public IGroupDeterminer Determiner { get; }
        public ICallHandler CallHandler { get; }
        public ReturnHandler ReturnHandler { get; private set; }
        private readonly IParameterHandler[] _parameters;

        public FunctionBuildController(ActionSet actionSet, ICallHandler callHandler, IGroupDeterminer determiner)
        {
            ActionSet = actionSet;
            Determiner = determiner;
            CallHandler = callHandler;
            _parameters = determiner.Parameters();
        }

        public IWorkshopTree Call()
        {
            // Call subroutine.
            if (Determiner.IsSubroutine())
            {
                var subroutineInfo = Determiner.GetSubroutineInfo();
                SetSubroutineParameters(subroutineInfo);

                // Store the object the subroutine is executing with.
                if (Determiner.IsObject())
                {
                    // Normal
                    if (!Determiner.IsRecursive())
                        ActionSet.AddAction(subroutineInfo.ObjectStore.SetVariable((Element)ActionSet.CurrentObject));
                    // Recursive: Stack
                    else
                        ActionSet.AddAction(subroutineInfo.ObjectStore.ModifyVariable(Operation.AppendToArray, Element.CreateArray(ActionSet.CurrentObject)));
                }
                
                ExecuteSubroutine(subroutineInfo.Subroutine);
                return subroutineInfo.ReturnHandler.GetReturnedValue();
            }
            // Inline function
            else
            {
                // Recursive stack.
                if (Determiner.IsRecursive())
                {
                    var lastCall = Determiner.GetExistingRecursiveStack(ActionSet.Translate.MethodStack);

                    // Function is not yet on the stack.
                    if (lastCall == null)
                        return Build();
                    else // Recursive call.
                    {
                        lastCall.RecursiveCall(CallHandler, ActionSet);
                        return ActionSet.ReturnHandler.GetReturnedValue();
                    }
                }
                else
                    return Build();
            }
        }

        public IWorkshopTree Build()
        {
            // Setup inline-recursive handler.
            RecursiveStack stack = null;
            if (!Determiner.IsSubroutine() && Determiner.IsRecursive())
            {
                stack = new RecursiveStack(this, Determiner.GetStackIdentifier());
                stack.Init();
                ActionSet.Translate.MethodStack.Add(stack);
            }

            ModifySet(a => a.PackThis());
            SetupReturnHandler(); // Setup the return handler.
            SetParameters(); // Set the parameters.
            stack?.StartRecursiveLoop(); // Start the recursion loop.
            ParseContents(); // Parse the lookup table.
            stack?.EndRecursiveLoop(); // End the recursive loop.

            if (stack != null) ActionSet.Translate.MethodStack.Remove(stack); // Remove recursion info from the stack.

            return ReturnHandler.GetReturnedValue();
        }

        void SetupReturnHandler()
        {
            ReturnHandler = new ReturnHandler(ActionSet, Determiner.GroupName(), Determiner.MultiplePaths());
            ModifySet(a => a.New(ReturnHandler));
        }

        void SetParameters()
        {
            if (!Determiner.IsSubroutine())
                for (int i = 0; i < _parameters.Length; i++)
                    _parameters[i].Set(ActionSet, CallHandler.ParameterValues[i]);
        }

        void SetSubroutineParameters(SubroutineInfo subroutine)
        {
            for (int i = 0; i < _parameters.Length && i < CallHandler.ParameterValues.Length; i++)
                _parameters[i].SetSubroutine(ActionSet, subroutine.ParameterStores[i], CallHandler.ParameterValues[i]);
        }

        void ParseContents() => Determiner.GetLookupTable().Build(this);

        public void Subcall(ActionSet actionSet, IFunctionHandler function)
        {
            // We use a new actionSet rather than _actionSet in case the caller needs special adjustments.
            if (function.IsSubroutine())
            {
                ExecuteSubroutine(function, CallHandler.ParallelMode);
                // Bridge the returned value.
                if (Determiner.ReturnsValue()) ReturnHandler.ReturnValue(function.GetSubroutineInfo().ReturnHandler.GetReturnedValue());
            }
            else
                function.ParseInner(actionSet);
        }

        void ExecuteSubroutine(IFunctionHandler function, CallParallel executeOption = CallParallel.NoParallel) => ExecuteSubroutine(function.GetSubroutineInfo().Subroutine);
        void ExecuteSubroutine(Subroutine subroutine, CallParallel executeOption = CallParallel.NoParallel)
        {
            switch (executeOption)
            {
                case CallParallel.NoParallel:
                    ActionSet.AddAction(Element.Part<A_CallSubroutine>(subroutine));
                    break;
                
                case CallParallel.AlreadyRunning_DoNothing:
                    ActionSet.AddAction(Element.Part<A_StartRule>(subroutine, EnumData.GetEnumValue(CallParallel.AlreadyRunning_DoNothing)));
                    break;
                
                case CallParallel.AlreadyRunning_RestartRule:
                    ActionSet.AddAction(Element.Part<A_StartRule>(subroutine, EnumData.GetEnumValue(CallParallel.AlreadyRunning_RestartRule)));
                    break;
            }
        }

        public void PushParameters(ICallHandler callHandler)
        {
            for (int i = 0; i < _parameters.Length; i++)
                _parameters[i].Push(ActionSet, callHandler.ParameterValues[i]);
        }

        public void PopParameters()
        {
            for (int i = 0; i < _parameters.Length; i++)
                _parameters[i].Pop(ActionSet);
        }

        public void ModifySet(Func<ActionSet, ActionSet> modify) => ActionSet = modify(ActionSet);
    }

    public interface IGroupDeterminer
    {
        string GroupName();
        bool IsRecursive();
        bool IsObject();
        bool IsSubroutine();
        bool MultiplePaths();
        bool IsVirtual();
        bool ReturnsValue();
        IFunctionLookupTable GetLookupTable();
        SubroutineInfo GetSubroutineInfo();
        IParameterHandler[] Parameters();
        RecursiveStack GetExistingRecursiveStack(List<RecursiveStack> stack);
        object GetStackIdentifier();
    }

    public class DefaultGroupDeterminer : IGroupDeterminer
    {
        public IFunctionHandler[] VirtualOptions { get; }
        private IFunctionHandler _root => VirtualOptions[0];

        public DefaultGroupDeterminer(IFunctionHandler[] virtualOptions)
        {
            VirtualOptions = virtualOptions;
        }

        public string GroupName() => _root.GetName();
        public bool IsRecursive() => _root.IsRecursive();
        public bool IsObject() => _root.IsObject();
        public bool IsSubroutine() => _root.IsSubroutine();
        public bool IsVirtual() => VirtualOptions.Length > 1;
        public bool ReturnsValue() => _root.DoesReturnValue();
        public bool MultiplePaths() => _root.DoesReturnValue() && (_root.IsSubroutine() || _root.MultiplePaths() || IsVirtual());
        public IFunctionLookupTable GetLookupTable() => new VirtualLookupTable(VirtualOptions);
        public SubroutineInfo GetSubroutineInfo() => _root.GetSubroutineInfo();

        public IParameterHandler[] Parameters()
        {
            var parameters = new IParameterHandler[_root.ParameterCount()];
            for (int i = 0; i < parameters.Length; i++)
            {
                // Get all vars in each function.
                var vars = new IIndexReferencer[VirtualOptions.Length];
                for (int v = 0; v < vars.Length; v++) vars[v] = VirtualOptions[v].GetParameterVar(i);

                parameters[i] = new DefinedParameterHandler(vars, IsRecursive());
            }

            return parameters;
        }

        public RecursiveStack GetExistingRecursiveStack(List<RecursiveStack> stack)
        {
            foreach (var item in stack)
                if (item.Identifier == GetStackIdentifier())
                    return item;
            return null;
        }
        public object GetStackIdentifier() => _root.UniqueIdentifier();
    }

    public interface IFunctionHandler
    {
        CodeType ContainingType { get; }
        string GetName();
        bool IsRecursive();
        bool IsObject();
        bool IsSubroutine();
        bool MultiplePaths();
        bool DoesReturnValue();
        int ParameterCount();
        SubroutineInfo GetSubroutineInfo();
        IIndexReferencer GetParameterVar(int index);
        void ParseInner(ActionSet actionSet);
        object UniqueIdentifier();
    }

    public class DefinedFunctionHandler : IFunctionHandler
    {
        private readonly DefinedMethod _method;

        public DefinedFunctionHandler(DefinedMethod method)
        {
            _method = method;
        }

        public string GetName() => _method.Name;
        public bool IsObject() => _method.Attributes.ContainingType != null && !_method.Static;
        public bool IsRecursive() => _method.Attributes.Recursive;
        public bool IsSubroutine() => _method.IsSubroutine;
        public int ParameterCount() => _method.Parameters.Length;
        public bool MultiplePaths() => _method.MultiplePaths;
        public bool DoesReturnValue() => _method.DoesReturnValue;
        public SubroutineInfo GetSubroutineInfo() => _method.GetSubroutineInfo();
        public IIndexReferencer GetParameterVar(int index) => index < ParameterCount() ? _method.ParameterVars[index] : null;
        public void ParseInner(ActionSet actionSet) => _method.Block.Translate(actionSet);
        public object UniqueIdentifier() => _method;

        public CodeType ContainingType => _method.Attributes.ContainingType;
    }

    public interface IParameterHandler
    {
        void Set(ActionSet actionSet, IWorkshopTree value);
        void SetSubroutine(ActionSet actionSet, IndexReference parameterStore, IWorkshopTree value);
        void Push(ActionSet actionSet, IWorkshopTree value);
        void Pop(ActionSet actionSet);
        IndexReference GetSubroutineStack(ActionSet actionSet, bool defaultGlobal);
    }

    public class DefinedParameterHandler : IParameterHandler
    {
        private readonly IIndexReferencer[] _variables;
        private readonly bool _recursive;

        public DefinedParameterHandler(IIndexReferencer[] variables, bool recursive)
        {
            _variables = variables;
            _recursive = recursive;
        }

        public void Set(ActionSet actionSet, IWorkshopTree value)
        {
            IGettable indexResult = actionSet.IndexAssigner.Add(actionSet.VarCollection, (Var)_variables[0], actionSet.IsGlobal, value, _recursive);
            CopyToAll(actionSet, indexResult);

            if (indexResult is IndexReference indexReference && value != null)
                actionSet.AddAction(indexReference.SetVariable((Element)value));
        }

        public void SetSubroutine(ActionSet actionSet, IndexReference parameterStore, IWorkshopTree value)
        {
            actionSet.AddAction(parameterStore.SetVariable((Element)value));
        }

        public IndexReference GetSubroutineStack(ActionSet actionSet, bool defaultGlobal)
        {
            // Create the workshop variable the parameter will be stored as.
            IndexReference indexResult = actionSet.IndexAssigner.AddIndexReference(actionSet.VarCollection, (Var)_variables[0], defaultGlobal, _recursive);
            CopyToAll(actionSet, indexResult);
        
            return indexResult;
        }

        private void CopyToAll(ActionSet actionSet, IGettable gettable)
        {
            for (int i = 1; i < _variables.Length; i++)
                actionSet.IndexAssigner.Add(_variables[i], gettable);
        }

        public void Push(ActionSet actionSet, IWorkshopTree value)
        {
            var varReference = actionSet.IndexAssigner[_variables[0]];
            if (varReference is RecursiveIndexReference recursive)
                actionSet.AddAction(recursive.Push((Element)value));
        }

        public void Pop(ActionSet actionSet)
        {
            var pop = (actionSet.IndexAssigner[_variables[0]] as RecursiveIndexReference)?.Pop();
            if (pop != null) actionSet.AddAction(pop);
        }

        public static IParameterHandler[] GetDefinedParameters(int parameterCount, IFunctionHandler[] handlers, bool isRecursive)
        {
            var parameters = new IParameterHandler[parameterCount];
            for (int i = 0; i < parameters.Length; i++)
            {
                // Get all vars in each function.
                var vars = new List<IIndexReferencer>();
                for (int v = 0; v < handlers.Length; v++)
                {
                    var result = handlers[v].GetParameterVar(i);
                    if (result != null) vars.Add(result);
                }

                parameters[i] = new DefinedParameterHandler(vars.ToArray(), isRecursive);
            }

            return parameters;
        }
    }

    public interface ICallHandler
    {
        IWorkshopTree[] ParameterValues { get; }
        CallParallel ParallelMode { get; }
    }

    public class CallHandler : ICallHandler
    {
        public IWorkshopTree[] ParameterValues { get; set; }
        public CallParallel ParallelMode { get; set; }

        public CallHandler()
        {
            ParameterValues = new IWorkshopTree[0];
        }

        public CallHandler(IWorkshopTree[] parameterValues)
        {
            ParameterValues = parameterValues;
        }

        public CallHandler(IWorkshopTree[] parameterValues, CallParallel parallelMode)
        {
            ParameterValues = parameterValues;
            ParallelMode = parallelMode;
        }
    }

    public class SubroutineBuilder
    {
        private readonly DeltinScript _deltinScript;
        private readonly ISubroutineContext _context;
        public SubroutineInfo SubroutineInfo { get; private set; }

        public SubroutineBuilder(DeltinScript deltinScript, ISubroutineContext context)
        {
            _deltinScript = deltinScript;
            _context = context;
        }

        public void SetupSubroutine()
        {
            // Setup the subroutine element.
            Subroutine subroutine = _deltinScript.SubroutineCollection.NewSubroutine(_context.ElementName());

            // Create the rule.
            TranslateRule subroutineRule = new TranslateRule(_deltinScript, subroutine, _context.RuleName(), _context.VariableGlobalDefault());

            // Setup the return handler.
            ActionSet actionSet = subroutineRule.ActionSet.New(subroutineRule.ActionSet.IndexAssigner.CreateContained());

            // Get the variables that will be used to store the parameters.
            IndexReference[] parameterStores = new IndexReference[_context.Parameters().Length];
            for (int i = 0; i < parameterStores.Length; i++)
                parameterStores[i] = _context.Parameters()[i].GetSubroutineStack(actionSet, _context.VariableGlobalDefault());
            
            // Create the function builder.
            var determiner = _context.GetDeterminer();
            
            // If the subroutine is an object function inside a class, create a variable to store the class object.
            IndexReference objectStore = null;
            if (determiner.IsObject())
            {
                objectStore = actionSet.VarCollection.Assign(_context.ThisArrayName(), true, !determiner.IsRecursive());

                // Set the objectStore as an empty array if the subroutine is recursive.
                if (determiner.IsRecursive())
                {
                    actionSet.InitialSet().AddAction(objectStore.SetVariable(new V_EmptyArray()));
                    _context.ContainingType()?.AddObjectVariablesToAssigner(Element.Part<V_LastOf>(objectStore.GetVariable()), actionSet.IndexAssigner);
                    actionSet = actionSet.New(Element.Part<V_LastOf>(objectStore.Get())).PackThis().New(objectStore.CreateChild(Element.Part<V_CountOf>(objectStore.Get()) - 1));
                }
                else
                {
                    _context.ContainingType()?.AddObjectVariablesToAssigner(objectStore.GetVariable(), actionSet.IndexAssigner);
                    actionSet = actionSet.New(objectStore.Get()).PackThis().New(objectStore);
                }
            }
            
            var functionBuilder = new FunctionBuildController(actionSet, null, determiner);

            // Set the subroutine info.
            SubroutineInfo = new SubroutineInfo(subroutine, functionBuilder, parameterStores, objectStore);
            _context.SetSubroutineInfo(SubroutineInfo);

            functionBuilder.Build();

            // Pop object array if recursive.
            if (determiner.IsRecursive() && determiner.IsObject())
                actionSet.AddAction(objectStore.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.Part<V_CountOf>(objectStore.GetVariable()) - 1));

            // Add the subroutine.
            Rule translatedRule = subroutineRule.GetRule();
            _deltinScript.WorkshopRules.Add(translatedRule);

            // Done.
            _context.Finish(translatedRule);
        }
    }

    public interface ISubroutineContext
    {
        string RuleName();
        string ElementName();
        string ThisArrayName();
        bool VariableGlobalDefault();
        IParameterHandler[] Parameters();
        IGroupDeterminer GetDeterminer();
        CodeType ContainingType();
        void SetSubroutineInfo(SubroutineInfo subroutineInfo);
        void Finish(Rule rule);
    }

    public class DefinedSubroutineContext : ISubroutineContext
    {
        private readonly ParseInfo _parseInfo;
        private readonly DefinedMethod _method;

        public DefinedSubroutineContext(ParseInfo parseInfo, DefinedMethod method)
        {
            _parseInfo = parseInfo;
            _method = method;
        }

        public IParameterHandler[] Parameters() => DefinedParameterHandler.GetDefinedParameters(_method.Parameters.Length, new IFunctionHandler[] { new DefinedFunctionHandler(_method) }, _method.Attributes.Recursive);
        public string ElementName() => _method.Name;
        public string RuleName() => _method.SubroutineName;
        public string ThisArrayName() => "_" + ElementName() + "_object_stack";
        public bool VariableGlobalDefault() => _method.SubroutineDefaultGlobal;
        public CodeType ContainingType() => _method.Attributes.ContainingType;
        public void SetSubroutineInfo(SubroutineInfo subroutineInfo) => _method.SubroutineInfo = subroutineInfo;

        public void Finish(Rule rule)
        {
            var codeLens = new ElementCountCodeLens(_method.DefinedAt.range, _parseInfo.TranslateInfo.OptimizeOutput);
            _parseInfo.Script.AddCodeLensRange(codeLens);
            codeLens.RuleParsed(rule);
        }

        public IGroupDeterminer GetDeterminer() => new DefaultGroupDeterminer(new DefinedFunctionHandler[] { new DefinedFunctionHandler(_method) });
    }

    public class RecursiveStack
    {
        public object Identifier { get; }

        private readonly FunctionBuildController _builder;

        /// <summary>An array of positions to return to after a recursive call.</summary>
        private IndexReference continueArray;

        /// <summary>The next spot to continue at.</summary>
        private IndexReference nextContinue;

        /// <summary>Stores the current object.</summary>
        private IndexReference objectStore;

        /// <summary>The skip used to return the executing position after a recursive call.</summary>
        private SkipStartMarker continueAt;
        
        /// <summary>Marks the end of the method.</summary>
        private readonly SkipEndMarker endOfMethod = new SkipEndMarker();

        private ActionSet actionSet => _builder.ActionSet;
        private VarCollection varCollection => actionSet.VarCollection;
        private bool isGlobal => actionSet.IsGlobal;
        private string name => "func";

        public RecursiveStack(FunctionBuildController builder, object identifier)
        {
            Identifier = identifier;
            _builder = builder;
        }

        public void Init()
        {
            // Create the array used for continuing after a recursive call.
            continueArray = varCollection.Assign("_" + name + "_recursiveContinue", isGlobal, false);
            nextContinue = varCollection.Assign("_" + name + "_nextContinue", isGlobal, true);
            actionSet.InitialSet().AddAction(continueArray.SetVariable(new V_EmptyArray()));
            
            if (_builder.Determiner.IsVirtual())
            {
                objectStore = varCollection.Assign("_" + name + "_objectStack", isGlobal, false);
                actionSet.AddAction(objectStore.SetVariable(Element.CreateArray(actionSet.CurrentObject)));

                _builder.ModifySet(actionSet => actionSet.New(Element.Part<V_LastOf>(objectStore.GetVariable())).PackThis());
            }
            _builder.ModifySet(actionSet => actionSet.New(true));
        }

        public void StartRecursiveLoop()
        {
            // Create the recursive loop.
            actionSet.AddAction(Element.Part<A_While>(new V_True()));

            // Create the continue skip action.
            continueAt = new SkipStartMarker(actionSet);
            continueAt.SetSkipCount((Element)nextContinue.GetVariable());
            actionSet.AddAction(continueAt);
        }

        public void EndRecursiveLoop()
        {
            // Pop the object store array.
            if (_builder.Determiner.IsVirtual())
                actionSet.AddAction(objectStore.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.Part<V_CountOf>(objectStore.GetVariable()) - 1));
            
            // Pop the parameters.
            _builder.PopParameters();

            // Restart the method from the specified position if there are any elements in the continue array.
            actionSet.AddAction(Element.Part<A_SkipIf>(new V_Compare(
                Element.Part<V_CountOf>(continueArray.GetVariable()),
                Operators.Equal,
                new V_Number(0)
            ), new V_Number(3)));

            // Store the next continue and pop the continue array.
            actionSet.AddAction(nextContinue.SetVariable(Element.Part<V_LastOf>(continueArray.GetVariable())));
            actionSet.AddAction(continueArray.ModifyVariable(Operation.RemoveFromArrayByIndex, Element.Part<V_CountOf>(continueArray.GetVariable()) - 1));

            // Mark the end of the method.
            actionSet.AddAction(endOfMethod);
            actionSet.AddAction(new A_End());

            // Reset nextContinue.
            actionSet.AddAction(nextContinue.SetVariable(0));
        }

        /// <summary>The method was already called in the stack.</summary>
        public void RecursiveCall(ICallHandler callHandler, ActionSet callerSet)
        {
            // Push object array.
            if (_builder.Determiner.IsVirtual())
                actionSet.AddAction(objectStore.ModifyVariable(Operation.AppendToArray, (Element)callerSet.CurrentObject));

            // Push new parameters.
            _builder.PushParameters(callHandler);

            // Add to the continue skip array.
            V_Number skipLength = new V_Number();
            actionSet.AddAction(continueArray.ModifyVariable(
                Operation.AppendToArray,
                skipLength
            ));

            // Restart the method.
            SkipStartMarker resetSkip = new SkipStartMarker(actionSet);
            resetSkip.SetEndMarker(endOfMethod);
            actionSet.AddAction(resetSkip);

            SkipEndMarker continueAtMarker = new SkipEndMarker();
            actionSet.AddAction(continueAtMarker);
            skipLength.Value = continueAt.NumberOfActionsToMarker(continueAtMarker);
        }
    }

    public interface IFunctionLookupTable
    {
        void Build(FunctionBuildController builder);
    }

    class VirtualLookupTable : IFunctionLookupTable
    {
        private readonly IFunctionHandler[] _options;
        private readonly CodeType[] _allContainingTypes;

        public VirtualLookupTable(IFunctionHandler[] options)
        {
            _options = options;
            _allContainingTypes = _options.Select(o => o.ContainingType).ToArray();
        }

        public void Build(FunctionBuildController builder)
        {
            // Only parse the first option if there is only one.
            if (_options.Length == 1)
            {
                _options[0].ParseInner(builder.ActionSet);
                return;
            }

            // Create the switch that chooses the overload.
            SwitchBuilder typeSwitch = new SwitchBuilder(builder.ActionSet);

            foreach (IFunctionHandler option in _options)
            {
                // The action set for the overload.
                ActionSet optionSet = builder.ActionSet.New(builder.ActionSet.IndexAssigner.CreateContained());

                // Add the object variables of the selected method.
                option.ContainingType.AddObjectVariablesToAssigner(optionSet.CurrentObject, optionSet.IndexAssigner);

                // Go to next case then parse the block.
                typeSwitch.NextCase(new V_Number(((ClassType)option.ContainingType).Identifier));

                // Iterate through every type.
                foreach (CodeType type in builder.ActionSet.Translate.DeltinScript.Types.AllTypes)
                    // If 'type' does not equal the current virtual option's containing class...
                    if (option.ContainingType != type
                        // ...and 'type' implements the containing class...
                        && type.Implements(option.ContainingType)
                        // ...and 'type' does not have their own function implementation...
                        && AutoImplemented(option.ContainingType, _allContainingTypes, type))
                        // ...then add an additional case for 'type's class identifier.
                        typeSwitch.NextCase(new V_Number(((ClassType)type).Identifier));
                
                builder.Subcall(optionSet, option);
            }

            ClassData classData = builder.ActionSet.Translate.DeltinScript.GetComponent<ClassData>();

            // Finish the switch.
            typeSwitch.Finish(Element.Part<V_ValueInArray>(classData.ClassIndexes.GetVariable(), builder.ActionSet.CurrentObject));
        }

        /// <summary>Determines if the specified type does not have their own implementation for the specified virtual function.</summary>
        /// <param name="virtualFunction">The virtual function to check overrides of.</param>
        /// <param name="options">All potential virtual functions.</param>
        /// <param name="type">The type to check.</param>
        public static bool AutoImplemented(CodeType virtualType, CodeType[] allOptionTypes, CodeType type)
        {
            // Go through each class in the inheritance tree and check if it implements the function.
            CodeType current = type;
            while (current != null && current != virtualType)
            {
                // If it does, return false.
                if (allOptionTypes.Contains(current)) return false;
                current = current.Extends;
            }
            return true;
        }
    }
}