using System;
using System.Linq;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    abstract class FunctionAttributesGetter
    {
        public MethodAttributeContext[] ObtainedAttributes { get; private set; } // Attribute context.
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
                // Apply accessor
                case MethodAttributeType.Accessor: ResultAppender.SetAccessLevel(newAttribute.AttributeContext.accessor().GetAccessLevel()); break;
                
                // Apply static
                case MethodAttributeType.Static: ResultAppender.SetStatic(); break;
                
                // Apply virtual
                case MethodAttributeType.Virtual: ResultAppender.SetVirtual(); break;
                
                // Apply override
                case MethodAttributeType.Override: ResultAppender.SetOverride(); break;
                
                // Apply Recursive
                case MethodAttributeType.Recursive: ResultAppender.SetRecursive(); break;
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
    }
    
    // Attribute handler for defined methods
    class MethodAttributesGetter : FunctionAttributesGetter
    {
        private DeltinScriptParser.Define_methodContext Context { get; }

        public MethodAttributesGetter(DeltinScriptParser.Define_methodContext context, IFunctionAppendResult result) : base(result)
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

        public MacroAttributesGetter(DeltinScriptParser.Define_macroContext context, IFunctionAppendResult result) : base(result)
        {
            Context = context;
        }

        protected override DeltinScriptParser.Method_attributesContext[] GetAttributeContext() => Context.method_attributes();
        protected override string GetSubroutineName() => null;
        protected override MethodAttributeType[] DisallowAttributes() => new MethodAttributeType[] { MethodAttributeType.Recursive };
    }
}