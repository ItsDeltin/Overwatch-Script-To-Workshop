using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    class WorkshopVariableParameter : CodeParameter
    {
        public bool IsGlobal { get; }

        public WorkshopVariableParameter(string name, string documentation, bool isGlobal) : base(name, documentation)
        {
            IsGlobal = isGlobal;
        }

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            CallVariableAction call = value as CallVariableAction;
            if (call == null || call.Calling.DefineType != VariableDefineType.RuleLevel)
                script.Diagnostics.Error("Expected a variable defined on the rule level.", valueRange);

            if (call != null && (call.Calling.VariableType == VariableType.Global) != IsGlobal)
                script.Diagnostics.Error($"Expected a {(IsGlobal ? "global" : "player")} variable.", valueRange);
            
            if (call != null && call.Index.Length > 0)
                script.Diagnostics.Error("Variable cannot be indexed.", valueRange);

            return null;
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, bool asElement)
        {
            return ((IndexReference)actionSet.IndexAssigner[((CallVariableAction)expression).Calling]).WorkshopVariable;
        }
    }

    class VariableParameter : CodeParameter
    {
        private VariableType VariableType { get; }

        public VariableParameter(string name, string documentation) : base(name, documentation)
        {
            VariableType = VariableType.Dynamic;
        }
        public VariableParameter(string name, string documentation, VariableType variableType) : base(name, documentation)
        {
            if (variableType == VariableType.ElementReference) throw new Exception("Only the variable types Dynamic, Global, and Player is valid.");
            VariableType = variableType;
        }

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            CallVariableAction call = value as CallVariableAction;

            // Syntax error if the expression is not a variable.
            if (call == null)
                script.Diagnostics.Error("Expected a variable.", valueRange);
            
            // Syntax error if the variable is not settable.
            else if (!call.Calling.Settable())
                script.Diagnostics.Error($"The {call.Calling.Name} variable cannot be set to.", valueRange);
            
            else if (VariableType != VariableType.Dynamic && call.Calling.VariableType != VariableType)
            {
                if (VariableType == VariableType.Global)
                    script.Diagnostics.Error($"Expected a global variable.", valueRange);
                else
                    script.Diagnostics.Error($"Expected a player variable.", valueRange);
            }
            
            else return call;
            return null;
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, bool asElement) => null;
    }

    class ConstBoolParameter : CodeParameter
    {
        private bool DefaultConstValue { get; }

        public ConstBoolParameter(string name, string documentation) : base(name, documentation) {}
        public ConstBoolParameter(string name, string documentation, bool defaultValue)
            : base(name, documentation, new ExpressionOrWorkshopValue(defaultValue ? (Element)new V_True() : new V_False()))
        {
            DefaultConstValue = defaultValue;
        }

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            if (value is ExpressionOrWorkshopValue)
                return ((ExpressionOrWorkshopValue)value).WorkshopValue is V_True;

            if (value is BoolAction == false)
            {
                script.Diagnostics.Error("Expected a boolean constant.", valueRange);
                return null;
            }

            return ((BoolAction)value).Value;
        }
    }

    class ConstNumberParameter : CodeParameter
    {
        private double DefaultConstValue { get; }

        public ConstNumberParameter(string name, string documentation) : base(name, documentation) {}
        public ConstNumberParameter(string name, string documentation, double defaultValue) : base(name, documentation, new ExpressionOrWorkshopValue(new V_Number(defaultValue)))
        {
            DefaultConstValue = defaultValue;
        }

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            if (value == null) return DefaultConstValue;

            if (value is NumberAction == false)
            {
                script.Diagnostics.Error("Expected a number constant.", valueRange);
                return null;
            }

            return ((NumberAction)value).Value;
        }
    }

    class ConstStringParameter : CodeParameter
    {
        public ConstStringParameter(string name, string documentation) : base(name, documentation) {}

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            StringAction str = value as StringAction;
            if (str == null) script.Diagnostics.Error("Expected string constant.", valueRange);
            return str?.Value;
        }

        public override IWorkshopTree Parse(ActionSet actionSet, IExpression expression, bool asElement) => null;
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

        public override object Validate(ScriptFile script, IExpression value, DocRange valueRange)
        {
            StringAction str = value as StringAction;
            if (str == null)
            {
                script.Diagnostics.Error("Expected string constant.", valueRange);
                return null;
            }

            string resultingPath = Extras.CombinePathWithDotNotation(script.Uri.FilePath(), str.Value);
            
            if (resultingPath == null)
            {
                script.Diagnostics.Error("File path contains invalid characters.", valueRange);
                return null;
            }

            string dir = Path.GetDirectoryName(resultingPath);
            if (Directory.Exists(dir))
                DeltinScript.AddImportCompletion(script, dir, valueRange);

            if (!File.Exists(resultingPath))
            {
                script.Diagnostics.Error($"No file was found at '{resultingPath}'.", valueRange);
                return null;
            }

            if (FileTypes != null && !FileTypes.Contains(Path.GetExtension(resultingPath).ToLower()))
            {
                script.Diagnostics.Error($"Expected a file with the file type '{string.Join(", ", FileTypes)}'.", valueRange);
                return null;
            }

            return resultingPath;
        }
    }
}