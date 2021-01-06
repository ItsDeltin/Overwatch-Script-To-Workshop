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
        IVariableComponent[] GetComponents();
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

        public IVariableComponent[] GetComponents()
        {
            var components = new List<IVariableComponent>();
            
            // Add attribute components.
            components.AddRange(ExtractAttributeComponents.Get(ParseInfo.Script.Diagnostics, _defineContext.Attributes));

            // Add workshop ID
            if (_defineContext.ID)
                components.Add(new WorkshopIndexComponent(int.Parse(_defineContext.ID.Text), _defineContext.ID.Range));
            
            // Extended collection
            if (_defineContext.Extended)
                components.Add(new ExtendedCollectionComponent(_defineContext.Range));
            
            // Initial value
            if (_defineContext.InitialValue != null)
                components.Add(new InitialValueComponent(_defineContext.InitialValue));
            
            // Macro
            if (_defineContext.MacroSymbol)
                components.Add(new MacroComponent(_defineContext.MacroSymbol.Range));

            return components.ToArray();
        }
    }
}