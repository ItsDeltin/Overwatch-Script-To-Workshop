using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    public class DeltinScript
    {
        public Diagnostics Diagnostics { get; }
        private List<ScriptFile> ScriptFiles { get; } = new List<ScriptFile>();
        private List<CodeType> types { get; } = new List<CodeType>();
        public Scope PlayerVariableScope { get; private set; } = new Scope();
        public Scope GlobalScope { get; }
        public Scope RulesetScope { get; }

        public DeltinScript(Diagnostics diagnostics, ScriptFile rootRuleset)
        {
            Diagnostics = diagnostics;
            types.AddRange(CodeType.GetDefaultTypes());
            CollectScriptFiles(rootRuleset);
            GlobalScope = Scope.GetGlobalScope();
            RulesetScope = GlobalScope.Child();
            Translate();
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
            Importer importFile = new Importer(importer.Diagnostics, importedFiles, importFileContext.STRINGLITERAL().GetText(), importer.File, new Location(importer.File, DocRange.GetRange(importFileContext)));
            if (importFile.AlreadyImported) return;

            importedFiles.Add(importFile.ResultingPath);

            var cache = importFile.FileData;
            ScriptFile importedScript;

            if (cache.Update() || cache.Cache == null)
            {
                importedScript = new ScriptFile(Diagnostics, importFile.ResultingPath, cache.Content);
                cache.Cache = importedScript;
            }
            else importedScript = (ScriptFile)cache.Cache;

            CollectScriptFiles(importedScript);
        }

        private List<RuleAction> translatedRules { get; } = new List<RuleAction>();

        void Translate()
        {
            // Get the types
            foreach (ScriptFile script in ScriptFiles)
            foreach (var typeContext in script.Context.type_define())
                types.Add(new DefinedType(script, this, GlobalScope, typeContext));
            
            // Get the methods and macros
            foreach (ScriptFile script in ScriptFiles)
            {
                // Get the methods.
                foreach (var methodContext in script.Context.define_method())
                {
                    var newMethod = new DefinedMethod(script, this, methodContext);
                    RulesetScope.AddMethod(newMethod, script.Diagnostics, DocRange.GetRange(methodContext.name));
                }
                
                // Get the macros.
                foreach (var macroContext in script.Context.define_macro())
                {
                    var newMacro = new DefinedMacro(script, this, macroContext);
                    RulesetScope.AddMethod(newMacro, script.Diagnostics, DocRange.GetRange(macroContext.name));
                }
            }

            // Get the defined variables.
            foreach (ScriptFile script in ScriptFiles)
            foreach (var varContext in script.Context.define())
            {
                var newVar = Var.CreateVarFromContext(VariableDefineType.RuleLevel, script, this, varContext);
                newVar.Finalize(RulesetScope);
                PlayerVariableScope.AddVariable(newVar, null, null);
            }

            // Get the rules
            foreach (ScriptFile script in ScriptFiles)
            foreach (var ruleContext in script.Context.ow_rule())
                translatedRules.Add(new RuleAction(script, this, RulesetScope, ruleContext));
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
    }

    public abstract class CodeAction
    {
        public static IStatement GetStatement(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.StatementContext statementContext)
        {
            if (statementContext.define() != null)
            {
                var newVar = Var.CreateVarFromContext(VariableDefineType.Scoped, script, translateInfo, statementContext.define());
                newVar.Finalize(scope);
                return new DefineAction(newVar);
            }
            if (statementContext.method() != null) return new CallMethodAction(script, translateInfo, scope, statementContext.method());
            if (statementContext.varset() != null) return new SetVariableAction(script, translateInfo, scope, statementContext.varset());

            throw new Exception("Could not determine the statement type.");
        }

        public static IExpression GetExpression(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.ExprContext exprContext)
        {
            if (exprContext.ChildCount == 1)
            {
                // Number
                if (exprContext.number() != null) return new NumberAction(script, exprContext.number());
                // True/false
                if (exprContext.@true()  != null) return new BoolAction  (script, true);
                if (exprContext.@false() != null) return new BoolAction  (script, false);
                // Variable
                if (exprContext.PART()   != null)
                {
                    string variableName = exprContext.PART().GetText();

                    var type = translateInfo.GetCodeType(variableName, null, null);
                    if (type != null) return type;

                    IScopeable element = scope.GetVariable(variableName, script.Diagnostics, DocRange.GetRange(exprContext));
                    if (element == null)
                        return null;

                    if (element is Var)
                    {
                        Var var = (Var)element;
                        var.Call(new Location(script.File, DocRange.GetRange(exprContext)));
                        return var;
                    }

                    else if (element is ScopedEnumMember)
                    {
                        return (ScopedEnumMember)element;
                    }

                    else throw new NotImplementedException();
                }
                // Method
                if (exprContext.method() != null) return new CallMethodAction(script, translateInfo, scope, exprContext.method());
            }
            else if (exprContext.SEPERATOR() != null)
            {
                return new ExpressionTree(script, translateInfo, scope, exprContext);
            }
            else if (exprContext.INDEX_START() != null) return new ArrayAction(script, translateInfo, scope, exprContext);

            throw new Exception("Could not determine the expression type.");
        }
    }

    public enum AccessLevel
    {
        Public,
        Private
    }
}