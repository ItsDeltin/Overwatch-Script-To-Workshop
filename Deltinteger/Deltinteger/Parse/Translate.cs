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
        private Scope RulesetScope { get; }
        public VarCollection VarCollection { get; } = new VarCollection();
        private List<Var> rulesetVariables { get; } = new List<Var>();
        public VarIndexAssigner DefaultIndexAssigner { get; } = new VarIndexAssigner();
        public TranslateRule InitialGlobal { get; private set; }
        public TranslateRule InitialPlayer { get; private set; }

        public DeltinScript(FileGetter fileGetter, Diagnostics diagnostics, ScriptFile rootRuleset)
        {
            FileGetter = fileGetter;
            Diagnostics = diagnostics;

            types.AddRange(CodeType.DefaultTypes);
            Importer = new Importer(rootRuleset.Uri);

            CollectScriptFiles(rootRuleset);
            
            GlobalScope = Scope.GetGlobalScope();
            RulesetScope = GlobalScope.Child();
            
            Translate();
            if (!diagnostics.ContainsErrors())
                ToWorkshop();
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
                    AddImportCompletion(scriptFile, directory, DocRange.GetRange(importFileContext.STRINGLITERAL()));
                }
            }
        }

        static void AddImportCompletion(ScriptFile script, string directory, DocRange range)
        {
            List<CompletionItem> completionItems = new List<CompletionItem>();
            var directories = Directory.GetDirectories(directory);
            var files = Directory.GetFiles(directory);

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
            
            script.AddCompletionRange(new CompletionRange(completionItems.ToArray(), range, true));
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
            List<DefinedFunction> applyMethods = new List<DefinedFunction>();

            // Get the types
            foreach (ScriptFile script in ScriptFiles)
            foreach (var typeContext in script.Context.type_define())
            {
                var newType = new DefinedType(script, this, GlobalScope, typeContext, applyMethods);
                types.Add(newType);
                definedTypes.Add(newType);
            }
            
            // Get the methods and macros
            foreach (ScriptFile script in ScriptFiles)
            {
                // Get the methods.
                foreach (var methodContext in script.Context.define_method())
                {
                    var newMethod = new DefinedMethod(script, this, RulesetScope, methodContext);
                    applyMethods.Add(newMethod);
                    //RulesetScope.AddMethod(newMethod, script.Diagnostics, DocRange.GetRange(methodContext.name));
                }
                
                // Get the macros.
                foreach (var macroContext in script.Context.define_macro())
                {
                    var newMacro = new DefinedMacro(script, this, RulesetScope, macroContext);
                    RulesetScope.AddMethod(newMacro, script.Diagnostics, DocRange.GetRange(macroContext.name));
                    applyMethods.Add(newMacro);
                }
            }

            // Get the defined variables.
            foreach (ScriptFile script in ScriptFiles)
            foreach (var varContext in script.Context.define())
            {
                var newVar = Var.CreateVarFromContext(VariableDefineType.RuleLevel, script, this, varContext);
                newVar.Finalize(RulesetScope);
                rulesetVariables.Add(newVar);
                // Add the variable to the player variables scope if it is a player variable.
                if (newVar.VariableType == VariableType.Player)
                    PlayerVariableScope.AddVariable(newVar, null, null);
            }

            foreach (var apply in applyMethods)
                apply.SetupBlock();

            // Get the rules
            foreach (ScriptFile script in ScriptFiles)
            foreach (var ruleContext in script.Context.ow_rule())
                rules.Add(new RuleAction(script, this, RulesetScope, ruleContext));
        }

        public string WorkshopCode { get; private set; }
        public List<Rule> WorkshopRules { get; private set; }

        void ToWorkshop()
        {
            VarCollection.Setup();
            InitialGlobal = new TranslateRule(this, "Initial Global", RuleEvent.OngoingGlobal);
            InitialPlayer = new TranslateRule(this, "Initial Player", RuleEvent.OngoingPlayer);
            WorkshopRules = new List<Rule>();

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

            if (InitialGlobal.Actions.Count > 0)
                WorkshopRules.Add(InitialGlobal.GetRule());
            
            if (InitialPlayer.Actions.Count > 0)
                WorkshopRules.Add(InitialPlayer.GetRule());

            foreach (var rule in rules)
            {
                var translate = new TranslateRule(this, rule);
                WorkshopRules.Add(translate.GetRule());
            }

            // Get the final workshop string.
            StringBuilder result = new StringBuilder();
            // Get the variables.
            VarCollection.ToWorkshop(result);
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

        public static IStatement GetStatement(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.StatementContext statementContext)
        {
            switch (statementContext)
            {
                case DeltinScriptParser.S_defineContext define    : {
                    var newVar = Var.CreateVarFromContext(VariableDefineType.Scoped, script, translateInfo, define.define());
                    newVar.Finalize(scope);
                    return new DefineAction(newVar);
                }
                case DeltinScriptParser.S_methodContext method    : return new CallMethodAction(script, translateInfo, scope, method.method(), false);
                case DeltinScriptParser.S_varsetContext varset    : return new SetVariableAction(script, translateInfo, scope, varset.varset());
                case DeltinScriptParser.S_exprContext s_expr      : {

                    var expr = GetExpression(script, translateInfo, scope, s_expr.expr());
                    if (expr is ExpressionTree == false || ((ExpressionTree)expr)?.Result is IStatement == false)
                    {
                        if (expr != null)
                            script.Diagnostics.Error("Expressions can't be used as statements.", DocRange.GetRange(statementContext));
                        return null;
                    }
                    else return (ExpressionTree)expr;
                }
                case DeltinScriptParser.S_ifContext s_if          : return new IfAction(script, translateInfo, scope, s_if.@if());
                case DeltinScriptParser.S_whileContext s_while    : return new WhileAction(script, translateInfo, scope, s_while.@while());
                case DeltinScriptParser.S_forContext s_for        : return new ForAction(script, translateInfo, scope, s_for.@for());
                case DeltinScriptParser.S_foreachContext s_foreach: return new ForeachAction(script, translateInfo, scope, s_foreach.@foreach());
                case DeltinScriptParser.S_returnContext s_return  : return new ReturnAction(script, translateInfo, scope, s_return.@return());
                case DeltinScriptParser.S_deleteContext s_delete  : return new DeleteAction(script, translateInfo, scope, s_delete.delete());
                default: throw new Exception($"Could not determine the statement type '{statementContext.GetType().Name}'.");
            }
        }

        public static IExpression GetExpression(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.ExprContext exprContext, bool selfContained = true)
        {
            switch (exprContext)
            {
                case DeltinScriptParser.E_numberContext number: return new NumberAction(script, number.number());
                case DeltinScriptParser.E_trueContext @true: return new BoolAction(script, true);
                case DeltinScriptParser.E_falseContext @false: return new BoolAction(script, false);
                case DeltinScriptParser.E_nullContext @null: return new NullAction();
                case DeltinScriptParser.E_stringContext @string: return new StringAction(script, @string.@string());
                case DeltinScriptParser.E_formatted_stringContext formattedString: return new StringAction(script, translateInfo, scope, formattedString.formatted_string());
                case DeltinScriptParser.E_variableContext variable: {
                    string variableName = variable.PART().GetText();

                    var type = translateInfo.GetCodeType(variableName, null, null);
                    if (type != null)
                    {
                        if (selfContained)
                            script.Diagnostics.Error("Types can't be used as expressions.", DocRange.GetRange(variable));

                        return type;
                    }

                    IScopeable element = scope.GetVariable(variableName, script.Diagnostics, DocRange.GetRange(variable));
                    if (element == null)
                        return null;

                    if (element is Var)
                    {
                        Var var = (Var)element;
                        var.Call(script, DocRange.GetRange(variable));
                        return var;
                    }
                    else if (element is ScopedEnumMember) return (ScopedEnumMember)element;
                    else throw new NotImplementedException();
                }
                case DeltinScriptParser.E_methodContext method: return new CallMethodAction(script, translateInfo, scope, method.method(), true);
                case DeltinScriptParser.E_new_objectContext newObject: return new CreateObjectAction(script, translateInfo, scope, newObject.create_object());
                case DeltinScriptParser.E_expr_treeContext exprTree: return new ExpressionTree(script, translateInfo, scope, exprTree);
                case DeltinScriptParser.E_array_indexContext arrayIndex: return new ValueInArrayAction(script, translateInfo, scope, arrayIndex);
                case DeltinScriptParser.E_create_arrayContext createArray: return new CreateArrayAction(script, translateInfo, scope, createArray.createarray());
                case DeltinScriptParser.E_expr_groupContext group: return GetExpression(script, translateInfo, scope, group.exprgroup().expr());
                case DeltinScriptParser.E_type_convertContext typeConvert: return new TypeConvertAction(script, translateInfo, scope, typeConvert.typeconvert());
                case DeltinScriptParser.E_notContext not: return new NotAction(script, translateInfo, scope, not.expr());
                case DeltinScriptParser.E_inverseContext inverse: return new InverseAction(script, translateInfo, scope, inverse.expr());
                case DeltinScriptParser.E_op_1Context             op1: return new OperatorAction(script, translateInfo, scope, op1);
                case DeltinScriptParser.E_op_2Context             op2: return new OperatorAction(script, translateInfo, scope, op2);
                case DeltinScriptParser.E_op_boolContext       opBool: return new OperatorAction(script, translateInfo, scope, opBool);
                case DeltinScriptParser.E_op_compareContext opCompare: return new OperatorAction(script, translateInfo, scope, opCompare);
                default: throw new Exception($"Could not determine the expression type '{exprContext.GetType().Name}'.");
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
}