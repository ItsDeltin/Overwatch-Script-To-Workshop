using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class DeltinScript
    {
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

        public DeltinScript(Diagnostics diagnostics, ScriptFile rootRuleset)
        {
            Diagnostics = diagnostics;
            types.AddRange(CodeType.DefaultTypes);
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

            // Get the imported files.
            if (scriptFile.Context.import_file() != null)
            {
                List<string> importedFiles = new List<string>();
                foreach (var importFileContext in scriptFile.Context.import_file())
                    GetImportedFile(scriptFile, importFileContext, importedFiles);
            }
        }

        void GetImportedFile(ScriptFile importer, DeltinScriptParser.Import_fileContext importFileContext, List<string> importedFiles)
        {
            Importer importFile = new Importer(importer.Diagnostics, importedFiles, Extras.RemoveQuotes(importFileContext.STRINGLITERAL().GetText()), importer.Uri.AbsoluteUri, new Location(importer.Uri, DocRange.GetRange(importFileContext)));
            if (importFile.AlreadyImported) return;

            importedFiles.Add(importFile.ResultingPath);

            var cache = importFile.FileData;
            ScriptFile importedScript;

            if (cache.Update() || cache.Cache == null)
            {
                importedScript = new ScriptFile(Diagnostics, new Uri(importFile.ResultingPath), cache.Content);
                cache.Cache = importedScript;
            }
            else importedScript = (ScriptFile)cache.Cache;

            CollectScriptFiles(importedScript);
        }

        private List<RuleAction> rules { get; } = new List<RuleAction>();

        void Translate()
        {
            // Get the types
            foreach (ScriptFile script in ScriptFiles)
            foreach (var typeContext in script.Context.type_define())
            {
                var newType = new DefinedType(script, this, GlobalScope, typeContext);
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
                    RulesetScope.AddMethod(newMethod, script.Diagnostics, DocRange.GetRange(methodContext.name));
                }
                
                // Get the macros.
                foreach (var macroContext in script.Context.define_macro())
                {
                    var newMacro = new DefinedMacro(script, this, RulesetScope, macroContext);
                    RulesetScope.AddMethod(newMacro, script.Diagnostics, DocRange.GetRange(macroContext.name));
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

            // Get the rules
            foreach (ScriptFile script in ScriptFiles)
            foreach (var ruleContext in script.Context.ow_rule())
                rules.Add(new RuleAction(script, this, RulesetScope, ruleContext));
        }

        public string WorkshopCode { get; private set; }

        void ToWorkshop()
        {
            VarCollection.Setup();

            foreach (var variable in rulesetVariables)
            {
                // Assign the variable an index.
                // TODO: set initial value
                DefaultIndexAssigner.Add(VarCollection, variable, true, null);
            }

            List<Rule> ruleElements = new List<Rule>();
            foreach (var rule in rules)
            {
                var translate = new TranslateRule(rule.Script, this, rule);
                ruleElements.Add(translate.GetRule());
            }

            // Get the final workshop string.
            StringBuilder result = new StringBuilder();
            // Get the variables.
            VarCollection.ToWorkshop(result);
            result.AppendLine();

            // Get the rules.
            foreach (var rule in ruleElements)
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

        public ScriptFile ScriptFromUri(Uri uri) => ScriptFiles.FirstOrDefault(script => script.Uri == uri);

        private Dictionary<ICallable, List<Location>> callRanges { get; } = new Dictionary<ICallable, List<Location>>();
        public void AddSymbolLink(ICallable callable, Location calledFrom)
        {
            if (callable == null) throw new ArgumentNullException(nameof(callable));
            if (calledFrom == null) throw new ArgumentNullException(nameof(calledFrom));

            if (!callRanges.ContainsKey(callable)) callRanges.Add(callable, new List<Location>());
            callRanges[callable].Add(calledFrom);
        }
        public Dictionary<ICallable, List<Location>> GetSymbolLinks() => callRanges;

        public static IStatement GetStatement(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.StatementContext statementContext)
        {
            switch (statementContext)
            {
                case DeltinScriptParser.S_defineContext define    : {
                    var newVar = Var.CreateVarFromContext(VariableDefineType.Scoped, script, translateInfo, define.define());
                    newVar.Finalize(scope);
                    return new DefineAction(newVar);
                }
                case DeltinScriptParser.S_methodContext method    : return new CallMethodAction(script, translateInfo, scope, method.method());
                case DeltinScriptParser.S_varsetContext varset    : return new SetVariableAction(script, translateInfo, scope, varset.varset());
                case DeltinScriptParser.S_exprContext s_expr      : {
                    var expr = GetExpression(script, translateInfo, scope, s_expr.expr());
                    if (expr is ExpressionTree == false || ((ExpressionTree)expr)?.Result is IStatement == false)
                    {
                        if (expr != null)
                            script.Diagnostics.Error("Expressions can't be used as statements.", DocRange.GetRange(statementContext));
                        return null;
                    }
                    else return (IStatement)((ExpressionTree)expr).Result;
                }
                case DeltinScriptParser.S_ifContext s_if          : return new IfAction(script, translateInfo, scope, s_if.@if());
                case DeltinScriptParser.S_whileContext s_while    : return new WhileAction(script, translateInfo, scope, s_while.@while());
                case DeltinScriptParser.S_forContext s_for        : return new ForAction(script, translateInfo, scope, s_for.@for());
                case DeltinScriptParser.S_foreachContext s_foreach: return new ForeachAction(script, translateInfo, scope, s_foreach.@foreach());
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
                case DeltinScriptParser.E_methodContext method: return new CallMethodAction(script, translateInfo, scope, method.method());
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

        // public static bool IsExpression(DeltinScriptParser.ExprContext exprContext) => exprContext != null && exprContext.GetType() != typeof(DeltinScriptParser.ExprContext);
    
        private ClassData _classData = null;
        public ClassData SetupClasses()
        {
            // TODO: Set class indexes as empty array.
            if (_classData == null) _classData = new ClassData(VarCollection);
            return _classData;
        }
    }

    public enum AccessLevel
    {
        Public,
        Private
    }

    public class ClassData
    {
        public IndexReference ClassIndexes { get; }
        public IndexReference ClassArray { get; }

        public ClassData(VarCollection varCollection)
        {
            ClassArray = varCollection.Assign("_classArray", true, false);
            if (DefinedType.CLASS_INDEX_WORKAROUND)
                ClassIndexes = varCollection.Assign("_classIndexes", true, false);
        }
    }
}