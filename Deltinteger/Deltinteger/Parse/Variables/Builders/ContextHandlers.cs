using System.Collections.Generic;
using Deltin.Deltinteger.LanguageServer;
using Deltin.Deltinteger.Compiler;
using Deltin.Deltinteger.Compiler.SyntaxTree;
using Deltin.Deltinteger.Parse.Variables.Build;

namespace Deltin.Deltinteger.Parse
{
    public interface IVarContextHandler
    {
        ParseInfo ParseInfo { get; }
        Location GetDefineLocation();
        string GetName();
        DocRange GetNameRange();
        void GetComponents(VariableComponentCollection componentCollection);
        IParseType GetCodeType();
        DocRange GetTypeRange();
    }

    class DefineContextHandler : IVarContextHandler
    {
        public ParseInfo ParseInfo { get; }
        private readonly VariableDeclaration _defineContext;

        public DefineContextHandler(ParseInfo parseInfo, VariableDeclaration defineContext)
        {
            _defineContext = defineContext;
            ParseInfo = parseInfo;
        }

        public Location GetDefineLocation() => ParseInfo.Script.GetLocation(GetNameRange());
        public string GetName() => _defineContext.Identifier.GetText();
        public DocRange GetNameRange() => _defineContext.Identifier.GetRange(_defineContext.Range);
        public IParseType GetCodeType() => _defineContext.Type;
        public DocRange GetTypeRange() => _defineContext.Type.Range;

        public void GetComponents(VariableComponentCollection componentCollection)
        {            
            // Add attribute components.
            new AttributesGetter(_defineContext.Attributes, componentCollection).GetAttributes(ParseInfo.Script.Diagnostics);

            // Add workshop ID
            if (_defineContext.ID)
                componentCollection.AddComponent(new WorkshopIndexComponent(int.Parse(_defineContext.ID.Text), _defineContext.ID.Range));
            
            // Extended collection
            if (_defineContext.Extended)
                componentCollection.AddComponent(new ExtendedCollectionComponent(_defineContext.Range));
            
            // Initial value
            if (_defineContext.InitialValue != null)
                componentCollection.AddComponent(new InitialValueComponent(_defineContext.InitialValue));
            
            // Macro
            if (_defineContext.MacroSymbol)
                componentCollection.AddComponent(new MacroComponent(_defineContext.MacroSymbol.Range));
        }
    }
}