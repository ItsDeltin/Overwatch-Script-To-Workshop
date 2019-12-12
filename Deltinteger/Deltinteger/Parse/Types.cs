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
        public Constructor[] Constructors { get; protected set; }

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
        public string KindString { get; }
        private Scope objectScope { get; }
        private Scope staticScope { get; }

        public DefinedType(ScriptFile script, DeltinScript translateInfo, Scope scope, DeltinScriptParser.Type_defineContext typeContext) : base(typeContext.name.Text)
        {
            if (typeContext.CLASS() != null) 
            { 
                TypeKind = TypeKind.Class;
                KindString = "class";
            }
            else if (typeContext.STRUCT() != null) 
            { 
                TypeKind = TypeKind.Struct;
                KindString = "struct";
            }
            else throw new NotImplementedException();

            staticScope = new Scope(KindString + " " + Name);
            objectScope = staticScope.Child(KindString + " " + Name);

            // Get the variables defined in the type.
            foreach (var definedVariable in typeContext.define())
            {
                Var newVar = Var.CreateVarFromContext(VariableDefineType.InClass, script, translateInfo, definedVariable);
                newVar.Finalize(UseScope(newVar.Static));
            }

            // Todo: Static methods/macros.
            foreach (var definedMethod in typeContext.define_method())
                UseScope(false).AddMethod(new DefinedMethod(script, translateInfo, definedMethod), script.Diagnostics, DocRange.GetRange(definedMethod.name));

            foreach (var definedMacro in typeContext.define_macro())
                UseScope(false).AddMethod(new DefinedMacro(script, translateInfo, definedMacro), script.Diagnostics, DocRange.GetRange(definedMacro.name));
            
            // Get the constructors.
            if (typeContext.constructor().Length > 0)
            {
                Constructors = new Constructor[typeContext.constructor().Length];
                for (int i = 0; i < Constructors.Length; i++)
                    Constructors[i] = new DefinedConstructor(script, translateInfo, this, typeContext.constructor(i));
            }
            else
            {
                // If there are no constructors, create a default constructor.
                Constructors = new Constructor[] {
                    new Constructor(new Location(script.Uri, DocRange.GetRange(typeContext.name)), AccessLevel.Public)
                };
            }
        }

        private Scope UseScope(bool isStatic)
        {
            return isStatic ? staticScope : objectScope;
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

    public class Constructor : IParameterCallable
    {
        public AccessLevel AccessLevel { get; }
        public CodeParameter[] Parameters { get; protected set; }
        public Location DefinedAt { get; }

        public Constructor(Location definedAt, AccessLevel accessLevel)
        {
            DefinedAt = definedAt;
            AccessLevel = accessLevel;
            Parameters = new CodeParameter[0];
        }

        public Constructor(Location definedAt, AccessLevel accessLevel, CodeParameter[] parameters)
        {
            DefinedAt = definedAt;
            AccessLevel = accessLevel;
            Parameters = parameters;
        }

        public virtual void Parse(ActionSet actionSet) {}
    }

    public class DefinedConstructor : Constructor
    {
        public Var[] ParameterVars { get; }
        public Scope ConstructorScope { get; }
        public CodeType Type { get; }
        public BlockAction Block { get; }

        public DefinedConstructor(ScriptFile script, DeltinScript translateInfo, CodeType type, DeltinScriptParser.ConstructorContext context) : base(
            new Location(script.Uri, DocRange.GetRange(context.name)),
            context.accessor()?.GetAccessLevel() ?? AccessLevel.Private)
        {
            ConstructorScope = type.GetObjectScope().Child();
            Type = type;

            var parameterInfo = CodeParameter.GetParameters(script, translateInfo, ConstructorScope, context.setParameters());
            Parameters = parameterInfo.Parameters;
            ParameterVars = parameterInfo.Variables;

            Block = new BlockAction(script, translateInfo, ConstructorScope, context.block());
        }

        public override void Parse(ActionSet actionSet)
        {
            // TODO: Assign parameters. Make DefinedMethod uses and this use the same method.
            Block.Translate(actionSet);
        }
    }
}