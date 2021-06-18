using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Parse.Types.Constructors;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedClassInitializer : ClassInitializer, IDefinedTypeInitializer, IDeclarationKey, IGetMeta
    {
        public Location DefinedAt { get; }
        public List<IElementProvider> DeclaredElements { get; } = new List<IElementProvider>();
        public IConstructorProvider<Constructor>[] Constructors { get; private set; }
        readonly ValueSolveSource _onReady = new ValueSolveSource();

        readonly ParseInfo _parseInfo;
        readonly ClassContext _typeContext;
        readonly List<Var> _staticVariables = new List<Var>();
        readonly Scope _scope;

        Scope _operationalObjectScope;
        Scope _operationalStaticScope;
        Scope _conflictScope;

        public DefinedClassInitializer(ParseInfo parseInfo, Scope scope, ClassContext typeContext) : base(typeContext.Identifier.GetText())
        {
            _parseInfo = parseInfo;
            _typeContext = typeContext;
            _scope = scope;
            _conflictScope = new Scope(Name);
            MetaGetter = this;
            OnReady = _onReady;
            DefinedAt = parseInfo.Script.GetLocation(typeContext.Identifier.GetRange(typeContext.Range));

            parseInfo.TranslateInfo.StagedInitiation.On(this);

            // Get the generics.
            GenericTypes = AnonymousType.GetGenerics(parseInfo, _typeContext.Generics, this);

            if (typeContext.Identifier)
            {
                parseInfo.Script.AddHover(
                    range: typeContext.Identifier.Range,
                    content: IDefinedTypeInitializer.Hover("class", this));
                parseInfo.Script.Elements.AddDeclarationCall(this, new DeclarationCall(typeContext.Identifier.Range, true));
            }
        }

        public void GetMeta()
        {
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
                    TryToExtend(inheriting, inheritContext.GenericToken.GetRange(inheritContext.Range));
                }
            }

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

            (Extends as ClassType)?.Elements.AddToScope(_parseInfo.TranslateInfo, _operationalStaticScope, false);
            (Extends as ClassType)?.Elements.AddToScope(_parseInfo.TranslateInfo, _operationalObjectScope, true);

            WorkingInstance = GetInstance();
            _onReady.Set();

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
        
        void TryToExtend(CodeType type, DocRange range)
        {
            // Make sure the type is a class.
            if (type is not ClassType classType)
            {
                _parseInfo.Script.Diagnostics.Error("'" + type.Name + "' is not a class", range);
                return;
            }
            
            // Do not extend self.
            if (classType.Provider == this)
            {
                _parseInfo.Script.Diagnostics.Error("Cannot extend self", range);
                return;
            }

            // Check for circular hierarchy.
            var current = classType.Provider.Extends?.Provider;
            while (current != null)
            {
                if (current == this)
                {
                    _parseInfo.Script.Diagnostics.Error("Circular hierarchies are not allowed", range);
                    return;
                }
                current = (current.Extends as ClassType)?.Provider;
            }

            Extends = classType;

            // Ok
            if (classType.Provider.MetaGetter != null)
                _parseInfo.TranslateInfo.StagedInitiation.Meta.Depend(classType.Provider.MetaGetter);
        }

        public override CodeType GetInstance() => new DefinedClass(_parseInfo, this, GenericTypes);
        public override CodeType GetInstance(GetInstanceInfo instanceInfo) => new DefinedClass(_parseInfo, this, instanceInfo.Generics);
        public IMethod GetOverridenFunction(DeltinScript deltinScript, FunctionOverrideInfo overrideInfo) => Extends.Elements.GetVirtualFunction(deltinScript, overrideInfo.Name, overrideInfo.ParameterTypes);
        public IVariableInstance GetOverridenVariable(string variableName) => Extends.Elements.GetVirtualVariable(variableName);
        Scope IScopeProvider.GetObjectBasedScope() => _operationalObjectScope;
        Scope IScopeProvider.GetStaticBasedScope() => _operationalStaticScope;
        void IScopeAppender.AddObjectBasedScope(IMethod function)
        {
            _operationalObjectScope.AddNativeMethod(function);
            _conflictScope.AddNative(function);
        }
        void IScopeAppender.AddStaticBasedScope(IMethod function)
        {
            _operationalStaticScope.AddNativeMethod(function);
            _conflictScope.AddNative(function);
        }
        void IScopeAppender.AddObjectBasedScope(IVariableInstance variable)
        {
            _operationalObjectScope.AddNativeVariable(variable);
            _conflictScope.AddNative(variable);
        }
        void IScopeAppender.AddStaticBasedScope(IVariableInstance variable)
        {
            _operationalStaticScope.AddNativeVariable(variable);
            _conflictScope.AddNative(variable);
        }

        public void CheckConflict(ParseInfo parseInfo, CheckConflict identifier, DocRange range) => SemanticsHelper.ErrorIfConflicts(
            parseInfo: parseInfo,
            identifier: identifier,
            nameConflictMessage: Parse.CheckConflict.CreateNameConflictMessage(Name, identifier.Name),
            overloadConflictMessage: Parse.CheckConflict.CreateOverloadConflictMessage(Name, identifier.Name),
            range: range,
            _conflictScope);
    }
}