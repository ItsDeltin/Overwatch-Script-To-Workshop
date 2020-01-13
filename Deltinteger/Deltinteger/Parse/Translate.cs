using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.Pathfinder;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class DeltinScript
    {
        private FileGetter FileGetter { get; }
        private Importer Importer { get; }
        public Diagnostics Diagnostics { get; }
        public List<ScriptFile> ScriptFiles { get; } = new List<ScriptFile>();
        private List<CodeType> types { get; } = new List<CodeType>();
        public List<CodeType> definedTypes { get; } = new List<CodeType>();
        public Scope PlayerVariableScope { get; private set; } = new Scope();
        public Scope GlobalScope { get; }
        public Scope RulesetScope { get; }
        public VarCollection VarCollection { get; } = new VarCollection();
        private List<Var> rulesetVariables { get; } = new List<Var>();
        public VarIndexAssigner DefaultIndexAssigner { get; } = new VarIndexAssigner();
        public TranslateRule InitialGlobal { get; private set; }
        public TranslateRule InitialPlayer { get; private set; }

        public DeltinScript(FileGetter fileGetter, Diagnostics diagnostics, ScriptFile rootRuleset, Func<VarCollection, Rule[]> addRules = null)
        {
            FileGetter = fileGetter;
            Diagnostics = diagnostics;

            types.AddRange(CodeType.DefaultTypes);
            Importer = new Importer(rootRuleset.Uri);

            CollectScriptFiles(rootRuleset);
            
            GlobalScope = Scope.GetGlobalScope();
            RulesetScope = GlobalScope.Child();
            RulesetScope.GroupCatch = true;
            
            Translate();
            if (!diagnostics.ContainsErrors())
                ToWorkshop(addRules);
        }

        void CollectScriptFiles(ScriptFile scriptFile)
        {
            ScriptFiles.Add(scriptFile);

            FileImporter importer = new FileImporter(scriptFile.Diagnostics, Importer);

            // Get the imported files.
            if (scriptFile.Context.import_file() != null)
            {
                foreach (var importFileContext in scriptFile.Context.import_file())
                {
                    string directory = GetImportedFile(scriptFile, importer, importFileContext);
                    if (Directory.Exists(directory))
                        AddImportCompletion(scriptFile, directory, DocRange.GetRange(importFileContext.STRINGLITERAL()));
                }
            }
        }

        public static void AddImportCompletion(ScriptFile script, string directory, DocRange range)
        {
            List<CompletionItem> completionItems = new List<CompletionItem>();
            var directories = Directory.GetDirectories(directory);
            var files = Directory.GetFiles(directory);

            completionItems.Add(new CompletionItem() {
                Label = "../",
                Detail = Directory.GetParent(directory).FullName,
                Kind = CompletionItemKind.Folder
            });

            foreach (var dir in directories)
                completionItems.Add(new CompletionItem() {
                    Label = Path.GetFileName(dir),
                    Detail = dir,
                    Kind = CompletionItemKind.Folder
                });
            
            foreach (var file in files)
                completionItems.Add(new CompletionItem() {
                    Label = Path.GetFileName(file),
                    Detail = file,
                    Kind = CompletionItemKind.File
                });
            
            script.AddCompletionRange(new CompletionRange(completionItems.ToArray(), range, CompletionRangeKind.ClearRest));
        }

        string GetImportedFile(ScriptFile script, FileImporter importer, DeltinScriptParser.Import_fileContext importFileContext)
        {
            DocRange stringRange = DocRange.GetRange(importFileContext.STRINGLITERAL());

            var importResult = importer.Import(
                stringRange,
                Extras.RemoveQuotes(importFileContext.STRINGLITERAL().GetText()),
                script.Uri
            );
            if (!importResult.SuccessfulReference) return importResult.Directory;

            script.AddDefinitionLink(stringRange, new Location(importResult.Uri, DocRange.Zero));
            script.AddHover(stringRange, importResult.FilePath);

            if (importResult.ShouldImport)
            {
                ScriptFile importedScript = new ScriptFile(Diagnostics, importResult.Uri, FileGetter.GetScript(importResult.Uri));
                CollectScriptFiles(importedScript);
            }
            return importResult.Directory;
        }

        private List<RuleAction> rules { get; } = new List<RuleAction>();

        void Translate()
        {
            List<IApplyBlock> applyMethods = new List<IApplyBlock>();

            // Get the reserved variables and IDs
            foreach (ScriptFile script in ScriptFiles)
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
            foreach (ScriptFile script in ScriptFiles)
            foreach (var enumContext in script.Context.enum_define())
            {
                var newEnum = new DefinedEnum(new ParseInfo(script, this), enumContext);
                types.Add(newEnum); 
                definedTypes.Add(newEnum);
            }

            // Get the types
            foreach (ScriptFile script in ScriptFiles)
            foreach (var typeContext in script.Context.type_define())
            {
                var newType = new DefinedType(new ParseInfo(script, this), GlobalScope, typeContext, applyMethods);
                types.Add(newType);
                definedTypes.Add(newType);
            }
            
            // Get the methods and macros
            foreach (ScriptFile script in ScriptFiles)
            {
                // Get the methods.
                foreach (var methodContext in script.Context.define_method())
                {
                    var newMethod = new DefinedMethod(new ParseInfo(script, this), RulesetScope, methodContext);
                    applyMethods.Add(newMethod);
                    //RulesetScope.AddMethod(newMethod, script.Diagnostics, DocRange.GetRange(methodContext.name));
                }
                
                // Get the macros.
                foreach (var macroContext in script.Context.define_macro())
                {
                    GetMacro(new ParseInfo(script, this), RulesetScope, macroContext, applyMethods);
                }
            }

            // Get the defined variables.
            foreach (ScriptFile script in ScriptFiles)
            foreach (var varContext in script.Context.define())
            {
                var newVar = Var.CreateVarFromContext(VariableDefineType.RuleLevel, new ParseInfo(script, this), varContext);
                newVar.Finalize(RulesetScope);
                rulesetVariables.Add(newVar);
                // Add the variable to the player variables scope if it is a player variable.
                if (newVar.VariableType == VariableType.Player)
                    PlayerVariableScope.AddVariable(newVar, null, null);
            }

            foreach (var apply in applyMethods) apply.SetupBlock();
            foreach (var apply in applyMethods) apply.CallInfo.CheckRecursion();

            // Get the rules
            foreach (ScriptFile script in ScriptFiles)
            foreach (var ruleContext in script.Context.ow_rule())
                rules.Add(new RuleAction(new ParseInfo(script, this), RulesetScope, ruleContext));
        }

        public string WorkshopCode { get; private set; }
        public List<Rule> WorkshopRules { get; private set; }

        void ToWorkshop(Func<VarCollection, Rule[]> addRules)
        {
            VarCollection.Setup();
            InitialGlobal = new TranslateRule(this, "Initial Global", RuleEvent.OngoingGlobal);
            InitialPlayer = new TranslateRule(this, "Initial Player", RuleEvent.OngoingPlayer);
            WorkshopRules = new List<Rule>();

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

            // Assign static variables.
            foreach (var type in types) type.AddStaticVariablesToAssigner(DefaultIndexAssigner);

            // Setup single-instance methods.
            foreach (var method in singleInstanceMethods) method.SetupSingleInstance();

            // Parse the rules.
            foreach (var rule in rules)
            {
                var translate = new TranslateRule(this, rule);
                WorkshopRules.Add(translate.GetRule());
            }

            if (InitialPlayer.Actions.Count > 0)
                WorkshopRules.Insert(0, InitialPlayer.GetRule());

            if (InitialGlobal.Actions.Count > 0)
                WorkshopRules.Insert(0, InitialGlobal.GetRule());
            
            if (addRules != null)
                WorkshopRules.AddRange(addRules.Invoke(VarCollection).Where(rule => rule != null));

            // Get the final workshop string.
            StringBuilder result = new StringBuilder();
            I18n.I18n.I18nWarningMessage(result, I18n.I18n.CurrentLanguage);

            // Get the variables.
            VarCollection.ToWorkshop(result, I18n.I18n.CurrentLanguage);
            result.AppendLine();

            // Get the rules.
            foreach (var rule in WorkshopRules)
                result.AppendLine(rule.ToWorkshop(I18n.I18n.CurrentLanguage));
            
            WorkshopCode = result.ToString();
        }

        public CodeType GetCodeType(string name, FileDiagnostics diagnostics, DocRange range)
        {
            var type = types.FirstOrDefault(type => type.Name == name);

            if (range != null && type == null)
                diagnostics.Error(string.Format("The type {0} does not exist.", name), range);
            
            return type;
        }
        public bool IsCodeType(string name)
        {
            return GetCodeType(name, null, null) != null;
        }

        public ScriptFile ScriptFromUri(Uri uri) => ScriptFiles.FirstOrDefault(script => script.Uri.Compare(uri));

        private Dictionary<ICallable, List<Location>> callRanges { get; } = new Dictionary<ICallable, List<Location>>();
        public void AddSymbolLink(ICallable callable, Location calledFrom)
        {
            if (callable == null) throw new ArgumentNullException(nameof(callable));
            if (calledFrom == null) throw new ArgumentNullException(nameof(calledFrom));

            if (!callRanges.ContainsKey(callable)) callRanges.Add(callable, new List<Location>());
            callRanges[callable].Add(calledFrom);
        }
        public Dictionary<ICallable, List<Location>> GetSymbolLinks() => callRanges;

        private TranslateRule GetInitialRule(bool isGlobal)
        {
            return isGlobal ? InitialGlobal : InitialPlayer;
        }

        private List<DefinedMethod> singleInstanceMethods = new List<DefinedMethod>();
        public void AddSingleInstanceMethod(DefinedMethod method)
        {
            singleInstanceMethods.Add(method);
        }

        public static IStatement GetStatement(ParseInfo parseInfo, Scope scope, DeltinScriptParser.StatementContext statementContext)
        {
            switch (statementContext)
            {
                case DeltinScriptParser.S_defineContext define    : {
                    var newVar = Var.CreateVarFromContext(VariableDefineType.Scoped, parseInfo, define.define());
                    newVar.Finalize(scope);
                    return new DefineAction(newVar);
                }
                case DeltinScriptParser.S_methodContext method    : return new CallMethodAction(parseInfo, scope, method.method(), false, scope);
                case DeltinScriptParser.S_varsetContext varset    : return new SetVariableAction(parseInfo, scope, varset.varset());
                case DeltinScriptParser.S_exprContext s_expr      : {

                    var expr = GetExpression(parseInfo, scope, s_expr.expr(), true, false);
                    if (expr is ExpressionTree == false || ((ExpressionTree)expr)?.Result is IStatement == false)
                    {
                        if (expr != null)
                            parseInfo.Script.Diagnostics.Error("Expressions can't be used as statements.", DocRange.GetRange(statementContext));
                        return null;
                    }
                    else return (ExpressionTree)expr;
                }
                case DeltinScriptParser.S_ifContext s_if          : return new IfAction(parseInfo, scope, s_if.@if());
                case DeltinScriptParser.S_whileContext s_while    : return new WhileAction(parseInfo, scope, s_while.@while());
                case DeltinScriptParser.S_forContext s_for        : return new ForAction(parseInfo, scope, s_for.@for());
                case DeltinScriptParser.S_foreachContext s_foreach: return new ForeachAction(parseInfo, scope, s_foreach.@foreach());
                case DeltinScriptParser.S_returnContext s_return  : return new ReturnAction(parseInfo, scope, s_return.@return());
                case DeltinScriptParser.S_deleteContext s_delete  : return new DeleteAction(parseInfo, scope, s_delete.delete());
                default: return null;
            }
        }

        public static IExpression GetExpression(ParseInfo parseInfo, Scope scope, DeltinScriptParser.ExprContext exprContext, bool selfContained = true, bool usedAsValue = true, Scope getter = null)
        {
            if (getter == null) getter = scope;

            switch (exprContext)
            {
                case DeltinScriptParser.E_numberContext number: return new NumberAction(parseInfo.Script, number.number());
                case DeltinScriptParser.E_trueContext @true: return new BoolAction(parseInfo.Script, true);
                case DeltinScriptParser.E_falseContext @false: return new BoolAction(parseInfo.Script, false);
                case DeltinScriptParser.E_nullContext @null: return new NullAction();
                case DeltinScriptParser.E_stringContext @string: return new StringAction(parseInfo.Script, @string.@string());
                case DeltinScriptParser.E_formatted_stringContext formattedString: return new StringAction(parseInfo, scope, formattedString.formatted_string());
                case DeltinScriptParser.E_variableContext variable: return GetVariable(parseInfo, scope, getter, variable.variable(), selfContained);
                case DeltinScriptParser.E_methodContext method: return new CallMethodAction(parseInfo, scope, method.method(), usedAsValue, getter);
                case DeltinScriptParser.E_new_objectContext newObject: return new CreateObjectAction(parseInfo, scope, newObject.create_object());
                case DeltinScriptParser.E_expr_treeContext exprTree: return new ExpressionTree(parseInfo, scope, exprTree, usedAsValue);
                case DeltinScriptParser.E_array_indexContext arrayIndex: return new ValueInArrayAction(parseInfo, scope, arrayIndex);
                case DeltinScriptParser.E_create_arrayContext createArray: return new CreateArrayAction(parseInfo, scope, createArray.createarray());
                case DeltinScriptParser.E_expr_groupContext group: return GetExpression(parseInfo, scope, group.exprgroup().expr());
                case DeltinScriptParser.E_type_convertContext typeConvert: return new TypeConvertAction(parseInfo, scope, typeConvert.typeconvert());
                case DeltinScriptParser.E_notContext not: return new NotAction(parseInfo, scope, not.expr());
                case DeltinScriptParser.E_inverseContext inverse: return new InverseAction(parseInfo, scope, inverse.expr());
                case DeltinScriptParser.E_op_1Context             op1: return new OperatorAction(parseInfo, scope, op1);
                case DeltinScriptParser.E_op_2Context             op2: return new OperatorAction(parseInfo, scope, op2);
                case DeltinScriptParser.E_op_boolContext       opBool: return new OperatorAction(parseInfo, scope, opBool);
                case DeltinScriptParser.E_op_compareContext opCompare: return new OperatorAction(parseInfo, scope, opCompare);
                case DeltinScriptParser.E_ternary_conditionalContext ternary: return new TernaryConditionalAction(parseInfo, scope, ternary);
                case DeltinScriptParser.E_rootContext root: return new RootAction(parseInfo.TranslateInfo);
                case DeltinScriptParser.E_thisContext @this: return new ThisAction(parseInfo, scope, @this);
                default: throw new Exception($"Could not determine the expression type '{exprContext.GetType().Name}'.");
            }
        }

        public static IExpression GetVariable(ParseInfo parseInfo, Scope scope, Scope getter, DeltinScriptParser.VariableContext variableContext, bool selfContained)
        {
            string variableName = variableContext.PART().GetText();
            DocRange variableRange = DocRange.GetRange(variableContext.PART());

            var type = parseInfo.TranslateInfo.GetCodeType(variableName, null, null);
            if (type != null)
            {
                if (selfContained)
                    parseInfo.Script.Diagnostics.Error("Types can't be used as expressions.", variableRange);
                
                if (variableContext.array() != null)
                    parseInfo.Script.Diagnostics.Error("Indexers cannot be used with types.", DocRange.GetRange(variableContext.array()));

                type.Call(parseInfo.Script, variableRange);
                return type;
            }

            IScopeable element = scope.GetVariable(variableName, getter, parseInfo.Script.Diagnostics, variableRange);
            if (element == null)
                return null;
            
            if (element is ICallable)
                ((ICallable)element).Call(parseInfo.Script, variableRange);
            
            if (element is IApplyBlock)
                parseInfo.CurrentCallInfo?.Call((IApplyBlock)element, variableRange);

            if (element is Var)
            {
                Var var = (Var)element;
                IExpression[] index;
                if (variableContext.array() == null) index = new IExpression[0];
                else
                {
                    index = new IExpression[variableContext.array().expr().Length];
                    for (int i = 0; i < index.Length; i++)
                        index[i] = GetExpression(parseInfo, getter, variableContext.array().expr(i));
                }

                return new CallVariableAction(var, index);
            }
            else if (element is ScopedEnumMember) return (ScopedEnumMember)element;
            else if (element is DefinedEnumMember) return (DefinedEnumMember)element;
            else if (element is MacroVar) return (MacroVar)element;
            else throw new NotImplementedException();
        }
    
        public static void GetMacro(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Define_macroContext macroContext, List<IApplyBlock> applyMethods)
        {
            if (macroContext.LEFT_PAREN() != null || macroContext.RIGHT_PAREN() != null)
            {
                var newMacro = new DefinedMacro(parseInfo, scope, macroContext);
                scope.AddMethod(newMacro, parseInfo.Script.Diagnostics, DocRange.GetRange(macroContext.name));
                applyMethods.Add(newMacro);
            }
            else
            {
                var newMacro = new MacroVar(parseInfo, scope, macroContext);
                scope.AddVariable(newMacro, parseInfo.Script.Diagnostics, DocRange.GetRange(macroContext.name));
                applyMethods.Add(newMacro);
            }
        }

        private ClassData _classData = null;
        public ClassData SetupClasses()
        {
            if (_classData == null)
            {
                _classData = new ClassData(VarCollection);
                InitialGlobal.ActionSet.AddAction(_classData.ClassArray.SetVariable(new V_EmptyArray()));

                if (DefinedType.CLASS_INDEX_WORKAROUND)
                    InitialGlobal.ActionSet.AddAction(_classData.ClassIndexes.SetVariable(new V_EmptyArray()));
            }
            return _classData;
        }

        private PathfinderInfo _pathfinderInfo = null;
        public PathfinderInfo SetupPathfinder()
        {
            if (_pathfinderInfo == null) _pathfinderInfo = new PathfinderInfo(this);
            return _pathfinderInfo;
        }
    }

    public enum AccessLevel
    {
        Public,
        Private
    }

    public class ParseInfo
    {
        public ScriptFile Script { get; }
        public DeltinScript TranslateInfo { get; }

        public CallInfo CurrentCallInfo { get; private set; }

        public ParseInfo(ScriptFile script, DeltinScript translateInfo)
        {
            Script = script;
            TranslateInfo = translateInfo;
        }
        private ParseInfo(ParseInfo other)
        {
            Script = other.Script;
            TranslateInfo = other.TranslateInfo;
            CurrentCallInfo = other.CurrentCallInfo;
        }
        public ParseInfo SetCallInfo(CallInfo currentCallInfo) => new ParseInfo(this) { CurrentCallInfo = currentCallInfo };
    }
}