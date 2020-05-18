using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedType : ClassType
    {
        public Location DefinedAt { get; }

        private readonly ParseInfo parseInfo;
        private readonly DeltinScriptParser.Type_defineContext typeContext;
        private readonly List<Var> staticVariables = new List<Var>();

        public DefinedType(ParseInfo parseInfo, Scope scope, DeltinScriptParser.Type_defineContext typeContext) : base(typeContext.name.Text)
        {
            this.typeContext = typeContext;
            this.parseInfo = parseInfo;

            if (parseInfo.TranslateInfo.Types.IsCodeType(Name))
                parseInfo.Script.Diagnostics.Error($"A type with the name '{Name}' already exists.", DocRange.GetRange(typeContext.name));
            
            DefinedAt = new LanguageServer.Location(parseInfo.Script.Uri, DocRange.GetRange(typeContext.name));
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);
        }

        public override void ResolveElements()
        {
            if (elementsResolved) return;

            // Get the type being extended.
            if (typeContext.TERNARY_ELSE() != null)
            {
                // If there is no type name, error.
                if (typeContext.extends == null)
                    parseInfo.Script.Diagnostics.Error("Expected type name.", DocRange.GetRange(typeContext.TERNARY_ELSE()));
                else
                {
                    // Get the type being inherited.
                    CodeType inheriting = parseInfo.TranslateInfo.Types.GetCodeType(typeContext.extends.Text, parseInfo.Script.Diagnostics, DocRange.GetRange(typeContext.extends));

                    // GetCodeType will return null if the type is not found.
                    if (inheriting != null)
                    {
                        inheriting.Call(parseInfo, DocRange.GetRange(typeContext.extends));

                        Inherit(inheriting, parseInfo.Script.Diagnostics, DocRange.GetRange(typeContext.extends));
                        (Extends as ClassType)?.ResolveElements();
                    }
                }
            }

            base.ResolveElements();

            // Give DefinedMethod and GetMacro a scope to use in case of the static attribute.
            foreach (var definedMethod in typeContext.define_method())
            {
                var newMethod = new DefinedMethod(parseInfo, operationalScope, staticScope, definedMethod, this, true);

                // Copy to serving scopes.
                if (newMethod.Static) operationalScope.CopyMethod(newMethod);
                else serveObjectScope.CopyMethod(newMethod);
            }

            // Get the macros.
            foreach (var macroContext in typeContext.define_macro())
            {
                var newMacro = parseInfo.GetMacro(operationalScope, staticScope, macroContext);

                // Copy to serving scopes.
                if (newMacro is IMethod asMethod)
                {
                    if (newMacro.Static) operationalScope.CopyMethod(asMethod);
                    else serveObjectScope.CopyMethod(asMethod);
                }
                else
                {
                    if (newMacro.Static) operationalScope.CopyVariable((IVariable)newMacro);
                    else serveObjectScope.CopyVariable((IVariable)newMacro);
                }
            }

            // Get the variables defined in the type.
            foreach (var definedVariable in typeContext.define())
            {
                Var newVar = new ClassVariable(operationalScope, staticScope, new DefineContextHandler(parseInfo, definedVariable));

                // Copy to serving scopes.
                if (!newVar.Static)
                {
                    ObjectVariables.Add(new ObjectVariable(newVar));
                    serveObjectScope.CopyVariable(newVar);
                }
                // Add to static scope.
                else
                {
                    staticVariables.Add(newVar);
                    staticScope.CopyVariable(newVar);
                    operationalScope.CopyVariable(newVar);
                }
            }

            // Get the constructors.
            if (typeContext.constructor().Length > 0)
            {
                Constructors = new Constructor[typeContext.constructor().Length];
                for (int i = 0; i < Constructors.Length; i++)
                {
                    Constructors[i] = new DefinedConstructor(parseInfo, operationalScope, this, typeContext.constructor(i));
                }
            }
            else
            {
                // If there are no constructors, create a default constructor.
                Constructors = new Constructor[] {
                    new Constructor(this, new Location(parseInfo.Script.Uri, DocRange.GetRange(typeContext.name)), AccessLevel.Public)
                };
            }

            // If the extend token exists, add completion that only contains all extendable classes.
            if (typeContext.TERNARY_ELSE() != null)
                parseInfo.Script.AddCompletionRange(new CompletionRange(
                    // Get the completion items of all types.
                    parseInfo.TranslateInfo.Types.AllTypes
                        .Where(t => t is ClassType ct && ct.CanBeExtended)
                        .Select(t => t.GetCompletion())
                        .ToArray(),
                    // Get the completion range.
                    DocRange.GetRange(typeContext.TERNARY_ELSE(), parseInfo.Script.NextToken(typeContext.TERNARY_ELSE())),
                    // This completion takes priority.
                    CompletionRangeKind.ClearRest
                ));
            parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, parseInfo, CodeLensSourceType.Type, DefinedAt.range));
        }

        protected override void BaseScopes(string scopeName)
        {
            Scope global = parseInfo.TranslateInfo.GlobalScope;

            staticScope = global.Child(scopeName);
            operationalScope = global.Child(scopeName);
            serveObjectScope = new Scope(scopeName);
        }

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            if (workshopInitialized) return;
            base.WorkshopInit(translateInfo);

            foreach (Var staticVariable in staticVariables)
                DefineAction.Assign(translateInfo.InitialGlobal.ActionSet, staticVariable);
        }

        protected override void New(ActionSet actionSet, NewClassInfo newClassInfo)
        {
            // Run the constructor.
            AddObjectVariablesToAssigner((Element)newClassInfo.ObjectReference.GetVariable(), actionSet.IndexAssigner);
            newClassInfo.Constructor.Parse(actionSet.New((Element)newClassInfo.ObjectReference.GetVariable()), newClassInfo.ConstructorValues, null);
        }

        public override void Call(ParseInfo parseInfo, DocRange callRange)
        {
            base.Call(parseInfo, callRange);
            parseInfo.Script.AddDefinitionLink(callRange, DefinedAt);
            AddLink(new LanguageServer.Location(parseInfo.Script.Uri, callRange));
        }
        public void AddLink(LanguageServer.Location location)
        {
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, location);
        }
    }    
}