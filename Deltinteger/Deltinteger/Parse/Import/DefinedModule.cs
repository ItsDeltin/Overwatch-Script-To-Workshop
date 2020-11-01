using Deltin.Deltinteger;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deltin.Parse.Import
{
    class DefinedModule
    {
        string Name { get; }
        List<DefinedModule> Children { get; } = new List<DefinedModule>();

        List<RuleAction> Rules { get; } = new List<RuleAction>();

        List<DefinedFunction> Functions { get; } = new List<DefinedFunction>();
        List<IVariable> Variables { get; } = new List<IVariable>();
        List<DefinedType> Classes { get; } = new List<DefinedType>();


        Scope ModuleScope { get; }

        public DefinedModule(ParseInfo parseInfo, Scope scope, RootContext modContext)
        {
            ModuleScope = scope;

            Name = modContext.Name;
            foreach(var mod in modContext.Modules)
            {
                Children.Add(new DefinedModule(parseInfo, ModuleScope.Child(mod.Name), mod));
            }
            foreach(var rule in modContext.Rules)
            {
                Rules.Add(new RuleAction(parseInfo, ModuleScope, rule));
            }
            foreach(var @class in modContext.Classes)
            {
                Classes.Add(new DefinedType(parseInfo, ModuleScope, @class));
            }
            foreach(var dec in modContext.Declarations)
            {
                if(dec is MacroFunctionContext macroContext)
                {
                    CodeType type = CodeType.GetCodeTypeFromContext(parseInfo, macroContext.Type);
                    Functions.Add(new DefinedMacro(parseInfo, null, ModuleScope, macroContext, type));
                } else if(dec is FunctionContext functionContext)
                {
                    CodeType type = CodeType.GetCodeTypeFromContext(parseInfo, functionContext.Type);
                    Functions.Add(new DefinedMethod(parseInfo, null, ModuleScope, functionContext, type));
                } else if(dec is MacroVarDeclaration macroVarDec)
                {
                    CodeType type = CodeType.GetCodeTypeFromContext(parseInfo, macroVarDec.Type);
                    Variables.Add(new MacroVar(parseInfo, null, ModuleScope, macroVarDec, type));
                } else if(dec is VariableDeclaration varDec)
                {
                    CodeType type = CodeType.GetCodeTypeFromContext(parseInfo, varDec.Type);
                    Var var = new Var(new VarInfo(varDec.Identifier.Text, new Deltinteger.LanguageServer.Location(parseInfo.Script.Uri, varDec.Range), parseInfo));
                    Variables.Add(var);
                }
            }
        }
    }
}
