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
        public override int GenericsCount => AnonymousTypes?.Length ?? 0;
        public Location DefinedAt { get; }
        public List<IElementProvider> DeclaredElements { get; } = new List<IElementProvider>();
        private readonly ParseInfo _parseInfo;
        private readonly ClassContext _typeContext;
        private readonly List<Var> _staticVariables = new List<Var>();
        private readonly Scope _scope;
        private IConstructorProvider<Constructor>[] _constructors;
        public AnonymousType[] AnonymousTypes { get; private set; }
        private Scope _operationalObjectScope;
        private Scope _operationalStaticScope;

        public DefinedClassInitializer(ParseInfo parseInfo, Scope scope, ClassContext typeContext) : base(typeContext.Identifier.GetText())
        {
            _parseInfo = parseInfo;
            _typeContext = typeContext;
            DefinedAt = parseInfo.Script.GetLocation(typeContext.Identifier.GetRange(typeContext.Range));
            _scope = scope;

            parseInfo.TranslateInfo.AddResolve(this);

            // Get the generics.
            AnonymousTypes = AnonymousType.GetGenerics(_typeContext.Generics);
        }

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

            if (Extends == null)
            {   
                _operationalStaticScope = _parseInfo.TranslateInfo.RulesetScope.Child(Name);
                _operationalObjectScope = _operationalStaticScope.Child(Name);
            }
            else
            {
                // TODO
                throw new NotImplementedException();
            }

            foreach (var type in AnonymousTypes)
            {
                _operationalStaticScope.AddType(new GenericCodeTypeInitializer(type));
                _operationalObjectScope.AddType(new GenericCodeTypeInitializer(type));
            }

            // Get declarations.
            foreach (var declaration in _typeContext.Declarations)
                DeclaredElements.Add(((IDefinedTypeInitializer)this).ApplyDeclaration(declaration, _parseInfo));

            WorkingInstance = GetInstance();

            // Get the constructors.
            if (_typeContext.Constructors.Count > 0)
            {
                _constructors = new IConstructorProvider<Constructor>[_typeContext.Constructors.Count];
                for (int i = 0; i < _constructors.Length; i++)
                    _constructors[i] = new DefinedConstructorProvider(_parseInfo, _operationalObjectScope, WorkingInstance, _typeContext.Constructors[i]);
            }
            else
            {
                // If there are no constructors, create a default constructor.
                _constructors = new IConstructorProvider<Constructor>[] {
                    new EmptyConstructorProvider(WorkingInstance, DefinedAt)
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
        
        public override CodeType GetInstance() => new DefinedClass(_parseInfo, this, AnonymousTypes);
        public override CodeType GetInstance(GetInstanceInfo instanceInfo) => new DefinedClass(_parseInfo, this, instanceInfo.Generics);
        public IMethod GetOverridenFunction(DeltinScript deltinScript, FunctionOverrideInfo overrideInfo) => Extends.GetVirtualFunction(deltinScript, overrideInfo.Name, overrideInfo.ParameterTypes);
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
    }
}