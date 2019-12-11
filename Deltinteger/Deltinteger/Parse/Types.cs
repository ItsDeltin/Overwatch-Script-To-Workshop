using System;
using System.Collections.Generic;
using Deltin.Deltinteger.Elements;
using Deltin.Deltinteger.LanguageServer;
using CompletionItem = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItem;
using CompletionItemKind = OmniSharp.Extensions.LanguageServer.Protocol.Models.CompletionItemKind;

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

        public static readonly CodeType[] DefaultTypes = GetDefaultTypes();

        private static CodeType[] GetDefaultTypes()
        {
            var defaultTypes = new List<CodeType>();
            foreach (var enumData in EnumData.GetEnumData())
                defaultTypes.Add(new WorkshopEnumType(enumData));
            return defaultTypes.ToArray();
        }

        public CodeType Type() => null;

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            throw new Exception("Types can't be used like expressions.");
        }

        /// <summary>
        /// Determines if variables with this type can have their value changed.
        /// </summary>
        /// <returns></returns>
        public virtual bool Constant() => false;

        public abstract CompletionItem GetCompletion();

        public static bool TypeMatches(CodeType parameterType, CodeType valueType)
        {
            return parameterType == null || parameterType == valueType;
        }
    }

    public class WorkshopEnumType : CodeType
    {
        private Scope EnumScope { get; } = new Scope();
        public EnumData EnumData { get; }

        public WorkshopEnumType(EnumData enumData) : base(enumData.CodeName)
        {
            EnumData = enumData;
            foreach (var member in enumData.Members)
            {
                var scopedMember = new ScopedEnumMember(this, member);
                EnumScope.AddVariable(scopedMember, null, null);
            }
            EnumScope.ErrorName = "enum " + Name;
        }

        override public Scope ReturningScope()
        {
            return EnumScope;
        }

        override public bool Constant() => true;

        override public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = EnumData.CodeName,
                Kind = CompletionItemKind.Enum
            };
        }
    }

    public class ScopedEnumMember : IScopeable, IExpression
    {
        public string Name { get; }
        public AccessLevel AccessLevel { get; } = AccessLevel.Public;
        public string ScopeableType { get; } = "enum value";
        public Location DefinedAt { get; } = null;
        public bool WholeContext { get; } = true;
        
        public CodeType Enum { get; }
        public EnumMember EnumMember { get; }

        private Scope debugScope { get; } = new Scope();
        
        public ScopedEnumMember(CodeType parent, EnumMember enumMember)
        {
            Enum = parent;
            Name = enumMember.CodeName;
            EnumMember = enumMember;
            debugScope.ErrorName = "enum value " + Name;
        }

        public Scope ReturningScope()
        {
            return debugScope;
        }

        public CodeType Type() => Enum;

        public IWorkshopTree Parse(ActionSet actionSet)
        {
            return (IWorkshopTree)EnumMember;
        }

        public CompletionItem GetCompletion()
        {
            return new CompletionItem()
            {
                Label = Name,
                Kind = CompletionItemKind.EnumMember
            };
        }
    }

    public class DefinedType : CodeType
    {
        public TypeKind TypeKind { get; }
        private Scope objectScope { get; }
        private Scope staticScope { get; }

        public DefinedType(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Type_defineContext typeContext) : base(typeContext.name.Text)
        {
            if (typeContext.CLASS() != null) 
            { 
                TypeKind = TypeKind.Class;
                objectScope = new Scope("class " + Name);
                staticScope = new Scope("class " + Name);
            }
            else if (typeContext.STRUCT() != null) 
            { 
                TypeKind = TypeKind.Struct;
                objectScope = new Scope("struct " + Name);
                staticScope = new Scope("struct " + Name);
            }
            else throw new NotImplementedException();

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

        override public CompletionItem GetCompletion()
        {
            CompletionItemKind kind;
            if (TypeKind == TypeKind.Class) kind = CompletionItemKind.Class;
            else if (TypeKind == TypeKind.Struct) kind = CompletionItemKind.Struct;
            else throw new NotImplementedException();

            return new CompletionItem()
            {
                Label = Name,
                Kind = kind
            };
        }
    }
    
    public enum TypeKind
    {
        Class,
        Struct
    }
}