using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;

namespace Deltin.Deltinteger.Parse
{
    class DefineContextHandler : IVarContextHandler
    {
        public ParseInfo ParseInfo { get; }
        private readonly VariableDeclaration _defineContext;
        private readonly AttributeTokens _attributes;

        public DefineContextHandler(ParseInfo parseInfo, VariableDeclaration defineContext)
        {
            _defineContext = defineContext;
            _attributes = _defineContext.Attributes;
            ParseInfo = parseInfo;
        }

        // Define location.
        public Location GetDefineLocation() => new Location(ParseInfo.Script.Uri, GetNameRange());

        // Get the name.
        public string GetName() => _defineContext.Identifier.GetText();
        public DocRange GetNameRange()
        {
            if (_defineContext.Identifier == null) return _defineContext.Range;
            return _defineContext.Identifier.Range;
        }

        // Gets the code type context.
        public IParseType GetCodeType() => _defineContext.Type;

        // Gets the attributes.
        public VarBuilderAttribute[] GetAttributes()
        {
            List<VarBuilderAttribute> attributes = new List<VarBuilderAttribute>();

            // Get the accessor.

            // Get the static attribute.
            if (_attributes.Static) attributes.Add(new VarBuilderAttribute(AttributeType.Static, _attributes.Static.Range));

            // Get the globalvar attribute.
            if (_attributes.GlobalVar) attributes.Add(new VarBuilderAttribute(AttributeType.Globalvar, _attributes.GlobalVar.Range));

            // Get the playervar attribute.
            if (_attributes.PlayerVar) attributes.Add(new VarBuilderAttribute(AttributeType.Playervar, _attributes.PlayerVar.Range));

            // Get the ref attribute.
            if (_attributes.Ref) attributes.Add(new VarBuilderAttribute(AttributeType.Ref, _attributes.Ref.Range));

            // Get the in attribute.
            if (_attributes.In) attributes.Add(new VarBuilderAttribute(AttributeType.In, _attributes.In.Range));

            // Get Accessors.
            // Private
            if (_attributes.Private) attributes.Add(new VarBuilderAttribute(AttributeType.Private, _attributes.Private.Range));
            // Protected
            if (_attributes.Protected) attributes.Add(new VarBuilderAttribute(AttributeType.Protected, _attributes.Protected.Range));
            // Public
            if (_attributes.Public) attributes.Add(new VarBuilderAttribute(AttributeType.Public, _attributes.Public.Range));

            // Get the ID attribute.
            if (_defineContext.ID) attributes.Add(new IDAttribute(_defineContext.ID));

            // Get the extended attribute.
            if (_defineContext.Extended) attributes.Add(new VarBuilderAttribute(AttributeType.Ext, _defineContext.Extended.Range));

            // Get the initial value.
            if (_defineContext.InitialValue != null) attributes.Add(new InitialValueAttribute(_defineContext.InitialValue));

            return attributes.ToArray();
        }

        public DocRange GetTypeRange() => _defineContext.Type.Range;

        public bool CheckName() => _defineContext.Identifier;
    }
}