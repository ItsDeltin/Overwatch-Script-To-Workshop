using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Parse.Settings;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;
using Deltin.Deltinteger.I18n;
using Deltin.Deltinteger.Debugger;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Workshop;

namespace Deltin.Deltinteger.Parse
{
    public class DeltinScript : IScopeHandler
    {
        private FileGetter FileGetter { get; }
        public Importer Importer { get; }
        public Diagnostics Diagnostics { get; }
        public ScriptTypes Types { get; }
        public Scope PlayerVariableScope { get; set; }
        public Scope GlobalScope { get; }
        public Scope RulesetScope { get; }
        public VarCollection VarCollection { get; } = new VarCollection();
        public SubroutineCollection SubroutineCollection { get; } = new SubroutineCollection();
        private List<Var> rulesetVariables { get; } = new List<Var>();
        public VarIndexAssigner DefaultIndexAssigner { get; } = new VarIndexAssigner();
        public TranslateRule InitialGlobal { get; private set; }
        public TranslateRule InitialPlayer { get; private set; }
        public StagedInitiation StagedInitiation { get; } = new StagedInitiation();
        public ProjectSettings Settings { get; }
        private readonly OutputLanguage Language;
        public readonly bool OptimizeOutput;
        private List<IComponent> Components { get; } = new List<IComponent>();
        private List<InitComponent> InitComponent { get; } = new List<InitComponent>();
        public DebugVariableLinkCollection DebugVariables { get; } = new DebugVariableLinkCollection();

        // TODO: Move workshopconverter outta here
        public ToWorkshop WorkshopConverter { get; private set; }

        public DeltinScript(TranslateSettings translateSettings)
        {
            FileGetter = translateSettings.FileGetter;
            Diagnostics = translateSettings.Diagnostics;
            Settings = translateSettings.Settings;
            Language = translateSettings.OutputLanguage;
            OptimizeOutput = translateSettings.OptimizeOutput;

            Types = new ScriptTypes(this);
            Types.GetDefaults();

            GlobalScope = new Scope("global scope");
            RulesetScope = GlobalScope.Child();
            RulesetScope.PrivateCatch = true;
            Types.AddTypesToScope(GlobalScope);

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

        private void AddDefaultComponents()
        {
            AddComponent<Pathfinder.PathfinderTypesComponent>();
        }

        private T AddComponent<T>() where T : IComponent, new()
        {
            T newT = new T();

            for (int i = InitComponent.Count - 1; i >= 0; i--)
                if (typeof(T) == InitComponent[i].ComponentType)
                {
                    InitComponent[i].Apply(newT);
                    InitComponent.RemoveAt(i);
                }

            Components.Add(newT);
            newT.Init(this);

            return newT;
        }

        public T GetComponent<T>() where T : IComponent, new()
        {
            foreach (IComponent component in Components)
                if (component is T t)
                    return t;

            return AddComponent<T>();
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
            AddComponent<RecursionCheckComponent>();

            // Get the enums
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (var enumContext in script.Context.Enums)
                {
                    var newEnum = new GenericCodeTypeInitializer(new DefinedEnum(new ParseInfo(script, this), enumContext));
                    RulesetScope.AddType(newEnum);
                    Types.AllTypes.Add(newEnum);
                    Types.DefinedTypes.Add(newEnum);
                }

            // Get the types
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (var typeContext in script.Context.Classes)
                {
                    var newType = IDefinedTypeInitializer.GetInitializer(new ParseInfo(script, this), RulesetScope, typeContext);
                    RulesetScope.AddType(newType);
                    Types.AllTypes.Add(newType);
                    Types.DefinedTypes.Add(newType);
                }

            // Get the variable reservations
            foreach (ScriptFile script in Importer.ScriptFiles)
            {
                foreach (Token reservation in script.Context.GlobalvarReservations)
                {
                    string text = reservation.GetText().RemoveQuotes();

                    if (Int32.TryParse(text, out int id))
                        VarCollection.Reserve(id, true, script.Diagnostics, reservation.Range);
                    else
                        VarCollection.Reserve(text, true);
                }
                foreach (Token reservation in script.Context.PlayervarReservations)
                {
                    string text = reservation.GetText().RemoveQuotes();

                    if (Int32.TryParse(text, out int id))
                        VarCollection.Reserve(id, false, script.Diagnostics, reservation.Range);
                    else
                        VarCollection.Reserve(text, false);
                }
            }
            // Get variable declarations
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (var declaration in script.Context.Declarations)
                    if (declaration is VariableDeclaration variable)
                    {
                        Var var = new RuleLevelVariable(RulesetScope, new DefineContextHandler(new ParseInfo(script, this), variable)).GetVar();

                        if (var.StoreType != StoreType.None)
                        {
                            rulesetVariables.Add(var);

                            // Add the variable to the player variables scope if it is a player variable.
                            if (var.VariableType == VariableType.Player)
                                PlayerVariableScope.CopyVariable(var.GetDefaultInstance(null));
                        }
                    }

            ElementList.AddWorkshopFunctionsToScope(GlobalScope, Types); // Add workshop methods to global scope.
            GlobalFunctions.GlobalFunctions.Add(this, GlobalScope); // Add built-in methods.

            // Get the function declarations
            foreach (ScriptFile script in Importer.ScriptFiles)
            {
                ParseInfo parseInfo = new ParseInfo(script, this);
                foreach (var declaration in script.Context.Declarations)
                    if (declaration is FunctionContext function)
                        DefinedMethodProvider.GetDefinedMethod(parseInfo, this, function, null);
            }

            StagedInitiation.Start();

            // Get hooks
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (var hookContext in script.Context.Hooks)
                    HookVar.GetHook(new ParseInfo(script, this), RulesetScope, hookContext);

            // Get the rules
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (var ruleContext in script.Context.Rules)
                    rules.Add(new RuleAction(new ParseInfo(script, this), RulesetScope, ruleContext));

            GetComponent<SymbolLinkComponent>().Collect();
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

            WorkshopConverter = new ToWorkshop(this);
            WorkshopConverter.InitStatic();

            // Init called types.
            foreach (var workshopInit in _workshopInit) workshopInit.WorkshopInit(this);

            // Assign variables at the rule-set level.
            foreach (var variable in rulesetVariables)
            {
                var addToInitialRule = GetInitialRule(variable.VariableType == VariableType.Global);

                // Assign the variable an index.
                IGettable value = variable
                    .GetDefaultInstance(null)
                    .GetAssigner(new(addToInitialRule.ActionSet))
                    .GetValue(new GettableAssignerValueInfo(addToInitialRule.ActionSet)
                    {
                        SetInitialValue = SetInitialValue.SetIfExists
                    });
                DefaultIndexAssigner.Add(variable, value);

                if (value is IndexReference indexReference)
                    DebugVariables.Add(variable, indexReference);
            }

            // Parse the rules.
            foreach (var rule in rules)
            {
                var translate = new TranslateRule(this, rule);
                Rule newRule = GetRule(translate.GetRule());
                WorkshopRules.Add(newRule);
                rule.ElementCountLens.RuleParsed(newRule);
            }

            // Add built-in rules.
            // Initial player
            if (InitialPlayer.Actions.Count > 0)
                WorkshopRules.Insert(0, GetRule(InitialPlayer.GetRule()));

            // Initial global
            if (InitialGlobal.Actions.Count > 0)
                WorkshopRules.Insert(0, GetRule(InitialGlobal.GetRule()));

            // Additional
            if (addRules != null)
                WorkshopRules.AddRange(addRules.Invoke(VarCollection).Where(rule => rule != null));

            // Complete portable functions
            WorkshopConverter.LambdaBuilder.Complete();

            // Order the workshop rules by priority.
            WorkshopRules = WorkshopRules.OrderBy(wr => wr.Priority).ToList();

            // Get the final workshop string.
            WorkshopBuilder result = new WorkshopBuilder(Language);
            LanguageInfo.I18nWarningMessage(result, Language);

            // Get the custom game settings.
            if (Importer.MergedLobbySettings != null)
            {
                Ruleset settings = Ruleset.Parse(Importer.MergedLobbySettings);
                settings.ToWorkshop(result);
                result.AppendLine();
            }

            // Get the variables.
            VarCollection.ToWorkshop(result);
            result.AppendLine();

            // Get the subroutines.
            SubroutineCollection.ToWorkshop(result);

            // Get the rules.
            for (int i = 0; i < WorkshopRules.Count; i++)
            {
                WorkshopRules[i].ToWorkshop(result);
                ElementCount += WorkshopRules[i].ElementCount();
                if (i != WorkshopRules.Count - 1) result.AppendLine();
            }

            WorkshopCode = result.GetResult();
        }

        Rule GetRule(Rule rule)
        {
            if (OptimizeOutput)
                rule = rule.Optimized();
            return rule;
        }

        public ScriptFile ScriptFromUri(Uri uri) => Importer.ScriptFiles.FirstOrDefault(script => script.Uri.Compare(uri));

        private TranslateRule GetInitialRule(bool isGlobal)
        {
            return isGlobal ? InitialGlobal : InitialPlayer;
        }

        // Workshop init
        private readonly List<IWorkshopInit> _workshopInit = new List<IWorkshopInit>();
        public void AddWorkshopInit(IWorkshopInit workshopInit)
        {
            if (!_workshopInit.Contains(workshopInit))
                _workshopInit.Add(workshopInit);
        }

        Scope IScopeProvider.GetObjectBasedScope() => RulesetScope;
        Scope IScopeProvider.GetStaticBasedScope() => RulesetScope;
        void IScopeAppender.AddObjectBasedScope(IMethod function) => RulesetScope.AddNativeMethod(function);
        void IScopeAppender.AddStaticBasedScope(IMethod function) => RulesetScope.AddNativeMethod(function);
        void IScopeAppender.AddObjectBasedScope(IVariableInstance variable) => RulesetScope.AddNativeVariable(variable);
        void IScopeAppender.AddStaticBasedScope(IVariableInstance variable) => RulesetScope.AddNativeVariable(variable);
        IMethod IScopeProvider.GetOverridenFunction(DeltinScript deltinScript, FunctionOverrideInfo functionOverloadInfo) => null;
        IVariableInstance IScopeProvider.GetOverridenVariable(string variableName) => null;
        public void CheckConflict(ParseInfo parseInfo, CheckConflict identifier, DocRange range) => RulesetScope.CheckConflict(parseInfo, identifier, range);
    }

    public interface IComponent
    {
        void Init(DeltinScript deltinScript);
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
