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
        private List<DefineAction> ruleLevelVariables = new List<DefineAction>();
        public Scope PlayerVariableScope { get; private set; } = new Scope();

        public DeltinScript(Diagnostics diagnostics, ScriptFile rootRuleset)
        {
            Diagnostics = diagnostics;
            types.AddRange(CodeType.GetDefaultTypes());
            CollectScriptFiles(rootRuleset);
            Translate(Scope.GetGlobalScope());
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

        void Translate(Scope global)
        {
            // Get the types
            foreach (ScriptFile script in ScriptFiles)
            foreach (var typeContext in script.Context.type_define())
                types.Add(new DefinedType(script, this, global, typeContext));

            foreach (ScriptFile script in ScriptFiles)
            {
                // Get the defined variables
                foreach (var varContext in script.Context.define())
                {
                    var newVar = new DefineAction(VariableDefineType.RuleLevel, script, this, global, varContext);
                    // newVar.Var can be null because of certain syntax errors that can arise from defining a variable.
                    if (!newVar.IsGlobal && newVar.Var != null) PlayerVariableScope.In(newVar.Var);
                    ruleLevelVariables.Add(newVar);
                }

                // Get the rules
                foreach (var ruleContext in script.Context.ow_rule())
                    translatedRules.Add(new RuleAction(script, this, global, ruleContext));
            }
        }

        public CodeType GetCodeType(string name)
        {
            return types.FirstOrDefault(type => type.Name == name);
        }
        public bool IsCodeType(string name)
        {
            return GetCodeType(name) != null;
        }
    }

    public abstract class CodeAction
    {
        public static IStatement GetStatement(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.StatementContext statementContext)
        {
            if (statementContext.define() != null) return new DefineAction(VariableDefineType.Scoped, script, translateInfo, scope, statementContext.define());
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

                    var type = translateInfo.GetCodeType(variableName);
                    if (type != null) return type;

                    IScopeable element = scope.GetInScope(variableName, "variable", script.Diagnostics, DocRange.GetRange(exprContext));

                    if (element == null)
                        return null;

                    else if (element is IMethod)
                    {
                        script.Diagnostics.Error(variableName + " is a method, not a variable.", DocRange.GetRange(exprContext));
                        return null;
                    }
                    
                    else if (element is Var)
                    {
                        Var var = (Var)element;
                        return var.Call(translateInfo, new Location(script.File, DocRange.GetRange(exprContext)));
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