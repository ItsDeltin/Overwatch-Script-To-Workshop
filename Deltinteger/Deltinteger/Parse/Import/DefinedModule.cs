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
        public string Name { get; }
        List<DefinedModule> Children { get; } = new List<DefinedModule>();

        List<RuleAction> Rules { get; } = new List<RuleAction>();

        List<DefinedFunction> Functions { get; } = new List<DefinedFunction>();
        List<IVariable> Variables { get; } = new List<IVariable>();
        List<DefinedType> Classes { get; } = new List<DefinedType>();

        List<Deltinteger.Compiler.SyntaxTree.Import> Imports { get; } = new List<Deltinteger.Compiler.SyntaxTree.Import>();


        Scope ModuleScope { get; }

        public CodeType CodeType => Type();

        public bool Static => true;

        public bool WholeContext => false;

        string INamed.Name => Name;

        public Deltinteger.LanguageServer.Location DefinedAt { get; }

        public AccessLevel AccessLevel { get; }

        public DefinedModule(ParseInfo parseInfo, Scope scope, ModuleContext modContext)
        {
            ModuleScope = scope;
            DefinedAt = new Deltinteger.LanguageServer.Location(parseInfo.Script.Uri, modContext.Name.Range);

            AccessLevel = modContext.Attributes.GetAccessLevel();

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
                    ModuleScope.AddMethod(new DefinedMacro(parseInfo, ModuleScope, ModuleScope, macroContext, type), parseInfo.Script.Diagnostics, macroContext.Range);
                } else if(dec is FunctionContext functionContext)
                {
                    Functions.Add(new DefinedMethod(parseInfo, null, ModuleScope, functionContext, type));
                } else if(dec is MacroVarDeclaration macroVarDec)
                {
                    var macro = new MacroVar(parseInfo, ModuleScope, ModuleScope, macroVarDec, type);
                    parseInfo.TranslateInfo.ApplyBlock(macro);
                } 
                else if(dec is VariableDeclaration varDec)
                {
                    Var variable = new RuleLevelVariable(ModuleScope, new DefineContextHandler(parseInfo, varDec));
                    ModuleScope.CopyVariable(variable);
                    Variables.Add(variable);
                }
            }
        }

        public Scope ReturningScope() => ModuleScope;

        public CodeType Type() => new ModuleType(this);

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            
            return Type().Parse(actionSet);
        }

        public CompletionItem GetCompletion()
        {
            throw new NotImplementedException();
        }
    }

    class ModuleType : CodeType
    {
        DefinedModule Module { get; }

        public override bool IsConstant() => true;


        public ModuleType(DefinedModule module) : base(module.Name)
        {
            Module = module;
        }

        public override CompletionItem GetCompletion()
        {
            throw new NotImplementedException();
        }

        public override Scope ReturningScope() => Module.ReturningScope();
    }
}
