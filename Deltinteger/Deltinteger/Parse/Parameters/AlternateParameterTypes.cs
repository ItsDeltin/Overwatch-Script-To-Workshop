using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Cache;

namespace Deltin.Deltinteger.Parse
{
    /// <summary>
    /// `VariableParameter` takes indexed variables as parameters.
    /// </summary>
    class VariableParameter : CodeParameter
    {
        private VariableType VariableType { get; }
        private VariableResolveOptions Options { get; }

        public VariableParameter(string name, string documentation, ICodeTypeSolver typeSolver, VariableResolveOptions options = null) : base(name, documentation, typeSolver)
        {
            VariableType = VariableType.Dynamic;
            Options = options ?? new VariableResolveOptions();
        }
        public VariableParameter(string name, string documentation, VariableType variableType, ICodeTypeSolver typeSolver, VariableResolveOptions options = null) : base(name, documentation, typeSolver)
        {
            if (variableType == VariableType.ElementReference) throw new Exception("Only the variable types Dynamic, Global, and Player is valid.");
            VariableType = variableType;
            Options = options ?? new VariableResolveOptions();
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            VariableResolve resolvedVariable = new VariableResolve(parseInfo, Options, value, valueRange);

            // Syntax error if the expression is not a variable.
            if (!resolvedVariable.DoesResolveToVariable)
                parseInfo.Script.Diagnostics.Error("Expected a variable.", valueRange);

            else if (VariableType != VariableType.Dynamic && resolvedVariable.SetVariable.Calling.Provider.VariableType != VariableType)
            {
                if (VariableType == VariableType.Global)
                    parseInfo.Script.Diagnostics.Error($"Expected a global variable.", valueRange);
                else
                    parseInfo.Script.Diagnostics.Error($"Expected a player variable.", valueRange);
            }

            else return resolvedVariable;
            return null;
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData) => null;
    }

    class ConstBoolParameter : CodeParameter
    {

        public ConstBoolParameter(string name, string documentation, ITypeSupplier typeSupplier) : base(name, documentation, typeSupplier.Boolean()) { }
        public ConstBoolParameter(string name, string documentation, ITypeSupplier typeSupplier, bool defaultValue)
            : base(name, documentation, typeSupplier.Boolean(), new ExpressionOrWorkshopValue(defaultValue ? Element.True() : Element.False()))
        {
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            if (value is ExpressionOrWorkshopValue expressionOrWorkshop && expressionOrWorkshop.WorkshopValue is Element asElement)
                return asElement.Function.Name == "True";

            if (value is BoolAction == false)
            {
                parseInfo.Script.Diagnostics.Error("Expected a boolean constant.", valueRange);
                return null;
            }

            return ((BoolAction)value).Value;
        }
    }

    class ConstNumberParameter : CodeParameter
    {
        private double DefaultConstValue { get; }

        public ConstNumberParameter(string name, string documentation, ITypeSupplier typeSupplier) : base(name, documentation, typeSupplier.Number()) { }
        public ConstNumberParameter(string name, string documentation, ITypeSupplier typeSupplier, double defaultValue) : base(name, documentation, typeSupplier.Number(), new ExpressionOrWorkshopValue(Element.Num(defaultValue)))
        {
            DefaultConstValue = defaultValue;
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            if (value == null) return DefaultConstValue;

            if (value is NumberAction == false)
            {
                parseInfo.Script.Diagnostics.Error("Expected a number constant.", valueRange);
                return null;
            }

            return ((NumberAction)value).Value;
        }
    }

    class ConstStringParameter : CodeParameter
    {
        public ConstStringParameter(string name, string documentation, ITypeSupplier typeSupplier) : base(name, documentation, typeSupplier.String()) { }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            StringAction str = value as StringAction;
            if (str == null && valueRange != null) parseInfo.Script.Diagnostics.Error("Expected string constant.", valueRange);
            return str?.Value;
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData) => null;
    }

    class ConstHeroParameter : CodeParameter
    {
        public ConstHeroParameter(string name, string documentation, ITypeSupplier typeSupplier) : base(name, documentation, typeSupplier.Hero()) { }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            // ConstantExpressionResolver.Resolve's callback will be called after this function runs,
            // so we store the value in an object reference whose value will be set later.
            var promise = new ConstHeroValueResolver();

            // Resolve the expression.
            ConstantExpressionResolver.Resolve(value, expr =>
            {
                // If the resulting expression is an EnumValuePair and the EnumValuePair's enum is Hero,
                if (expr is CallVariableAction call && call.Calling is EnumValuePair pair && pair.Member.Enum.Name == "Hero")
                    // Resolve the value.
                    promise.Resolve(pair.Member.Name);
                // Otherwise, add an error.
                else if (valueRange != null)
                    parseInfo.Script.Diagnostics.Error("Expected hero constant", valueRange);
            });

            return promise;
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData) => null;
    }

    class ConstHeroValueResolver
    {
        public string Hero { get; private set; }
        public void Resolve(string hero) => Hero = hero;
    }

    abstract class ConstArrayParameter<T> : CodeParameter
    {
        private readonly string _elementKindName;

        protected ConstArrayParameter(ICodeTypeSolver type, string elementKindName, string name, MarkupBuilder documentation, bool optional = false)
            : base(name, documentation, type, optional ? new ExpressionOrWorkshopValue(Element.EmptyArray()) : null)
        {
            _elementKindName = elementKindName;
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            var values = new List<T>();
            ConstantExpressionResolver.Resolve(value, expr =>
            {
                // If the resulting expression is a CreateArray,
                if (expr is CreateArrayAction array)
                {
                    var error = new ConstElementErrorHandler(parseInfo.Script.Diagnostics, valueRange, _elementKindName);

                    // Iterate through each element in the array and get the value.
                    foreach (var value in array.Values)
                        ConstantExpressionResolver.Resolve(value, expr =>
                        {
                            // Make sure the value is a string.
                            if (TryGetValue(expr, out T value))
                                values.Add(value);
                            // Otherwise, add an error.
                            else
                                error.AddError();
                        });
                }
                // Otherwise, add an error.
                else if (valueRange != null)
                    parseInfo.Script.Diagnostics.Error($"Expected a {_elementKindName} array", valueRange);
            });
            return values;
        }

        protected abstract bool TryGetValue(IExpression expression, out T value);

        class ConstElementErrorHandler
        {
            private readonly FileDiagnostics _diagnostics;
            private readonly DocRange _errorRange;
            private readonly string _kindName;
            private bool _addedError;

            public ConstElementErrorHandler(FileDiagnostics diagnostics, DocRange errorRange, string kindName)
            {
                _diagnostics = diagnostics;
                _errorRange = errorRange;
                _kindName = kindName;
            }

            public void AddError()
            {
                if (_addedError) return;
                _addedError = true;
                _diagnostics.Error($"One or more values in the {_kindName} array is not a constant {_kindName} expression", _errorRange);
            }
        }
    }

    class ConstStringArrayParameter : ConstArrayParameter<string>
    {
        public ConstStringArrayParameter(string name, string documentation, ITypeSupplier types) : base(types.Array(types.String()), "string", name, documentation) { }

        protected override bool TryGetValue(IExpression expression, out string value)
        {
            if (expression is StringAction stringAction)
            {
                value = stringAction.Value;
                return true;
            }
            value = null;
            return false;
        }
    }

    class ConstIntegerArrayParameter : ConstArrayParameter<int>
    {
        public ConstIntegerArrayParameter(string name, MarkupBuilder documentation, ITypeSupplier types, bool optional = false) : base(types.NumberArray(), "integer", name, documentation, optional) { }

        protected override bool TryGetValue(IExpression expression, out int value)
        {
            if (expression is NumberAction numberAction)
            {
                value = (int)numberAction.Value;
                return true;
            }
            value = 0;
            return false;
        }
    }

    class FileParameter : CodeParameter
    {
        public string[] FileTypes { get; }

        /// <summary>
        /// A parameter that resolves to a file. AdditionalParameterData will return the file path.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="description">The parameter's description. Can be null.</param>
        /// <param name="fileTypes">The expected file types. Can be null.</param>
        public FileParameter(string parameterName, string description, ITypeSupplier typeSupplier, params string[] fileTypes) : base(parameterName, description, typeSupplier.String())
        {
            if (fileTypes != null)
            {
                if (fileTypes.Length == 0)
                    FileTypes = null;
                else
                    FileTypes = fileTypes.Select(f => f.ToLower()).ToArray();
            }
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            StringAction str = value as StringAction;
            if (str == null)
            {
                parseInfo.Script.Diagnostics.Error("Expected string constant.", valueRange);
                return null;
            }

            string resultingPath = Extras.CombinePathWithDotNotation(parseInfo.Script.Uri.LocalPath, str.Value);

            if (resultingPath == null)
            {
                parseInfo.Script.Diagnostics.Error("File path contains invalid characters.", valueRange);
                return null;
            }

            string dir = Path.GetDirectoryName(resultingPath);
            if (Directory.Exists(dir))
                Importer.AddImportCompletion(parseInfo.TranslateInfo, parseInfo.Script, dir, valueRange);

            if (!File.Exists(resultingPath))
            {
                parseInfo.Script.Diagnostics.Error($"No file was found at '{resultingPath}'.", valueRange);
                return null;
            }

            if (FileTypes != null && !FileTypes.Contains(Path.GetExtension(resultingPath).ToLower()))
            {
                parseInfo.Script.Diagnostics.Error($"Expected a file with the file type '{string.Join(", ", FileTypes)}'.", valueRange);
                return null;
            }

            parseInfo.Script.AddDefinitionLink(valueRange, new Location(new Uri(resultingPath), DocRange.Zero));
            parseInfo.Script.AddHover(valueRange, resultingPath);

            return resultingPath;
        }

        protected T GetFile<T>(ParseInfo parseInfo, string path, Func<Uri, T> factory) where T : LoadedFile
        {
            return Cache.FileIdentifier<T>.FromFile(parseInfo.Script.Document.Cache, path, factory);
        }
    }

    class TextFileParameter : FileParameter
    {
        public TextFileParameter(string parameterName, string description, ITypeSupplier typeSupplier, params string[] fileTypes) : base(parameterName, description, typeSupplier, fileTypes)
        {
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange, object additionalData)
        {
            string filepath = base.Validate(parseInfo, value, valueRange, additionalData) as string;

            if (filepath == null)
                return null;

            return GetFile(parseInfo, filepath, uri => new TextFile(uri)).Content;
        }
    }
}