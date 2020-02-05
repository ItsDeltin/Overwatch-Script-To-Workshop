using System;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class Var : IIndexReferencer
    {
        // IScopeable
        public string Name { get; }
        public AccessLevel AccessLevel { get; private set; }
        public Location DefinedAt { get; }
        public bool WholeContext { get; private set; }

        public CodeType CodeType { get; set; }

        // Attributes
        public VariableType VariableType { get; set; }
        public VariableDefineType DefineType { get; private set; }
        public bool InExtendedCollection { get; private set; }
        public int ID { get; private set; } = -1;
        public bool Static { get; private set; }

        private DeltinScriptParser.DefineContext context;
        private ParseInfo parseInfo { get; }
        private bool finalized;

        public IExpression InitialValue { get; private set; }

        public Var(string name, Location definedAt, ParseInfo parseInfo)
        {
            Name = name;
            DefinedAt = definedAt;
            this.parseInfo = parseInfo;
        }

        public bool Settable()
        {
            return (CodeType == null || CodeType.Constant() == TypeSettable.Normal) && (VariableType == VariableType.Global || VariableType == VariableType.Player || VariableType == VariableType.Dynamic);
        }

        // IExpression
        public Scope ReturningScope()
        {
            ThrowIfNotFinalized();
            if (CodeType == null) return parseInfo.TranslateInfo.PlayerVariableScope;
            else return CodeType.GetObjectScope();
        }
        public CodeType Type()
        {
            ThrowIfNotFinalized();
            return CodeType;
        }

        // ICallable
        public void Call(ScriptFile script, DocRange callRange)
        {
            ThrowIfNotFinalized();
            script.AddDefinitionLink(callRange, DefinedAt);
            script.AddHover(callRange, GetLabel(true));
            parseInfo.TranslateInfo.AddSymbolLink(this, new Location(script.Uri, callRange));
        }

        public static Var CreateVarFromContext(VariableDefineType defineType, ParseInfo parseInfo, DeltinScriptParser.DefineContext context)
        {
            Var newVar;
            if (context.name != null)
            {
                newVar = new Var(context.name.Text, new Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)), parseInfo);
                parseInfo.TranslateInfo.AddSymbolLink(newVar, new Location(parseInfo.Script.Uri, DocRange.GetRange(context.name)));
            }
            else
                newVar = new Var(null, new Location(parseInfo.Script.Uri, DocRange.GetRange(context)), parseInfo);

            newVar.context = context;
            newVar.InExtendedCollection = context.NOT() != null;
            newVar.DefineType = defineType;

            // Check if global/player.
            if (defineType == VariableDefineType.RuleLevel)
            {
                if (context.GLOBAL() != null)
                    newVar.VariableType = VariableType.Global;
                else if (context.PLAYER() != null)
                    newVar.VariableType = VariableType.Player;
                else
                    parseInfo.Script.Diagnostics.Error("Expected the globalvar/playervar attribute.", DocRange.GetRange(context));
            }
            else
            {
                if (context.GLOBAL() != null)
                    parseInfo.Script.Diagnostics.Error("The globalvar attribute is only allowed on variables defined at the rule level.", DocRange.GetRange(context.GLOBAL()));
                if (context.PLAYER() != null)
                    parseInfo.Script.Diagnostics.Error("The playervar attribute is only allowed on variables defined at the rule level.", DocRange.GetRange(context.PLAYER()));
            }

            // Get the ID
            if (context.id != null)
            {
                if (defineType != VariableDefineType.RuleLevel)
                    parseInfo.Script.Diagnostics.Error("Only defined variables at the rule level can be assigned an ID.", DocRange.GetRange(context.id));
                else
                {
                    newVar.ID = int.Parse(context.id.GetText());
                    parseInfo.TranslateInfo.VarCollection.Reserve(newVar.ID, newVar.VariableType == VariableType.Global, parseInfo.Script.Diagnostics, DocRange.GetRange(context.id));
                }
            }

            if (defineType == VariableDefineType.InClass)
            {
                // Get the accessor
                newVar.AccessLevel = AccessLevel.Private;
                if (context.accessor() != null)
                    newVar.AccessLevel = context.accessor().GetAccessLevel();
                // Get the static attribute.
                newVar.Static = context.STATIC() != null;

                // Syntax error if the variable has '!'.
                if (!newVar.Static && newVar.InExtendedCollection)
                    parseInfo.Script.Diagnostics.Error("Non-static type variables can not be placed in the extended collection.", DocRange.GetRange(context.NOT()));
            }
            else
            {
                // Syntax error if class only attributes is used somewhere else.
                if (context.accessor() != null)
                    parseInfo.Script.Diagnostics.Error("Only defined variables in classes can have an accessor.", DocRange.GetRange(context.accessor()));
                if (context.STATIC() != null)
                    parseInfo.Script.Diagnostics.Error("Only defined variables in classes can be static.", DocRange.GetRange(context.STATIC()));
            }

            // If the type is InClass or RuleLevel, set WholeContext to true.
            // WholeContext's value for parameters don't matter since parameters are defined at the start anyway.
            newVar.WholeContext = defineType == VariableDefineType.InClass || defineType == VariableDefineType.RuleLevel;

            // Get the type.
            newVar.CodeType = CodeType.GetCodeTypeFromContext(parseInfo, context.code_type());
            
            if (newVar.CodeType != null)
            {
                if (defineType == VariableDefineType.RuleParameter)
                {
                    if (newVar.CodeType.Constant() == TypeSettable.Constant)
                        parseInfo.Script.Diagnostics.Error($"Constant types cannot be used as a parameter's type in methods with the 'rule' attribute.", DocRange.GetRange(context.code_type()));
                }
                else if (newVar.CodeType.Constant() != TypeSettable.Normal)
                    newVar.VariableType = VariableType.ElementReference;
            }

            // Get the 'ref' attribute.
            if (context.REF() != null)
            {
                if (defineType == VariableDefineType.Parameter)
                    newVar.VariableType = VariableType.ElementReference;
                else
                    parseInfo.Script.Diagnostics.Error("'ref' attribute is not valid here.", DocRange.GetRange(context.REF()));
            }

            // Syntax error if there is an '=' but no expression.
            if (context.EQUALS() != null && context.expr() == null)
                parseInfo.Script.Diagnostics.Error("Expected expression.", DocRange.GetRange(context).end.ToRange());
            
            return newVar;
        }

        public void Finalize(Scope scope)
        {
            // Get the initial value.
            if (context?.expr() != null)
            {
                InitialValue = DeltinScript.GetExpression(parseInfo, scope, context.expr());
                if (InitialValue?.Type() != null && InitialValue.Type().Constant() == TypeSettable.Constant && CodeType != InitialValue.Type())
                    parseInfo.Script.Diagnostics.Error($"The type '{InitialValue.Type().Name}' cannot be stored.", DocRange.GetRange(context.expr()));
            }
            
            // Add the variable to the scope.
            scope.AddVariable(this, parseInfo.Script.Diagnostics, DefinedAt.range);
            finalized = true;

            parseInfo.Script.AddHover(DefinedAt.range, GetLabel(true));
        }

        private void ThrowIfNotFinalized()
        {
            if (!finalized) throw new Exception("Var not finalized.");
        }
    
        public IWorkshopTree Parse(ActionSet actionSet, bool asElement = true)
        {
            return actionSet.IndexAssigner[this].GetVariable();
        }
    
        public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = Name,
                Kind = CompletionItemKind.Variable
            };
        }

        public string GetLabel(bool markdown)
        {
            string typeName = "define";
            if (CodeType != null) typeName = CodeType.Name;
            return HoverHandler.Sectioned(typeName + " " + Name, null);
        }
    }

    public enum VariableDefineType
    {
        RuleLevel,
        Scoped,
        InClass,
        Parameter,
        RuleParameter
    }

    public enum VariableType
    {
        // Dynamic variables are either global or player, depending on the rule it is defined in.
        Dynamic,
        // Global variable.
        Global,
        // Player variable.
        Player,
        // The variable references an element.
        ElementReference
    }
}