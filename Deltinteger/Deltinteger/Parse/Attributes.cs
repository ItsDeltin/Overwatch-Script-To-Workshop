using System;
using System.Linq;
using System.Collections.Generic;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    public enum AccessLevel
    {
        Public,
        Private,
        Protected
    }

    class AttributesGetter
    {
        public List<AttributeContext> ObtainedAttributes { get; } = new List<AttributeContext>(); // Attribute context.
        public IApplyAttribute ApplyAttributes { get; protected set; }

        private AttributesGetter(IApplyAttribute applyAttributes)
        {
            ApplyAttributes = applyAttributes;
        }

        public static void GetAttributes(FileDiagnostics diagnostics, AttributeTokens attributes, IApplyAttribute applyAttributes)
        {
            if (attributes == null) return;

            var getter = new AttributesGetter(applyAttributes);
            getter.CheckAttribute(diagnostics, attributes.GlobalVar, AttributeType.GlobalVar);
            getter.CheckAttribute(diagnostics, attributes.PlayerVar, AttributeType.PlayerVar);
            getter.CheckAttribute(diagnostics, attributes.Persist, AttributeType.Persist);
            getter.CheckAttribute(diagnostics, attributes.Override, AttributeType.Override);
            getter.CheckAttribute(diagnostics, attributes.Private, AttributeType.Private);
            getter.CheckAttribute(diagnostics, attributes.Protected, AttributeType.Protected);
            getter.CheckAttribute(diagnostics, attributes.Public, AttributeType.Public);
            getter.CheckAttribute(diagnostics, attributes.Recursive, AttributeType.Recursive);
            getter.CheckAttribute(diagnostics, attributes.Ref, AttributeType.Ref);
            getter.CheckAttribute(diagnostics, attributes.In, AttributeType.In);
            getter.CheckAttribute(diagnostics, attributes.Static, AttributeType.Static);
            getter.CheckAttribute(diagnostics, attributes.Virtual, AttributeType.Virtual);
        }

        void CheckAttribute(FileDiagnostics diagnostics, Token attribute, AttributeType type)
        {
            if (attribute == null) return;

            var newAttribute = new AttributeContext(attribute, type);

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
            if (!wasCopy && ApplyAttributes.Apply(diagnostics, newAttribute))
                ValidateAttribute(diagnostics, newAttribute);
        }

        private void ValidateAttribute(FileDiagnostics diagnostics, AttributeContext newAttribute)
        {
            // Virtual attribute on a static method (static attribute was first.)
            if (IsAttribute(AttributeType.Static) && newAttribute.Type == AttributeType.Virtual)
                diagnostics.Error("Static methods cannot be virtual.", newAttribute.Range);

            // Static attribute on a virtual method (virtual attribute was first.)
            if (IsAttribute(AttributeType.Virtual) && newAttribute.Type == AttributeType.Static)
                diagnostics.Error("Virtual methods cannot be static.", newAttribute.Range);
        }

        bool IsAttribute(AttributeType type) => ObtainedAttributes.Any(attribute => attribute.Type == type);
    }

    interface IApplyAttribute
    {
        bool Apply(FileDiagnostics diagnostics, AttributeContext attribute);
    }

    public class GenericAttributeAppender : IApplyAttribute
    {
        public AccessLevel Accessor { get; private set; }
        public bool IsStatic { get; private set; }
        public bool IsVirtual { get; private set; }
        public bool IsOverride { get; private set; }
        public bool IsRecursive { get; private set; }
        public bool Ref { get; private set; }
        private readonly AttributeType[] _disallowedAttributes;

        public GenericAttributeAppender(params AttributeType[] disallow)
        {
            _disallowedAttributes = disallow;
        }

        public bool Apply(FileDiagnostics diagnostics, AttributeContext attribute)
        {
            if (_disallowedAttributes.Contains(attribute.Type))
            {
                diagnostics.Error("The '" + attribute.Type.ToString().ToLower() + "' attribute is not allowed.", attribute.Range);
                return false;
            }

            // Apply the attribute.
            switch (attribute.Type)
            {
                // Accessors
                case AttributeType.Public: Accessor = AccessLevel.Public; break;
                case AttributeType.Protected: Accessor = AccessLevel.Protected; break;
                case AttributeType.Private: Accessor = AccessLevel.Private; break;

                // Apply static
                case AttributeType.Static: IsStatic = true; break;

                // Apply virtual
                case AttributeType.Virtual: IsVirtual = true; break;

                // Apply override
                case AttributeType.Override: IsOverride = true; break;

                // Apply Recursive
                case AttributeType.Recursive: IsRecursive = true; break;

                // Apply Ref
                case AttributeType.Ref: Ref = true; break;

                // Apply Variables

                default: throw new NotImplementedException();
            }

            return true;
        }
    }

    public class AttributeContext
    {
        public Token Token { get; }
        public DocRange Range => Token.Range;
        public AttributeType Type { get; }

        public AttributeContext(Token token, AttributeType type)
        {
            Token = token;
            Type = type;
        }

        public void Copy(FileDiagnostics diagnostics)
        {
            diagnostics.Error($"Multiple '{Type.ToString().ToLower()}' attributes.", Range);
        }
    }

    public enum AttributeType
    {
        GlobalVar,
        PlayerVar,
        Persist,
        In,
        Ref,
        Public,
        Private,
        Protected,
        Static,
        Override,
        Virtual,
        Recursive
    }
}