using System.Collections.Generic;
using Deltin.Deltinteger.Parse;
using Deltin.Deltinteger.Compiler;


namespace Deltin.Deltinteger.Lobby
{
    public class SettingValidation
    {
        private readonly List<string> _errors = new List<string>();

        public SettingValidation() { }

        public void Error(string error)
        {
            _errors.Add(error);
        }

        public void InvalidSetting(string propertyName)
        {
            _errors.Add($"The setting '{propertyName}' is not valid.");
        }

        public void IncorrectType(string propertyName, string expectedType)
        {
            _errors.Add($"The setting '{propertyName}' requires a value of type " + expectedType + ".");
        }

        public bool HasErrors() => _errors.Count > 0;

        public void Dump(FileDiagnostics diagnostics, DocRange range)
        {
            foreach (string error in _errors) diagnostics.Error(error, range);
        }
    }
}
