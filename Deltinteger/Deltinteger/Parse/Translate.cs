using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Lobby;
using Deltin.Deltinteger.I18n;

namespace Deltin.Deltinteger.Parse
{
    public class DeltinScript
    {
        private FileGetter FileGetter { get; }
        private Importer Importer { get; }
        public Diagnostics Diagnostics { get; }
        public ScriptTypes Types { get; } = new ScriptTypes();
        public Scope PlayerVariableScope { get; private set; } = new Scope();
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

        public DeltinScript(TranslateSettings translateSettings)
        {
            FileGetter = translateSettings.FileGetter;
            Diagnostics = translateSettings.Diagnostics;
            Language = translateSettings.OutputLanguage;
            OptimizeOutput = translateSettings.OptimizeOutput;

            GlobalScope = Scope.GetGlobalScope();
            RulesetScope = GlobalScope.Child();
            RulesetScope.PrivateCatch = true;

            Types.GetDefaults(this);
            Importer = new Importer(this, FileGetter, translateSettings.Root.Uri);
            Importer.CollectScriptFiles(translateSettings.Root);            
            
            Translate();
            if (!Diagnostics.ContainsErrors())
                ToWorkshop(translateSettings.AdditionalRules);
            
            foreach (IComponent component in Components)
                if (component is IDisposable disposable)
                    disposable.Dispose();
        }

        public T GetComponent<T>() where T: IComponent, new()
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

        public bool IsComponent<T>() where T: IComponent => Components.Any(component => component is T);
        public bool IsComponent<T>(out T component) where T: IComponent
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

        public void ExecOnComponent<T>(Action<T> apply) where T: IComponent
        {
            if (IsComponent<T>(out T existing))
                apply.Invoke(existing);
            else
                InitComponent.Add(new InitComponent(typeof(T), component => apply.Invoke((T)component)));
        }

        private List<RuleAction> rules { get; } = new List<RuleAction>();

        void Translate()
        {
            // Get the reserved variables and IDs
            foreach (ScriptFile script in Importer.ScriptFiles)
            {
                if (script.Context.reserved_global()?.reserved_list() != null)
                {
                    foreach (var name in script.Context.reserved_global().reserved_list().PART()) VarCollection.Reserve(name.GetText(), true);
                    foreach (var id in script.Context.reserved_global().reserved_list().NUMBER()) VarCollection.Reserve(int.Parse(id.GetText()), true, null, null);
                }
                if (script.Context.reserved_player()?.reserved_list() != null)
                {
                    foreach (var name in script.Context.reserved_player().reserved_list().PART()) VarCollection.Reserve(name.GetText(), false);
                    foreach (var id in script.Context.reserved_player().reserved_list().NUMBER()) VarCollection.Reserve(int.Parse(id.GetText()), false, null, null);
                }
            }

            // Get the enums
            foreach (ScriptFile script in Importer.ScriptFiles)
            foreach (var enumContext in script.Context.enum_define())
            {
                var newEnum = new DefinedEnum(new ParseInfo(script, this), enumContext);
                Types.AllTypes.Add(newEnum); 
                Types.DefinedTypes.Add(newEnum);
                Types.CalledTypes.Add(newEnum);
            }

            // Get the types
            foreach (ScriptFile script in Importer.ScriptFiles)
            foreach (var typeContext in script.Context.type_define())
            {
                var newType = new DefinedType(new ParseInfo(script, this), GlobalScope, typeContext);
                Types.AllTypes.Add(newType);
                Types.DefinedTypes.Add(newType);
                Types.CalledTypes.Add(newType);
            }
            
            // Get the methods and macros
            foreach (ScriptFile script in Importer.ScriptFiles)
            {
                ParseInfo parseInfo = new ParseInfo(script, this);

                // Get the methods.
                foreach (var methodContext in script.Context.define_method())
                    new DefinedMethod(parseInfo, RulesetScope, RulesetScope, methodContext, null);
                
                // Get the macros.
                foreach (var macroContext in script.Context.define_macro())
                    parseInfo.GetMacro(RulesetScope, RulesetScope, macroContext);
            }

            // Get the defined variables.
            foreach (ScriptFile script in Importer.ScriptFiles)
            foreach (var varContext in script.Context.define())
            {
                Var newVar = new RuleLevelVariable(RulesetScope, new DefineContextHandler(new ParseInfo(script, this), varContext));
                rulesetVariables.Add(newVar);

                // Add the variable to the player variables scope if it is a player variable.
                if (newVar.VariableType == VariableType.Player)
                    PlayerVariableScope.CopyVariable(newVar);
            }

            foreach (var applyType in Types.AllTypes) if (applyType is ClassType classType) classType.ResolveElements();
            foreach (var apply in applyBlocks) apply.SetupParameters();
            foreach (var apply in applyBlocks) apply.SetupBlock();
            foreach (var apply in applyBlocks) apply.CallInfo?.CheckRecursion();

            // Get hooks
            foreach (ScriptFile script in Importer.ScriptFiles)
            foreach (var hookContext in script.Context.hook())
                HookVar.GetHook(new ParseInfo(script, this), RulesetScope, hookContext);

            // Get the rules
            foreach (ScriptFile script in Importer.ScriptFiles)
            foreach (var ruleContext in script.Context.ow_rule())
                rules.Add(new RuleAction(new ParseInfo(script, this), RulesetScope, ruleContext));

            //Get operators
            foreach (ScriptFile script in Importer.ScriptFiles)
            {

            }

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
            foreach (var type in Types.CalledTypes.Distinct()) type.WorkshopInit(this);

             // Assign variables at the rule-set level.
            foreach (var variable in rulesetVariables)
            {
                // Assign the variable an index.
                DefaultIndexAssigner.Add(VarCollection, variable, true, null);

                var assigner = DefaultIndexAssigner[variable] as IndexReference;
                if (assigner != null && variable.InitialValue != null)
                {
                    var addToInitialRule = GetInitialRule(variable.VariableType == VariableType.Global);

                    addToInitialRule.ActionSet.AddAction(assigner.SetVariable(
                        (Element)variable.InitialValue.Parse(addToInitialRule.ActionSet)
                    ));
                }
            }

            // Setup single-instance methods.
            foreach (var method in subroutines) method.SetupSubroutine();

            // Parse the rules.
            foreach (var rule in rules)
            {
                var translate = new TranslateRule(this, rule);
                Rule newRule = translate.GetRule();
                WorkshopRules.Add(newRule);
                rule.ElementCountLens.RuleParsed(newRule);
            }

            if (InitialPlayer.Actions.Count > 0)
                WorkshopRules.Insert(0, InitialPlayer.GetRule());

            if (InitialGlobal.Actions.Count > 0)
                WorkshopRules.Insert(0, InitialGlobal.GetRule());
            
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
                Ruleset settings = Ruleset.Parse(Importer.MergedLobbySettings);
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
            
            WorkshopCode = result.ToString();
        }

        public ScriptFile ScriptFromUri(Uri uri) => Importer.ScriptFiles.FirstOrDefault(script => script.Uri.Compare(uri));

        private TranslateRule GetInitialRule(bool isGlobal)
        {
            return isGlobal ? InitialGlobal : InitialPlayer;
        }

        // Subroutine methods
        private List<DefinedMethod> subroutines = new List<DefinedMethod>();
        public void AddSubroutine(DefinedMethod method)
        {
            subroutines.Add(method);
        }

        // Applyable blocks
        private List<IApplyBlock> applyBlocks = new List<IApplyBlock>();
        public void ApplyBlock(IApplyBlock apply)
        {
            applyBlocks.Add(apply);
        }
    }

    public class ScriptTypes
    {
        public List<CodeType> AllTypes { get; } = new List<CodeType>();
        public List<CodeType> DefinedTypes { get; } = new List<CodeType>();
        public List<CodeType> CalledTypes { get; } = new List<CodeType>();

        public void GetDefaults(DeltinScript deltinScript)
        {
            AllTypes.AddRange(CodeType.DefaultTypes);
            AllTypes.Add(new Pathfinder.PathmapClass(deltinScript));
            AllTypes.Add(new Pathfinder.PathResolveClass());
        }

        public CodeType GetCodeType(string name, FileDiagnostics diagnostics, DocRange range)
        {
            var type = AllTypes.FirstOrDefault(type => type.Name == name);

            if (range != null && type == null)
                diagnostics.Error(string.Format("The type {0} does not exist.", name), range);
            
            return type;
        }
        public bool IsCodeType(string name)
        {
            return GetCodeType(name, null, null) != null;
        }
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