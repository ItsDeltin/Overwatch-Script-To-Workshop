using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedClassInitializer : ClassInitializer, IResolveElements
    {
        public override int GenericsCount => _anonymousTypes.Count;
        private readonly ParseInfo _parseInfo;
        private readonly ClassContext _typeContext;
        private readonly Location _definedAt;
        private readonly List<Var> _staticVariables = new List<Var>();
        private readonly Scope _scope;
        private Constructor[] _constructors;
        private readonly List<AnonymousType> _anonymousTypes = new List<AnonymousType>();

        public DefinedClassInitializer(ParseInfo parseInfo, Scope scope, ClassContext typeContext) : base(typeContext.Identifier.GetText())
        {
            _parseInfo = parseInfo;
            _typeContext = typeContext;
            _definedAt = parseInfo.Script.GetLocation(typeContext.Identifier.GetRange(typeContext.Range));
            _scope = scope;

            parseInfo.TranslateInfo.AddWorkshopInit(this);
            parseInfo.TranslateInfo.AddResolve(this);

            /*
            this._typeContext = typeContext;
            this._parseInfo = parseInfo;

            if (parseInfo.TranslateInfo.Types.IsCodeType(Name))
                parseInfo.Script.Diagnostics.Error($"A type with the name '{Name}' already exists.", typeContext.Identifier.Range);
            
            DefinedAt = new LanguageServer.Location(parseInfo.Script.Uri, typeContext.Identifier.GetRange(typeContext.Range));
            parseInfo.TranslateInfo.GetComponent<SymbolLinkComponent>().AddSymbolLink(this, DefinedAt, true);
            */
        }

        /*
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
        */

        public override bool BuiltInTypeMatches(Type type) => false;

        public override void ResolveElements()
        {
            if (_elementsResolved) return;

            // Get the type being extended.
            // This is an array for future interface support.
            if (_typeContext.Inheriting.Count > 0)
            {
                var inheritContext = _typeContext.Inheriting[0];

                // Get the type being inherited.
                var inheriting = TypeFromContext.GetCodeTypeFromContext(_parseInfo, _scope, inheritContext);

                // GetCodeType will return null if the type is not found.
                if (inheriting != null)
                {
                    inheriting.Call(_parseInfo, inheritContext.Range);

                    if (CodeTypeHelpers.CanExtend(WorkingInstance, inheriting, _parseInfo.Script.Diagnostics, inheritContext.Range))
                    {
                        Extends = inheriting;
                        Extends.ResolveElements();
                    }
                }
            }

            base.ResolveElements();

            // Get the generics.
            for (int i = 0; i <  _typeContext.Generics.Count; i++)
            {
                var anonymousType = new AnonymousType(_typeContext.Generics[i].GetText(), i);
                _anonymousTypes.Add(anonymousType);
                OperationalScope.AddType(new GenericCodeTypeInitializer(anonymousType));
            }

            // Get the declarations.
            foreach (var declaration in _typeContext.Declarations)
            {
                IScopeable scopeable;

                // Function
                if (declaration is FunctionContext function)
                    scopeable = new DefinedMethod(_parseInfo, OperationalScope, StaticScope, function, WorkingInstance);
                // Macro function
                else if (declaration is MacroFunctionContext macroFunction)
                    scopeable = _parseInfo.GetMacro(OperationalScope, StaticScope, macroFunction);
                // Variable
                else if (declaration is VariableDeclaration variable)
                    scopeable = new ClassVariable(OperationalScope, StaticScope, new DefineContextHandler(_parseInfo, variable)).GetVar();
                // Macro variable
                else if (declaration is MacroVarDeclaration macroVar)
                    scopeable = _parseInfo.GetMacro(OperationalScope, StaticScope, macroVar);
                // Unknown
                else throw new NotImplementedException(declaration.GetType().ToString());

                // Add the object variable if it is an IIndexReferencer.
                if (scopeable is IIndexReferencer referencer)
                    AddObjectVariable(referencer);
                
                // Copy to scopes.
                // Method copy
                if (scopeable is IMethod method)
                {
                    if (method.Static) OperationalScope.CopyMethod(method);
                    else ServeObjectScope.CopyMethod(method);
                }
                // Variable copy
                else if (scopeable is IVariable variable)
                {
                    if (scopeable.Static) OperationalScope.CopyVariable(variable);
                    else ServeObjectScope.CopyVariable(variable);
                }
                else throw new NotImplementedException();
            }

            // Get the constructors.
            if (_typeContext.Constructors.Count > 0)
            {
                _constructors = new Constructor[_typeContext.Constructors.Count];
                for (int i = 0; i < _constructors.Length; i++)
                    _constructors[i] = new DefinedConstructor(_parseInfo, OperationalScope, WorkingInstance, _typeContext.Constructors[i]);
            }
            else
            {
                // If there are no constructors, create a default constructor.
                _constructors = new Constructor[] {
                    new Constructor(WorkingInstance, _definedAt, AccessLevel.Public)
                };
            }

            // TODO: update these
            // If the extend token exists, add completion that only contains all extendable classes.
            // if (_typeContext.InheritToken != null)
            //     _parseInfo.Script.AddCompletionRange(new CompletionRange(
            //         // Get the completion items of all types.
            //         _parseInfo.TranslateInfo.Types.AllTypes
            //             .Where(t => t is ClassType ct && ct.CanBeExtended)
            //             .Select(t => t.GetCompletion())
            //             .ToArray(),
            //         // Get the completion range.
            //         _typeContext.InheritToken.Range.End + _parseInfo.Script.NextToken(_typeContext.InheritToken).Range.Start,
            //         // This completion takes priority.
            //         CompletionRangeKind.ClearRest
            //     ));
            // _parseInfo.Script.AddCodeLensRange(new ReferenceCodeLensRange(this, _parseInfo, CodeLensSourceType.Type, _definedAt.range));
        }

        protected override void BaseScopes(string scopeName)
        {
            Scope classContainer = _parseInfo.TranslateInfo.GlobalScope.Child();
            classContainer.CatchConflict = true;

            StaticScope = classContainer.Child(scopeName);
            OperationalScope = classContainer.Child(scopeName);
            ServeObjectScope = new Scope(scopeName);
        }
        
        public override CodeType GetInstance() => new DefinedClass(_parseInfo, this, _anonymousTypes.ToArray());
        public override CodeType GetInstance(GetInstanceInfo instanceInfo) => new DefinedClass(_parseInfo, this, instanceInfo.Generics);
    }
}