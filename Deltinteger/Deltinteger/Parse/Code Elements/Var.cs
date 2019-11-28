using System;
using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public class Var : IScopeable
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; }
        public Location DefinedAt { get; }
        public List<Location> CalledFrom { get; } = new List<Location>();
        public string ScopeableType { get; } = "variable";

        public Var(string name, AccessLevel accessLevel, Location definedAt)
        {
            Name = name;
            AccessLevel = accessLevel;
            DefinedAt = definedAt;
        }

        public CallVariableAction Call(Location calledFrom)
        {
            CalledFrom.Add(calledFrom);
            return new CallVariableAction(this);
        }
    }
}