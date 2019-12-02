using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public abstract class DefinedFunction : IMethod
    {
        public string ScopeableType { get; } = "method";
        public string Name { get; }
        public Location DefinedAt { get; }
        public AccessLevel AccessLevel { get; }

        public DefinedFunction(string name, Location definedAt, AccessLevel accessLevel)
        {
            Name = name;
            DefinedAt = definedAt;
            AccessLevel = accessLevel;
        }
    }
}