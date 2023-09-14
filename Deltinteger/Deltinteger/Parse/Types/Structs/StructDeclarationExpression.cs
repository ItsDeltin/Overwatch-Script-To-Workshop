using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Parse.Variables.Build;
using Deltin.Deltinteger.Elements;

namespace Deltin.Deltinteger.Parse
{
    public class StructDeclarationExpression : IExpression, IStructProvider
    {
        public string Name { get; private set; }

        // The variables of the struct declaration. 
        public IVariable[] Variables { get; private set; }

        // The struct type created from the declaration.
        public StructInstance Type { get; private set; }

        // Is the struct single or parallel?
        public bool Parallel { get; private set; }

        // We do not need to worry about these values.
        public AnonymousType[] GenericTypes { get; } = new AnonymousType[0];
        public IMethodProvider[] Methods { get; } = new IMethodProvider[0]; // The methods of the struct declaration. This is currently unused.
        public IVariable[] StaticVariables { get; } = new IVariable[0];

        private readonly ParseInfo _parseInfo;
        private readonly Scope _scope;
        private readonly StructDeclarationContext _context;
        private readonly bool _isExplicit;

        private IExpression _spreadValue;

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
            var variables = new List<IVariable>();

            // Empty struct error.
            if (_context.Values.Count == 0)
                _parseInfo.Script.Diagnostics.Error("Empty structs are not allowed", _context.Range);

            var scopeHandler = new StructValueScopeHandler(_scope);

            var expectingStruct = _parseInfo.ExpectingType as StructInstance;

            // Parallel will be true by default if a struct is not expected.
            Parallel = expectingStruct?.Attributes.IsStruct ?? true;

            // Create the struct type from the values.
            for (int i = 0; i < _context.Values.Count; i++)
            {
                // Is the current value a spread?
                if (_context.Values[i].SpreadToken)
                {
                    // Error if this is not the last element in the struct.
                    if (i != _context.Values.Count - 1)
                    {
                        _parseInfo.Script.Diagnostics.Error("Spread operator must be final element in struct", _context.Values[i].SpreadToken);
                    }
                    else
                    {
                        // Get the spread value.
                        _spreadValue = _parseInfo.ClearExpectations().GetExpression(_scope, _context.Values[i].SpreadValue);

                        // Make sure the spread value is a struct.
                        if (_spreadValue.Type() is not StructInstance structInstance)
                        {
                            _parseInfo.Script.Diagnostics.Error("Spread value must be a struct", _context.Values[i].SpreadValue.Range);
                        }
                        else
                        {
                            // Add remaining elements to struct.
                            foreach (var structVariable in structInstance.Variables)
                            {
                                if (variables.All(v => v.Name != structVariable.Name))
                                {
                                    variables.Add(VariableMaker.New(
                                        structVariable.Name,
                                        structVariable.CodeType.GetCodeType(_parseInfo.TranslateInfo),
                                        IVariableDefault.Create(actionSet => actionSet.SpreadHelper.GetValue(structVariable.Name))
                                    ));
                                }
                            }
                        }
                    }
                }
                else
                {
                    string newVariableName = _context.Values[i].Identifier?.Text;

                    // No type in an explicit struct declaration.
                    if (_isExplicit && _context.Values[i].Type == null)
                        _parseInfo.Script.Diagnostics.Error("Inconsistent struct value usage; value types must be all explicit or all implicit", _context.Values[i].Identifier.Range);

                    ParseInfo variableParseInfo = _parseInfo.ClearExpectations();

                    // Is there an expected struct type in the current context?
                    if (expectingStruct != null)
                    {
                        // Find a variable in the expected struct with the matching name.
                        var expectingVariable = expectingStruct.Variables.FirstOrDefault(var => var.Name == newVariableName);

                        // If the variable in the expected type exists, set expected type for the variable being declared.
                        if (expectingVariable != null)
                            variableParseInfo = variableParseInfo.SetExpectType(expectingVariable.CodeType.GetCodeType(variableParseInfo.TranslateInfo));
                    }

                    // Create the struct variable.
                    var newVariable = new StructValueVariable(scopeHandler, new StructValueContextHandler(variableParseInfo, _context.Values[i])).GetVar();

                    variables.Add(newVariable);
                }
            }

            if (expectingStruct != null)
            {
                Variables = variables.OrderBy(var =>
                    Array.FindIndex(expectingStruct.Variables, expectingVar => var.Name == expectingVar.Name)
                ).ToArray();
            }
            else
            {
                Variables = variables.ToArray();
            }
            // 'variables' should not be used at this point.

            Name = $"{{{string.Join(", ", Variables.Select(v => v.GetDefaultInstance(null).GetLabel(_parseInfo.TranslateInfo)))}}}";

            // Add completion.
            if (expectingStruct != null)
            {
                // Create completions from the expected struct's variables.
                var completions = expectingStruct.Variables
                    // Do not add the completion if the variable already exists in the struct.
                    .Where(expectingVariable => Variables.All(v => v.Name != expectingVariable.Name))
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
        public IWorkshopTree Parse(ActionSet actionSet)
        {
            if (Parallel)
                return new StructAssigner(Type, new StructAssigningAttributes(), false).GetValues(actionSet.SetSpreadHelper(_spreadValue?.Parse(actionSet) as IStructValue));
            else
                return Element.CreateArray(Type.Variables.SelectMany(var => StructHelper.Flatten(var.GetAssigner().GetValue(new(actionSet) { Inline = true }).GetVariable())).ToArray());
        }

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