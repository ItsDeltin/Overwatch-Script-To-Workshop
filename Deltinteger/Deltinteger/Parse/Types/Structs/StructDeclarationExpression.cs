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
        public IVariable[] Variables { get; private set; }
        public IMethodProvider[] Methods { get; } = new IMethodProvider[0];
        public StructInstance Type { get; private set; }
        public IValueSolve OnReady { get; } = new ValueSolveSource(true);
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

            // Create the struct type from the values.
            Variables = new IVariable[_context.Values.Count];
            for (int i = 0; i < _context.Values.Count; i++)
            {
                variableNames[i] = _context.Values[i].Identifier?.Text;
                if (i > 0) Name += ", ";

                if (_isExplicit && _context.Values[i].Type == null)
                    _parseInfo.Script.Diagnostics.Error("Inconsistent struct value usage; value types must be all explicit or all implicit", _context.Values[i].Identifier.Range);
                
                Variables[i] = new StructValueVariable(_scope, new StructValueContextHandler(_parseInfo, _context.Values[i])).GetVar();
                Name += Variables[i].GetDefaultInstance().GetLabel(_parseInfo.TranslateInfo);
            }

            Name += "}";
            Type = new StructInstance(this, InstanceAnonymousTypeLinker.Empty);

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
        }

        // public IWorkshopTree Parse(ActionSet actionSet) => new StructAssigner(Type, null, false).GetResult(new GettableAssignerValueInfo(actionSet) { Inline = true }).Gettable.GetVariable();
        public IWorkshopTree Parse(ActionSet actionSet) => new StructAssigner(Type, new StructAssigningAttributes(), false).GetValues(actionSet);

        CodeType IExpression.Type() => Type;
    }

    public class StructValueContextHandler : IVarContextHandler
    {
        public ParseInfo ParseInfo { get; }
        private readonly StructDeclarationVariableContext _context;

        public StructValueContextHandler(ParseInfo parseInfo, StructDeclarationVariableContext context)
        {
            ParseInfo = parseInfo;
            _context = context;
        }

        public IVariableComponent[] GetComponents() => new[] { new InitialValueComponent(_context.Value) };
        public IParseType GetCodeType() => _context.Type;
        public Location GetDefineLocation() => new Location(ParseInfo.Script.Uri, GetNameRange());
        public string GetName() => _context.Identifier.GetText();
        public DocRange GetNameRange() => _context.Identifier.GetRange(_context.Range);
        public DocRange GetTypeRange() => _context.Type?.Range;
    }
}