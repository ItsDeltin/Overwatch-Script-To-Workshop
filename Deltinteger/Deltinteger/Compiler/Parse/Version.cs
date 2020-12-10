using System;

namespace Deltin.Deltinteger.Compiler.Parse
{
    public class ParserSettings
    {
        public DeclarationVersion DeclarationVersion { get; set; }

        public static readonly ParserSettings Default = new ParserSettings() {
            DeclarationVersion = DeclarationVersion.Let
        };

        public override bool Equals(object obj) => obj is ParserSettings settings &&
                                                   DeclarationVersion == settings.DeclarationVersion;

        public override int GetHashCode() => HashCode.Combine(DeclarationVersion);
    }

    public enum DeclarationVersion
    {
        Let,
        Define
    }
}