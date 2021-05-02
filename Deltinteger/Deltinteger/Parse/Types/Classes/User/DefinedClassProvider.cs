using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Types.Constructors;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedClassInitializer : ClassInitializer, IResolveElements, IDefinedTypeInitializer
    {
        public Location DefinedAt { get; }
        public List<IElementProvider> DeclaredElements { get; } = new List<IElementProvider>();
        public IConstructorProvider<Constructor>[] Constructors { get; private set; }

        readonly ParseInfo _parseInfo;
        readonly ClassContext _typeContext;
        readonly List<Var> _staticVariables = new List<Var>();
        readonly Scope _scope;

        Scope _operationalObjectScope;
        Scope _operationalStaticScope;

        public DefinedClassInitializer(ParseInfo parseInfo, Scope scope, ClassContext typeContext) : base(typeContext.Identifier.GetText())
        {
            _parseInfo = parseInfo;
            _typeContext = typeContext;
            DefinedAt = parseInfo.Script.GetLocation(typeContext.Identifier.GetRange(typeContext.Range));
            _scope = scope;

            parseInfo.TranslateInfo.AddResolve(this);

            // Get the generics.
            GenericTypes = AnonymousType.GetGenerics(_typeContext.Generics, AnonymousTypeContext.Type);
        }

        public override bool BuiltInTypeMatches(Type type) => false;

        public override void ResolveElements()
        {
            if (_elementsResolved) return;

            // Setup scopes.
            _operationalStaticScope = _parseInfo.TranslateInfo.RulesetScope.Child(Name);
            _operationalObjectScope = _parseInfo.TranslateInfo.RulesetScope.Child(Name);

            // Add typeargs to scopes.
            foreach (var type in GenericTypes)
            {
                _operationalStaticScope.AddType(new GenericCodeTypeInitializer(type));
                _operationalObjectScope.AddType(new GenericCodeTypeInitializer(type));
            }

            // Get the type being extended.
            // This is an array for future interface support.
            if (_typeContext.Inheriting.Count > 0)
            {
                var inheritContext = _typeContext.Inheriting[0];

                // Get the type being inherited.
                var inheriting = TypeFromContext.GetCodeTypeFromContext(_parseInfo, _operationalStaticScope, inheritContext);

                // GetCodeType will return null if the type is not found.
                if (inheriting != null)
                {
                    inheriting.Call(_parseInfo, inheritContext.Range);

                    // Make sure the type being extended can actually be extended.
                    // TODO: update CanExtend!
                    // if (CodeTypeHelpers.CanExtend(WorkingInstance, inheriting, _parseInfo.Script.Diagnostics, inheritContext.Range))
                    // {
                        // CanExtend will return false if 'inheriting' is not a ClassType so we can safely cast here.
                        Extends = (ClassType)inheriting;
                        Extends.ResolveElements();
                    // }
                }
            }

            base.ResolveElements();

            (Extends as ClassType)?.Elements.AddToScope(_operationalStaticScope, false);
            (Extends as ClassType)?.Elements.AddToScope(_operationalObjectScope, true);

            // Get declarations.
            foreach (var declaration in _typeContext.Declarations)
                DeclaredElements.Add(((IDefinedTypeInitializer)this).ApplyDeclaration(declaration, _parseInfo));

            // Get the constructors.
            if (_typeContext.Constructors.Count > 0)
            {
                Constructors = new IConstructorProvider<Constructor>[_typeContext.Constructors.Count];
                for (int i = 0; i < Constructors.Length; i++)
                    Constructors[i] = new DefinedConstructorProvider(this, _parseInfo, _operationalObjectScope, _typeContext.Constructors[i]);
            }
            else
            {
                // If there are no constructors, create a default constructor.
                Constructors = new IConstructorProvider<Constructor>[] {
                    new EmptyConstructorProvider(DefinedAt)
                };
            }

            WorkingInstance = GetInstance();

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
        
        public override CodeType GetInstance() => new DefinedClass(_parseInfo, this, GenericTypes);
        public override CodeType GetInstance(GetInstanceInfo instanceInfo) => new DefinedClass(_parseInfo, this, instanceInfo.Generics);
        public IMethod GetOverridenFunction(DeltinScript deltinScript, FunctionOverrideInfo overrideInfo) => Extends.Elements.GetVirtualFunction(deltinScript, overrideInfo.Name, overrideInfo.ParameterTypes);
        Scope IScopeProvider.GetObjectBasedScope() => _operationalObjectScope;
        Scope IScopeProvider.GetStaticBasedScope() => _operationalStaticScope;
        void IScopeAppender.AddObjectBasedScope(IMethod function) => _operationalObjectScope.CopyMethod(function);
        void IScopeAppender.AddStaticBasedScope(IMethod function) => _operationalStaticScope.CopyMethod(function);
        void IScopeAppender.AddObjectBasedScope(IVariableInstance variable) => _operationalObjectScope.CopyVariable(variable);
        void IScopeAppender.AddStaticBasedScope(IVariableInstance variable) => _operationalStaticScope.CopyVariable(variable);

        public IVariableInstance GetOverridenVariable(string variableName)
        {
            throw new NotImplementedException();
        }

        public void AddMacro(MacroVarProvider macro)
        {
            throw new NotImplementedException();
        }

        public override int TypeArgIndexFromAnonymousType(AnonymousType anonymousType) => Array.IndexOf(GenericTypes, anonymousType);
    }
}