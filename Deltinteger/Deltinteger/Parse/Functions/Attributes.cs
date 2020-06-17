using System;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    abstract class FunctionAttributesGetter
    {
        public bool IsSubroutine { get; private set; } // Determines if the function is a subroutine.
        public string SubroutineName { get; private set; } // The name of the subroutine if applicable.
        public MethodAttributeContext[] ObtainedAttributes { get; private set; } // Attribute context.
        public AccessLevel AccessLevel { get; private set; } // The access level of the function.
        public bool IsStatic { get; private set; } // Is the function static?
        private MethodAttributes Attributes { get; } // The actual attributes.

        protected FunctionAttributesGetter(MethodAttributes attributes)
        {
            Attributes = attributes;
        }
        
        public void GetAttributes(FileDiagnostics diagnostics)
        {
            // Get the name of the rule the method will be stored in.
            SubroutineName = GetSubroutineName();
            IsSubroutine = SubroutineName != null;
            
            // context will be null if there are no attributes.
            var context = GetAttributeContext();
            if (context == null) return;

            // Initialize the ObtainedAttributes array.
            int numberOfAttributes = context.Length;
            ObtainedAttributes = new MethodAttributeContext[numberOfAttributes];

            // Loop through all attributes.
            for (int i = 0; i < numberOfAttributes; i++)
            {
                var newAttribute = new MethodAttributeContext(context[i]);
                ObtainedAttributes[i] = newAttribute;

                // If the attribute already exists, syntax error.
                bool wasCopy = false;
                for (int c = i - 1; c >= 0; c--)
                    if (ObtainedAttributes[c].Type == newAttribute.Type)
                    {
                        newAttribute.Copy(diagnostics);
                        wasCopy = true;
                        break;
                    }
                
                // Additonal syntax errors. Only throw if the attribute is not a copy.
                if (!wasCopy)
                {
                    ValidateAttribute(diagnostics, newAttribute);
                    ApplyAttribute(newAttribute);
                }
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
                if (IsStatic && newAttribute.Type == MethodAttributeType.Virtual)
                    diagnostics.Error("Static methods cannot be virtual.", newAttribute.Range);
                
                // Static attribute on a virtual method (virtual attribute was first.)
                if (Attributes.Virtual && newAttribute.Type == MethodAttributeType.Static)
                    diagnostics.Error("Virtual methods cannot be static.", newAttribute.Range);
            }
        }

        private void ApplyAttribute(MethodAttributeContext newAttribute)
        {
            // Apply the attribute.
            switch (newAttribute.Type)
            {
                // Apply accessor
                case MethodAttributeType.Accessor: AccessLevel = newAttribute.AttributeContext.accessor().GetAccessLevel(); break;
                
                // Apply static
                case MethodAttributeType.Static: IsStatic = true; break;
                
                // Apply virtual
                case MethodAttributeType.Virtual: Attributes.Virtual = true; break;
                
                // Apply override
                case MethodAttributeType.Override: Attributes.Override = true; break;
                
                // Apply Recursive
                case MethodAttributeType.Recursive: Attributes.Recursive = true; break;
            }
        }

        // Implementers
        protected abstract string GetSubroutineName();
        protected abstract DeltinScriptParser.Method_attributesContext[] GetAttributeContext();
        protected virtual MethodAttributeType[] DisallowAttributes() => new MethodAttributeType[0];
    }

    class MethodAttributeContext
    {
        public MethodAttributeType Type { get; }
        public DocRange Range { get; }
        public DeltinScriptParser.Method_attributesContext AttributeContext { get; }

        public MethodAttributeContext(DeltinScriptParser.Method_attributesContext attributeContext)
        {
            AttributeContext = attributeContext; 
            Range = DocRange.GetRange(attributeContext);

            if (attributeContext.accessor() != null) Type = MethodAttributeType.Accessor;
            else if (attributeContext.STATIC() != null) Type = MethodAttributeType.Static;
            else if (attributeContext.VIRTUAL() != null) Type = MethodAttributeType.Virtual;
            else if (attributeContext.OVERRIDE() != null) Type = MethodAttributeType.Override;
            else if (attributeContext.RECURSIVE() != null) Type = MethodAttributeType.Recursive;
            else throw new NotImplementedException();
        }

        public void Copy(FileDiagnostics diagnostics)
        {
            diagnostics.Error($"Multiple '{Type.ToString().ToLower()}' attributes.", Range);
        }
    }

    enum MethodAttributeType
    {
        Accessor,
        Static,
        Override,
        Virtual,
        Recursive
    }

    // Attribute handler for defined methods
    class MethodAttributesGetter : FunctionAttributesGetter
    {
        private DeltinScriptParser.Define_methodContext Context { get; }

        public MethodAttributesGetter(DeltinScriptParser.Define_methodContext context, MethodAttributes attributes) : base(attributes)
        {
            Context = context;
        }

        protected override DeltinScriptParser.Method_attributesContext[] GetAttributeContext() => Context.method_attributes();
        protected override string GetSubroutineName() => Context.STRINGLITERAL() == null ? null : Extras.RemoveQuotes(Context.STRINGLITERAL().GetText());
    }

    // Attribute handler for defined macros
    class MacroAttributesGetter : FunctionAttributesGetter
    {
        private DeltinScriptParser.Define_macroContext Context { get; }

        public MacroAttributesGetter(DeltinScriptParser.Define_macroContext context, MethodAttributes attributes) : base(attributes)
        {
            Context = context;
        }

        protected override DeltinScriptParser.Method_attributesContext[] GetAttributeContext() => Context.method_attributes();
        protected override string GetSubroutineName() => null;
        protected override MethodAttributeType[] DisallowAttributes() => new MethodAttributeType[] { MethodAttributeType.Recursive };
    }
}