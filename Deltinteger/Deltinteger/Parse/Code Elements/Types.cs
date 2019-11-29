using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    public abstract class CodeType : IExpression
    {
        public string Name { get; }

        public CodeType(string name)
        {
            Name = name;
        }

        public static CodeType[] GetDefaultTypes()
        {
            var defaultTypes = new List<CodeType>();
            foreach (var enumData in EnumData.GetEnumData())
                defaultTypes.Add(new WorkshopEnumType(enumData));
            return defaultTypes.ToArray();
        }

        public abstract Scope ReturningScope();
    }

    public class WorkshopEnumType : CodeType
    {
        private Scope EnumScope { get; } = new Scope();

        public WorkshopEnumType(EnumData enumData) : base(enumData.CodeName)
        {
            foreach (var member in enumData.Members)
            {
                var scopedMember = new ScopedEnumMember(member);
                EnumScope.In(scopedMember);
            }
            EnumScope.ErrorName = "enum " + Name;
        }

        override public Scope ReturningScope()
        {
            return EnumScope;
        }
    }

    class ScopedEnumMember : IScopeable, IExpression
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public string ScopeableType { get; } = "enumvalue";
        public Location DefinedAt { get; } = null;

        private EnumMember EnumMember { get; }

        private Scope debugScope { get; } = new Scope();
        
        public ScopedEnumMember(EnumMember enumMember)
        {
            Name = enumMember.CodeName;
            EnumMember = enumMember;
            debugScope.ErrorName = "enum value " + Name;
        }

        public Scope ReturningScope()
        {
            return debugScope;
        }
    }
}