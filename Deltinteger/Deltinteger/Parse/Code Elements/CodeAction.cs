using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Elements;
using Antlr4.Runtime;

namespace Deltin.Deltinteger.Parse
{
    class DeltinScript
    {
        public Diagnostics Diagnostics { get; }
        private List<ScriptFile> ScriptFiles { get; }

        public DeltinScript(Diagnostics diagnostics, ScriptFile rootRuleset)
        {
            Diagnostics = diagnostics;
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
                    GetImportedFile(importFileContext, scriptFile.File, importedFiles);
            }
        }

        void GetImportedFile(DeltinScriptParser.Import_fileContext importFileContext, string importer, List<string> importedFiles)
        {
            Importer importFile = new Importer(Diagnostics, importedFiles, importFileContext.STRINGLITERAL().GetText(), importer, new Location(importer, DocRange.GetRange(importFileContext)));
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
            foreach (ScriptFile script in ScriptFiles)
            {
                // Get the rules
                foreach (var ruleContext in script.Context.ow_rule())
                    translatedRules.Add(new RuleAction(script, global, ruleContext));
            }
        }
    }

    public abstract class CodeAction
    {
        public static IStatement GetStatement(ScriptFile script, Scope scope, DeltinScriptParser.StatementContext statementContext)
        {
            if (statementContext.define() != null) return new DefineAction(script, scope, statementContext.define());
            if (statementContext.method() != null) return new CallMethodAction(script, scope, statementContext.method());

            throw new Exception("Could not determine the statement type.");
        }

        public static IExpression GetExpression(ScriptFile script, Scope scope, DeltinScriptParser.ExprContext exprContext)
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
                    IScopeable element = scope.GetInScope(variableName);

                    if (element == null)
                        script.Diagnostics.Error(variableName + " does not exist in the current scope.", DocRange.GetRange(exprContext));

                    else if (element is Var == false)
                        script.Diagnostics.Error(variableName + " is a " + element.ScopeableType + ", not a variable.", DocRange.GetRange(exprContext));
                    
                    else
                    {
                        Var var = (Var)element;
                        return var.Call(new Location(script.File, DocRange.GetRange(exprContext)));
                    }
                }
                // Method
                if (exprContext.method() != null) return new CallMethodAction(script, scope, exprContext.method());
            }

            throw new Exception("Could not determine the expression type.");
        }
    }
}