using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Deltin.Deltinteger.Parse.Settings;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;
using Deltin.Deltinteger.I18n;
using Deltin.Deltinteger.Debugger;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Workshop;
using Deltin.Deltinteger.Parse.Vanilla;
using Deltin.Deltinteger.Model;
using Deltin.Deltinteger.Parse.Vanilla.Settings;
using Deltin.Deltinteger.Lobby2.KeyValues;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Deltin.Deltinteger.Parse.Vanilla.Cache;

namespace Deltin.Deltinteger.Parse
{
    public class DeltinScript : IScopeHandler
    {
        private IFileGetter FileGetter { get; }
        public Importer Importer { get; }
        public Diagnostics Diagnostics { get; }
        public ScriptTypes Types { get; }
        public Scope PlayerVariableScope { get; set; }
        public Scope GlobalScope { get; }
        public Scope RulesetScope { get; }
        public VarCollection VarCollection { get; }
        public SubroutineCollection SubroutineCollection { get; } = new SubroutineCollection();
        public VarIndexAssigner DefaultIndexAssigner { get; } = new VarIndexAssigner();
        public TranslateRule InitialGlobal { get; private set; }
        public TranslateRule InitialPlayer { get; private set; }
        public StagedInitiation StagedInitiation { get; } = new StagedInitiation();
        public Uri SettingsSource { get; }
        public DsTomlSettings Settings { get; }
        private readonly OutputLanguage Language;
        private List<IComponent> Components { get; } = new List<IComponent>();
        private List<InitComponent> InitComponent { get; } = new List<InitComponent>();
        public DebugVariableLinkCollection DebugVariables { get; } = new DebugVariableLinkCollection();

        // TODO: Move workshopconverter outta here
        public ToWorkshop WorkshopConverter { get; private set; }

        // Vanilla compiling stuff
        readonly Dictionary<VanillaVariableCollection, IAnalyzedVanillaCollection> analyzedVanillaVariables = new();
        readonly DefaultVariableReport defaultVanillaVariables = new();
        public GroupSettingValue WorkshopSettings { get; } = new();

        // Project wide items
        readonly List<Var> rulesetVariables = new();

        public DeltinScript(TranslateSettings translateSettings)
        {
            FileGetter = translateSettings.FileGetter;
            Diagnostics = translateSettings.Diagnostics;
            SettingsSource = translateSettings.SourcedSettings.Uri;
            Settings = translateSettings.SourcedSettings.Settings ?? DsTomlSettings.Default;
            Language = translateSettings.OutputLanguage;

            VarCollection = new VarCollection(Settings.VariableTemplate);

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

        private readonly List<Variant<RuleAction, VanillaRuleAnalysis>> rules = new();

        void Translate()
        {
            AddComponent<RecursionCheckComponent>();

            CollectVanillaSettings();
            CollectTypes();
            CollectVariableReservations();
            CollectVariableDeclarations();

            ElementList.AddWorkshopFunctionsToScope(GlobalScope, Types); // Add workshop methods to global scope.
            GlobalFunctions.GlobalFunctions.Add(this, GlobalScope); // Add built-in methods.

            CollectFunctionDeclarations();

            StagedInitiation.Start();

            CollectHooks();
            CollectRules();

            GetComponent<SymbolLinkComponent>().Collect();
        }

        void CollectTypes()
        {
            foreach (var script in Importer.ScriptFiles)
                RootElement.Iter(script.Context.RootItems,
                    classContext: classContext =>
                    {
                        var newType = IDefinedTypeInitializer.GetInitializer(new ParseInfo(script, this), RulesetScope, classContext);
                        RulesetScope.AddType(newType);
                        Types.AllTypes.Add(newType);
                        Types.DefinedTypes.Add(newType);
                    },
                    enumContext: enumContext =>
                    {
                        var newEnum = new GenericCodeTypeInitializer(new DefinedEnum(new ParseInfo(script, this), enumContext));
                        RulesetScope.AddType(newEnum);
                        Types.AllTypes.Add(newEnum);
                        Types.DefinedTypes.Add(newEnum);
                    });
        }

        void CollectVariableReservations()
        {
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
        }

        void CollectVariableDeclarations()
        {
            foreach (ScriptFile script in Importer.ScriptFiles)
            {
                var scopedVanillaVariables = new VanillaScope(defaultVanillaVariables);
                RootElement.Iter(script.Context.RootItems,
                    declaration: declaration =>
                    {
                        if (declaration is VariableDeclaration variable)
                        {
                            Var var = new RuleLevelVariable(
                                RulesetScope,
                                new DefineContextHandler(new ParseInfo(script, this) { ScopedVanillaVariables = scopedVanillaVariables }, variable)).GetVar();

                            if (var.StoreType != StoreType.None)
                            {
                                rulesetVariables.Add(var);

                                // Add the variable to the player variables scope if it is a player variable.
                                if (var.VariableType == VariableType.Player)
                                    PlayerVariableScope.CopyVariable(var.GetDefaultInstance(null));
                            }
                        }
                    },
                    // Vanilla variable collection. This is collected alongside the OSTW variables
                    // so that the ostw variables can hook into the vanilla variables.
                    variables: vanillaVariables =>
                    {
                        var analysis = VanillaAnalysis.AnalyzeCollection(script, vanillaVariables);

                        // Lets rule-level variables see the vanilla variables in scope.
                        analysis.AddToScope(scopedVanillaVariables);

                        // Link the syntax to the analysis so that the variables are properly scoped
                        // when analyzing the vanilla rules.
                        analyzedVanillaVariables.Add(vanillaVariables, analysis);
                    });
            }
        }

        void CollectFunctionDeclarations()
        {
            // Get the function declarations
            foreach (ScriptFile script in Importer.ScriptFiles)
            {
                var scopedSubroutines = new VanillaScope(defaultVanillaVariables);
                ParseInfo parseInfo = new(script, this)
                {
                    ScopedVanillaVariables = scopedSubroutines
                };
                RootElement.Iter(script.Context.RootItems,
                    // OSTW function
                    declaration: declaration =>
                    {
                        if (declaration is FunctionContext function)
                            DefinedMethodProvider.GetDefinedMethod(parseInfo, this, function, null);
                    },
                    // Vanilla subroutine
                    variables: vanillaCollection =>
                    {
                        var analyzedSubroutineCollection = analyzedVanillaVariables[vanillaCollection];
                        analyzedSubroutineCollection.AddToScope(scopedSubroutines);
                    }
                );
            }
        }

        void CollectHooks()
        {
            foreach (ScriptFile script in Importer.ScriptFiles)
                foreach (var hookContext in script.Context.Hooks)
                    HookVar.GetHook(new ParseInfo(script, this), RulesetScope, hookContext);
        }

        void CollectRules()
        {
            VanillaCache.Instance.BeginTracking();
            foreach (ScriptFile script in Importer.ScriptFiles)
            {
                var analyzeVanillaRules = new List<VanillaRule>();
                var scopedVanillaVariables = new VanillaScope(defaultVanillaVariables);
                RootElement.Iter(script.Context.RootItems,
                    // ostw
                    rule: rule =>
                    {
                        rules.Add(new RuleAction(new ParseInfo(script, this), RulesetScope, rule));
                    },
                    // vanilla
                    vanillaRule: analyzeVanillaRules.Add,
                    // scope vanilla variables
                    variables: syntax =>
                    {
                        if (analyzedVanillaVariables.TryGetValue(syntax, out var analysis))
                            analysis.AddToScope(scopedVanillaVariables);
                    });

                var group = VanillaCache.Instance.GetGroup(script.Uri, new(scopedVanillaVariables));
                var result = new ConcurrentBag<(long I, VanillaRule, VanillaRuleAnalysis, IdeItems, bool)>();
                Parallel.ForEach(analyzeVanillaRules, (vanillaRule, s, i) =>
                {
                    if (group.TryGetCacheItem(vanillaRule, out var cache))
                    {
                        result.Add((i, vanillaRule, cache.Value.Analysis, cache.Value.IdeItems, false));
                    }
                    else
                    {
                        var ideItems = new IdeItems();
                        var rule = VanillaAnalysis.AnalyzeRule(script, vanillaRule, scopedVanillaVariables, ideItems);
                        result.Add((i, vanillaRule, rule, ideItems, true));
                    }
                });
                foreach (var (_, syntax, analysis, ide, cache) in result.OrderBy(r => r.I))
                {
                    rules.Add(analysis);
                    ide.AddToScript(script);
                    if (cache)
                        group.Cache(syntax, new(new(scopedVanillaVariables), analysis, ide));
                }
            }
            VanillaCache.Instance.RemoveUnused();
        }

        void CollectVanillaSettings()
        {
            foreach (var script in Importer.ScriptFiles)
                RootElement.Iter(script.Context.RootItems, vanillaSettings: vanillaSettings =>
                {
                    WorkshopSettings.Merge(AnalyzeSettings.Analyze(script, vanillaSettings));
                });
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

            // Assign vanilla variables
            foreach (var vanillaVariables in analyzedVanillaVariables.Values)
            {
                vanillaVariables.AssignWorkshopVariables(WorkshopConverter.LinkableVanillaVariables, VarCollection, SubroutineCollection);
            }
            defaultVanillaVariables.Assign(VarCollection, WorkshopConverter.LinkableVanillaVariables);

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
            foreach (var ruleVariant in rules)
            {
                ruleVariant.Match(
                    // OSTW rule
                    ostwRule =>
                    {
                        var translate = new TranslateRule(this, ostwRule);
                        Rule newRule = GetRule(translate.GetRule());
                        WorkshopRules.Add(newRule);
                        ostwRule.ElementCountLens.RuleParsed(newRule);
                    },
                    // Vanilla rule
                    owRule => owRule.ToElement(new(WorkshopConverter.LinkableVanillaVariables)).Match(
                        WorkshopRules.Add,
                        err =>
                        {
                            throw new Exception(err);
                        }
                    ));
            }

            GetComponent<AutoCompileSubroutine>().ToWorkshop(WorkshopConverter);

            WorkshopConverter.PersistentVariables.ToWorkshop();

            // Complete portable functions
            WorkshopConverter.LambdaBuilder.Complete();

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

            // Order the workshop rules by priority.
            WorkshopRules = WorkshopRules.OrderBy(wr => wr.Priority).ToList();

            // Get the final workshop string.
            WorkshopBuilder result = new WorkshopBuilder(Language, Settings.CStyleWorkshopOutput, Settings.CompileMiscellaneousComments, Settings.UseTabsInWorkshopOutput);
            LanguageInfo.I18nWarningMessage(result, Language);

            // Get the custom game settings.
            WorkshopSettings.ToWorkshopTop(result);

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

            // Out file
            if (!string.IsNullOrEmpty(Settings.OutFile) && SettingsSource != null)
            {
                // Path to out file.
                var outFile = Extras.CombinePathWithDotNotation(SettingsSource.LocalPath, Settings.OutFile);
                var fi = new FileInfo(outFile);
                fi.Directory.Create();
                File.WriteAllText(fi.FullName, WorkshopCode);
            }
        }

        public Rule GetRule(Rule rule)
        {
            if (Settings.OptimizeOutput)
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
