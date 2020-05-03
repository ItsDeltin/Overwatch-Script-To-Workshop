using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Parse.Functions;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class MacroVar : IVariable, IExpression, ICallable, IApplyBlock
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; private set; }

        public MethodAttributes Attributes { get; } = new MethodAttributes();
        private MethodAttributeHandler[] attributes;


        public LanguageServer.Location DefinedAt { get; }
        public bool Static { get; private set; }
        public bool WholeContext => true;

        public IExpression Expression { get; private set; }
        public CodeType ReturnType { get; private set; }

        private DeltinScriptParser.ExprContext ExpressionToParse { get; }
        private Scope scope { get; }
        private ParseInfo parseInfo { get; }

        public CallInfo CallInfo { get; }

        private DeltinScriptParser.Define_macroContext context;


        public MacroVar(ParseInfo parseInfo, Scope objectScope, Scope staticScope, DeltinScriptParser.Define_macroContext macroContext, CodeType returnType)
        {
            context = macroContext;

            GetAttributes(macroContext);

            Attributes.ContainingType = (Static ? staticScope : objectScope).This;

            Name = macroContext.name.Text;
            //AccessLevel = macroContext.accessor().GetAccessLevel();
            DefinedAt = new Location(parseInfo.Script.Uri, DocRange.GetRange(macroContext.name));
            CallInfo = new CallInfo(this, parseInfo.Script);
            //Static = macroContext.STATIC() != null;

            ReturnType = returnType;
            ExpressionToParse = macroContext.expr();

            scope = Static ? staticScope : objectScope;
            this.parseInfo = parseInfo;

            //SetupBlock();

            
            //scope.AddMethod(this.ToDefinedMacro(), parseInfo.Script.Diagnostics, DocRange.GetRange(macroContext.name));
            scope.AddMacro(this, parseInfo.Script.Diagnostics, DocRange.GetRange(macroContext.name), !Attributes.Override);
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);
            parseInfo.Script.AddHover(DocRange.GetRange(macroContext.name), GetLabel(true));
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.Variable, DefinedAt.range));

            DocRange nameRange = DocRange.GetRange(context.name);

            if (Attributes.Override)
            {
                MacroVar overriding = (MacroVar)objectScope.GetMacroOverload(Name, DefinedAt);

                if(overriding == this) {
                    parseInfo.Script.Diagnostics.Error("Overriding itself!", nameRange);
                }

                // No method with the name and parameters found.
                if (overriding == null) parseInfo.Script.Diagnostics.Error("Could not find a macro to override.", nameRange);
                else if (!overriding.Attributes.IsOverrideable) parseInfo.Script.Diagnostics.Error("The specified method is not marked as virtual.", nameRange);
                else overriding.Attributes.AddMacroOverride(this);

                if (overriding != null && overriding.DefinedAt != null)
                {
                    // Make the override keyword go to the base method.
                    parseInfo.Script.AddDefinitionLink(
                        attributes.First(at => at.Type == MethodAttributeType.Override).Range,
                        overriding.DefinedAt
                    );
                }
            }
        }

        public void SetupParameters() {}

        public void SetupBlock()
        {
            if (ExpressionToParse != null) Expression = parseInfo.SetCallInfo(CallInfo).GetExpression(scope.Child(), ExpressionToParse);
            foreach (var listener in listeners) listener.Applied();
        }

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            actionSet = actionSet.New(actionSet.IndexAssigner.CreateContained());

            return MacroBuilder.Call(this, actionSet);
        }

        public Scope ReturningScope() => ReturnType?.GetObjectScope() ?? parseInfo.TranslateInfo.PlayerVariableScope;

        public CodeType Type() => ReturnType;

        public void Call(ParseInfo parseInfo, DocRange callRange)
        {
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            parseInfo.Script.AddHover(callRange, GetLabel(true));
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, new Location(parseInfo.Script.Uri, callRange));
        }

        public CompletionItem GetCompletion()
        {
            return new CompletionItem() {
                Label = Name,
                Kind = CompletionItemKind.Property
            };
        }

        public string GetLabel(bool markdown)
        {
            string name = ReturnType?.Name ?? "define" + " " + Name;

            if (markdown) return HoverHandler.Sectioned(name, null);
            else return name;
        }

        private List<IOnBlockApplied> listeners = new List<IOnBlockApplied>();
        public void OnBlockApply(IOnBlockApplied onBlockApplied)
        {
            listeners.Add(onBlockApplied);
        }

        public DefinedMacro ToDefinedMacro()
        {
            return new DefinedMacro(parseInfo, scope, scope, context, ReturnType, false);
        }


        private void GetAttributes(DeltinScriptParser.Define_macroContext context)
        {
            // method_attributes will ne null if there are no attributes.
            if (context.method_attributes() == null) return;

            int numberOfAttributes = context.method_attributes().Length;
            attributes = new MethodAttributeHandler[numberOfAttributes];

            // Loop through all attributes.
            for (int i = 0; i < numberOfAttributes; i++)
            {
                var newAttribute = new MethodAttributeHandler(context.method_attributes(i));
                attributes[i] = newAttribute;

                bool wasCopy = false;

                // If the attribute already exists, syntax error.
                for (int c = i - 1; c >= 0; c--)
                    if (attributes[c].Type == newAttribute.Type)
                    {
                        newAttribute.Copy(parseInfo.Script.Diagnostics);
                        wasCopy = true;
                        break;
                    }

                // Additonal syntax errors. Only throw if the attribute is not a copy.
                if (!wasCopy)
                {
                    // Virtual attribute on a static method (static attribute was first.)
                    if (Static && newAttribute.Type == MethodAttributeType.Virtual)
                        parseInfo.Script.Diagnostics.Error("Static macros cannot be virtual.", newAttribute.Range);

                    // Static attribute on a virtual method (virtual attribute was first.)
                    if (Attributes.Virtual && newAttribute.Type == MethodAttributeType.Static)
                        parseInfo.Script.Diagnostics.Error("Virtual macros cannot be static.", newAttribute.Range);
                }

                // Apply the attribute.
                switch (newAttribute.Type)
                {
                    // Apply accessor
                    case MethodAttributeType.Accessor: AccessLevel = newAttribute.AttributeContext.accessor().GetAccessLevel(); break;

                    // Apply static
                    case MethodAttributeType.Static: Static = true; break;

                    // Apply virtual
                    case MethodAttributeType.Virtual: Attributes.Virtual = true; break;

                    // Apply override
                    case MethodAttributeType.Override: Attributes.Override = true; break;

                    // Apply Recursive
                    case MethodAttributeType.Recursive: parseInfo.Script.Diagnostics.Error("Macros cannot be recursive.", newAttribute.Range); break;
                }
            }
        }

    }
}