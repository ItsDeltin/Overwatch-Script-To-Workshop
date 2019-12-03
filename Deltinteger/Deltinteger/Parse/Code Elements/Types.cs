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

        // Static
        public abstract Scope ReturningScope();
        // Object
        public virtual Scope GetObjectScope()
        {
            return null;
        }

        public static CodeType[] GetDefaultTypes()
        {
            var defaultTypes = new List<CodeType>();
            foreach (var enumData in EnumData.GetEnumData())
                defaultTypes.Add(new WorkshopEnumType(enumData));
            return defaultTypes.ToArray();
        }

        public CodeType Type() => null;

        public static bool TypeMatches(CodeType parameterType, CodeType valueType)
        {
            return parameterType == null || parameterType == valueType;
        }
    }

    public class WorkshopEnumType : CodeType
    {
        private Scope EnumScope { get; } = new Scope();

        public WorkshopEnumType(EnumData enumData) : base(enumData.CodeName)
        {
            foreach (var member in enumData.Members)
            {
                var scopedMember = new ScopedEnumMember(member);
                EnumScope.AddVariable(scopedMember, null, null);
            }
            EnumScope.ErrorName = "enum " + Name;
        }

        override public Scope ReturningScope()
        {
            return EnumScope;
        }
    }

    public class ScopedEnumMember : IScopeable, IExpression
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public string ScopeableType { get; } = "enum value";
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

        public CodeType Type() => null;
    }

    public class DefinedType : CodeType
    {
        private Scope objectScope { get; }
        private Scope staticScope { get; }

        public DefinedType(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Type_defineContext typeContext) : base(typeContext.name.Text)
        {
            objectScope = new Scope("class " + Name);
            staticScope = new Scope("class " + Name);

            // Get the variables defined in the type.
            foreach (var definedVariable in typeContext.define())
            {
                Var newVar = Var.CreateVarFromContext(VariableDefineType.InClass, script, translateInfo, definedVariable);
                if (newVar.Static)
                    newVar.Finalize(staticScope);
                else
                    newVar.Finalize(objectScope);
            }
        }

        override public Scope ReturningScope()
        {
            return null;
        }

        override public Scope GetObjectScope()
        {
            return objectScope;
        }
    }
}