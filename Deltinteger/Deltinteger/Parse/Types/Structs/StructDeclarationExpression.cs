using System;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse.Variables.Build;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

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
        public IValueSolve OnReady { get; } = new ValueSolveSource(true); // Used by StructInstance to determine when a provider is done collecting its values.
        public AnonymousType[] GenericTypes { get; } = new AnonymousType[0];
        public IMethodProvider[] Methods { get; } = new IMethodProvider[0]; // The methods of the struct declaration. This is currently unused.

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
                
                // Create the struct variable.
                Variables[i] = new StructValueVariable(_scope, new StructValueContextHandler(_parseInfo, _context.Values[i])).GetVar();

                // Add the variable label.
                Name += Variables[i].GetDefaultInstance().GetLabel(_parseInfo.TranslateInfo);
            }

            Name += "}";

            // Add completion.
            if (_parseInfo.ExpectingType is StructInstance expectingStruct)
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
    }
}