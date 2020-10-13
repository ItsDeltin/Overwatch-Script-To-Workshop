using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;

namespace Deltin.Deltinteger.Parse
{
    /// <summary>
    /// `VariableParameter` takes indexed variables as parameters.
    /// </summary>
    class VariableParameter : CodeParameter
    {
        private VariableType VariableType { get; }
        private VariableResolveOptions Options { get; }

        public VariableParameter(string name, string documentation, VariableResolveOptions options = null) : base(name, documentation)
        {
            VariableType = VariableType.Dynamic;
            Options = options ?? new VariableResolveOptions();
        }
        public VariableParameter(string name, string documentation, VariableType variableType, VariableResolveOptions options = null) : base(name, documentation)
        {
            if (variableType == VariableType.ElementReference) throw new Exception("Only the variable types Dynamic, Global, and Player is valid.");
            VariableType = variableType;
            Options = options ?? new VariableResolveOptions();
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
        {
            VariableResolve resolvedVariable = new VariableResolve(Options, value, valueRange, parseInfo.Script.Diagnostics);

            // Syntax error if the expression is not a variable.
            if (!resolvedVariable.DoesResolveToVariable)
                parseInfo.Script.Diagnostics.Error("Expected a variable.", valueRange);
                        
            else if (VariableType != VariableType.Dynamic && resolvedVariable.SetVariable.Calling.VariableType != VariableType)
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
        private bool DefaultConstValue { get; }

        public ConstBoolParameter(string name, string documentation) : base(name, documentation) {}
        public ConstBoolParameter(string name, string documentation, bool defaultValue)
            : base(name, documentation, new ExpressionOrWorkshopValue(defaultValue ? Element.True() : Element.False()))
        {
            DefaultConstValue = defaultValue;
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
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

        public ConstNumberParameter(string name, string documentation) : base(name, documentation) {}
        public ConstNumberParameter(string name, string documentation, double defaultValue) : base(name, documentation, new ExpressionOrWorkshopValue(Element.Num(defaultValue)))
        {
            DefaultConstValue = defaultValue;
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
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
        public ConstStringParameter(string name, string documentation) : base(name, documentation) {}

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
        {
            StringAction str = value as StringAction;
            if (str == null) parseInfo.Script.Diagnostics.Error("Expected string constant.", valueRange);
            return str?.Value;
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, object additionalParameterData) => null;
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
        public FileParameter(string parameterName, string description, params string[] fileTypes) : base(parameterName, description)
        {
            if (fileTypes != null)
            {
                if (fileTypes.Length == 0)
                    FileTypes = null;
                else
                    FileTypes = fileTypes.Select(f => f.ToLower()).ToArray();
            }
        }

        public override object Validate(ParseInfo parseInfo, IExpression value, DocRange valueRange)
        {
            StringAction str = value as StringAction;
            if (str == null)
            {
                parseInfo.Script.Diagnostics.Error("Expected string constant.", valueRange);
                return null;
            }

            string resultingPath = Extras.CombinePathWithDotNotation(parseInfo.Script.Uri.FilePath(), str.Value);
            
            if (resultingPath == null)
            {
                parseInfo.Script.Diagnostics.Error("File path contains invalid characters.", valueRange);
                return null;
            }

            string dir = Path.GetDirectoryName(resultingPath);
            if (Directory.Exists(dir))
                Importer.AddImportCompletion(parseInfo.Script, dir, valueRange);

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

            parseInfo.Script.AddDefinitionLink(valueRange, new Location(Extras.Definition(resultingPath), DocRange.Zero));
            parseInfo.Script.AddHover(valueRange, resultingPath);

            return resultingPath;
        }
    }
}