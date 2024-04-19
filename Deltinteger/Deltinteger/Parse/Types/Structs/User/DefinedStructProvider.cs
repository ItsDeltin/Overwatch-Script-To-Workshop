using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedStructInitializer : StructInitializer, IDefinedTypeInitializer, IDeclarationKey, IGetMeta, IGetContent
    {
        public CodeType WorkingInstance { get; }
        public Location DefinedAt { get; }
        public bool[] GenericAssigns { get; }
        public ParsedMetaComment Doc { get; }
        readonly ParseInfo _parseInfo;
        readonly ClassContext _context;
        readonly Scope _scope;
        // Makes local struct variables unsettable in functions.
        readonly VariableModifierGroup _contextualVariableModifiers = new VariableModifierGroup();
        // Tracks the assigning types that the struct variables use. 
        readonly List<HashSet<DefinedStructInitializer>> _variablesCallTypeAssigners = new List<HashSet<DefinedStructInitializer>>();

        private Scope _staticScope;
        private Scope _objectScope;

        public DefinedStructInitializer(ParseInfo parseInfo, Scope scope, ClassContext typeContext) : base(typeContext.Identifier.GetText(), parallel: !typeContext.Single)
        {
            _parseInfo = parseInfo;
            _context = typeContext;
            _scope = scope;
            Doc = ParsedMetaComment.FromMetaComment(typeContext.Doc);
            DefinedAt = parseInfo.Script.GetLocation(typeContext.Identifier.GetRange(typeContext.Range));
            parseInfo.TranslateInfo.StagedInitiation.Meta.Execute(this);
            parseInfo.TranslateInfo.StagedInitiation.Content.Execute(this);

            // Get the type args.
            GenericTypes = AnonymousType.GetGenerics(parseInfo, typeContext.Generics, this);
            GenericAssigns = new bool[GenericTypes.Length];

            // Add the declaration link.
            if (typeContext.Identifier)
            {
                parseInfo.Script.AddHover(
                    range: typeContext.Identifier.Range,
                    content: IDefinedTypeInitializer.Hover("struct", this));
                parseInfo.Script.Elements.AddDeclarationCall(this, new DeclarationCall(typeContext.Identifier.Range, true));
            }

            WorkingInstance = GetInstance();
        }

        public void GetMeta()
        {
            _staticScope = _scope.Child(true);
            _objectScope = _staticScope.Child(true);

            // Add type args to scopes.
            foreach (var type in GenericTypes)
            {
                _staticScope.AddType(new GenericCodeTypeInitializer(type));
                _objectScope.AddType(new GenericCodeTypeInitializer(type));
            }

            var declarationParseInfo = _parseInfo.SetContextualModifierGroup(_contextualVariableModifiers);

            // Get declarations.
            foreach (var declaration in _context.Declarations)
            {
                var element = ((IDefinedTypeInitializer)this).ApplyDeclaration(declaration, declarationParseInfo);

                if (element is IMethodProvider method)
                    Methods.Add(method);
            }

            if (Variables.Count == 0)
                _parseInfo.Script.Diagnostics.Error("Structs require at least 1 assignable variable", DefinedAt.range);
        }

        public void GetContent()
        {
            if (DoesRecursiveCall())
                _parseInfo.Script.Diagnostics.Error("A variable defined in the struct recursively calls the struct", DefinedAt.range);
        }

        public override StructInstance GetInstance() => new DefinedStructInstance(this, InstanceAnonymousTypeLinker.Empty);
        public override StructInstance GetInstance(InstanceAnonymousTypeLinker typeLinker) => new DefinedStructInstance(this, typeLinker);

        public override bool BuiltInTypeMatches(Type type) => false;
        public Scope GetObjectBasedScope() => _objectScope;
        public Scope GetStaticBasedScope() => _staticScope;
        public IMethod GetOverridenFunction(DeltinScript deltinScript, FunctionOverrideInfo functionOverloadInfo) => throw new NotImplementedException();
        public IVariableInstance GetOverridenVariable(string variableName) => throw new NotImplementedException();
        public void AddObjectBasedScope(IMethod function) => _objectScope.AddNativeMethod(function);
        public void AddStaticBasedScope(IMethod function) => _staticScope.AddNativeMethod(function);
        public void AddObjectBasedScope(IVariableInstance variable)
        {
            // Add to scope.
            _objectScope.CopyVariable(variable);

            // Make sure the variable is not a macro.
            if (variable.Attributes.StoreType != StoreType.None)
            {
                var variableType = variable.CodeType.GetCodeType(_parseInfo.TranslateInfo);

                // Add to list of variables.
                Variables.Add(variable.Provider);

                // Make the variable unsettable when used locally.
                _contextualVariableModifiers.MakeUnsettable(_parseInfo.TranslateInfo, variable);

                var structCalls = new HashSet<DefinedStructInitializer>();

                // Iterate through each assigning type in the variable's type.
                // An 'assigning type' is a type within a type's tree that may potentially be used to assign a data-type.
                // This is used to ensure that recursive structs do not exist.
                foreach (var descendant in variableType.GetAssigningTypes())
                    // If the variable type contains a struct, add it to the hashset of root variable types.
                    if (descendant is DefinedStructInstance definedStructInstance)
                        structCalls.Add(definedStructInstance.Provider);
                    // If the variable type contains an anonymous type, mark that type-arg as an assigner.
                    else if (descendant is AnonymousType anonymousType)
                    {
                        int index = Array.IndexOf(GenericTypes, anonymousType);
                        if (index != -1)
                            GenericAssigns[index] = true;
                    }

                _variablesCallTypeAssigners.Add(structCalls);
            }
            else InstanceInlineVariables.Add(variable.Provider);
        }
        public void AddStaticBasedScope(IVariableInstance variable)
        {
            _staticScope.CopyVariable(variable);
            _objectScope.CopyVariable(variable);
            StaticVariables.Add(variable.Provider);
            _parseInfo.TranslateInfo.GetComponent<StaticVariableCollection>().AddVariable(variable.Provider);
        }
        public void CheckConflict(ParseInfo parseInfo, CheckConflict identifier, DocRange range) => SemanticsHelper.ErrorIfConflicts(
            parseInfo: parseInfo,
            identifier: identifier,
            nameConflictMessage: Parse.CheckConflict.CreateNameConflictMessage(Name, identifier.Name),
            overloadConflictMessage: Parse.CheckConflict.CreateOverloadConflictMessage(Name, identifier.Name),
            range: range,
            _objectScope, _staticScope);
        public override void DependMeta() => _parseInfo.TranslateInfo.StagedInitiation.Meta.Depend(this);
        public override void DependContent() => _parseInfo.TranslateInfo.StagedInitiation.Content.Depend(this);

        bool DoesRecursiveCall()
        {
            foreach (var variable in _variablesCallTypeAssigners)
                foreach (var root in variable)
                {
                    var watched = new HashSet<DefinedStructInitializer>();
                    foreach (var rootCall in EnumerateRootCallsRecursively(root))
                    {
                        if (!watched.Add(rootCall)) return false;
                        if (rootCall == this) return true;
                    }
                }
            return false;
        }

        static IEnumerable<DefinedStructInitializer> EnumerateRootCallsRecursively(DefinedStructInitializer target)
        {
            yield return target;
            foreach (var variable in target._variablesCallTypeAssigners)
                foreach (var root in variable)
                    foreach (var recursive in EnumerateRootCallsRecursively(root))
                        yield return recursive;
        }
    }
}