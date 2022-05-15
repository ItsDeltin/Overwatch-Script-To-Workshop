using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    public class StructDeclarationExpression : IExpression, IStructProvider
    {
        public string Name { get; private set; }

        // The variables of the struct declaration. 
        public IVariable[] Variables { get; private set; }

        // The struct type created from the declaration.
        public StructInstance Type { get; private set; }

        // We do not need to worry about these values.
        public AnonymousType[] GenericTypes { get; } = new AnonymousType[0];
        public IMethodProvider[] Methods { get; } = new IMethodProvider[0]; // The methods of the struct declaration. This is currently unused.
        public IVariable[] StaticVariables { get; } = new IVariable[0];

        private readonly ParseInfo _parseInfo;
        private readonly Scope _scope;
        private readonly StructDeclarationContext _context;
        private readonly bool _isExplicit;

        public StructDeclarationExpression(ParseInfo parseInfo, Scope scope, StructDeclarationContext context)
        {
            _parseInfo = parseInfo;
            _scope = scope;
            _context = context;
            _isExplicit = context.Values.Any(v => v.Type != null);
            GetValue();
        }

        void GetValue()
        {
            Name = "{";

            string[] variableNames = new string[_context.Values.Count];

            // Empty struct error.
            if (_context.Values.Count == 0)
                _parseInfo.Script.Diagnostics.Error("Empty structs are not allowed", _context.Range);

            var scopeHandler = new StructValueScopeHandler(_scope);

            var expectingStruct = _parseInfo.ExpectingType as StructInstance;

            // Create the struct type from the values.
            Variables = new IVariable[_context.Values.Count];
            for (int i = 0; i < _context.Values.Count; i++)
            {
                variableNames[i] = _context.Values[i].Identifier?.Text;

                // Separate variable names with a comma.
                if (i > 0) Name += ", ";

                // No type in an explicit struct declaration.
                if (_isExplicit && _context.Values[i].Type == null)
                    _parseInfo.Script.Diagnostics.Error("Inconsistent struct value usage; value types must be all explicit or all implicit", _context.Values[i].Identifier.Range);

                ParseInfo variableParseInfo = _parseInfo;

                // Is there an expected struct type in the current context?
                if (expectingStruct != null)
                {
                    // Find a variable in the expected struct with the matching name.
                    var expectingVariable = expectingStruct.Variables.FirstOrDefault(var => var.Name == variableNames[i]);

                    // If the variable in the expected type exists, set expected type for the variable being declared.
                    if (expectingVariable != null)
                        variableParseInfo = variableParseInfo.SetExpectType(expectingVariable.CodeType.GetCodeType(variableParseInfo.TranslateInfo));
                }

                // Create the struct variable.
                Variables[i] = new StructValueVariable(scopeHandler, new StructValueContextHandler(variableParseInfo, _context.Values[i])).GetVar();

                // Add the variable label.
                Name += Variables[i].GetDefaultInstance(null).GetLabel(_parseInfo.TranslateInfo);
            }

            Name += "}";

            // Add completion.
            if (expectingStruct != null)
            {
                // Create completions from the expected struct's variables.
                var completions = expectingStruct.Variables
                    // Do not add the completion if the variable already exists in the struct.
                    .Where(expectingVariable => !variableNames.Contains(expectingVariable.Name))
                    // Convert to CompletionItem.
                    .Select(expectingVariable => expectingVariable.GetCompletion(_parseInfo.TranslateInfo));

                // Add completion range to script.
                _parseInfo.Script.AddCompletionRange(new CompletionRange(
                    deltinScript: _parseInfo.TranslateInfo,
                    completionItems: completions.ToArray(),
                    range: _context.Range,
                    kind: CompletionRangeKind.ClearRest
                ));
            }

            Type = new StructInstance(this, InstanceAnonymousTypeLinker.Empty);
        }

        // Struct as workshop value. 
        public IWorkshopTree Parse(ActionSet actionSet) => new StructAssigner(Type, new StructAssigningAttributes(), false).GetValues(actionSet);

        CodeType IExpression.Type() => Type;

        public StructInstance GetInstance(InstanceAnonymousTypeLinker typeLinker) => new StructInstance(this, typeLinker);

        void IStructProvider.DependMeta() { }
        void IStructProvider.DependContent() { }

        // Handles the syntax tree context for struct variables.
        class StructValueContextHandler : IVarContextHandler
        {
            public ParseInfo ParseInfo { get; }
            private readonly StructDeclarationVariableContext _context;

            public StructValueContextHandler(ParseInfo parseInfo, StructDeclarationVariableContext context)
            {
                ParseInfo = parseInfo;
                _context = context;
            }

            public void GetComponents(VariableComponentCollection componentCollection)
            {
                componentCollection.AddComponent(new InitialValueComponent(_context.Value));
            }
            public IParseType GetCodeType() => _context.Type;
            public Location GetDefineLocation() => new Location(ParseInfo.Script.Uri, GetNameRange());
            public string GetName() => _context.Identifier.GetText();
            public DocRange GetNameRange() => _context.Identifier.GetRange(_context.Range);
            public DocRange GetTypeRange() => _context.Type?.Range;
        }

        // Handles the struct expression's scope and checks for duplicates.
        class StructValueScopeHandler : IScopeHandler
        {
            readonly Scope _scope;
            readonly List<string> _variableNames = new List<string>();

            public StructValueScopeHandler(Scope scope) => _scope = scope;
            Scope IScopeProvider.GetObjectBasedScope() => _scope;
            Scope IScopeProvider.GetStaticBasedScope() => _scope;
            IMethod IScopeProvider.GetOverridenFunction(DeltinScript deltinScript, FunctionOverrideInfo provider) => null;
            IVariableInstance IScopeProvider.GetOverridenVariable(string variableName) => null;
            void IScopeAppender.AddObjectBasedScope(IMethod function) { }
            void IScopeAppender.AddStaticBasedScope(IMethod function) { }
            void IScopeAppender.AddObjectBasedScope(IVariableInstance variable) => _variableNames.Add(variable.Name);
            void IScopeAppender.AddStaticBasedScope(IVariableInstance variable) => _variableNames.Add(variable.Name);
            void IConflictChecker.CheckConflict(ParseInfo parseInfo, CheckConflict identifier, DocRange range)
            {
                if (_variableNames.Contains(identifier.Name))
                    parseInfo.Script.Diagnostics.Error("Struct cannot have multiple properties with the same name", range);
            }
        }
    }
}