using Deltin.Deltinteger;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Deltin.Parse.Import
{
    class DefinedModule : IVariable, IExpression
    {
        string Name { get; }
        List<DefinedModule> Children { get; } = new List<DefinedModule>();

        List<RuleAction> Rules { get; } = new List<RuleAction>();

        List<DefinedFunction> Functions { get; } = new List<DefinedFunction>();
        List<IVariable> Variables { get; } = new List<IVariable>();
        List<DefinedType> Classes { get; } = new List<DefinedType>();


        Scope ModuleScope { get; }

        public CodeType CodeType => NullType.Instance;

        public bool Static => true;

        public bool WholeContext => false;

        string INamed.Name => Name;

        public Deltinteger.LanguageServer.Location DefinedAt { get; }

        //TODO: Public vs private modules
        public AccessLevel AccessLevel => AccessLevel.Public;

        public DefinedModule(ParseInfo parseInfo, Scope scope, RootContext modContext)
        {
            ModuleScope = scope;
            DefinedAt = new Deltinteger.LanguageServer.Location(parseInfo.Script.Uri, modContext.Name.Range);

            Name = modContext.Name?.Text;
            foreach(var mod in modContext.Modules)
            {
                Children.Add(new DefinedModule(parseInfo, ModuleScope.Child(mod.Name?.Text), mod));
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
                CodeType type = CodeType.GetCodeTypeFromContext(parseInfo, dec.Type);

                if (dec is MacroFunctionContext macroContext)
                {
                    ModuleScope.AddMethod(new DefinedMacro(parseInfo, null, ModuleScope, macroContext, type), parseInfo.Script.Diagnostics, macroContext.Range);
                } else if(dec is FunctionContext functionContext)
                {
                    Functions.Add(new DefinedMethod(parseInfo, null, ModuleScope, functionContext, type));
                } else if(dec is MacroVarDeclaration macroVarDec)
                {
                    Variables.Add(new MacroVar(parseInfo, null, ModuleScope, macroVarDec, type));
                } else if(dec is VariableDeclaration varDec)
                {
                    var info = new VarInfo(varDec.Identifier.Text, new Deltinteger.LanguageServer.Location(parseInfo.Script.Uri, varDec.Range), parseInfo);
                    
                    Var var = new Var(info);
                    ModuleScope.AddVariable(var, parseInfo.Script.Diagnostics, varDec.Range);
                    Variables.Add(var);
                }
            }
        }

        public Scope ReturningScope() => ModuleScope;

        public CodeType Type() => NullType.Instance;

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return Type().Parse(actionSet);
        }

        public CompletionItem GetCompletion()
        {
            throw new NotImplementedException();
        }
    }
}
