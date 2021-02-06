using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;
using Deltin.Deltinteger.I18n;
using Deltin.Deltinteger.Debugger;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public class DeltinScript
    {
        private FileGetter FileGetter { get; }
        private Importer Importer { get; }
        public Diagnostics Diagnostics { get; }
        public ScriptTypes Types { get; } = new ScriptTypes();
        public Scope PlayerVariableScope { get; set; }
        public Scope GlobalScope { get; }
        public Scope RulesetScope { get; }
        public VarCollection VarCollection { get; } = new VarCollection();
        public SubroutineCollection SubroutineCollection { get; } = new SubroutineCollection();
        private List<Var> rulesetVariables { get; } = new List<Var>();
        public VarIndexAssigner DefaultIndexAssigner { get; } = new VarIndexAssigner();
        public TranslateRule InitialGlobal { get; private set; }
        public TranslateRule InitialPlayer { get; private set; }
        private readonly OutputLanguage Language;
        public readonly bool OptimizeOutput;
        private List<IComponent> Components { get; } = new List<IComponent>();
        private List<InitComponent> InitComponent { get; } = new List<InitComponent>();
        public DebugVariableLinkCollection DebugVariables { get; } = new DebugVariableLinkCollection();

        public DeltinScript(TranslateSettings translateSettings)
        {
            FileGetter = translateSettings.FileGetter;
            Diagnostics = translateSettings.Diagnostics;
            Language = translateSettings.OutputLanguage;
            OptimizeOutput = translateSettings.OptimizeOutput;

            Types.GetDefaults(this);

            GlobalScope = new Scope("global scope");
            RulesetScope = GlobalScope.Child();
            RulesetScope.PrivateCatch = true;

            Importer = new Importer(this, FileGetter, translateSettings.Root.Uri);
            Importer.CollectScriptFiles(this, translateSettings.Root);

            Translate();
            if (!Diagnostics.ContainsErrors())
                try
                {
                    ToWorkshop(translateSettings.AdditionalRules);
                }
                catch (Exception ex)
                {
                    WorkshopCode = "An exception was thrown while translating to workshop.\r\n" + ex.ToString();
                }

            foreach (IComponent component in Components)
                if (component is IDisposable disposable)
                    disposable.Dispose();
        }

        public T GetComponent<T>() where T : IComponent, new()
        {
            foreach (IComponent component in Components)
                if (component is T t)
                    return t;

            T newT = new T();
            newT.DeltinScript = this;

            for (int i = InitComponent.Count - 1; i >= 0; i--)
                if (typeof(T) == InitComponent[i].ComponentType)
                {
                    InitComponent[i].Apply(newT);
                    InitComponent.RemoveAt(i);
                }

            Components.Add(newT);
            newT.Init();

            return newT;
        }

        public bool IsComponent<T>() where T : IComponent => Components.Any(component => component is T);
        public bool IsComponent<T>(out T component) where T : IComponent
        {
            foreach (IComponent iterateComponent in Components)
                if (iterateComponent is T t)
                {
                    component = t;
                    return true;
                }
            component = default(T);
            return false;
        }

        public void ExecOnComponent<T>(Action<T> apply) where T : IComponent
        {
            if (IsComponent<T>(out T existing))
                apply.Invoke(existing);
            else
                InitComponent.Add(new InitComponent(typeof(T), component => apply.Invoke((T)component)));
        }

        private List<RuleAction> rules { get; } = new List<RuleAction>();

        void Translate()
        {
            // Get the variable reservations
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (Token reservation in script.Context.GlobalvarReservations)
                {
                    string text = reservation.GetText().RemoveQuotes();
                    if(Int32.TryParse(text, out int id)){
                        VarCollection.Reserve(id, true, script.Diagnostics, reservation.Range);
                    } else {
                        VarCollection.Reserve(text, true);
                    }
                }
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (Token reservation in script.Context.PlayervarReservations)
                {
                    string text = reservation.GetText().RemoveQuotes();
                    if(Int32.TryParse(text, out int id)){
                        VarCollection.Reserve(id, false, script.Diagnostics, reservation.Range);
                    } else {
                        VarCollection.Reserve(text, false);
                    }
                }

            // Get the enums
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (var enumContext in script.Context.Enums)
                    if (enumContext.Identifier)
                    {
                        var newEnum = new DefinedEnum(new ParseInfo(script, this), enumContext);
                        Types.AllTypes.Add(newEnum);
                        Types.DefinedTypes.Add(newEnum);
                        Types.CalledTypes.Add(newEnum);
                    }

            // Get the types
            foreach (ScriptFile script in Importer.ScriptFiles)
            foreach (var typeContext in script.Context.Classes)
            {
                var newType = new DefinedType(new ParseInfo(script, this), GlobalScope, typeContext);
                Types.AllTypes.Add(newType);
                Types.DefinedTypes.Add(newType);
                Types.CalledTypes.Add(newType);
            }
	
			//Get the type aliases
            foreach (ScriptFile script in Importer.ScriptFiles)
			{    
				ParseInfo parseInfo = new ParseInfo(script, this);
            	foreach (var typeContext in script.Context.TypeAliases)
                    if (typeContext.NewTypeName)
                    {
                        var aliasType = Types.GetCodeType(typeContext.NewTypeName?.Text);
                        var type = PipeType.GetCodeTypeFromContext(parseInfo, typeContext.OtherType);
                        if(aliasType != null)
                            parseInfo.Script.Diagnostics.Error("type name already in use.", typeContext.Range);
                        else if(type != null)
                            Types.TypeAliases.Add(typeContext.NewTypeName.GetText(), type);
                    }
			}
            
            // Get variable declarations
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (var declaration in script.Context.Declarations)
                    if (declaration is VariableDeclaration variable)
                    {
                        ParseInfo parseInfo = new ParseInfo(script, this);
                        
                        Var newVar = new RuleLevelVariable(RulesetScope, new DefineContextHandler(new ParseInfo(script, this), variable));
                        rulesetVariables.Add(newVar);

                        // Add the variable to the player variables scope if it is a player variable.
                        if (newVar.VariableType == VariableType.Player)
                            PlayerVariableScope.CopyVariable(newVar);
                    }
            
            ElementList.AddWorkshopFunctionsToScope(GlobalScope, Types); // Add workshop methods to global scope.
            GlobalFunctions.GlobalFunctions.Add(this, GlobalScope); // Add built-in methods.

            // Get the function declarations
            foreach (ScriptFile script in Importer.ScriptFiles)
            {
                ParseInfo parseInfo = new ParseInfo(script, this);

                // Get the functions.
                foreach (var declaration in script.Context.Declarations)
                {
                    // Function
                    if (declaration is FunctionContext function)
                        new DefinedMethod(parseInfo, RulesetScope, RulesetScope, function, null);
                    // Macro function
                    else if (declaration is MacroFunctionContext macroFunction)
                        parseInfo.GetMacro(RulesetScope, RulesetScope, macroFunction);
                    // Macro var
                    else if (declaration is MacroVarDeclaration macroVar)
                        parseInfo.GetMacro(RulesetScope, RulesetScope, macroVar);
                }
            }

            foreach (var applyType in Types.AllTypes) if (applyType is ClassType classType) classType.ResolveElements();
            foreach (var apply in _applyBlocks) apply.SetupParameters();
            foreach (var apply in _applyBlocks) apply.SetupBlock();
            foreach (var callInfo in _recursionCheck) callInfo.CheckRecursion(this);

            // Get hooks
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (var hookContext in script.Context.Hooks)
                    HookVar.GetHook(new ParseInfo(script, this), RulesetScope, hookContext);

            // Get the rules
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (var ruleContext in script.Context.Rules)
                    rules.Add(new RuleAction(new ParseInfo(script, this), RulesetScope, ruleContext));
        }

        public string WorkshopCode { get; private set; }
        public int ElementCount { get; private set; }
        public List<Rule> WorkshopRules { get; private set; }

        void ToWorkshop(Func<VarCollection, Rule[]> addRules)
        {
            // Set up the variable collection.
            VarCollection.Setup();

            // Set up initial global and player rules.
            InitialGlobal = new TranslateRule(this, "Initial Global", RuleEvent.OngoingGlobal);
            InitialPlayer = new TranslateRule(this, "Initial Player", RuleEvent.OngoingPlayer);
            WorkshopRules = new List<Rule>();

            // Init called types.
            foreach (var type in Types.AllTypes)
                if (Types.CalledTypes.Contains(type))
                    type.WorkshopInit(this);

            // Assign variables at the rule-set level.
            foreach (var variable in rulesetVariables)
            {
                // Assign the variable an index.
                var assigner = DefaultIndexAssigner.Add(VarCollection, variable, true, null) as IndexReference;

                // Assigner will be non-null if it is an IndexReference.
                if (assigner != null)
                {
                    DebugVariables.Add(variable, assigner);
                    // Initial value.
                    if (variable.InitialValue != null)
                    {
                        var addToInitialRule = GetInitialRule(variable.VariableType == VariableType.Global);

                        addToInitialRule.ActionSet.AddAction(assigner.SetVariable(
                            (Element)variable.InitialValue.Parse(addToInitialRule.ActionSet)
                        ));
                    }
                }
            }

            // Parse the rules.
            foreach (var rule in rules)
            {
                var translate = new TranslateRule(this, rule);
                Rule newRule = translate.GetRule();
                WorkshopRules.Add(newRule);
                rule.ElementCountLens.RuleParsed(newRule);
            }

            // Add built-in rules.
            // Initial player
            if (InitialPlayer.Actions.Count > 0)
                WorkshopRules.Insert(0, InitialPlayer.GetRule());

            // Initial global
            if (InitialGlobal.Actions.Count > 0)
                WorkshopRules.Insert(0, InitialGlobal.GetRule());

            // Additional
            if (addRules != null)
                WorkshopRules.AddRange(addRules.Invoke(VarCollection).Where(rule => rule != null));

            // Order the workshop rules by priority.
            WorkshopRules = WorkshopRules.OrderBy(wr => wr.Priority).ToList();

            // Get the final workshop string.
            WorkshopBuilder result = new WorkshopBuilder(Language);
            LanguageInfo.I18nWarningMessage(result, Language);

            // Get the custom game settings.
            if (Importer.MergedLobbySettings != null)
            {
                Ruleset settings = Deltin.Deltinteger.Lobby.Deserializer.RulesetDeserializer.Deserialize(Importer.MergedLobbySettings);
                settings.ToWorkshop(result);
                result.AppendLine();
            }

            // Get the variables.
            VarCollection.ToWorkshop(result);
            result.AppendLine();

            // Print class identifiers.
            Types.PrintClassIdentifiers(result);

            // Get the subroutines.
            SubroutineCollection.ToWorkshop(result);

            // Get the rules.
            for (int i = 0; i < WorkshopRules.Count; i++)
            {
                WorkshopRules[i].ToWorkshop(result, OptimizeOutput);
                ElementCount += WorkshopRules[i].ElementCount(OptimizeOutput);
                if (i != WorkshopRules.Count - 1) result.AppendLine();
            }
            
            WorkshopCode = result.GetResult();
        }

        public ScriptFile ScriptFromUri(Uri uri) => Importer.ScriptFiles.FirstOrDefault(script => script.Uri.Compare(uri));

        private TranslateRule GetInitialRule(bool isGlobal)
        {
            return isGlobal ? InitialGlobal : InitialPlayer;
        }

        // Applyable blocks
        private readonly List<IApplyBlock> _applyBlocks = new List<IApplyBlock>();
        private readonly List<CallInfo> _recursionCheck = new List<CallInfo>();
        public void ApplyBlock(IApplyBlock apply)
        {
            _applyBlocks.Add(apply);
            if (apply.CallInfo != null) _recursionCheck.Add(apply.CallInfo);
        }
        public void RecursionCheck(CallInfo callInfo)
        {
            _recursionCheck.Add(callInfo ?? throw new ArgumentNullException(nameof(callInfo)));
        }
    }

    public class ScriptTypes : ITypeSupplier
    {
        public List<CodeType> AllTypes { get; } = new List<CodeType>();
        public List<CodeType> DefinedTypes { get; } = new List<CodeType>();
        public List<CodeType> CalledTypes { get; } = new List<CodeType>();
		public Dictionary<String, CodeType> TypeAliases {get; } =  new Dictionary<String, CodeType>();
        private readonly PlayerType _playerType;
        private readonly VectorType _vectorType;
        private readonly NumberType _numberType;
        private readonly StringType _stringType;
        private readonly BooleanType _booleanType;
        private AnyType _anyType;
        private AnyType _unknownType;

        public ScriptTypes()
        {
            _playerType = new PlayerType(this);
            _vectorType = new VectorType(this);
            _numberType = new NumberType(this);
            _stringType = new StringType(this);
            _booleanType = new BooleanType(this);
        }

        public void GetDefaults(DeltinScript deltinScript)
        {
            _anyType = new AnyType(deltinScript);
            _unknownType = new AnyType("?", deltinScript);
            AddType(_anyType);
            AddType(_playerType);
            AddType(_vectorType);
            AddType(_numberType);
            AddType(_stringType);
            AddType(_booleanType);
            AddType(Pathfinder.SegmentsStruct.Instance);
            // Pathfinder classes
            AddType(new Pathfinder.PathmapClass(deltinScript));
            AddType(new Pathfinder.PathResolveClass(this));
            AddType(new Pathfinder.BakemapClass(this));
            // Constant lambda types.
            AddType(new Lambda.BlockLambda());
            AddType(new Lambda.ValueBlockLambda(_anyType));
            AddType(new Lambda.MacroLambda(_anyType));
            // Model static class.
            // AddType(new Models.AssetClass());
            // Enums
            foreach (var type in ValueGroupType.GetEnumTypes(this))
                AddType(type);

            foreach (var type in AllTypes)
                if (type is IResolveElements resolveElements)
                    resolveElements.ResolveElements();

            deltinScript.PlayerVariableScope = _playerType.PlayerVariableScope;
        }

        private void AddType(CodeType type) => AllTypes.Add(type);

        public CodeType GetCodeType(string name) => AllTypes.FirstOrDefault(type => type.Name == name);
        public CodeType GetCodeType(string name, FileDiagnostics diagnostics, DocRange range)
        {
            var type = AllTypes.FirstOrDefault(type => type.Name == name);
			if(type == null) {
				CodeType typeAlias;
				if(TypeAliases.TryGetValue(name, out typeAlias))
					type = typeAlias;
			}

            if (range != null && type == null)
                diagnostics.Error(string.Format("The type {0} does not exist.", name), range);

            return type;
        }
        public bool IsCodeType(string name) => GetCodeType(name, null, null) != null;
        public T GetCodeType<T>() where T: CodeType => (T)AllTypes.FirstOrDefault(type => type.GetType() == typeof(T));

        public void CallType(CodeType type)
        {
            if (!CalledTypes.Contains(type))
                CalledTypes.Add(type);
        }

        public void PrintClassIdentifiers(WorkshopBuilder builder)
        {
            builder.AppendLine("// Class identifiers:");

            foreach (CodeType type in AllTypes)
                if (type is ClassType classType && classType.Identifier > 0)
                    builder.AppendLine("// " + classType.Name + ": " + classType.Identifier);

            builder.AppendLine();
        }

        public T GetInstance<T>() where T: CodeType => (T)AllTypes.First(type => type.GetType() == typeof(T));

        public CodeType Default() => Any();
        public CodeType Any() => _anyType;
        public CodeType AnyArray() => new ArrayType(this, Any());
        public CodeType Boolean() => _booleanType;
        public CodeType Number() => _numberType;
        public CodeType String() => _stringType;
        public CodeType Player() => _playerType;
        public CodeType Vector() => _vectorType;
        public CodeType Unknown() => _unknownType;

        public CodeType EnumType(string typeName)
        {
            foreach (var type in AllTypes)
                if (type is ValueGroupType valueGroupType && type.Name == typeName)
                    return type;
            throw new Exception("No enum type by the name of '" + typeName + "' exists.");
        }
    }

    public interface IComponent
    {
        DeltinScript DeltinScript { get; set; }
        void Init();
    }

    class InitComponent
    {
        public Type ComponentType { get; }
        public Action<IComponent> Apply { get; }

        public InitComponent(Type componentType, Action<IComponent> apply)
        {
            ComponentType = componentType;
            Apply = apply;
        }
    }
}
