using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedType : ClassType
    {
        public Location DefinedAt { get; }

        private readonly ParseInfo _parseInfo;
        private readonly ClassContext _typeContext;
        private readonly List<Var> _staticVariables = new List<Var>();

        public DefinedType(ParseInfo parseInfo, Scope scope, ClassContext typeContext) : base(typeContext.Identifier.GetText())
        {
            this._typeContext = typeContext;
            this._parseInfo = parseInfo;

            if (parseInfo.TranslateInfo.Types.IsCodeType(Name))
                parseInfo.Script.Diagnostics.Error($"A type with the name '{Name}' already exists.", typeContext.Identifier.Range);

            DefinedAt = new LanguageServer.Location(parseInfo.Script.Uri, typeContext.Identifier.GetRange(typeContext.Range));
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);
        }

        public override void ResolveElements()
        {
            if (elementsResolved) return;

            // Get the type being extended.
            // This is an array for future interface support.
            if (_typeContext.Inheriting.Count > 0 && _typeContext.Inheriting[0])
            {
                var inheritToken = _typeContext.Inheriting[0];

                // Get the type being inherited.
                CodeType inheriting = _parseInfo.TranslateInfo.Types.GetCodeType(inheritToken.Text, _parseInfo.Script.Diagnostics, inheritToken.Range);

                // GetCodeType will return null if the type is not found.
                if (inheriting != null)
                {
                    inheriting.Call(_parseInfo, inheritToken.Range);

                    Inherit(inheriting, _parseInfo.Script.Diagnostics, inheritToken.Range);
                    (Extends as ClassType)?.ResolveElements();
                }
            }

            base.ResolveElements();

            // Get the declarations.
            foreach (var declaration in _typeContext.Declarations)
            {
                IScopeable scopeable;

                // Function
                if (declaration is FunctionContext function)
                    scopeable = new DefinedMethod(_parseInfo, operationalScope, staticScope, function, this);
                // Macro function
                else if (declaration is MacroFunctionContext macroFunction)
                    scopeable = _parseInfo.GetMacro(operationalScope, staticScope, macroFunction);
                // Variable
                else if (declaration is VariableDeclaration variable)
                {
                    scopeable = new ClassVariable(operationalScope, staticScope, new DefineContextHandler(_parseInfo, variable)).GetVar();
                    if (scopeable.Static) _staticVariables.Add((Var)scopeable);
                }
                // Macro variable
                else if (declaration is MacroVarDeclaration macroVar)
                    scopeable = _parseInfo.GetMacro(operationalScope, staticScope, macroVar);
                // Unknown
                else throw new NotImplementedException(declaration.GetType().ToString());

                // Add the object variable if it is an IIndexReferencer.
                if (scopeable is IIndexReferencer referencer)
                    AddObjectVariable(referencer);

                // Copy to scopes.
                // Method copy
                if (scopeable is IMethod method)
                {
                    if (method.Static) operationalScope.CopyMethod(method);
                    else serveObjectScope.CopyMethod(method);
                }
                // Variable copy
                else if (scopeable is IVariable variable)
                {
                    if (scopeable.Static) operationalScope.CopyVariable(variable);
                    else serveObjectScope.CopyVariable(variable);
                }
                else throw new NotImplementedException();
            }

            // Get the constructors.
            if (_typeContext.Constructors.Count > 0)
            {
                Constructors = new Constructor[_typeContext.Constructors.Count];
                for (int i = 0; i < Constructors.Length; i++)
                    Constructors[i] = new DefinedConstructor(_parseInfo, operationalScope, this, _typeContext.Constructors[i]);
            }
            else
            {
                // If there are no constructors, create a default constructor.
                Constructors = new Constructor[] {
                    new Constructor(this, new Location(_parseInfo.Script.Uri, DefinedAt.range), AccessLevel.Public)
                };
            }

            // If the extend token exists, add completion that only contains all extendable classes.
            if (_typeContext.InheritToken != null && !_parseInfo.Script.IsTokenLast(_typeContext.InheritToken))
                _parseInfo.Script.AddCompletionRange(new CompletionRange(
                    // Get the completion items of all types.
                    _parseInfo.TranslateInfo.Types.AllTypes
                        .Where(t => t is ClassType ct && ct.CanBeExtended)
                        .Select(t => t.GetCompletion())
                        .ToArray(),
                    // Get the completion range.
                    _typeContext.InheritToken.Range.End + _parseInfo.Script.NextToken(_typeContext.InheritToken).Range.Start,
                    // This completion takes priority.
                    CompletionRangeKind.ClearRest
                ));
            _parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, _parseInfo, CodeLensSourceType.Type, DefinedAt.range));
        }

        protected override void BaseScopes(string scopeName)
        {
            Scope classContainer = _parseInfo.TranslateInfo.GlobalScope.Child();
            classContainer.CatchConflict = true;

            staticScope = classContainer.Child(scopeName);
            operationalScope = classContainer.Child(scopeName);
            serveObjectScope = new Scope(scopeName);
        }

        public override void WorkshopInit(DeltinScript translateInfo)
        {
            if (workshopInitialized) return;
            base.WorkshopInit(translateInfo);

            foreach (Var staticVariable in _staticVariables)
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
            _parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, location);
        }
    }
}