using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.WorkshopWiki;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public abstract class DefinedFunction : IMethod, ICallable, IParameterCallable
    {
        public string ScopeableType { get; } = "method";
        public string Name { get; }
        public CodeType ReturnType { get; protected set; }
        public CodeParameter[] Parameters { get; private set; }
        public AccessLevel AccessLevel { get; protected set; }
        public Location DefinedAt { get; }
        public bool WholeContext { get; } = true;

        public WikiMethod Wiki { get; }

        // ICallable
        private List<Location> CalledFrom { get; } = new List<Location>();

        private DeltinScript translateInfo { get; }
        protected Scope methodScope { get; }
        protected Var[] ParameterVars { get; private set; }
        
        public DefinedFunction(DeltinScript translateInfo, string name, Location definedAt)
        {
            Name = name;
            DefinedAt = definedAt;
            this.translateInfo = translateInfo;
            methodScope = translateInfo.GlobalScope.Child();
        }

        protected static CodeType GetCodeType(ScriptFile script, DeltinScript translateInfo, string name, DocRange range)
        {
            if (name == null)
                return null;
            else
                return translateInfo.GetCodeType(name, script.Diagnostics, range);
        }

        protected void SetupParameters(ScriptFile script, DeltinScriptParser.SetParametersContext context)
        {
            var parameterInfo = CodeParameter.GetParameters(script, translateInfo, methodScope, context);
            Parameters = parameterInfo.Parameters;
            ParameterVars = parameterInfo.Variables;

            // todo: remove i guess
            // if (context == null) 
            // {
            //     Parameters = new CodeParameter[0];
            //     return;
            // }

            // Parameters = new CodeParameter[context.define().Length];
            // ParameterVars = new Var[Parameters.Length];
            // for (int i = 0; i < context.define().Length; i++)
            // {
            //     var newVar = Var.CreateVarFromContext(VariableDefineType.Parameter, script, translateInfo, context.define(i));
            //     newVar.Finalize(methodScope);
            //     ParameterVars[i] = newVar;
            //     Parameters[i] = new CodeParameter(context.define(i).name.Text, newVar.CodeType);
            // }
        }

        public void Call(Location calledFrom)
        {
            CalledFrom.Add(calledFrom);
        }

        public string GetLabel(bool markdown)
        {
            // TODO: this
            throw new NotImplementedException();
        }

        public abstract IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues);

        public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = Name,
                Kind = CompletionItemKind.Method
            };
        }
    }

    public class DefinedMethod : DefinedFunction
    {
        public bool IsRecursive { get; private set; }
        private BlockAction block { get; set; }

        public DefinedMethod(ScriptFile script, DeltinScript translateInfo, DeltinScriptParser.Define_methodContext context)
            : base(translateInfo, context.name.Text, new Location(script.Uri, DocRange.GetRange(context)))
        {
            // Check if recursion is enabled.
            IsRecursive = context.RECURSIVE() != null;

            // Get the type.
            if (context.type != null)
                ReturnType = GetCodeType(script, translateInfo, context.type.Text, DocRange.GetRange(context.type));

            // Get the access level.
            AccessLevel = context.accessor().GetAccessLevel();

            // Setup the parameters and parse the block.
            SetupParameters(script, context.setParameters());
            block = new BlockAction(script, translateInfo, methodScope, context.block());
        }

        override public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            for (int i = 0; i < ParameterVars.Length; i++)
            {
                actionSet.IndexAssigner.Add(actionSet.VarCollection, ParameterVars[i], actionSet.IsGlobal, parameterValues[i]);

                // todo: improve this
                if (actionSet.IndexAssigner[ParameterVars[i]] is IndexReference)
                    actionSet.AddAction(
                        ((IndexReference)actionSet.IndexAssigner[ParameterVars[i]]).SetVariable((Element)parameterValues[i])
                    );
            }

            block.Translate(actionSet);

            // todo: return statement and stuff
            return new V_Null();
        }
    }

    public class DefinedMacro : DefinedFunction
    {
        public IExpression Expression { get; private set; }

        public DefinedMacro(ScriptFile script, DeltinScript translateInfo, DeltinScriptParser.Define_macroContext context)
            : base(translateInfo, context.name.Text, new Location(script.Uri, DocRange.GetRange(context)))
        {
            AccessLevel = context.accessor().GetAccessLevel();
            SetupParameters(script, context.setParameters());

            if (context.expr() == null)
                script.Diagnostics.Error("Expected expression.", DocRange.GetRange(context.TERNARY_ELSE()));
            else
            {
                Expression = DeltinScript.GetExpression(script, translateInfo, methodScope, context.expr());
                if (Expression != null)
                    ReturnType = Expression.Type();
            }
        }

        override public IWorkshopTree Parse(ActionSet actionSet, IWorkshopTree[] parameterValues)
        {
            // TODO: fix this
            return Expression.Parse(actionSet);
            // throw new NotImplementedException();
        }
    }
}