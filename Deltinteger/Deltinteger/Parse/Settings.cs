using System;

namespace Deltin.Deltinteger.Parse.Settings
{
    public class DsTomlSettings
    {
        public string EntryPoint { get; set; } = null;
        public bool ResetNonpersistent { get; set; } = false;
        public bool PasteCheckIsExtended { get; set; } = false;
        public bool LogDeleteReferenceZero { get; set; } = true;
        public string OutFile { get; set; }
        public bool VariableTemplate { get; set; }

        public static readonly DsTomlSettings Default = new DsTomlSettings();
    }
}