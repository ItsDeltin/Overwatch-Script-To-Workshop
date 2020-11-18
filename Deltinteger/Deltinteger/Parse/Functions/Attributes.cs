using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    abstract class FunctionAttributesGetter
    {
        public List<MethodAttributeContext> ObtainedAttributes { get; } = new List<MethodAttributeContext>(); // Attribute context.
        public IFunctionAppendResult ResultAppender { get; protected set; }

        protected FunctionAttributesGetter(IFunctionAppendResult resultAppender)
        {
            ResultAppender = resultAppender;
        }
        
        public void GetAttributes(FileDiagnostics diagnostics)
        {
            // Get the name of the rule the method will be stored in.
            string subroutineName = GetSubroutineName();
            if (subroutineName != null) ResultAppender.SetSubroutine(subroutineName);
            
            // context will be null if there are no attributes.
            var context = GetAttributeContext();
            if (context == null) return;

            CheckAttribute(diagnostics, context.GlobalVar, MethodAttributeType.GlobalVar);
            CheckAttribute(diagnostics, context.Override, MethodAttributeType.Override);
            CheckAttribute(diagnostics, context.PlayerVar, MethodAttributeType.PlayerVar);
            CheckAttribute(diagnostics, context.Private, MethodAttributeType.Private);
            CheckAttribute(diagnostics, context.Protected, MethodAttributeType.Protected);
            CheckAttribute(diagnostics, context.Public, MethodAttributeType.Public);
            CheckAttribute(diagnostics, context.Recursive, MethodAttributeType.Recursive);
            CheckAttribute(diagnostics, context.Ref, MethodAttributeType.Ref);
            CheckAttribute(diagnostics, context.Static, MethodAttributeType.Static);
            CheckAttribute(diagnostics, context.Virtual, MethodAttributeType.Virtual);
        }

        void CheckAttribute(FileDiagnostics diagnostics, Token attribute, MethodAttributeType type)
        {
            if (attribute == null) return;

            var newAttribute = new MethodAttributeContext(attribute, type);

            // If the attribute already exists, syntax error.
            bool wasCopy = false;
            for (int c = 0; c < ObtainedAttributes.Count; c++)
                if (ObtainedAttributes[c].Type == newAttribute.Type)
                {
                    newAttribute.Copy(diagnostics);
                    wasCopy = true;
                    break;
                }
            
            // Add the attribute.
            ObtainedAttributes.Add(newAttribute);
            
            // Additonal syntax errors. Only throw if the attribute is not a copy.
            if (!wasCopy)
            {
                ValidateAttribute(diagnostics, newAttribute);
                ApplyAttribute(newAttribute);
            }
        }

        private void ValidateAttribute(FileDiagnostics diagnostics, MethodAttributeContext newAttribute)
        {
            // The attribute is not allowed.
            if (DisallowAttributes().Contains(newAttribute.Type))
                diagnostics.Error("The '" + newAttribute.Type.ToString().ToLower() + "' attribute is not allowed.", newAttribute.Range);
            else
            {
                // Virtual attribute on a static method (static attribute was first.)
                if (ResultAppender.IsStatic() && newAttribute.Type == MethodAttributeType.Virtual)
                    diagnostics.Error("Static methods cannot be virtual.", newAttribute.Range);
                
                // Static attribute on a virtual method (virtual attribute was first.)
                if (ResultAppender.IsVirtual() && newAttribute.Type == MethodAttributeType.Static)
                    diagnostics.Error("Virtual methods cannot be static.", newAttribute.Range);
            }
        }

        private void ApplyAttribute(MethodAttributeContext newAttribute)
        {
            // Apply the attribute.
            switch (newAttribute.Type)
            {
                // Accessors
                case MethodAttributeType.Public: ResultAppender.SetAccessLevel(AccessLevel.Public); break;
                case MethodAttributeType.Protected: ResultAppender.SetAccessLevel(AccessLevel.Protected); break;
                case MethodAttributeType.Private: ResultAppender.SetAccessLevel(AccessLevel.Private); break;
                
                // Apply static
                case MethodAttributeType.Static: ResultAppender.SetStatic(); break;
                
                // Apply virtual
                case MethodAttributeType.Virtual: ResultAppender.SetVirtual(); break;
                
                // Apply override
                case MethodAttributeType.Override: ResultAppender.SetOverride(); break;
                
                // Apply Recursive
                case MethodAttributeType.Recursive: ResultAppender.SetRecursive(); break;

                // Apply Variables

                default: throw new NotImplementedException();
            }
        }

        // Implementers
        protected abstract string GetSubroutineName();
        protected abstract AttributeTokens GetAttributeContext();
        protected virtual MethodAttributeType[] DisallowAttributes() => new MethodAttributeType[0];
    }

    class MethodAttributeContext
    {
        public Token Token { get; }
        public DocRange Range => Token.Range;
        public MethodAttributeType Type { get; }

        public MethodAttributeContext(Token token, MethodAttributeType type)
        {
            Token = token;
            Type = type;
        }

        public void Copy(FileDiagnostics diagnostics)
        {
            diagnostics.Error($"Multiple '{Type.ToString().ToLower()}' attributes.", Range);
        }
    }

    enum MethodAttributeType
    {
        GlobalVar,
        PlayerVar,
        Ref,
        Public,
        Private,
        Protected,
        Static,
        Override,
        Virtual,
        Recursive
    }

    // Result Appenders
    interface IFunctionAppendResult
    {
        void SetAccessLevel(AccessLevel accessLevel);
        void SetStatic();
        void SetVirtual();
        void SetOverride();
        void SetRecursive();
        void SetSubroutine(string name);
        bool IsVirtual();
        bool IsStatic();
        void SetVariableType(bool isGlobal);
    }

    class MethodAttributeAppender : IFunctionAppendResult
    {
        private readonly MethodAttributes _attributes;
        public AccessLevel AccessLevel { get; private set; } // The access level of the function.
        public bool Static { get; private set; } // Determines if the function is static.
        public string SubroutineName { get; private set; } // The name of the subroutine if applicable.
        public bool IsSubroutine => SubroutineName != null; // Determines if the function is a subroutine.

        public MethodAttributeAppender(MethodAttributes attributes)
        {
            _attributes = attributes;
        }

        public bool IsVirtual() => _attributes.Virtual;
        public bool IsStatic() => Static;
        public void SetAccessLevel(AccessLevel accessLevel) => AccessLevel = accessLevel;
        public void SetOverride() => _attributes.Override = true;
        public void SetRecursive() => _attributes.Recursive = true;
        public void SetStatic() => Static = true;
        public void SetVirtual() => _attributes.Virtual = true;
        public void SetSubroutine(string name) => SubroutineName = name;
        public void SetVariableType(bool isGlobal) => throw new NotImplementedException();
    }

    class GenericAttributeAppender : IFunctionAppendResult
    {
        public bool IsStatic { get; private set; }
        public bool IsVirtual { get; private set; }
        public AccessLevel AccessLevel { get; private set; }
        public bool IsOverride { get; private set; }
        public bool IsRecursive { get; private set; }
        public string SubroutineName { get; private set; }
        public bool DefaultVariableType { get; private set; }
        public bool IsOverridable => IsVirtual || IsOverride;
        public bool IsSubroutine { get; private set; }

        bool IFunctionAppendResult.IsStatic() => IsStatic;
        bool IFunctionAppendResult.IsVirtual() => IsVirtual;
        void IFunctionAppendResult.SetAccessLevel(AccessLevel accessLevel) => AccessLevel = accessLevel;
        void IFunctionAppendResult.SetOverride() => IsOverride = true;
        void IFunctionAppendResult.SetRecursive() => IsRecursive = true;
        void IFunctionAppendResult.SetStatic() => IsStatic = true;
        void IFunctionAppendResult.SetSubroutine(string name)
        {
            SubroutineName = name;
            IsSubroutine = true;
        }
        void IFunctionAppendResult.SetVariableType(bool isGlobal) => DefaultVariableType = isGlobal;
        void IFunctionAppendResult.SetVirtual() => IsVirtual = true;

        public void Apply(MethodAttributes attributes)
        {
            attributes.Virtual = IsVirtual;
            attributes.Override = IsOverridable;
            attributes.Recursive = IsRecursive;
            attributes.Parallelable = IsSubroutine;
        }
    }
    
    // Attribute handler for defined methods
    class MethodAttributesGetter : FunctionAttributesGetter
    {
        private FunctionContext Context { get; }

        public MethodAttributesGetter(FunctionContext context, IFunctionAppendResult result) : base(result)
        {
            Context = context;
        }

        protected override AttributeTokens GetAttributeContext() => Context.Attributes;
        protected override string GetSubroutineName() => Context.Subroutine?.Text.RemoveQuotes();
        protected override MethodAttributeType[] DisallowAttributes() => new MethodAttributeType[] { MethodAttributeType.GlobalVar, MethodAttributeType.PlayerVar, MethodAttributeType.Ref };
    }

    // Attribute handler for defined macros
    class MacroAttributesGetter : FunctionAttributesGetter
    {
        private IDeclaration Declaration { get; }

        public MacroAttributesGetter(IDeclaration context, IFunctionAppendResult result) : base(result)
        {
            Declaration = context;
        }

        protected override AttributeTokens GetAttributeContext() => Declaration.Attributes;
        protected override string GetSubroutineName() => null;
        protected override MethodAttributeType[] DisallowAttributes() => new MethodAttributeType[] { MethodAttributeType.Recursive, MethodAttributeType.GlobalVar, MethodAttributeType.PlayerVar, MethodAttributeType.Ref };
    }
}