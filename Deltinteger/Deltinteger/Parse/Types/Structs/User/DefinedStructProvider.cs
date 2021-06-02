using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedStructInitializer : StructInitializer, IDefinedTypeInitializer, IResolveElements, IDeclarationKey
    {
        public CodeType WorkingInstance => throw new NotImplementedException();
        public Location DefinedAt { get; }
        public Scope StaticScope { get; private set; }
        public Scope ObjectScope { get; private set; }
        readonly ParseInfo _parseInfo;
        readonly ClassContext _context;
        readonly Scope _scope;
        readonly ValueSolveSource _onReady = new ValueSolveSource();

        public DefinedStructInitializer(ParseInfo parseInfo, Scope scope, ClassContext typeContext) : base(typeContext.Identifier.GetText())
        {
            _parseInfo = parseInfo;
            _context = typeContext;
            _scope = scope;
            DefinedAt = parseInfo.Script.GetLocation(typeContext.Identifier.GetRange(typeContext.Range));
            parseInfo.TranslateInfo.AddResolve(this);
            OnReady = _onReady;

            // Get the type args.
            GenericTypes = AnonymousType.GetGenerics(parseInfo, typeContext.Generics, this);

            // Add the declaration link.
            if (typeContext.Identifier)
            {
                parseInfo.Script.AddHover(
                    range: typeContext.Identifier.Range,
                    content: IDefinedTypeInitializer.Hover("struct", this));
                parseInfo.Script.Elements.AddDeclarationCall(this, new DeclarationCall(typeContext.Identifier.Range, true));
            }
        }

        public void ResolveElements()
        {
            StaticScope = _scope.Child();
            ObjectScope = StaticScope.Child();

            // Add type args to scopes.
            foreach (var type in GenericTypes)
            {
                StaticScope.AddType(new GenericCodeTypeInitializer(type));
                ObjectScope.AddType(new GenericCodeTypeInitializer(type));
            }

            var methods = new List<IMethodProvider>();

            // Get declarations.
            foreach (var declaration in _context.Declarations)
            {
                var element = ((IDefinedTypeInitializer)this).ApplyDeclaration(declaration, _parseInfo);

                if (element is IMethodProvider method)
                    Methods.Add(method);
            }
            
            _onReady.Set();
        }

        public void AddVariable(IVariable var) => base.Variables.Add(var);
        public void AddMacro(MacroVarProvider macro) {}
        // public void AddMacro(MacroVarProvider macro) => ObjectScope.AddNativeVariable(macro.GetDefaultInstance());

        public override CodeType GetInstance() => new DefinedStructInstance(this, InstanceAnonymousTypeLinker.Empty);
        public override CodeType GetInstance(GetInstanceInfo instanceInfo) => new DefinedStructInstance(this, new InstanceAnonymousTypeLinker(GenericTypes, instanceInfo.Generics));
        
        public override bool BuiltInTypeMatches(Type type) => false;
        public Scope GetObjectBasedScope() => ObjectScope;
        public Scope GetStaticBasedScope() => StaticScope;
        public IMethod GetOverridenFunction(DeltinScript deltinScript, FunctionOverrideInfo functionOverloadInfo) => throw new NotImplementedException();
        public IVariableInstance GetOverridenVariable(string variableName) => throw new NotImplementedException();
        public void AddObjectBasedScope(IMethod function) => ObjectScope.CopyMethod(function);
        public void AddStaticBasedScope(IMethod function) => StaticScope.CopyMethod(function);
        public void AddObjectBasedScope(IVariableInstance variable) => ObjectScope.CopyVariable(variable);
        public void AddStaticBasedScope(IVariableInstance variable) => StaticScope.CopyVariable(variable);
    }
}