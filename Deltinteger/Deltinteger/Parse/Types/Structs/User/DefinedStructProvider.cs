using System;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class DefinedStructInitializer : StructInitializer, IDefinedTypeInitializer, IResolveElements
    {
        private readonly ParseInfo _parseInfo;
        private readonly ClassContext _context;
        private readonly Scope _scope;
        public Location DefinedAt { get; }
        public Scope StaticScope { get; private set; }
        public Scope ObjectScope { get; private set; }

        public DefinedStructInitializer(ParseInfo parseInfo, Scope scope, ClassContext typeContext) : base(typeContext.Identifier.GetText())
        {
            _parseInfo = parseInfo;
            _context = typeContext;
            _scope = scope;
            DefinedAt = parseInfo.Script.GetLocation(typeContext.Identifier.GetRange(typeContext.Range));
            parseInfo.TranslateInfo.AddResolve(this);
        }

        public void ResolveElements()
        {
            StaticScope = _scope.Child();
            ObjectScope = StaticScope.Child();

            // Get declarations.
            foreach (var declaration in _context.Declarations)
                ((IDefinedTypeInitializer)this).ApplyDeclaration(declaration, _parseInfo);
        }

        public void AddVariable(IVariable var) => base.Variables.Add(var);
        public void AddMacro(MacroVarProvider macro) {}
        // public void AddMacro(MacroVarProvider macro) => ObjectScope.AddNativeVariable(macro.GetDefaultInstance());

        public override CodeType GetInstance() => new DefinedStructInstance(this, InstanceAnonymousTypeLinker.Empty);
        // TODO: generics support for structs.
        public override CodeType GetInstance(GetInstanceInfo instanceInfo) => new DefinedStructInstance(this, InstanceAnonymousTypeLinker.Empty);
        
        public override bool BuiltInTypeMatches(Type type) => false;
        public Scope GetObjectBasedScope() => ObjectScope;
        public Scope GetStaticBasedScope() => StaticScope;
        public IMethod GetOverridenFunction(IMethodProvider provider) => throw new NotImplementedException();
        public IVariableInstance GetOverridenVariable(string variableName) => throw new NotImplementedException();
        public void AddObjectBasedScope(IMethod function) => ObjectScope.CopyMethod(function);
        public void AddStaticBasedScope(IMethod function) => StaticScope.CopyMethod(function);
        public void AddObjectBasedScope(IVariableInstance variable) => ObjectScope.CopyVariable(variable);
        public void AddStaticBasedScope(IVariableInstance variable) => StaticScope.CopyVariable(variable);
    }
}