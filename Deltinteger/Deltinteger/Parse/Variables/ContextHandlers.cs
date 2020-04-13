using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;

namespace Deltin.Deltinteger.Parse
{
    class DefineContextHandler : IVarContextHandler
    {
        public ParseInfo ParseInfo { get; }
        private readonly DeltinScriptParser.DefineContext _defineContext;

        public DefineContextHandler(ParseInfo parseInfo, DeltinScriptParser.DefineContext defineContext)
        {
            _defineContext = defineContext;
            ParseInfo = parseInfo;
        }

        // Define location.
        public Location GetDefineLocation() => new Location(ParseInfo.Script.Uri, GetNameRange());

        // Get the name.
        public string GetName() => _defineContext.name?.Text;
        public DocRange GetNameRange()
        {
            if (_defineContext.name == null) return DocRange.GetRange(_defineContext);
            return DocRange.GetRange(_defineContext.name);
        }

        // Gets the code type context.
        public DeltinScriptParser.Code_typeContext GetCodeType() => _defineContext.code_type();

        // Gets the attributes.
        public VarBuilderAttribute[] GetAttributes()
        {
            List<VarBuilderAttribute> attributes = new List<VarBuilderAttribute>();

            // Get the accessor.
            if (_defineContext.accessor() != null)
            {
                DocRange accessorRange = DocRange.GetRange(_defineContext.accessor());

                if (_defineContext.accessor().PUBLIC() != null)
                    attributes.Add(new VarBuilderAttribute(AttributeType.Public, accessorRange));
                else if (_defineContext.accessor().PRIVATE() != null)
                    attributes.Add(new VarBuilderAttribute(AttributeType.Private, accessorRange));
                else if (_defineContext.accessor().PROTECTED() != null)
                    attributes.Add(new VarBuilderAttribute(AttributeType.Protected, accessorRange));
            }
            
            // Get the static attribute.
            if (_defineContext.STATIC() != null)
                attributes.Add(new VarBuilderAttribute(AttributeType.Static, DocRange.GetRange(_defineContext.STATIC())));
            
            // Get the globalvar attribute.
            if (_defineContext.GLOBAL() != null)
                attributes.Add(new VarBuilderAttribute(AttributeType.Globalvar, DocRange.GetRange(_defineContext.GLOBAL())));
            
            // Get the playervar attribute.
            if (_defineContext.PLAYER() != null)
                attributes.Add(new VarBuilderAttribute(AttributeType.Playervar, DocRange.GetRange(_defineContext.PLAYER())));
            
            // Get the ref attribute.
            if (_defineContext.REF() != null)
                attributes.Add(new VarBuilderAttribute(AttributeType.Ref, DocRange.GetRange(_defineContext.REF())));
            
            // Get the ID attribute.
            if (_defineContext.id != null)
                attributes.Add(new IDAttribute(_defineContext.id));
            
            // Get the extended attribute.
            if (_defineContext.NOT() != null)
                attributes.Add(new VarBuilderAttribute(AttributeType.Ext, DocRange.GetRange(_defineContext.NOT())));
            
            // Get the initial value.
            if (_defineContext.expr() != null)
                attributes.Add(new InitialValueAttribute(_defineContext.expr()));
            
            return attributes.ToArray();
        }

        public DocRange GetTypeRange() => DocRange.GetRange((object)_defineContext.code_type());
    }
}