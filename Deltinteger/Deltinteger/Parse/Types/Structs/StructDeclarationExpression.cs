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
        public AnonymousType[] GenericTypes { get; } = [];
        public IMethodProvider[] Methods { get; } = []; // The methods of the struct declaration. This is currently unused.
        public IVariable[] StaticVariables { get; } = [];
        public IEnumerable<IVariable> InstanceInlineVariables { get; } = [];

        private readonly ParseInfo _parseInfo;
        private readonly Scope _scope;
        private readonly StructDeclarationContext _context;
        private readonly bool _isExplicit;

        private IExpression _spreadValue;

        private StructInstance _generatedType;

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

            var scopeHandler = new StructValueScopeHandler(_scope);

            var expectingStruct = _parseInfo.ExpectingType as StructInstance;

            // Parallel will be true by default if a struct is not expected.
            Parallel = expectingStruct?.Attributes.IsStruct ?? !_context.Single;

            bool printMissingVariables = true;

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
                                // Do not add if it is already in the struct or, if a struct is expected, is not in the expected struct.
                                if (variables.All(v => v.Name != structVariable.Name))
                                {
                                    bool addVariable = true;

                                    var structVariableType = structVariable.CodeType.GetCodeType(_parseInfo.TranslateInfo);

                                    // Is a struct expected in the analysis?
                                    if (expectingStruct != null)
                                    {
                                        // Find the matching variable in the expected struct.
                                        var expectedVariable = expectingStruct.Variables.FirstOrDefault(v => v.Name == structVariable.Name);
                                        var expectingType = expectedVariable?.CodeType.GetCodeType(_parseInfo.TranslateInfo);

                                        // Do not add the variable if it is not expected.
                                        if (expectingType == null)
                                            addVariable = false;
                                        // Error if the type does not match.
                                        else if (!structVariableType.Implements(expectingType))
                                        {
                                            addVariable = false;
                                            _parseInfo.Script.Diagnostics.Error($"Variable '{structVariable.Name}' in spread must be of type '{expectingType.GetName()}', got '{structVariableType.GetName()}'", _context.Values[i].SpreadToken);
                                            printMissingVariables = false;
                                        }
                                    }

                                    if (addVariable)
                                    {
                                        variables.Add(VariableMaker.New(
                                            structVariable.Name,
                                            structVariableType,
                                            IVariableDefault.Create(actionSet => actionSet.SpreadHelper.FromName(structVariable.Name))
                                        ));
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    var newVariableIdentifier = _context.Values[i].Identifier;

                    // No type in an explicit struct declaration.
                    if (_isExplicit && _context.Values[i].Type == null)
                        _parseInfo.Script.Diagnostics.Error("Inconsistent struct value usage; value types must be all explicit or all implicit", _context.Values[i].Identifier.Range);

                    ParseInfo variableParseInfo = _parseInfo.ClearExpectations();
                    CodeType expectingType = null;

                    // Is there an expected struct type in the current context?
                    if (expectingStruct != null && newVariableIdentifier)
                    {
                        // Find a variable in the expected struct with the matching name.
                        var expectingVariable = expectingStruct.Variables.FirstOrDefault(var => var.Name == newVariableIdentifier.Text);

                        // If the variable in the expected type exists, set expected type for the variable being declared.
                        if (expectingVariable != null)
                        {
                            expectingType = expectingVariable.CodeType.GetCodeType(variableParseInfo.TranslateInfo);
                            variableParseInfo = variableParseInfo.SetExpectType(expectingType);
                        }
                        // Unexpected variable
                        else
                            _parseInfo.Script.Diagnostics.Error($"Unexpected struct variable '{newVariableIdentifier.Text}'", newVariableIdentifier.Range);
                    }

                    // Create the struct variable.
                    var newVariable = new StructValueVariable(scopeHandler, new StructValueContextHandler(variableParseInfo, _context.Values[i])).GetVar();

                    // Make sure the variable type matches the expected variable type.
                    if (expectingType != null && newVariableIdentifier && !newVariable.CodeType.Implements(expectingType))
                        _parseInfo.Script.Diagnostics.Error($"Expected value of type '{expectingType.GetName()}'", newVariableIdentifier.Range);

                    variables.Add(newVariable);
                }
            }

            // If no struct is expected, add the empty struct error.
            // If there is an expected struct then the missing variable error
            // will be added instead.
            if (_context.Values.Count == 0 && expectingStruct == null)
                _parseInfo.Script.Diagnostics.Error("Empty structs are not allowed", _context.Range);

            // Is the analysis expecting a struct?
            if (expectingStruct != null)
            {
                // Add an error for any missing variables.
                var missingVariables = expectingStruct.Variables.Where(expectingVariable => variables.All(declaredVariable => declaredVariable.Name != expectingVariable.Name)).ToArray();
                if (missingVariables.Length != 0 && _context.ClosingBracket && printMissingVariables)
                {
                    _parseInfo.Script.Diagnostics.Error(
                        "Missing struct values " + string.Join(", ", missingVariables.Select(v => $"'{v.Name}'")),
                        _context.ClosingBracket
                    );
                }

                // Make the variable order the same as the expected order.
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

            _generatedType = new StructInstance(this, InstanceAnonymousTypeLinker.Empty);
            Type = expectingStruct ?? _generatedType;
        }

        // Struct as workshop value. 
        public IWorkshopTree Parse(ActionSet actionSet)
        {
            var spreadValue = _spreadValue?.Parse(actionSet);
            var spreadHelper = ISpreadHelper.Create(spreadValue, _generatedType);
            actionSet = actionSet.SetSpreadHelper(spreadHelper);

            if (Parallel)
                return new StructAssigner(_generatedType, new StructAssigningAttributes(), false).GetValues(actionSet);
            else
                return Element.CreateArray(_generatedType.Variables.SelectMany(var => StructHelper.Flatten(var.GetAssigner().GetValue(new(actionSet) { Inline = true }).GetVariable())).ToArray());
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

            public void GetComponents(VariableComponentCollection componentCollection, VariableSetKind variableSetKind)
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

    /// <summary>
    /// When a spread operator is used in a struct declaration, this interface is used to locate
    /// the values for the missing variables.
    /// </summary>
    public interface ISpreadHelper
    {
        /// <summary>The name to get a value for.</summary>
        /// <returns>null if the value linked to the provided name is not found.</returns>
        IWorkshopTree FromName(string name);

        /// <summary>Creates an ISpreadHelper from a workshop value and a struct type.</summary>
        /// <param name="value">The workshop value that is spread. If this is an IStructValue,
        /// this is assumed to be a paralleled struct.</param>
        /// <param name="type">Only used if unparalleled. Used to format 'value' which is assumed
        /// to be an array.</param>
        /// <returns>null if the 'value' parameter is null, otherwise an ISpreadHelper value.</returns>
        public static ISpreadHelper Create(IWorkshopTree value, StructInstance type)
        {
            // Empty
            if (value == null)
                return null;
            // Paralleled
            else if (value is IStructValue structValue)
                return new FromStructValue(structValue);
            // Unparalleled
            else
                return new FromTypeAndWorkshopValue(value, type);
        }

        // Spread operator used on parallel structs.
        record FromStructValue(IStructValue StructValue) : ISpreadHelper
        {
            public IWorkshopTree FromName(string name) => StructValue.GetValue(name);
        }

        // Spread operator used on unparalleled structs.
        record FromTypeAndWorkshopValue(IWorkshopTree Value, StructInstance Type) : ISpreadHelper
        {
            public IWorkshopTree FromName(string name)
            {
                var stacks = UnparalleledStructStack.StacksFromType(new WorkshopElementReference(Value), Type);
                return stacks.First(s => s.Variable.Name == name).SteppedValue.GetVariable();
            }
        }
    }
}